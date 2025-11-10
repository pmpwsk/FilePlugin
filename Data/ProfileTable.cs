using uwap.Database;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin
{
    private class ProfileTable : LegacyTable<Profile>
    {
        private ProfileTable(string name) : base(name) { }

        protected static new ProfileTable Create(string name)
        {
            if (!name.All(LegacyTables.KeyChars.Contains))
                throw new Exception($"This name contains characters that are not part of Tables.KeyChars ({LegacyTables.KeyChars}).");
            if (Directory.Exists("../Database/" + name))
                throw new Exception("A table with this name already exists, try importing it instead.");

            Directory.CreateDirectory("../Database/" + name);
            ProfileTable table = new(name);
            LegacyTables.Dictionary[name] = table;
            return table;
        }

        public static new ProfileTable Import(string name, bool skipBroken = false)
        {
            if (LegacyTables.Dictionary.TryGetValue(name, out ILegacyTable? table))
                return (ProfileTable)table;
            if (!name.All(LegacyTables.KeyChars.Contains))
                throw new Exception($"This name contains characters that are not part of Tables.KeyChars ({LegacyTables.KeyChars}).");
            if (!Directory.Exists("../Database/" + name))
                return Create(name);

            if (Directory.Exists("../Database/Buffer/" + name) && Directory.GetFiles("../Database/Buffer/" + name, "*.json", SearchOption.AllDirectories).Length > 0)
                Console.WriteLine($"The database buffer of table '{name}' contains an entry because a database operation was interrupted. Please manually merge the files and delete the file from the buffer.");

            ProfileTable result = new(name);
            result.Reload(skipBroken);
            LegacyTables.Dictionary[name] = result;
            return result;
        }

        protected override IEnumerable<string> EnumerateDirectoriesToClear()
        {
            yield return "../FilePlugin.Profiles";
        }

        protected override IEnumerable<string> EnumerateOtherDirectories(LegacyTableEntry<Profile> entry)
        {
            yield return $"../FilePlugin.Profiles/{entry.Key}";
        }
    }
}