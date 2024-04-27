using System.Runtime.Serialization;
using uwap.Database;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    [DataContract]
    private class Profile() : ITableValue
    {
        [DataMember] public List<ShareInfo> SavedShares = [];

        [DataMember] public DirectoryNode RootNode = new();
    }
}