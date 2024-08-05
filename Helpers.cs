using System.Web;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    private void CreatePage(Request req, string title, out Page page, out List<IPageElement> e, out Profile? userProfile)
    {
        req.ForceGET();
        req.CreatePage(title, out page, out e);
        page.Head.Add($"<link rel=\"manifest\" href=\"{req.PluginPathPrefix}/manifest.json\" />");
        page.Favicon = $"{req.PluginPathPrefix}/icon.ico";
        page.Navigation =
        [
            page.Navigation.Count != 0 ? page.Navigation.First() : new Button(req.Domain, "/"),
            new Button("Files", $"{req.PluginPathPrefix}/")
        ];
        userProfile = null;
        if (req.LoggedIn)
            foreach (var cookie in req.Context.Request.Cookies)
                {
                    if (!cookie.Key.StartsWith("FilePluginShare_"))
                        continue;
                    if (!req.LoggedIn)
                        break;
                    if (cookie.Key[16..].SplitAtFirst('_', out var u, out var p))
                    {
                        p = HttpUtility.UrlDecode(p);
                        userProfile ??= GetOrCreateProfile(req);
                        if (!userProfile.SavedShares.Any(s => s.UserId == u && s.Path == p))
                        {
                            userProfile.Lock();
                            userProfile.SavedShares.Add(new(u, p));
                            userProfile.UnlockSave();
                        }
                        var node = FindNode(req, u, p.Split('/'), out var profile);
                        if (node != null && profile != null && !node.ShareAccess.ContainsKey(req.User.Id))
                        {
                            profile.Lock();
                            node.ShareAccess[req.User.Id] = false;
                            profile.UnlockSave();
                        }
                    }
                    req.Cookies.Delete(cookie.Key);
                }
    }

    private static void MissingFileOrAccess(Request req, List<IPageElement> e)
        => e.Add(new LargeContainerElement("Error", "The file/folder you're looking for either doesn't exist or you don't have access to it." + (req.LoggedIn?"":" You are not logged in, that might be the reason."), "red"));

    private void RemoveBrokenShare(Request req, string userId, string path)
    {
        if (!req.LoggedIn)
            return;
        var profile = GetOrCreateProfile(req);
        if (profile.SavedShares.Any(s => s.UserId == userId && s.Path == path))
        {
            profile.Lock();
            profile.SavedShares.RemoveAll(s => s.UserId == userId && s.Path == path);
            profile.UnlockSave();
        }
    }

    private static string FileSizeString(long size)
    {
        return size switch
        {
            0 => "0 Bytes",
            1 => "1 Byte",
            _ => (int)Math.Floor(Math.Log(size, 1024)) switch
            {
                0 => $"{size} Bytes",
                1 => $"{Number(1024)} KiB",
                2 => $"{Number(1048576)} MiB",
                3 => $"{Number(1073741824)} GiB",
                4 => $"{Number(1099511627776)} TiB",
                5 => $"{Number(1125899906842624)} PiB",
                _ => $"{Number(1152921504606846976)} EiB"
            }
        };
        
        string Number(double d)
        {
            string result = Math.Round(size / d, 2, MidpointRounding.AwayFromZero).ToString();
            if (result.SplitAtLast('.', out _, out var r))
                if (r.Length == 1)
                    return result + '0';
                else return result;
            else return result + ".00";
        }
    }

    // from https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories without parameter "recursive"
    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        // Get information about the source directory
        var dir = new DirectoryInfo(sourceDir);

        // Check if the source directory exists
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        // Cache directories before we start copying
        DirectoryInfo[] dirs = dir.GetDirectories();

        // Create the destination directory
        Directory.CreateDirectory(destinationDir);

        // Get the files in the source directory and copy to the destination directory
        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath);
        }

        // If recursive and copying subdirectories, recursively call this method
        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }
}