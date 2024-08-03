using System.Web;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    private Task HandleShares(Request req)
    {
        switch (req.Path)
        {
            case "/shares":
            { CreatePage(req, "Files", out var page, out var e, out var userProfile);
                page.Title = "Shares - Files";
                if (req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p))
                {
                    if (req.LoggedIn && req.User.Id == u)
                    {
                        req.Redirect(".");
                        return Task.CompletedTask;
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
                                return Task.CompletedTask;
                            }
                        }
                        else
                        {
                            RemoveBrokenShare(req, u, p);
                            MissingFileOrAccess(req, e);
                            return Task.CompletedTask;
                        }
                    }
                    if (!req.UserTable.TryGetValue(u, out var user))
                    {
                        RemoveBrokenShare(req, u, p);
                        MissingFileOrAccess(req, e);
                        return Task.CompletedTask;
                    }
                    page.Navigation.Add(new Button("Back", ".", "right"));
                    e.Add(new HeadingElement("Shares", $"@{user.Username}{p}"));
                    if (canEdit)
                        e.Add(new ButtonElement("Edit mode", null, $"edit?u={u}&p={pEnc}"));
                    e.Add(new ButtonElement(canEdit ? "View mode" : "View", null, $"@{user.Username}{(file != null && segments.Last().EndsWith(".wfpg") ? string.Join('/', ((IEnumerable<string>)[..segments.SkipLast(1), segments.Last()[..^5]]).Select(HttpUtility.UrlEncode)) : string.Join('/', p.Split('/').Select(HttpUtility.UrlEncode)))}{(directory == null ? "" : "/")}", canEdit ? null : "green"));
                    if (file != null && !canEdit)
                        e.Add(new ButtonElement("Download", null, $"download?u={u}&p={pEnc}"));
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
                        page.Scripts.Add(Presets.SendRequestScript);
                        page.Scripts.Add(new Script("query.js"));
                        page.Scripts.Add(new Script("shares.js"));
                        page.AddError();
                    }
                    else e.Add(new ButtonElement(null, "You are viewing this share without logging in. That means you will lose access to it once this link has expired and the owner of this share can't allow you to edit anything.</p><p>Click here to log in.", $"{Presets.LoginPath(req)}?redirect={HttpUtility.UrlEncode($"{req.PluginPathPrefix}/shares?u={u}&p={pEnc}")}", "red"));
                }
                else
                {
                    //list shares
                    req.ForceLogin();
                    userProfile ??= GetOrCreateProfile(req);
                    page.Navigation.Add(new Button("Back", ".", "right"));
                    page.Sidebar =
                    [
                        new ButtonElement("Menu:", null, "."),
                        new ButtonElement(null, "Edit mode", $"edit?u={req.User.Id}&p="),
                        new ButtonElement(null, "View mode", $"@{req.User.Username}/"),
                        new ButtonElement(null, "Shares", "shares", "green")
                    ];
                    e.Add(new HeadingElement("Shares"));
                    if (userProfile.SavedShares.Count == 0)
                        e.Add(new ContainerElement("No items!", "", "red"));
                    else foreach (var s in userProfile.SavedShares)
                        if (s.Path == "")
                            e.Add(new ButtonElement(req.UserTable.TryGetValue(s.UserId, out var user) ? $"@{user.Username}" : $"[{s.UserId}]", null, $"shares?u={s.UserId}&p={HttpUtility.UrlEncode(s.Path)}"));
                        else e.Add(new ButtonElement(s.Path.After('/'), (req.UserTable.TryGetValue(s.UserId, out var user) ? $"@{user.Username}" : $"[{s.UserId}]") + s.Path, $"shares?u={s.UserId}&p={HttpUtility.UrlEncode(s.Path)}"));
                }
            } break;

            case "/shares/add":
            { req.ForcePOST();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                if (!(req.LoggedIn && req.User.Id != u))
                    throw new ForbiddenSignal();
                var profile = GetOrCreateProfile(req);
                if (profile.SavedShares.Any(s => s.UserId == u && s.Path == p))
                    break;
                profile.Lock();
                profile.SavedShares.Add(new(u, p));
                profile.UnlockSave();
            } break;

            case "/shares/remove":
            { req.ForcePOST();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                if (!(req.LoggedIn && req.User.Id != u))
                    throw new ForbiddenSignal();
                if (!Table.TryGetValue($"{req.UserTable.Name}_{req.User.Id}", out var profile))
                    throw new NotFoundSignal();
                profile.Lock();
                if (profile.SavedShares.RemoveAll(s => s.UserId == u && s.Path == p) > 0)
                    profile.UnlockSave();
                else profile.UnlockIgnore();
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