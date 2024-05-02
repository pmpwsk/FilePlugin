using System.Web;
using Microsoft.AspNetCore.Http;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    protected override async Task<int> HandleNeatly(AppRequest req, string path, string pathPrefix, Page page, List<IPageElement> e)
    {
        string pluginHome = pathPrefix == "" ? "/" : pathPrefix;
        page.Favicon = pathPrefix + "/icon.ico";
        page.Head.Add($"<link rel=\"manifest\" href=\"{pathPrefix}/manifest.json\" />");
        page.Navigation =
        [
            page.Navigation.Count != 0 ? page.Navigation.First() : new Button(req.Domain, "/"),
            new Button("Files", pluginHome)
        ];

        Profile? userProfile = null;
        if (req.LoggedIn)
            foreach (var cookie in req.Context.Request.Cookies)
            {
                if (!cookie.Key.StartsWith("FilePluginShare_"))
                    continue;
                if (!req.LoggedIn)
                    break;
                if (cookie.Key[16..].SplitAtFirst('_', out var u, out var p))
                {
                    p = HttpUtility.UrlDecode(p);
                    userProfile ??= GetOrCreateProfile(req);
                    if (!userProfile.SavedShares.Any(s => s.UserId == u && s.Path == p))
                    {
                        userProfile.Lock();
                        userProfile.SavedShares.Add(new(u, p));
                        userProfile.UnlockSave();
                    }
                    var node = FindNode(req, u, p.Split('/'), out var profile);
                    if (node != null && profile != null && !node.ShareAccess.ContainsKey(req.User.Id))
                    {
                        profile.Lock();
                        node.ShareAccess[req.User.Id] = false;
                        profile.UnlockSave();
                    }
                }
                req.Cookies.Delete(cookie.Key);
            }

        switch (path)
        {
            case "":
            {
                //main page
                page.Title = "Files";
                if (!req.LoggedIn)
                    return -1;
                
                userProfile ??= GetOrCreateProfile(req);
                e.Add(new HeadingElement("Files", $"{FileSizeString(userProfile.SizeUsed)} / {FileSizeString(userProfile.SizeLimit)} used"));
                e.Add(new ButtonElement("Edit mode", null, $"{pathPrefix}/edit?u={req.User.Id}&p="));
                e.Add(new ButtonElement("View mode", null, $"{pathPrefix}/@{req.User.Username}"));
                if (userProfile.SavedShares.Count != 0)
                    e.Add(new ButtonElement("Shared with me", null, pathPrefix + "/shares"));
            } break;

            case "/edit":
            {
                //edit mode
                if (!req.LoggedIn)
                    return -1;
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    return 400;
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
                    else
                    {
                        page.Navigation.Add(new Button("Back", req.LoggedIn && req.User.Id == u ? pluginHome : $"{pathPrefix}/shares", "right"));
                        if (u == req.User.Id)
                            page.Sidebar =
                            [
                                new ButtonElement("Menu:", null, pluginHome),
                                new ButtonElement(null, "Edit mode", $"{pathPrefix}/edit?u={req.User.Id}&p=", "green"),
                                new ButtonElement(null, "View mode", $"{pathPrefix}/@{req.User.Username}"),
                                new ButtonElement(null, "Shares", $"{pathPrefix}/shares")
                            ];
                    }
                    page.Navigation.Add(new Button("More", $"{pathPrefix}/more?u={u}&p={pEnc}", "right"));
                    e.Add(new HeadingElement(name, "Edit mode"));
                    e.Add(new ContainerElement("New", new TextBox("Enter a name...", null, "name", onEnter: "AddNode('false')", autofocus: true))
                    { Buttons = [
                        new ButtonJS("Text file", "AddNode('false')", "green"),
                        new ButtonJS("Folder", "AddNode('true')", "green")
                    ]});
                    e.Add(new ContainerElement("Upload", new FileSelector("files", true)) { Button = new ButtonJS("Upload", "Upload()", "green", id: "upload")});
                    page.AddError();
                    if (directory.Files.Count == 0 && directory.Directories.Count == 0)
                        e.Add(new ContainerElement("No items!", "", "red"));
                    else
                    {
                        foreach (var dKV in directory.Directories)
                            e.Add(new ButtonElement(dKV.Key, null, $"{pathPrefix}/edit?u={u}&p={pEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}"));
                        foreach (var fKV in directory.Files)
                            e.Add(new ButtonElement(fKV.Key, $"{FileSizeString(fKV.Value.Size)} | {fKV.Value.ModifiedUtc.ToLongDateString()}", $"{pathPrefix}/edit?u={u}&p={pEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}"));
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
                    else
                    {
                        page.Navigation.Add(new Button("Back", $"{pathPrefix}/shares", "right"));
                        if (u == req.User.Id)
                            page.Sidebar =
                            [
                                new ButtonElement("Menu:", null, pluginHome),
                                new ButtonElement(null, "Edit mode", $"{pathPrefix}/edit?u={req.User.Id}&p=", "green"),
                                new ButtonElement(null, "View mode", $"{pathPrefix}/@{req.User.Username}"),
                                new ButtonElement(null, "Shares", $"{pathPrefix}/shares")
                            ];
                    }
                    if (parent != null || req.LoggedIn)
                        page.Navigation.Add(new Button("More", $"{pathPrefix}/more?u={u}&p={pEnc}", "right"));
                    e.Add(new HeadingElement(name, "Edit mode"));
                    string username = u == req.User.Id ? req.User.Username : req.UserTable[u].Username;
                    if (segments.Last().EndsWith(".wfpg"))
                        e.Add(new ButtonElement("View page", null, $"{pathPrefix}/@{username}{(segments.Last() == "index.wfpg" ? string.Join('/', segments.SkipLast(1).Select(HttpUtility.UrlEncode)) : string.Join('/', p[..^5].Split('/').Select(HttpUtility.UrlEncode)))}"));
                    e.Add(new ButtonElement("View in browser", null, $"{pathPrefix}/@{username}{string.Join('/', p.Split('/').Select(HttpUtility.UrlEncode))}"));
                    e.Add(new ButtonElement("Download", null, $"/dl{pathPrefix}?u={u}&p={pEnc}", newTab: true));
                    e.Add(new ButtonElement("Edit as text", null, $"{pathPrefix}/editor?u={u}&p={pEnc}"));
                }
                else MissingFileOrAccess(req, e);
            } break;

            case "/more":
            {
                //edit mode > more
                if (!req.LoggedIn)
                    return -1;
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    return 400;
                string pEnc = HttpUtility.UrlEncode(p);
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (directory != null || file != null)
                {
                    page.Title = name + " - Files";
                    page.Scripts.Add(new Script(pathPrefix + "/query.js"));
                    page.Scripts.Add(new Script(pathPrefix + "/more.js"));
                    page.Navigation.Add(new Button("Back", $"{pathPrefix}/edit?u={u}&p={pEnc}", "right"));
                    e.Add(new HeadingElement(name, "Edit mode > More"));
                    page.AddError();
                    if (parent != null)
                    {
                        string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                        page.Sidebar =
                        [
                            new ButtonElement(null, "Go up a level", $"{pathPrefix}/edit?u={u}&p={parentEnc}"),
                            ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"{pathPrefix}/edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null)),
                            ..parent.Files.Select(fKV => new ButtonElement(null, fKV.Key, $"{pathPrefix}/edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}", fKV.Key == name ? "green" : null))
                        ];
                        e.Add(new ButtonElementJS("Delete", null, $"Delete()", "red", id: "delete"));
                        e.Add(new ContainerElement("Rename", new TextBox("Enter a name...", name, "name", onEnter: "SaveName()", onInput: "NameChanged()", autofocus: true)) {Button = new ButtonJS("Saved!", "SaveName()", id: "name-save")});
                        e.Add(new ButtonElement("Move", null, $"{pathPrefix}/move?u={u}&p={pEnc}"));
                        e.Add(new ButtonElement("Copy", null, $"{pathPrefix}/copy?u={u}&p={pEnc}"));
                    }
                    else if (u == req.User.Id)
                        page.Sidebar =
                        [
                            new ButtonElement("Menu:", null, pluginHome),
                            new ButtonElement(null, "Edit mode", $"{pathPrefix}/edit?u={req.User.Id}&p=", "green"),
                            new ButtonElement(null, "View mode", $"{pathPrefix}/@{req.User.Username}"),
                            new ButtonElement(null, "Shares", $"{pathPrefix}/shares")
                        ];
                    if (u == req.User.Id)
                        e.Add(new ButtonElement("Share", null, $"{pathPrefix}/share?u={u}&p={pEnc}"));
                    else if (req.LoggedIn)
                    {
                        userProfile ??= GetOrCreateProfile(req);
                        if (userProfile.SavedShares.Any(x => x.Path == p && x.UserId == u))
                            e.Add(new ButtonElementJS("Remove from saved shares", null, "RemoveShare()", "red"));
                        else e.Add(new ButtonElementJS("Add to saved shares", null, "AddShare()", "green"));
                    }
                }
                else MissingFileOrAccess(req, e);
            } break;

            case "/editor":
            {
                //edit mode > file > editor
                if (!req.LoggedIn)
                    return -1;
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    return 400;
                string pEnc = HttpUtility.UrlEncode(p);
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (directory != null)
                    return 400;
                else if (file != null)
                {
                    page.Title = name + " - Files";
                    page.Scripts.Add(new Script(pathPrefix + "/query.js"));
                    page.Scripts.Add(new Script(pathPrefix + "/editor.js"));
                    if (parent != null)
                    {
                        string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                        string parentUrl = $"{pathPrefix}/edit?u={u}&p={parentEnc}";
                        page.Navigation.Add(new ButtonJS("Back", "GoBack()", "right", id: "back"));
                        page.Sidebar =
                        [
                            new ButtonElement(null, "Go up a level", parentUrl),
                            ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"{pathPrefix}/edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null)),
                            ..parent.Files.Select(fKV => new ButtonElement(null, fKV.Key, $"{pathPrefix}/editor?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}", fKV.Key == name ? "green" : null))
                        ];
                    }
                    else
                    {
                        page.Navigation.Add(new ButtonJS("Back", "GoBack()", "right", id: "back"));
                        if (u == req.User.Id)
                            page.Sidebar =
                            [
                                new ButtonElement("Menu:", null, pluginHome),
                                new ButtonElement(null, "Edit mode", $"{pathPrefix}/edit?u={req.User.Id}&p=", "green"),
                                new ButtonElement(null, "View mode", $"{pathPrefix}/@{req.User.Username}"),
                                new ButtonElement(null, "Shares", $"{pathPrefix}/shares")
                            ];
                    }
                    page.Navigation.Add(new Button("More", $"{pathPrefix}/more?u={u}&p={pEnc}", "right"));
                    page.Styles.Add(new Style(pathPrefix + "/editor.css"));
                    page.HideFooter = true;
                    e.Add(new LargeContainerElementIsoTop(name, new TextArea("Loading...", null, "text", null, onInput: "TextChanged(); Resize();"), classes: "editor", id: "editor")
                    {
                        Button = new ButtonJS("Saved!", $"Save()", null, id: "save")
                    });
                }
                else MissingFileOrAccess(req, e);
            } break;

            case "/share":
            {
                //edit mode > share
                if (!req.LoggedIn)
                    return -1;
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    return 400;
                if (u != req.User.Id)
                    return 403;
                
                string pEnc = HttpUtility.UrlEncode(p);
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (directory != null || file != null)
                {
                    page.Title = name + " - Files";
                    page.Scripts.Add(new Script(pathPrefix + "/query.js"));
                    page.Scripts.Add(new Script(pathPrefix + "/share.js"));
                    page.Navigation.Add(new Button("Back", $"{pathPrefix}/more?u={u}&p={pEnc}", "right"));
                    string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                    if (parent != null)
                        page.Sidebar =
                        [
                            new ButtonElement(null, "Go up a level", $"{pathPrefix}/edit?u={u}&p={parentEnc}"),
                            ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"{pathPrefix}/edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null)),
                            ..parent.Files.Select(fKV => new ButtonElement(null, fKV.Key, $"{pathPrefix}/edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}", fKV.Key == name ? "green" : null))
                        ];
                    else if (u == req.User.Id)
                        page.Sidebar =
                        [
                            new ButtonElement("Menu:", null, pluginHome),
                            new ButtonElement(null, "Edit mode", $"{pathPrefix}/edit?u={req.User.Id}&p=", "green"),
                            new ButtonElement(null, "View mode", $"{pathPrefix}/@{req.User.Username}"),
                            new ButtonElement(null, "Shares", $"{pathPrefix}/shares")
                        ];
                    e.Add(new HeadingElement(name, "Edit mode > Share"));
                    e.Add(new ButtonElementJS(null, "Copy link without inviting", $"navigator.clipboard.writeText('{req.Context.ProtoHost()}{pathPrefix}/shares?u={u}&p={pEnc}'); document.querySelector('#copy').children[0].innerText = 'Copied!'", "green", id: "copy"));
                    page.AddError();
                    e.Add(new ContainerElement("Add access",
                    [
                        new TextBox("Enter a username...", "", "name", onEnter: "AddAccess()", autofocus: true),
                        new Checkbox("Can edit", "edit")
                    ]) {Button = new ButtonJS("Add", "AddAccess()", "green")});
                    Node node;
                    if (directory != null)
                        node = directory;
                    else if (file != null)
                        node = file;
                    else break;
                    foreach (var sKV in node.ShareAccess)
                        e.Add(new ContainerElement(sKV.Key == "*" ? "*" : (req.UserTable.TryGetValue(sKV.Key, out var user) ? user.Username : $"[{sKV.Key}]"), "") { Buttons = 
                        [
                            sKV.Value ? new ButtonJS("View/Edit", $"SetAccess('{sKV.Key}', 'false')") : new ButtonJS("View only", $"SetAccess('{sKV.Key}', 'true')"),
                            new ButtonJS("Remove", $"RemoveAccess('{sKV.Key}')", "red")
                        ]});
                    if (node.ShareInvite == null || node.ShareInvite.Expiration < DateTime.UtcNow)
                        e.Add(new ContainerElement("Invite", new TextBox("Expires after x days...", null, "expiration", onEnter: "CreateInvite()")) { Button = new ButtonJS("Create", "CreateInvite()", "green") });
                    else e.Add(new ContainerElement("Invite", $"Expires: {(node.ShareInvite.Expiration.Year >= 9999 ? "Never" : node.ShareInvite.Expiration.ToLongDateString())}") { Buttons = 
                    [
                        new ButtonJS("Copy", $"navigator.clipboard.writeText('{req.Context.ProtoHost()}{pathPrefix}/shares?u={u}&p={pEnc}&c={node.ShareInvite.Code}'); document.querySelector('#copy-invite').innerText = 'Copied!'", id: "copy-invite"),
                        new ButtonJS("Delete", "DeleteInvite()", "red")
                    ]});
                }
                else return 404;
            } break;

            case "/shares":
            {
                page.Title = "Shares - Files";
                if (req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p))
                {
                    if (req.LoggedIn && req.User.Id == u)
                    {
                        req.Redirect(pluginHome);
                        break;
                    }
                    //share selected
                    var pEnc = HttpUtility.UrlEncode(p);
                    var segments = p.Split('/');
                    CheckAccess(req, u, segments, true, out _, out _, out var directory, out var file, out _);
                    bool canEdit;
                    if (directory != null || file != null)
                        canEdit = true;
                    else
                    {
                        CheckAccess(req, u, segments, false, out _, out _, out directory, out file, out _);
                        if (directory != null || file != null)
                            canEdit = false;
                        else if (req.Query.TryGetValue("c", out var c))
                        {
                            Node? node = FindNode(req, u, segments, out var profile);
                            if (node != null && node.ShareInvite != null && node.ShareInvite.Expiration >= DateTime.UtcNow && node.ShareInvite.Code == c)
                            {
                                if (profile != null && req.LoggedIn && !node.ShareAccess.ContainsKey(req.User.Id))
                                {
                                    profile.Lock();
                                    node.ShareAccess[req.User.Id] = false;
                                    profile.UnlockSave();
                                }
                                else req.Cookies.Add($"FilePluginShare_{u}_{pEnc}", c, new() {Expires = Min(node.ShareInvite.Expiration, DateTime.UtcNow.AddDays(14))});
                                canEdit = false;
                            }
                            else
                            {
                                RemoveBrokenShare(req, u, p);
                                MissingFileOrAccess(req, e);
                                break;
                            }
                        }
                        else
                        {
                            RemoveBrokenShare(req, u, p);
                            MissingFileOrAccess(req, e);
                            break;
                        }
                    }
                    if (!req.UserTable.TryGetValue(u, out var user))
                    {
                        RemoveBrokenShare(req, u, p);
                        MissingFileOrAccess(req, e);
                        break;
                    }
                    page.Navigation.Add(new Button("Back", pluginHome, "right"));
                    e.Add(new HeadingElement("Shares", $"@{user.Username}{p}"));
                    if (canEdit)
                        e.Add(new ButtonElement("Edit mode", null, $"{pathPrefix}/edit?u={u}&p={pEnc}"));
                    e.Add(new ButtonElement(canEdit ? "View mode" : "View", null, $"{pathPrefix}/@{user.Username}{(segments.Last().EndsWith(".wfpg") && segments.Last() != "index.wfpg" ? string.Join('/', ((IEnumerable<string>)[..segments.SkipLast(1), segments.Last()[..^5]]).Select(HttpUtility.UrlEncode)) : string.Join('/', p.Split('/').Select(HttpUtility.UrlEncode)))}"));
                    if (req.LoggedIn)
                    {
                        userProfile ??= GetOrCreateProfile(req);
                        if (!userProfile.SavedShares.Any(s => s.UserId == u && s.Path == p))
                        {
                            userProfile.Lock();
                            userProfile.SavedShares.Add(new(u, p));
                            userProfile.UnlockSave();
                        }
                        e.Add(new ButtonElementJS("Remove from saved shares", null, "RemoveShare()", "red"));
                    }
                    else e.Add(new ButtonElement(null, "You are viewing this share without logging in. That means you will lose access to it once this link has expired and the owner of this share can't allow you to edit anything.</p><p>Click here to log in.", $"{Server.Config.Accounts.LoginPath}?redirect={HttpUtility.UrlEncode($"{pathPrefix}/shares?u={u}&p={pEnc}")}", "red"));
                }
                else
                {
                    //list shares
                    if (!req.LoggedIn)
                        return -1;
                    userProfile ??= GetOrCreateProfile(req);
                    page.Navigation.Add(new Button("Back", pluginHome, "right"));
                    page.Sidebar =
                    [
                        new ButtonElement("Menu:", null, pluginHome),
                        new ButtonElement(null, "Edit mode", $"{pathPrefix}/edit?u={req.User.Id}&p="),
                        new ButtonElement(null, "View mode", $"{pathPrefix}/@{req.User.Username}"),
                        new ButtonElement(null, "Shares", $"{pathPrefix}/shares", "green")
                    ];
                    e.Add(new HeadingElement("Shares"));
                    if (userProfile.SavedShares.Count == 0)
                        e.Add(new ContainerElement("No items!", "", "red"));
                    else foreach (var s in userProfile.SavedShares)
                        if (s.Path == "")
                            e.Add(new ButtonElement(req.UserTable.TryGetValue(s.UserId, out var user) ? $"@{user.Username}" : $"[{s.UserId}]", null, $"{pathPrefix}/shares?u={s.UserId}&p={HttpUtility.UrlEncode(s.Path)}"));
                        else e.Add(new ButtonElement(s.Path.After('/'), (req.UserTable.TryGetValue(s.UserId, out var user) ? $"@{user.Username}" : $"[{s.UserId}]") + s.Path, $"{pathPrefix}/shares?u={s.UserId}&p={HttpUtility.UrlEncode(s.Path)}"));
                }
            } break;

            default:
                if (path.StartsWith("/@"))
                {
                    //view mode
                    string[] segments = path[2..].Split('/', '\\').Select(x => HttpUtility.UrlDecode(x)).ToArray();
                    
                    User? user = req.UserTable.FindByUsername(segments[0]);
                    if (user == null)
                        return 404;

                    segments[0] = "";
                    bool exists = CheckAccess(req, user.Id, segments, false, out _, out var parent, out var directory, out var file, out var name);
                    if (directory != null)
                    {
                        //view mode > directory
                        if (directory.Files.TryGetValue("index.wfpg", out file))
                        {
                            //wfpg (index.wfpg)
                            page.Title = name;
                            Server.ParseIntoPage(req, page, File.ReadAllLines($"../FilePlugin/{req.UserTable.Name}_{user.Id}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}/aW5kZXgud2ZwZw=="));
                            page.Title += " - Files";
                        }
                        else
                        {
                            //list directories and files
                            page.Title = name + " - Files";
                            if (parent != null)
                            {
                                string parentUrl = $"{pathPrefix}/@{user.Username}{string.Join('/', segments.SkipLast(1).Select(HttpUtility.UrlEncode))}";
                                page.Navigation.Add(new Button("Back", parentUrl, "right"));
                                page.Sidebar =
                                [
                                    new ButtonElement(null, "Go up a level", parentUrl),
                                    ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"{parentUrl}/{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null))
                                    //don't list files
                                ];
                            }
                            else
                            {
                                page.Navigation.Add(new Button("Back", req.LoggedIn && req.User.Id == user.Id ? pluginHome : $"{pathPrefix}/shares", "right"));
                                if (req.LoggedIn && user.Id == req.User.Id)
                                    page.Sidebar =
                                    [
                                        new ButtonElement("Menu:", null, pluginHome),
                                        new ButtonElement(null, "Edit mode", $"{pathPrefix}/edit?u={req.User.Id}&p="),
                                        new ButtonElement(null, "View mode", $"{pathPrefix}/@{req.User.Username}", "green"),
                                        new ButtonElement(null, "Shares", $"{pathPrefix}/shares")
                                    ];
                            }
                            e.Add(new HeadingElement(name, "View mode"));
                            if (directory.Files.Count == 0 && directory.Directories.Count == 0)
                                e.Add(new ContainerElement("No items!", "", "red"));
                            else
                            {
                                foreach (var dKV in directory.Directories)
                                    e.Add(new ButtonElement(dKV.Key, null, $"{req.Path}/{HttpUtility.UrlEncode(dKV.Key)}"));
                                foreach (var fKV in directory.Files)
                                    e.Add(new ButtonElement(fKV.Key, $"{FileSizeString(fKV.Value.Size)} | {fKV.Value.ModifiedUtc.ToLongDateString()}", $"{req.Path}/{HttpUtility.UrlEncode(fKV.Key.EndsWith(".wfpg") ? fKV.Key[..^5] : fKV.Key)}"));
                            }
                        }
                    }
                    else if (file != null)
                    {
                        //view mode > file
                        req.Context.Response.ContentType = segments.Last().SplitAtLast('.', out _, out var extension) && Server.Config.MimeTypes.TryGetValue('.' + extension, out var contentType) ? contentType : null;
                        if (req.Context.Request.Headers.TryGetValue("If-None-Match", out var sentEtag) && sentEtag == file.ModifiedUtc.Ticks.ToString())
                            return 304;
                        req.Context.Response.Headers.ETag = file.ModifiedUtc.Ticks.ToString();
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
                        {
                            //wfpg (not index.wfpg)
                            page.Title = segments[^1];
                            Server.ParseIntoPage(req, page, File.ReadAllLines($"../FilePlugin/{req.UserTable.Name}_{user.Id}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}"));
                            page.Title += " - Files";
                        }
                        else MissingFileOrAccess(req, e);
                    }
                }
                else return 404;
                break;
        }

        return 0;
    }

    private static void MissingFileOrAccess(AppRequest req, List<IPageElement> e)
        => e.Add(new LargeContainerElement("Error", "The file/folder you're looking for either doesn't exist or you don't have access to it." + (req.LoggedIn?"":" You are not logged in, that might be the reason."), "red"));

    private void RemoveBrokenShare(AppRequest req, string userId, string path)
    {
        if (!req.LoggedIn)
            return;
        var profile = GetOrCreateProfile(req);
        if (profile.SavedShares.Any(s => s.UserId == userId && s.Path == path))
        {
            profile.Lock();
            profile.SavedShares.RemoveAll(s => s.UserId == userId && s.Path == path);
            profile.UnlockSave();
        }
    }

    private static string FileSizeString(long size)
    {
        return size switch
        {
            0 => "0 Bytes",
            1 => "1 Byte",
            _ => (int)Math.Floor(Math.Log(size, 1024)) switch
            {
                0 => $"{size} Bytes",
                1 => $"{Number(1024)} KiB",
                2 => $"{Number(1048576)} MiB",
                3 => $"{Number(1073741824)} GiB",
                4 => $"{Number(1099511627776)} TiB",
                5 => $"{Number(1125899906842624)} PiB",
                _ => $"{Number(1152921504606846976)} EiB"
            }
        };
        
        string Number(double d)
        {
            string result = Math.Round(size / d, 2, MidpointRounding.AwayFromZero).ToString();
            if (result.SplitAtLast('.', out _, out var r))
                if (r.Length == 1)
                    return result + '0';
                else return result;
            else return result + ".00";
        }
    }

    private class EmptyPage : IPage
    {
        public IEnumerable<string> Export(AppRequest request)
            => [];
    }
}