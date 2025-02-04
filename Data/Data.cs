using System.Collections.Concurrent;
using System.Web;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    public long DefaultProfileSizeLimit {get;set;} = 4294967296;
    
    public long UploadSizeLimit {get;set;} = 26214400;

    private ConcurrentDictionary<string, HashSet<Request>> ChangeListeners = [];

    private readonly ProfileTable Table = ProfileTable.Import("FilePlugin.Profiles");

    private Profile GetOrCreateProfile(Request req)
    {
        string key = $"{req.UserTable.Name}_{req.User.Id}";
        if (Table.TryGetValue(key, out var profile))
            return profile;

        Directory.CreateDirectory("../FilePlugin.Profiles/" + key);
        profile = new(DefaultProfileSizeLimit);
        Table[key] = profile;
        return profile;
    }

    private Task RemoveChangeListener(Request req)
    {
        if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
            return Task.CompletedTask;
        
        string key = $"{u}/{p}";
        if (ChangeListeners.TryGetValue(key, out var set) && set.Remove(req) && set.Count == 0)
            ChangeListeners.Remove(key, out _);
        
        return Task.CompletedTask;
    }

    private async Task NotifyChangeListeners(string userId, IEnumerable<string> pSegments)
    {
        string key = string.Join('/', [userId, ..pSegments]);
        if (ChangeListeners.TryGetValue(key, out var set))
            foreach (var r in set)
                await r.EventMessage("changed");
    }

    private Node? FindNode(Request req, string userId, string[] segments, out Profile? profile)
    {
        if (!Table.TryGetValue($"{req.UserTable.Name}_{userId}", out profile))
            return null;
        DirectoryNode? current = profile.RootNode;
        if (segments.Length == 1)
            return current;
        foreach (string segment in segments.Skip(1).SkipLast(1))
            if (!current.Directories.TryGetValue(segment, out current))
                return null;
        if (current.Directories.TryGetValue(segments.Last(), out var d))
            return d;
        else if (current.Files.TryGetValue(segments.Last(), out var f))
            return f;
        else return null;
    }

    private bool CheckAccess(Request req, string userId, string[] segments, bool wantsEdit, out Profile? profile, out DirectoryNode? parent, out DirectoryNode? directory, out FileNode? file, out string name)
    {
        string pEnc = "";
        string? accessKey = req.LoggedIn ? req.User.Id : null;
        bool hasAccess = accessKey == userId;
        parent = null; directory = null; file = null; name = "?";
        if (segments[0] != "")
        {
            profile = null;
            return false;
        }
        if (!Table.TryGetValue($"{req.UserTable.Name}_{userId}", out profile))
            return false;
        DirectoryNode? current = profile.RootNode;
        CheckAccess(current, null);
        if (segments.Length == 1)
        {
            if (hasAccess)
            {
                directory = current;
                if (req.UserTable.TryGetValue(userId, out var user))
                    name = '@' + user.Username;
            }
            return true;
        }
        foreach (string segment in segments.Skip(1).SkipLast(1))
            if (current.Directories.TryGetValue(segment, out current))
                CheckAccess(current, segment);
            else return false;
        name = segments.Last();
        if (current.Directories.TryGetValue(name, out var d))
        {
            if (hasAccess)
            {
                if (current != d)
                    parent = current;
                directory = d;
            }
            else
            {
                CheckAccess(d, name);
                if (hasAccess)
                    directory = d;
            }
            return true;
        }
        else if (current.Files.TryGetValue(name, out var f))
        {
            if (hasAccess)
            {
                parent = current;
                file = f;
            }
            else
            {
                CheckAccess(f, name);
                if (hasAccess)
                    file = f;
            }
            return true;
        }
        else return false;

        void CheckAccess(Node n, string? s)
        {
            if (s != null)
                pEnc += "%2f" + HttpUtility.UrlEncode(s);

            if (hasAccess)
                return;
            var access = n.ShareAccess;
            if ((accessKey != null && CheckAccess(accessKey)) || CheckAccess("*"))
                hasAccess = true;
            
            if ((!wantsEdit) && n.ShareInvite != null && n.ShareInvite.Expiration >= DateTime.UtcNow
                && (req.Cookies.TryGet($"FilePluginShare_{userId}_{pEnc}") == n.ShareInvite.Code || req.Query.TryGet("c") == n.ShareInvite.Code))
            {
                req.Cookies.Add($"FilePluginShare_{userId}_{pEnc}", n.ShareInvite.Code, new() { Expires = Min(n.ShareInvite.Expiration, DateTime.UtcNow.AddDays(14)) });
                hasAccess = true;
            }

            bool CheckAccess(string k)
                => access.TryGetValue(k, out var canEdit) && (canEdit || !wantsEdit);
        }
    }

    private static DateTime Min(DateTime a, DateTime b)
        => a < b ? a : b;

    private static bool NameOkay(string name)
        => !(name == "" || name == "." || name == ".." || name.Contains('/') || name.Contains('\\'));
}