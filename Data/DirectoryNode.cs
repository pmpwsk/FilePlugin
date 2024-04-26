using System.Runtime.Serialization;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    [DataContract]
    private class DirectoryNode()
    {
        [DataMember] public ShareInvite? Share = null;

        [DataMember] public Dictionary<string, DirectoryNode> Directories = [];

        [DataMember] public Dictionary<string, FileNode> Files = [];
    }
}