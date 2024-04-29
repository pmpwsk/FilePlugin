namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    public override async Task Handle(PostRequest req, string path, string pathPrefix)
    {
        switch (path)
        {
            case "/save":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                {
                    req.Status = 400;
                    break;
                }
                
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (directory != null)
                    req.Status = 400;
                else if (profile == null)
                    req.Status = 500;
                else if (file != null)
                {
                    profile.Lock();
                    File.WriteAllText($"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}", await req.GetBodyText());
                    file.ModifiedUtc = DateTime.UtcNow;
                    profile.UnlockSave();
                }
                else req.Status = 404;
            } break;

            default:
                req.Status = 404;
                break;
        }
    }
}