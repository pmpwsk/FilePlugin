namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    protected override async Task<int> HandleNeatly(ApiRequest req, string path, string pathPrefix)
    {
        switch (path)
        {
            case "/add":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p) && req.Query.TryGetValue("n", out var n) && NameOkay(n) && req.Query.TryGetValue("d", out bool d)))
                    return 400;
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (directory == null)
                    return 404;
                if (directory.Directories.ContainsKey(n) || directory.Files.ContainsKey(n))
                    return 302;
                if (profile == null)
                    return 500;
                profile.Lock();
                string target = $"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', ((IEnumerable<string>)[..segments, n]).Select(Parsers.ToBase64PathSafe))}";
                if (d)
                {
                    Directory.CreateDirectory(target);
                    directory.Directories.Add(n, new());
                }
                else
                {
                    File.WriteAllText(target, "");
                    directory.Files.Add(n, new(DateTime.UtcNow, new FileInfo(target).Length));
                }
                profile.UnlockSave();
            } break;

            case "/delete":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    return 400;
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (parent == null || profile == null)
                    return 404;
                profile.Lock();
                string loc = $"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}";
                if (directory != null)
                {
                    profile.SizeUsed -= new DirectoryInfo(loc).GetFiles("*", SearchOption.AllDirectories).Sum(x => x.Length);
                    parent.Directories.Remove(name);
                    Directory.Delete(loc, true);
                }
                else if (file != null)
                {
                    profile.SizeUsed -= new FileInfo(loc).Length;
                    parent.Files.Remove(name);
                    File.Delete(loc);
                }
                profile.UnlockSave();
            } break;

            case "/load":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    return 400;
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (directory != null)
                    return 400;
                if (file == null)
                    return 404;
                var content = File.ReadAllText($"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}");
                if (content == "")
                    return 201;
                await req.Write(content);
            } break;

            case "/share":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    return 400;
                if (req.LoggedIn && u != req.User.Id)
                    return 403;
                if (!req.Query.TryGetValue("uid", out var uid))
                {
                    if (!req.Query.TryGetValue("un", out var un))
                        return 400;
                    if (un == "*")
                        uid = "*";
                    else
                    {
                        var user = req.UserTable.FindByUsername(un);
                        if (user == null)
                            return 404;
                        uid = user.Id;
                    }
                }
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out _, out var directory, out var file, out _);
                if (profile == null)
                    return 404;
                Dictionary<string,bool> shareAccess;
                if (directory != null)
                    shareAccess = directory.ShareAccess;
                else if (file != null)
                    shareAccess = file.ShareAccess;
                else return 404;
                profile.Lock();
                if (req.Query.TryGetValue("e", out bool canEdit))
                    shareAccess[uid] = canEdit;
                else shareAccess.Remove(uid);
                profile.UnlockSave();
            } break;

            case "/invite":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    return 400;
                if (req.LoggedIn && u != req.User.Id)
                    return 403;
                int e;
                if (req.Query.TryGetValue("e", out var eString))
                {
                    if (!(int.TryParse(eString, out e) && e >= 0))
                        return 400;
                }
                else e = -1;
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out _, out var directory, out var file, out _);
                if (profile == null)
                    return 404;
                Node node;
                if (directory != null)
                    node = directory;
                else if (file != null)
                    node = file;
                else return 404;
                profile.Lock();
                switch (e)
                {
                    case -1:
                        node.ShareInvite = null;
                        break;
                    case 0:
                        node.ShareInvite = new(Parsers.RandomString(24), DateTime.MaxValue);
                        break;
                    default:
                        node.ShareInvite = new(Parsers.RandomString(24), DateTime.UtcNow.AddDays(e));
                        break;
                }
                profile.UnlockSave();

            } break;

            default:
                return 404;
        }

        return 0;
    }
}