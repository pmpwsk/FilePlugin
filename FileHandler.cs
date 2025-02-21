namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
	public override byte[]? GetFile(string relPath, string pathPrefix, string domain)
		=> relPath switch
		{
			"/copy-move.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File0"),
			"/edit-d.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File1"),
			"/editor.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File2"),
			"/icon.ico" => (byte[]?)PluginFiles_ResourceManager.GetObject("File3"),
			"/icon.png" => (byte[]?)PluginFiles_ResourceManager.GetObject("File4"),
			"/icon.svg" => (byte[]?)PluginFiles_ResourceManager.GetObject("File5"),
			"/manifest.json" => System.Text.Encoding.UTF8.GetBytes($"{{\n    \"name\": \"Files ({Parsers.DomainMain(domain)})\",\n    \"short_name\": \"Files\",\n    \"start_url\": \"{pathPrefix}/\",\n    \"display\": \"minimal-ui\",\n    \"background_color\": \"#000000\",\n    \"theme_color\": \"#202024\",\n    \"orientation\": \"portrait-primary\",\n    \"icons\": [\n      {{\n        \"src\": \"{pathPrefix}/icon.svg\",\n        \"type\": \"image/svg+xml\",\n        \"sizes\": \"any\"\n      }},\n      {{\n        \"src\": \"{pathPrefix}/icon.png\",\n        \"type\": \"image/png\",\n        \"sizes\": \"512x512\"\n      }},\n      {{\n        \"src\": \"{pathPrefix}/icon.ico\",\n        \"type\": \"image/x-icon\",\n        \"sizes\": \"16x16 24x24 32x32 48x48 64x64 72x72 96x96 128x128 256x256\"\n      }}\n    ],\n    \"launch_handler\": {{\n      \"client_mode\": \"navigate-new\"\n    }},\n    \"related_applications\": [\n      {{\n        \"platform\": \"webapp\",\n        \"url\": \"{pathPrefix}/manifest.json\"\n      }}\n    ],\n    \"offline_enabled\": false,\n    \"omnibox\": {{\n      \"keyword\": \"files\"\n    }},\n    \"version\": \"0.1.0\"\n  }}\n  "),
			"/more.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File6"),
			"/profiles.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File7"),
			"/query.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File8"),
			"/share.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File9"),
			"/shares.js" => (byte[]?)PluginFiles_ResourceManager.GetObject("File10"),
			_ => null
		};
	
	public override string? GetFileVersion(string relPath)
		=> relPath switch
		{
			"/copy-move.js" => "1722874810335",
			"/edit-d.js" => "1732054999739",
			"/editor.js" => "1740172449539",
			"/icon.ico" => "1714607096462",
			"/icon.png" => "1714606567795",
			"/icon.svg" => "1714606558695",
			"/manifest.json" => "1721571035331",
			"/more.js" => "1721769336453",
			"/profiles.js" => "1722721885269",
			"/query.js" => "1714256773673",
			"/share.js" => "1740172543138",
			"/shares.js" => "1721768892378",
			_ => null
		};
	
	private static readonly System.Resources.ResourceManager PluginFiles_ResourceManager = new("FilePlugin.Properties.PluginFiles", typeof(FilePlugin).Assembly);
}