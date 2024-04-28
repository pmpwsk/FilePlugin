namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
	public override byte[]? GetFile(string relPath, string pathPrefix, string domain)
		=> relPath switch
		{
			"/edit-d.js" => System.Text.Encoding.UTF8.GetBytes($"async function AddNode(d) {{\n    try {{\n        var n = encodeURIComponent(document.querySelector(\"#name\").value);\n        if (n === \"\")\n        {{\n            ShowError(\"Enter a name.\");\n            return;\n        }}\n        var u = GetQuery(\"u\");\n        var p = encodeURIComponent(GetQuery(\"p\"));\n        switch ((await fetch(`/api{pathPrefix}/add?u=${{u}}&p=${{p}}&n=${{n}}&d=${{d}}`)).status) {{\n            case 200:\n                window.location.assign(`{pathPrefix}/edit?u=${{u}}&p=${{p}}%2f${{n}}`)\n                break;\n            case 302:\n                ShowError(\"Another file or directory with this name already exists.\");\n                break;\n            default:\n                ShowError(\"Connection failed.\");\n                break;\n        }}\n    }} catch {{\n        ShowError(\"Connection failed.\");\n    }}\n}}"),
			"/query.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File0"),
			_ => null
		};
	
	public override string? GetFileVersion(string relPath)
		=> relPath switch
		{
			"/edit-d.js" => "1714262881323",
			"/query.js" => "1714256773673",
			_ => null
		};
	
	private static readonly System.Resources.ResourceManager PluginFiles_ResourceManager = new("FilePlugin.Properties.PluginFiles", typeof(FilePlugin).Assembly);
}