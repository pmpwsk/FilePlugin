using System.Web;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    private async Task HandleAdd(Request req)
    {
        switch (req.Path)
        {
            case "/add":
            { CreatePage(req, "Files", out var page, out var e, out _);
                req.ForceLogin();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                string pEnc = HttpUtility.UrlEncode(p);
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (directory == null)
                    if (file == null)
                    {
                        MissingFileOrAccess(req, e);
                        break;
                    }
                    else throw new BadRequestSignal();
                
                //head
                page.Title = name + " - Files";
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("query.js"));
                page.Scripts.Add(new Script("add.js"));
                
                //sidebar + navigation
                page.Navigation.Add(new Button("Back", $"edit?u={u}&p={pEnc}", "right"));
                if (parent != null)
                {
                    string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                    page.Sidebar =
                    [
                        new ButtonElement(null, "Go up a level", $"add?u={u}&p={parentEnc}"),
                        ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"add?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null))
                    ];
                }
                
                //elements
                e.Add(new HeadingElement(name, "Add content"));
                
                page.AddError();
                
                e.Add(new ContainerElement("New folder", new TextBox("Enter a name...", null, "dir-name", onEnter: "AddNode(true)"))
                { Button = 
                    new ButtonJS("Create", "AddNode(true)", "green")
                });
                e.Add(new ContainerElement("New text file", new TextBox("Enter a name...", null, "file-name", onEnter: "AddNode(false)", autofocus: true))
                { Button = 
                    new ButtonJS("Create", "AddNode(false)", "green")
                });
                e.Add(new ContainerElement("Upload", new FileSelector("upload-files", true)) { Button = new ButtonJS("Upload", "Upload()", "green", id: "upload-button")});
            } break;

            case "/add/create":
            { req.ForcePOST();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p) && req.Query.TryGetValue("n", out var n) && NameOkay(n) && req.Query.TryGetValue("d", out bool d)))
                    throw new BadRequestSignal();
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (directory == null)
                    throw new NotFoundSignal();
                if (directory.Directories.ContainsKey(n) || directory.Files.ContainsKey(n))
                    throw new HttpStatusSignal(302);
                if (profile == null)
                    throw new ServerErrorSignal();
                profile.Lock();
                string target = $"../FilePlugin.Profiles/{req.UserTable.Name}_{u}{string.Join('/', ((IEnumerable<string>)[..segments, n]).Select(Parsers.ToBase64PathSafe))}";
                if (d)
                {
                    Directory.CreateDirectory(target);
                    directory.Directories.Add(n, new());
                }
                else
                {
                    File.WriteAllText(target, "");
                    directory.Files.Add(n, new(DateTime.UtcNow, 0));
                }
                profile.UnlockSave();
                await NotifyChangeListeners(u, segments);
            } break;

            case "/add/upload":
            { req.ForcePOST();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out _, out var directory, out _, out _);
                if (directory == null)
                    throw new NotFoundSignal();
                if (profile == null)
                    throw new ServerErrorSignal();
                long limit = req.IsAdmin ? long.MaxValue : profile.SizeLimit;
                req.BodySizeLimit = limit;
                if ((!req.IsForm) || req.Files.Count == 0)
                    req.Status = 400;
                if (req.Files.Any(x => directory.Directories.ContainsKey(x.FileName)))
                    throw new HttpStatusSignal(302);
                profile.Lock();
                foreach (var uploadedFile in req.Files)
                {
                    if (!NameOkay(uploadedFile.FileName))
                        continue;
                    string loc = $"../FilePlugin.Profiles/{req.UserTable.Name}_{u}{string.Join('/', ((IEnumerable<string>)[..segments, uploadedFile.FileName]).Select(Parsers.ToBase64PathSafe))}";
                    long oldSize;
                    if (directory.Files.TryGetValue(uploadedFile.FileName, out var f))
                    {
                        f.ModifiedUtc = DateTime.UtcNow;
                        oldSize = f.Size;
                    }
                    else
                    {
                        f = new(DateTime.UtcNow, 0);
                        directory.Files[uploadedFile.FileName] = f;
                        oldSize = 0;
                    }
                    try
                    {
                        if (!uploadedFile.Download(loc, limit))
                        {
                            profile.SizeUsed -= oldSize;
                            directory.Files.Remove(uploadedFile.FileName);
                            profile.UnlockSave();
                            throw new HttpStatusSignal(413);
                        }
                        f.Size = new FileInfo(loc).Length;
                        if (profile.SizeUsed + f.Size - oldSize > profile.SizeLimit && !req.IsAdmin)
                        {
                            profile.SizeUsed -= oldSize;
                            directory.Files.Remove(uploadedFile.FileName);
                            profile.UnlockSave();
                            throw new HttpStatusSignal(507);
                        }
                        profile.SizeUsed += f.Size - oldSize;
                    }
                    catch
                    {
                        try { File.Delete(loc); }
                        catch { /* ignored */ }

                        profile.SizeUsed -= oldSize;
                        directory.Files.Remove(uploadedFile.FileName);
                        profile.UnlockSave();
                        throw;
                    }
                }
                profile.UnlockSave();
                await NotifyChangeListeners(u, segments);
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }
    }
}