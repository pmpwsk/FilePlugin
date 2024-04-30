using System.Runtime.Serialization;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    [DataContract]
    private class FileNode(DateTime modifiedUtc) : Node
    {        
        [DataMember] public DateTime ModifiedUtc = modifiedUtc;
    }
}