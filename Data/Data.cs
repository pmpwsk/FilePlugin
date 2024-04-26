using uwap.Database;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    private Table<Profile> Table = Table<Profile>.Import("FilePlugin");
}