using System.Runtime.Serialization;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    [DataContract]
    private class ShareInvite(string code, DateTime expiration)
    {
        [DataMember] public string Code = code;

        [DataMember] public DateTime Expiration = expiration;
    }
}