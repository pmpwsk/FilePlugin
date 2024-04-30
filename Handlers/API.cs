namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    protected override async Task<int> HandleNeatly(ApiRequest req, string path, string pathPrefix)
    {
        switch (path)
        {
            case "/add":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p) && req.Query.TryGetValue("n", out var n) && n != "" && req.Query.TryGetValue<bool>("d", out var d)))
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
                        directory.Files.Add(n, new(DateTime.UtcNow));
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

            default:
                return 404;
        }

        return 0;
    }
}