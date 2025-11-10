using System.Runtime.Serialization;
using uwap.Database;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin
{
    [DataContract]
    private class Profile(long sizeLimit) : ILegacyTableValue
    {
        [DataMember] public List<ShareInfo> SavedShares = [];

        [DataMember] public DirectoryNode RootNode = new();

        [DataMember] public long SizeUsed = 0;

        [DataMember] public long SizeLimit = sizeLimit;

        [DataMember] public bool Trusted = false;
    }
}