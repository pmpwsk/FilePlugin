namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    public override async Task Handle(ApiRequest req, string path, string pathPrefix)
    {
        switch (path)
        {
            case "/add":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p) && req.Query.TryGetValue("n", out var n) && n != "" && req.Query.TryGetValue<bool>("d", out var d)))
                {
                    req.Status = 400;
                    break;
                }

                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (directory == null)
                    req.Status = 404;
                else if (directory.Directories.ContainsKey(n) || directory.Files.ContainsKey(n))
                    req.Status = 302;
                else if (profile == null)
                    req.Status = 500;
                else
                {
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
                }
                
            } break;
            default:
                req.Status = 404;
                break;
        }
    }
}