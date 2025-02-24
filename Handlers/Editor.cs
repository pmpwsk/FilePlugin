using System.Web;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    private async Task HandleEditor(Request req)
    {
        switch (req.Path)
        {
            case "/editor":
            { CreatePage(req, "Files", out var page, out var e, out _);
                req.ForceLogin();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                string pEnc = HttpUtility.UrlEncode(p);
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (file == null)
                    if (directory == null)
                    {
                        MissingFileOrAccess(req, e);
                        break;
                    }
                    else throw new BadRequestSignal();
                
                //head
                page.Title = name + " - Files";
                page.Scripts.Add(new Script("query.js"));
                page.Scripts.Add(new Script("editor.js"));
                
                //sidebar + navigation
                if (parent != null)
                {
                    string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                    string parentUrl = $"list?u={u}&p={parentEnc}";
                    page.Navigation.Add(new ButtonJS("Back", "GoBack()", "right", id: "back"));
                    page.Sidebar =
                    [
                        new ButtonElement(null, "Go up a level", parentUrl),
                        ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"list?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null)),
                        ..parent.Files.Select(fKV => new ButtonElement(null, fKV.Key, $"editor?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}", fKV.Key == name ? "green" : null))
                    ];
                }
                else page.Navigation.Add(new ButtonJS("Back", "GoBack()", "right", id: "back"));
                
                //content
                page.HideFooter = true;
                e.Add(new LargeContainerElementIsoTop(name, new TextArea("Loading...", null, "editor", classes: "wider grow", onInput: "TextChanged()"))
                {
                    Button = new ButtonJS("Saved!", $"Save()", id: "save")
                });
            } break;

            case "/editor/load":
            { req.ForceGET();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (directory != null)
                    throw new BadRequestSignal();
                if (file == null)
                    throw new NotFoundSignal();
                var content = File.ReadAllText($"../FilePlugin.Profiles/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}");
                if (content == "")
                    throw new HttpStatusSignal(201);
                await req.Write(content);
            } break;

            case "/editor/save":
            { req.ForcePOST();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                if (req.IsForm)
                    throw new BadRequestSignal();
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (directory != null)
                    throw new BadRequestSignal();
                if (profile == null)
                    throw new ServerErrorSignal();
                if (file == null)
                    throw new NotFoundSignal();
                profile.Lock();
                string loc = $"../FilePlugin.Profiles/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}";
                long oldSize = file.Size;
                string oldContent = File.ReadAllText(loc);
                File.WriteAllText(loc, await req.GetBodyText());
                file.Size = new FileInfo(loc).Length;
                if (profile.SizeUsed + file.Size - oldSize > profile.SizeLimit && !req.IsAdmin)
                {
                    File.WriteAllText(loc, oldContent);
                    file.Size = oldSize;
                    profile.UnlockSave();
                    throw new HttpStatusSignal(507);
                }
                file.ModifiedUtc = DateTime.UtcNow;
                profile.SizeUsed += file.Size - oldSize;
                profile.UnlockSave();
                await NotifyChangeListeners(u, segments);
                if (parent != null)
                    await NotifyChangeListeners(u, segments.SkipLast(1));
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }
    }
}