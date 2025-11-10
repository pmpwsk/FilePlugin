using System.Runtime.Serialization;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin
{
    [DataContract]
    private class FileNode(DateTime modifiedUtc, long size) : Node
    {        
        [DataMember] public DateTime ModifiedUtc = modifiedUtc;

        [DataMember] public long Size = size;

        public FileNode Copy()
            => new(ModifiedUtc, Size);
    }
}