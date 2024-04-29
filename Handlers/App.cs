using System.Web;
using Microsoft.AspNetCore.Http;
using uwap.WebFramework.Accounts;
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
                
                string pEnc = HttpUtility.UrlEncode(p);
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (directory != null)
                {
                    //edit mode > directory
                    page.Title = name + " - Files";
                    page.Scripts.Add(new Script(pathPrefix + "/query.js"));
                    page.Scripts.Add(new Script(pathPrefix + "/edit-d.js"));
                    if (parent != null)
                    {
                        string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                        string backUrl = $"{pathPrefix}/edit?u={u}&p={parentEnc}";
                        page.Navigation.Add(new Button("Back", backUrl, "right"));
                        page.Sidebar =
                        [
                            new ButtonElement(null, "Go up a level", backUrl),
                            ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"{pathPrefix}/edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null)),
                            ..parent.Files.Select(fKV => new ButtonElement(null, fKV.Key, $"{pathPrefix}/edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}", fKV.Key == name ? "green" : null))
                        ];
                    }
                    else page.Navigation.Add(new Button("Back", req.LoggedIn && req.User.Id == u ? pluginHome : $"{pathPrefix}/shares", "right"));
                    page.Navigation.Add(new Button("More", $"{pathPrefix}/more?u={u}&p={pEnc}", "right"));
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
                            e.Add(new ButtonElement(dKV.Key, null, $"{pathPrefix}/edit?u={u}&p={pEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}"));
                        foreach (var fKV in directory.Files)
                            e.Add(new ButtonElement(fKV.Key, fKV.Value.ModifiedUtc.ToLongDateString(), $"{pathPrefix}/edit?u={u}&p={pEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}"));
                    }
                }
                else if (file != null)
                {
                    //edit mode > file
                    page.Title = name + " - Files";
                    if (parent != null)
                    {
                        string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                        string backUrl = $"{pathPrefix}/edit?u={u}&p={parentEnc}";
                        page.Navigation.Add(new Button("Back", backUrl, "right"));
                        page.Sidebar =
                        [
                            new ButtonElement(null, "Go up a level", backUrl),
                            ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"{pathPrefix}/edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null)),
                            ..parent.Files.Select(fKV => new ButtonElement(null, fKV.Key, $"{pathPrefix}/edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}", fKV.Key == name ? "green" : null))
                        ];
                    }
                    else page.Navigation.Add(new Button("Back", $"{pathPrefix}/shares", "right"));
                    page.Navigation.Add(new Button("More", $"{pathPrefix}/more?u={u}&p={pEnc}", "right"));
                    e.Add(new HeadingElement(name, "Edit mode"));
                    string username = u == req.User.Id ? req.User.Username : req.UserTable[u].Username;
                    if (segments.Last().EndsWith(".wfpg"))
                        e.Add(new ButtonElement("View page", null, $"{pathPrefix}/@{username}{(segments.Last() == "default.wfpg" ? string.Join('/', segments.SkipLast(1)) : p[..^5])}"));
                    e.Add(new ButtonElement("View in browser", null, $"{pathPrefix}/@{username}{p}"));
                    e.Add(new ButtonElement("Download", null, $"/dl{pathPrefix}?u={u}&p={pEnc}", newTab: true));
                    e.Add(new ButtonElement("Edit as text", null, $"{pathPrefix}/editor?u={u}&p={pEnc}"));
                }
                else MissingFileOrAccess(req, e);
            } break;

            case "/editor":
            {
                //edit mode > file > editor
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
                
                string pEnc = HttpUtility.UrlEncode(p);
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (directory != null)
                    req.Status = 400;
                else if (file != null)
                {
                    page.Title = name + " - Files";
                    page.Scripts.Add(new Script(pathPrefix + "/query.js"));
                    page.Scripts.Add(new Script(pathPrefix + "/editor.js"));
                    if (parent != null)
                    {
                        string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                        string backUrl = $"{pathPrefix}/edit?u={u}&p={parentEnc}";
                        page.Navigation.Add(new ButtonJS("Back", $"GoBack('{backUrl}')", "right", id: "back"));
                        page.Sidebar =
                        [
                            new ButtonElementJS(null, "Go up a level", $"GoTo('{backUrl}')"),
                            ..parent.Directories.Select(dKV => new ButtonElementJS(null, dKV.Key, $"GoTo('{pathPrefix}/edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}')", dKV.Key == name ? "green" : null)),
                            ..parent.Files.Select(fKV => new ButtonElementJS(null, fKV.Key, $"GoTo('{pathPrefix}/editor?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}')", fKV.Key == name ? "green" : null))
                        ];
                    }
                    else page.Navigation.Add(new ButtonJS("Back", $"GoBack('{pathPrefix}/shares')", "right"));
                    page.Navigation.Add(new ButtonJS("More", $"GoTo('{pathPrefix}/more?u={u}&p={pEnc}')", "right"));
                    page.Styles.Add(new CustomStyle(
                        "div.editor { display: flex; flex-flow: column; }",
                        "div.editor textarea { flex: 1 1 auto; }",
                        "div.editor h1, div.editor h2, div.editor div.buttons { flex: 0 1 auto; }"
                    ));
                    page.HideFooter = true;
                    e.Add(new LargeContainerElementIsoTop(name, new TextArea("Loading...", null, "text", null, onInput: "TextChanged(); Resize();"), classes: "editor", id: "editor")
                    {
                        Button = new ButtonJS("Saved!", $"Save()", null, id: "save")
                    });
                }
                else MissingFileOrAccess(req, e);
            } break;

            default:
                if (path.StartsWith("/@"))
                {
                    //view mode
                    string[] segments = path[2..].Split('/', '\\').Select(x => HttpUtility.UrlDecode(x)).ToArray();
                    if (segments.Skip(1).Contains(".."))
                    {
                        req.Status = 400;
                        break;
                    }
                    
                    User? user = req.UserTable.FindByUsername(segments[0]);
                    if (user == null)
                    {
                req.Status = 404;
                        break;
                    }

                    segments[0] = "";
                    bool exists = CheckAccess(req, user.Id, segments, false, out _, out var parent, out var directory, out var file, out var name);
                    if (directory != null)
                    {
                        //view mode > directory
                        if (directory.Files.TryGetValue("default.wfpg", out file))
                            //wfpg (default.wfpg)
                            Server.ParseIntoPage(req, page, File.ReadAllLines($"../FilePlugin/{req.UserTable.Name}_{user.Id}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}/ZGVmYXVsdC53ZnBn"));
                        else
                        {
                            //list directories and files
                            req.Status = 501;
                        }
                    }
                    else if (file != null)
                    {
                        //view mode > file
                        req.Context.Response.ContentType = segments.Last().SplitAtLast('.', out _, out var extension) && Server.Config.MimeTypes.TryGetValue('.' + extension, out var contentType) ? contentType : null;
                        await req.Context.Response.SendFileAsync($"../FilePlugin/{req.UserTable.Name}_{user.Id}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}");
                        req.Page = new EmptyPage();
                        await req.Finish();
                    }
                    else if (exists)
                        MissingFileOrAccess(req, e);
                    else
                    {
                        segments[^1] += ".wfpg";
                        CheckAccess(req, user.Id, segments, false, out _, out _, out _, out file, out _);
                        if (file != null)
                            //wfpg (not default.wfpg)
                            Server.ParseIntoPage(req, page, File.ReadAllLines($"../FilePlugin/{req.UserTable.Name}_{user.Id}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}"));
                        else MissingFileOrAccess(req, e);
                    }
                }
                else req.Status = 404;
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