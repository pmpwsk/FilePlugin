using System.Web;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin
{
    private async Task HandleEdit(Request req)
    {
        switch (req.Path)
        {
            case "/edit":
            { CreatePage(req, "Files", out var page, out var e, out var userProfile);
                req.ForceLogin();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                string pEnc = HttpUtility.UrlEncode(p);
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (directory == null && file == null)
                {
                    MissingFileOrAccess(req, e);
                    break;
                }
                
                //head
                page.Title = name + " - Files";
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("query.js"));
                page.Scripts.Add(new Script("edit.js"));
                
                //sidebar
                if (parent != null)
                {
                    string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                    string backUrl = $"list?u={u}&p={parentEnc}";
                    page.Sidebar =
                    [
                        new ButtonElement(null, "Go up a level", backUrl),
                        ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null)),
                        ..parent.Files.Select(fKV => new ButtonElement(null, fKV.Key, $"edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}", fKV.Key == name ? "green" : null))
                    ];
                }
                
                //navigation
                if (directory != null)
                    page.Navigation.Add(new Button("Back", $"list?u={u}&p={pEnc}", "right"));
                else if (parent != null)
                {
                    string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                    string backUrl = $"list?u={u}&p={parentEnc}";
                    page.Navigation.Add(new Button("Back", backUrl, "right"));
                }
                else page.Navigation.Add(new Button("Back", $"list?u={req.User.Id}&p=", "right"));
                
                //elements
                e.Add(new HeadingElement(name));
                page.AddError();
                
                e.Add(new ButtonElement("View", null, BuildViewModeLink(req, directory != null, u, p)));
                
                if (file != null)
                {
                    e.Add(new ButtonElement("Download", null, $"download?u={u}&p={pEnc}", newTab: true));
                    e.Add(new ButtonElement("Text editor", null, $"editor?u={u}&p={pEnc}"));
                }
                else
                {
                    e.Add(new ButtonElement("Add content", null, $"add?u={u}&p={pEnc}", "green"));
                }
                
                if (parent != null)
                {
                    string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                    e.Add(new ContainerElement("Rename", new TextBox("Enter a name...", name, "name", onEnter: "SaveName()", onInput: "NameChanged()", autofocus: true)) {Button = new ButtonJS("Saved!", "SaveName()", id: "name-save")});
                    e.Add(new ButtonElementJS("Delete", null, $"Delete()", "red", id: "delete"));
                    e.Add(new ButtonElement("Move", null, $"move?u={u}&p={pEnc}&l={parentEnc}"));
                    e.Add(new ButtonElement("Copy", null, $"copy?u={u}&p={pEnc}&l={parentEnc}"));
                }
                
                if (u == req.User.Id)
                {
                    if (file == null || (name != "index.html" && name != "index.wfpg"))
                        e.Add(new ButtonElement("Share", null, $"share?u={u}&p={pEnc}"));
                }
                else if (req.LoggedIn)
                {
                    userProfile ??= GetOrCreateProfile(req);
                    if (userProfile.SavedShares.Any(x => x.Path == p && x.UserId == u))
                        e.Add(new ButtonElementJS("Remove from saved shares", null, "RemoveShare()", "red"));
                    else e.Add(new ButtonElementJS("Add to saved shares", null, "AddShare()", "green"));
                }
            } break;

            case "/edit/delete":
            { req.ForcePOST();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (parent == null || profile == null)
                    throw new NotFoundSignal();
                profile.Lock();
                string loc = $"../FilePlugin.Profiles/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}";
                if (directory != null)
                {
                    profile.SizeUsed -= new DirectoryInfo(loc).GetFiles("*", SearchOption.AllDirectories).Sum(x => x.Length);
                    parent.Directories.Remove(name);
                    Directory.Delete(loc, true);
                    await NotifyChangeListeners(u, segments.SkipLast(1));
                }
                else if (file != null)
                {
                    profile.SizeUsed -= new FileInfo(loc).Length;
                    parent.Files.Remove(name);
                    File.Delete(loc);
                    await NotifyChangeListeners(u, segments.SkipLast(1));
                }
                profile.UnlockSave();
            } break;

            case "/edit/rename":
            { req.ForcePOST();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p) && req.Query.TryGetValue("n", out var n) && NameOkay(n)))
                    throw new BadRequestSignal();
                var segments = p.Split('/');
                if (segments[^1] == n)
                    break;
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (parent == null || profile == null)
                    throw new NotFoundSignal();
                if (parent.Files.ContainsKey(n) || parent.Directories.ContainsKey(n))
                    throw new HttpStatusSignal(302);
                if (directory != null)
                {
                    profile.Lock();
                    parent.Directories.Remove(name);
                    parent.Directories[n] = directory;
                    Directory.Move(
                        $"../FilePlugin.Profiles/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}",
                        $"../FilePlugin.Profiles/{req.UserTable.Name}_{u}{string.Join('/', ((IEnumerable<string>)[..segments.SkipLast(1), n]).Select(Parsers.ToBase64PathSafe))}");
                    profile.UnlockSave();
                    await NotifyChangeListeners(u, segments.SkipLast(1));
                }
                else if (file != null)
                {
                    profile.Lock();
                    parent.Files.Remove(name);
                    parent.Files[n] = file;
                    File.Move(
                        $"../FilePlugin.Profiles/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}",
                        $"../FilePlugin.Profiles/{req.UserTable.Name}_{u}{string.Join('/', ((IEnumerable<string>)[..segments.SkipLast(1), n]).Select(Parsers.ToBase64PathSafe))}");
                    profile.UnlockSave();
                    await NotifyChangeListeners(u, segments.SkipLast(1));
                }
                else throw new NotFoundSignal();
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }
    }
}