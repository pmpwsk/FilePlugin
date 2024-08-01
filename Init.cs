namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    public FilePlugin()
    {
        Directory.CreateDirectory("../FilePlugin.Profiles");
    }
}