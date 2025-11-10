using System.Web;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin
{
    private Task HandleShare(Request req)
    {
        switch (req.Path)
        {
            case "/share":
            { CreatePage(req, "Files", out var page, out var e, out _);
                req.ForceLogin();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                if (u != req.User.Id)
                    throw new ForbiddenSignal();
                string pEnc = HttpUtility.UrlEncode(p);
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (file != null && (name == "index.html" || name == "index.wfpg"))
                    throw new BadRequestSignal();
                if (directory == null && file == null)
                {
                    MissingFileOrAccess(req, e);
                    break;
                }
            
                //head
                page.Title = name + " - Files";
                page.Scripts.Add(Presets.SendRequestScript);
                page.Scripts.Add(new Script("query.js"));
                page.Scripts.Add(new Script("share.js"));
                
                //sidebar + navigation
                page.Navigation.Add(new Button("Back", $"edit?u={u}&p={pEnc}", "right"));
                string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                if (parent != null)
                    page.Sidebar =
                    [
                        new ButtonElement(null, "Go up a level", $"edit?u={u}&p={parentEnc}"),
                        ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null)),
                        ..parent.Files.Select(fKV => new ButtonElement(null, fKV.Key, $"edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}", fKV.Key == name ? "green" : null))
                    ];
                
                //elements
                e.Add(new HeadingElement(name, "Share"));
                e.Add(new ButtonElementJS(null, "Copy link without inviting", $"navigator.clipboard.writeText('{req.PluginPathPrefix}/shares?u={u}&p={pEnc}'); document.getElementById('copy').children[0].innerText = 'Copied!'", "green", id: "copy"));
                page.AddError();
                e.Add(new ContainerElement("Add access",
                [
                    new TextBox("Enter a user's name...", "", "target-name", onEnter: "AddAccess()", autofocus: true),
                    new Checkbox("Can edit", "edit")
                ]) {Button = new ButtonJS("Add", "AddAccess()", "green")});
                Node node;
                if (directory != null)
                    node = directory;
                else if (file != null)
                    node = file;
                else return Task.CompletedTask;
                foreach (var sKV in node.ShareAccess)
                    e.Add(new ContainerElement(sKV.Key == "*" ? "*" : (req.UserTable.TryGetValue(sKV.Key, out var user) ? user.Username : $"[{sKV.Key}]"), "") { Buttons = 
                    [
                        sKV.Value ? new ButtonJS("View/Edit", $"SetAccess('{sKV.Key}', 'false')") : new ButtonJS("View only", $"SetAccess('{sKV.Key}', 'true')"),
                        new ButtonJS("Remove", $"RemoveAccess('{sKV.Key}')", "red")
                    ]});
                if (node.ShareInvite == null || node.ShareInvite.Expiration < DateTime.UtcNow)
                    e.Add(new ContainerElement("Invite", new TextBox("Expires after x days... (0=never)", null, "expiration", onEnter: "CreateInvite()")) { Button = new ButtonJS("Create", "CreateInvite()", "green") });
                else e.Add(new ContainerElement("Invite", $"Expires: {(node.ShareInvite.Expiration.Year >= 9999 ? "Never" : node.ShareInvite.Expiration.ToLongDateString())}") { Buttons = 
                [
                    new ButtonJS("Copy", $"navigator.clipboard.writeText('{req.PluginPathPrefix}/shares?u={u}&p={pEnc}&c={node.ShareInvite.Code}'); document.getElementById('copy-invite').innerText = 'Copied!'", id: "copy-invite"),
                    new ButtonJS("Delete", "DeleteInvite()", "red")
                ]});
            } break;

            case "/share/set":
            { req.ForcePOST();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                if (!(req.LoggedIn && req.User.Id == u))
                    throw new ForbiddenSignal();
                if (!req.Query.TryGetValue("uid", out var uid))
                {
                    if (!req.Query.TryGetValue("un", out var un))
                        throw new BadRequestSignal();
                    if (un == "*")
                        uid = "*";
                    else
                    {
                        var user = req.UserTable.FindByUsername(un);
                        if (user == null)
                            throw new NotFoundSignal();
                        uid = user.Id;
                    }
                }
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out _, out var directory, out var file, out var name);
                if (file != null && (name == "index.html" || name == "index.wfpg"))
                    throw new BadRequestSignal();
                if (profile == null)
                    throw new NotFoundSignal();
                Dictionary<string,bool> shareAccess;
                if (directory != null)
                    shareAccess = directory.ShareAccess;
                else if (file != null)
                    shareAccess = file.ShareAccess;
                else throw new NotFoundSignal();
                if (uid != null)
                {
                    profile.Lock();
                    if (req.Query.TryGetValue("e", out bool canEdit))
                        shareAccess[uid] = canEdit;
                    else shareAccess.Remove(uid);
                    profile.UnlockSave();
                }
            } break;

            case "/share/invite":
            { req.ForcePOST();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                if (!(req.LoggedIn && req.User.Id == u))
                    throw new ForbiddenSignal();
                int e;
                if (req.Query.TryGetValue("e", out var eString))
                {
                    if (!(int.TryParse(eString, out e) && e >= 0))
                        throw new BadRequestSignal();
                }
                else e = -1;
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out _, out var directory, out var file, out var name);
                if (file != null && (name == "index.html" || name == "index.wfpg"))
                    throw new BadRequestSignal();
                if (profile == null)
                    throw new NotFoundSignal();
                Node node;
                if (directory != null)
                    node = directory;
                else if (file != null)
                    node = file;
                else throw new NotFoundSignal();
                profile.Lock();
                node.ShareInvite = e switch
                {
                    -1 => null,
                    0 => new(Parsers.RandomString(24), DateTime.MaxValue),
                    _ => new(Parsers.RandomString(24), DateTime.UtcNow.AddDays(e)),
                };
                profile.UnlockSave();

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