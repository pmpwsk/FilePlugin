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
                long oldSize = file.Size;
                string oldContent = File.ReadAllText(loc);
                File.WriteAllText(loc, await req.GetBodyText());
                file.Size = new FileInfo(loc).Length;
                if (profile.SizeUsed + file.Size - oldSize > profile.SizeLimit && !req.IsAdmin())
                {
                    File.WriteAllText(loc, oldContent);
                    file.Size = oldSize;
                    profile.UnlockSave();
                    return 507;
                }
                file.ModifiedUtc = DateTime.UtcNow;
                profile.SizeUsed += file.Size - oldSize;
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
                long limit = req.IsAdmin() ? long.MaxValue : UploadSizeLimit;
                req.BodySizeLimit = limit;
                if ((!req.IsForm) || req.Files.Count == 0)
                    req.Status = 400;
                profile.Lock();
                foreach (var uploadedFile in req.Files)
                {
                    if (!NameOkay(uploadedFile.FileName))
                        continue;
                    string loc = $"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', ((IEnumerable<string>)[..segments, uploadedFile.FileName]).Select(Parsers.ToBase64PathSafe))}";
                    long oldSize;
                    if (directory.Files.TryGetValue(uploadedFile.FileName, out var f))
                    {
                        f.ModifiedUtc = DateTime.UtcNow;
                        oldSize = f.Size;
                    }
                    else
                    {
                        f = new(DateTime.UtcNow, 0);
                        directory.Files[uploadedFile.FileName] = f;
                        oldSize = 0;
                    }
                    try
                    {
                        if (!uploadedFile.Download(loc, limit))
                        {
                            profile.SizeUsed -= oldSize;
                            directory.Files.Remove(uploadedFile.FileName);
                            profile.UnlockSave();
                            return 413;
                        }
                        f.Size = new FileInfo(loc).Length;
                        if (profile.SizeUsed + f.Size - oldSize > profile.SizeLimit && !req.IsAdmin())
                        {
                            profile.SizeUsed -= oldSize;
                            directory.Files.Remove(uploadedFile.FileName);
                            profile.UnlockSave();
                            return 507;
                        }
                        profile.SizeUsed += f.Size - oldSize;
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