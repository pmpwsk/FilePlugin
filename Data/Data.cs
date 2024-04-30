using uwap.Database;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    private readonly Table<Profile> Table = Table<Profile>.Import("FilePlugin");

    private Profile GetOrCreateProfile(IRequest req)
    {
        string key = $"{req.UserTable.Name}_{req.User.Id}";
        if (Table.TryGetValue(key, out var profile))
            return profile;

        Directory.CreateDirectory("../FilePlugin/" + key);
        profile = new();
        Table[key] = profile;
        return profile;
    }

    private Node? FindNode(IRequest req, string userId, string[] segments, out Profile? profile)
    {
        if (!Table.TryGetValue($"{req.UserTable.Name}_{userId}", out profile))
            return null;
        DirectoryNode? current = profile.RootNode;
        if (segments.Length == 1)
        {
            return current;
        }
        foreach (string segment in segments.Skip(1).SkipLast(1))
            if (!current.Directories.TryGetValue(segment, out current))
                return null;
        if (current.Directories.TryGetValue(segments.Last(), out var d))
            return d;
        else if (current.Files.TryGetValue(segments.Last(), out var f))
            return f;
        else return null;
    }

    private bool CheckAccess(IRequest req, string userId, string[] segments, bool wantsEdit, out Profile? profile, out DirectoryNode? parent, out DirectoryNode? directory, out FileNode? file, out string name)
    {
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
        CheckAccess(current);
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
                CheckAccess(current);
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
                CheckAccess(d);
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
                CheckAccess(f);
                if (hasAccess)
                    file = f;
            }
            return true;
        }
        else return false;

        void CheckAccess(Node n)
        {
            if (hasAccess)
                return;
            var access = n.ShareAccess;
            if ((accessKey != null && CheckAccess(accessKey)) || CheckAccess("*"))
                hasAccess = true;

            bool CheckAccess(string k)
                => access.TryGetValue(k, out var canEdit) && (canEdit || !wantsEdit);
        }
    }
}