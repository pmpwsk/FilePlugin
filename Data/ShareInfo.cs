using System.Runtime.Serialization;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin
{
    [DataContract]
    private class ShareInfo(string userId, string path)
    {
        [DataMember] public string UserId = userId;

        [DataMember] public string Path = path;
    }
}