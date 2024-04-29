using System.Web;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    public override async Task Handle(AppRequest req, string path, string pathPrefix)
    {
        Presets.CreatePage(req, "Files", out var page, out var e);
        Presets.Navigation(req, page);

        string pluginHome = pathPrefix == "" ? "/" : pathPrefix;
        page.Navigation =
        [
            page.Navigation.Count != 0 ? page.Navigation.First() : new Button(req.Domain, "/"),
            new Button("Files", pluginHome)
        ];

        switch (path)
        {
            case "":
            {
                //main page
                if (!req.LoggedIn)
                {
                    req.RedirectToLogin();
                    break;
                }

                var profile = GetOrCreateProfile(req);
                e.Add(new HeadingElement("Files"));
                e.Add(new ButtonElement("Edit mode", null, $"{pathPrefix}/edit?u={req.User.Id}&p="));
                e.Add(new ButtonElement("View mode", null, $"{pathPrefix}/@{req.User.Username}"));
                if (profile.SavedShares.Count == 0)
                    e.Add(new ButtonElement("Shared with me", null, pathPrefix + "/shares"));
            } break;

            case "/edit":
            {
                //edit mode
                if (!req.LoggedIn)
                {
                    req.RedirectToLogin();
                    break;
                }
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                {
                    req.Status = 400;
                    break;
                }
                
                var segments = p.Split('/');
                bool exists = CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (directory != null)
                {
                    //edit mode > directory
                    page.Title = name + " - Files";
                    page.Scripts.Add(new Script(pathPrefix + "/query.js"));
                    page.Scripts.Add(new Script(pathPrefix + "/edit-d.js"));
                    if (parent != null)
                    {
                        string parentPath = string.Join('/', segments.SkipLast(1));
                        string backUrl = $"{pathPrefix}/edit?u={u}&p={HttpUtility.UrlEncode(parentPath)}";
                        page.Navigation.Add(new Button("Back", backUrl, "right"));
                        page.Sidebar =
                        [
                            new ButtonElement(null, "Go up a level", backUrl),
                            ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"{pathPrefix}/edit?u={u}&p={HttpUtility.UrlEncode($"{parentPath}/{dKV.Key}")}", dKV.Key == name ? "green" : null)),
                            ..parent.Files.Select(fKV => new ButtonElement(null, fKV.Key, $"{pathPrefix}/edit?u={u}&p={HttpUtility.UrlEncode($"{parentPath}/{fKV.Key}")}", fKV.Key == name ? "green" : null))
                        ];
                    }
                    else page.Navigation.Add(new Button("Back", req.LoggedIn && req.User.Id == u ? pluginHome : $"{pathPrefix}/shares", "right"));
                    page.Navigation.Add(new Button("More", $"{pathPrefix}/more?u={u}&p={HttpUtility.UrlEncode(p)}", "right"));
                    e.Add(new HeadingElement(name, "Edit mode"));
                    e.Add(new ContainerElement("New:", new TextBox("Enter a name...", null, "name", onEnter: "AddNode('false')", autofocus: true))
                    { Buttons = [
                        new ButtonJS("File", "AddNode('false')", "green"),
                        new ButtonJS("Folder", "AddNode('true')", "green")
                    ]});
                    page.AddError();
                    if (directory.Files.Count == 0 && directory.Directories.Count == 0)
                        e.Add(new ContainerElement("No items!", "", "red"));
                    else
                    {
                        foreach (var dKV in directory.Directories)
                            e.Add(new ButtonElement(dKV.Key, null, $"{pathPrefix}/edit?u={u}&p={HttpUtility.UrlEncode($"{p}/{dKV.Key}")}"));
                        foreach (var fKV in directory.Files)
                            e.Add(new ButtonElement(fKV.Key, fKV.Value.ModifiedUtc.ToLongDateString(), $"{pathPrefix}/edit?u={u}&p={HttpUtility.UrlEncode($"{p}/{fKV.Key}")}"));
                    }
                }
                else if (file != null)
                {
                    //edit mode > file
                    page.Title = name + " - Files";
                    //////////////////////////////////
                    req.Status = 501;
                }
                else if (exists)
                    if (req.LoggedIn)
                        req.Status = 403;
                    else req.RedirectToLogin();
                else req.Status = 404;
            } break;

            default:
                req.Status = 404;
                break;
        }
    }

    private static void MissingFileOrAccess(AppRequest req, List<IPageElement> e)
        => e.Add(new LargeContainerElement("Error", "The file/folder you're looking for either doesn't exist or you don't have access to it." + (req.LoggedIn?"":" You are not logged in, that might be the reason."), "red"));

    private class EmptyPage : IPage
    {
        public IEnumerable<string> Export(AppRequest request)
            => [];
    }
}