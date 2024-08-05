using System.Runtime.Serialization;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    [DataContract]
    private class DirectoryNode() : Node
    {
        [DataMember] public SortedList<string, DirectoryNode> Directories = [];

        [DataMember] public SortedList<string, FileNode> Files = [];

        public DirectoryNode Copy()
        {
            var result = new DirectoryNode();
            foreach (var kv in Directories)
                result.Directories[kv.Key] = kv.Value.Copy();
            foreach (var kv in Files)
                result.Files[kv.Key] = kv.Value.Copy();
            return result;
        }
    }
}