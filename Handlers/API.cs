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
                    directory.Files.Add(n, new(DateTime.UtcNow, 0));
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

            case "/rename":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p) && req.Query.TryGetValue("n", out var n) && NameOkay(n)))
                    return 400;
                var segments = p.Split('/');
                if (segments[^1] == n)
                    return 0;
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (parent == null || profile == null)
                    return 404;
                if (parent.Files.ContainsKey(n) || parent.Directories.ContainsKey(n))
                    return 302;
                if (directory != null)
                {
                    profile.Lock();
                    parent.Directories.Remove(name);
                    parent.Directories[n] = directory;
                    Directory.Move(
                        $"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}",
                        $"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', ((IEnumerable<string>)[..segments.SkipLast(1), n]).Select(Parsers.ToBase64PathSafe))}");
                    profile.UnlockSave();
                }
                else if (file != null)
                {
                    profile.Lock();
                    parent.Files.Remove(name);
                    parent.Files[n] = file;
                    File.Move(
                        $"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}",
                        $"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', ((IEnumerable<string>)[..segments.SkipLast(1), n]).Select(Parsers.ToBase64PathSafe))}");
                    profile.UnlockSave();
                }
                else return 404;
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
                if (!(req.LoggedIn && req.User.Id == u))
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
                if (!(req.LoggedIn && req.User.Id == u))
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
                node.ShareInvite = e switch
                {
                    -1 => null,
                    0 => new(Parsers.RandomString(24), DateTime.MaxValue),
                    _ => new(Parsers.RandomString(24), DateTime.UtcNow.AddDays(e)),
                };
                profile.UnlockSave();

            } break;

            case "/add-share":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    return 400;
                if (!(req.LoggedIn && req.User.Id != u))
                    return 403;
                var profile = GetOrCreateProfile(req);
                if (profile.SavedShares.Any(s => s.UserId == u && s.Path == p))
                    return 200;
                profile.Lock();
                profile.SavedShares.Add(new(u, p));
                profile.UnlockSave();
            } break;

            case "/remove-share":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    return 400;
                if (!(req.LoggedIn && req.User.Id != u))
                    return 403;
                if (!Table.TryGetValue($"{req.UserTable.Name}_{req.User.Id}", out var profile))
                    return 404;
                profile.Lock();
                if (profile.SavedShares.RemoveAll(s => s.UserId == u && s.Path == p) > 0)
                    profile.UnlockSave();
                else profile.UnlockIgnore();
            } break;

            case "/limit":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("v", out var v) && v != ""))
                    return 400;
                if (!req.IsAdmin())
                    return 403;
                if (!Table.TryGetValue($"{req.UserTable.Name}_{u}", out var profile))
                    return 404;
                char unit;
                if ("bkmgtpe".Contains(v[^1]))
                {
                    unit = v[^1];
                    v = v[..^1];
                }
                else unit = 'b';
                if (!long.TryParse(v, out var limit))
                    return 400;
                limit *= unit switch
                {
                    'b' => 1,
                    'k' => 1024,
                    'm' => 1048576,
                    'g' => 1073741824,
                    't' => 1099511627776,
                    'p' => 1125899906842624,
                    _ => 1152921504606846976
                };
                var checkLimit = limit;
                byte exp = 0;
                while (checkLimit % 1024 == 0 && "bkmgtpe"[exp] != unit)
                {
                    checkLimit /= 1024;
                    exp++;
                }
                if (checkLimit.ToString() != v || "bkmgtpe"[exp] != unit)
                    return 400;
                profile.Lock();
                profile.SizeLimit = limit;
                profile.UnlockSave();
            } break;

            case "/delete-profile":
            {
                if (!req.Query.TryGetValue("u", out var u))
                    return 400;
                if (!req.IsAdmin())
                    return 403;
                string key = $"{req.UserTable.Name}_{u}";
                if (!Table.TryGetValue(key, out var profile))
                    return 404;
                Directory.Delete("../FilePlugin/" + key, true);
                Table.Delete(key);
            } break;

            default:
                return 404;
        }

        return 0;
    }
}