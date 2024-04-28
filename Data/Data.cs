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
}