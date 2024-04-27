using System.Runtime.Serialization;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    [DataContract]
    private class DirectoryNode()
    {
        [DataMember] public ShareInvite? ShareInvite = null;

        [DataMember] public Dictionary<string, bool> ShareAccess = [];

        [DataMember] public SortedList<string, DirectoryNode> Directories = [];

        [DataMember] public SortedList<string, FileNode> Files = [];
    }
}