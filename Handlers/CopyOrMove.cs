using System.Web;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin
{
    private async Task HandleCopyOrMove(Request req, bool isCopy)
    {
        switch (req.Path)
        {
            case "/copy":
            case "/move":
            { CreatePage(req, $"{(isCopy ? "Copy" : "Move")} - Files", out var page, out var e, out _);
                req.ForceLogin();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p) && req.Query.TryGetValue("l", out var l)))
                    throw new BadRequestSignal();
                string item_pEnc = HttpUtility.UrlEncode(p);
                var item_segments = p.Split('/');
                CheckAccess(req, u, item_segments, true, out _, out var item_parent, out var item_directory, out var item_file, out var item_name);
                if (item_parent == null)
                    throw new ForbiddenSignal();
                if (item_directory == null && item_file == null)
                {
                    MissingFileOrAccess(req, e);
                    break;
                }
                string loc_pEnc = HttpUtility.UrlEncode(l);
                var loc_segments = l.Split('/');
                CheckAccess(req, u, loc_segments, true, out _, out var loc_parent, out var loc_directory, out var loc_file, out var loc_name);
                if (loc_directory == null)
                    if (loc_file != null)
                        throw new BadRequestSignal();
                    else
                    {
                        MissingFileOrAccess(req, e);
                        break;
                    }

                //head
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("query.js"));
                page.Scripts.Add(new Script("copy-move.js"));
                
                //sidebar + navigation
                if (loc_parent != null)
                {
                    string loc_parentEnc = HttpUtility.UrlEncode(string.Join('/', loc_segments.SkipLast(1)));
                    string parentUrl = $"{(isCopy?"copy":"move")}?u={u}&p={item_pEnc}&l={loc_parentEnc}";
                    page.Navigation.Add(new Button("Back", parentUrl, "right"));
                    page.Sidebar =
                    [
                        new ButtonElement(null, "Go up a level", parentUrl),
                        ..loc_parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"{(isCopy?"copy":"move")}?u={u}&p={item_pEnc}&l={loc_parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == loc_name ? "green" : null))
                    ];
                }
                
                //elements
                e.Add(new LargeContainerElement(loc_name, $"You are {(isCopy?"copy":"mov")}ing: {item_name}") { Button = new Button("Cancel", $"edit?u={u}&p={item_pEnc}", "red")});
                if (!(loc_directory == item_parent || loc_directory.Directories.ContainsKey(item_name) || loc_directory.Directories.ContainsKey(item_name) || (item_directory != null && loc_pEnc.StartsWith(item_pEnc + "%2f")) || loc_directory == item_directory))
                    e.Add(new ButtonElementJS($"{(isCopy?"Copy":"Move")} here", null, "CopyOrMove()", "green"));
                page.AddError();
                foreach (var dir in loc_directory.Directories)
                    e.Add(new ButtonElement(dir.Key, null, $"{(isCopy?"copy":"move")}?u={u}&p={item_pEnc}&l={loc_pEnc}%2f{HttpUtility.UrlEncode(dir.Key)}"));
                if (loc_directory.Directories.Count == 0)
                    e.Add(new ContainerElement("No items!", "", "red"));
            } break;

            case "/copy/do":
            case "/move/do":
            { req.ForcePOST(); req.ForceLogin(false);
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p) && req.Query.TryGetValue("l", out var l)))
                    throw new BadRequestSignal();
                string item_pEnc = HttpUtility.UrlEncode(p);
                var item_segments = p.Split('/');
                CheckAccess(req, u, item_segments, true, out var profile, out var item_parent, out var item_directory, out var item_file, out var item_name);
                if (item_parent == null)
                    throw new ForbiddenSignal();
                if (item_directory == null && item_file == null)
                    throw new NotFoundSignal();
                string loc_pEnc = HttpUtility.UrlEncode(l);
                var loc_segments = l.Split('/');
                CheckAccess(req, u, loc_segments, true, out _, out var loc_parent, out var loc_directory, out var loc_file, out var loc_name);
                if (loc_directory == null)
                    if (loc_file != null)
                        throw new BadRequestSignal();
                    else throw new NotFoundSignal();
                if (loc_directory == item_parent || loc_directory.Directories.ContainsKey(item_name) || loc_directory.Files.ContainsKey(item_name) || (item_directory != null && loc_pEnc.StartsWith(item_pEnc + "%2f")) || loc_directory == item_directory)
                    throw new BadRequestSignal();
                if (profile == null)
                    throw new ServerErrorSignal();
                profile.Lock();
                string source = $"../FilePlugin.Profiles/{req.UserTable.Name}_{u}{string.Join('/', item_segments.Select(Parsers.ToBase64PathSafe))}";
                string target = $"../FilePlugin.Profiles/{req.UserTable.Name}_{u}{string.Join('/', ((IEnumerable<string>)[..loc_segments, item_name]).Select(Parsers.ToBase64PathSafe))}";
                if (isCopy)
                {
                    if (item_directory != null)
                    {
                        CopyDirectory(source, target);
                        loc_directory.Directories[item_name] = item_directory.Copy();
                        await NotifyChangeListeners(u, loc_segments);
                    }
                    else if (item_file != null)
                    {
                        File.Copy(source, target);
                        loc_directory.Files[item_name] = item_file.Copy();
                        await NotifyChangeListeners(u, loc_segments);
                    }
                }
                else
                {
                    if (item_directory != null)
                    {
                        Directory.Move(source, target);
                        loc_directory.Directories[item_name] = item_directory;
                        item_parent.Directories.Remove(item_name);
                        await NotifyChangeListeners(u, item_segments.SkipLast(1));
                        await NotifyChangeListeners(u, loc_segments);
                    }
                    else if (item_file != null)
                    {
                        File.Move(source, target);
                        loc_directory.Files[item_name] = item_file;
                        item_parent.Files.Remove(item_name);
                        await NotifyChangeListeners(u, item_segments.SkipLast(1));
                        await NotifyChangeListeners(u, loc_segments);
                    }
                }
                profile.UnlockSave();
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }
    }
}