using System.Web;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin
{
    private Task HandleList(Request req)
    {
        switch (req.Path)
        {
            case "/list":
            { CreatePage(req, "Files", out var page, out var e, out var userProfile);
                req.ForceLogin();
                userProfile ??= GetOrCreateProfile(req);
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
                
                var profile = GetOrCreateProfile(req, u);
                
                //head
                page.Title = name + " - Files";
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("query.js"));
                page.Scripts.Add(new Script("list.js"));
                
                //sidebar + navigation
                if (parent != null)
                {
                    string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                    string backUrl = $"list?u={u}&p={parentEnc}";
                    page.Navigation.Add(new Button("Back", backUrl, "right"));
                    page.Sidebar =
                    [
                        new ButtonElement(null, "Go up a level", backUrl),
                        ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"list?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null)),
                        ..parent.Files.Select(fKV => new ButtonElement(null, fKV.Key, $"edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}", fKV.Key == name ? "green" : null))
                    ];
                }
                page.Navigation.Add(new Button("More", $"edit?u={u}&p={pEnc}", "right"));
                if (p == "")
                {
                    if (userProfile.SavedShares.Count != 0)
                        page.Navigation.Add(new Button("Shares", "shares"));
                    if (req.IsAdmin)
                        page.Navigation.Add(new Button("Manage", "profiles"));
                }
                page.Navigation.Add(new Button("Search", $"search?u={u}&p={pEnc}", "right"));

                //heading
                e.Add(new LargeContainerElement(name, $"{FileSizeString(profile.SizeUsed)} / {FileSizeString(profile.SizeLimit)} used") { Button = new Button("Add", $"add?u={u}&p={pEnc}", "green")});
                
                //items
                if (directory.Files.Count == 0 && directory.Directories.Count == 0)
                    e.Add(new ContainerElement("No items!", "", "red"));
                else
                {
                    foreach (var dKV in directory.Directories)
                        e.Add(new ButtonElement(dKV.Key, null, $"list?u={u}&p={pEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}"));
                    foreach (var fKV in directory.Files)
                        e.Add(new ButtonElement(fKV.Key, $"{FileSizeString(fKV.Value.Size)} | {fKV.Value.ModifiedUtc.ToLongDateString()}", $"edit?u={u}&p={pEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}"));
                }
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }

        return Task.CompletedTask;
    }
}