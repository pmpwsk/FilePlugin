using System.Runtime.Serialization;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin
{
    [DataContract]
    private abstract class Node
    {
        [DataMember] public ShareInvite? ShareInvite = null;

        [DataMember] public Dictionary<string, bool> ShareAccess = [];
    }
}