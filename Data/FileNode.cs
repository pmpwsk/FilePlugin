using System.Runtime.Serialization;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    [DataContract]
    private class FileNode(DateTime modifiedUtc)
    {
        [DataMember] public ShareInvite? ShareInvite = null;

        [DataMember] public Dictionary<string, bool> ShareAccess = [];
        
        [DataMember] public DateTime ModifiedUtc = modifiedUtc;
    }
}