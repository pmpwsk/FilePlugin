using System.Runtime.Serialization;
using uwap.Database;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    [DataContract]
    private class Profile() : ITableValue
    {
        [DataMember] List<ShareInfo> SavedShares = [];

        [DataMember] DirectoryNode RootNode = new();
    }
}