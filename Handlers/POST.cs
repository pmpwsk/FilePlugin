namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    protected override async Task<int> HandleNeatly(PostRequest req, string path, string pathPrefix)
    {
        switch (path)
        {
            case "/save":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    return 400;                
                if (req.IsForm)
                    return 400;
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (directory != null)
                    return 400;
                if (profile == null)
                    return 500;
                if (file == null)
                    return 404;
                    profile.Lock();
                    File.WriteAllText($"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}", await req.GetBodyText());
                    file.ModifiedUtc = DateTime.UtcNow;
                    profile.UnlockSave();
            } break;

            default:
                return 404;
        }

        return 0;
    }
}