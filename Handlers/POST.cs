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
                string loc = $"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}";
                long oldSize = new FileInfo(loc).Length;
                File.WriteAllText(loc, await req.GetBodyText());
                file.ModifiedUtc = DateTime.UtcNow;
                profile.SizeUsed += new FileInfo(loc).Length - oldSize;
                profile.UnlockSave();
            } break;

            case "/upload":
            {
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    return 400;                
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (directory == null)
                    return 404;
                if (profile == null)
                    return 500;
                long limit = req.IsAdmin() ? long.MaxValue : 25000000;
                req.BodySizeLimit = limit;
                if ((!req.IsForm) || req.Files.Count == 0)
                    req.Status = 400;
                profile.Lock();
                foreach (var uploadedFile in req.Files)
                {
                    string loc = $"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', ((IEnumerable<string>)[..segments, uploadedFile.FileName]).Select(Parsers.ToBase64PathSafe))}";
                    long oldSize;
                    if (directory.Files.TryGetValue(uploadedFile.FileName, out var f))
                    {
                        f.ModifiedUtc = DateTime.UtcNow;
                        oldSize = new FileInfo(loc).Length;
                    }
                    else
                    {
                        directory.Files[uploadedFile.FileName] = new(DateTime.UtcNow);
                        oldSize = 0;
                    }
                    try
                    {
                        uploadedFile.Download(loc, limit);
                        profile.SizeUsed += new FileInfo(loc).Length - oldSize;
                    }
                    catch
                    {
                        try { File.Delete(loc); } catch { }
                        profile.SizeUsed -= oldSize;
                        directory.Files.Remove(uploadedFile.FileName);
                        profile.UnlockSave();
                        throw;
                    }
                }
                profile.UnlockSave();
            } break;

            default:
                return 404;
        }

        return 0;
    }
}