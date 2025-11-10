namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    public override byte[]? GetFile(string relPath, string pathPrefix, string domain)
        => relPath switch
        {
            "/add.js" => (byte[]?)PackedFiles_ResourceManager.GetObject("File0"),
            "/copy-move.js" => (byte[]?)PackedFiles_ResourceManager.GetObject("File1"),
            "/edit.js" => (byte[]?)PackedFiles_ResourceManager.GetObject("File2"),
            "/editor.js" => (byte[]?)PackedFiles_ResourceManager.GetObject("File3"),
            "/icon.ico" => (byte[]?)PackedFiles_ResourceManager.GetObject("File4"),
            "/icon.png" => (byte[]?)PackedFiles_ResourceManager.GetObject("File5"),
            "/icon.svg" => (byte[]?)PackedFiles_ResourceManager.GetObject("File6"),
            "/list.js" => (byte[]?)PackedFiles_ResourceManager.GetObject("File7"),
            "/manifest.json" => System.Text.Encoding.UTF8.GetBytes($"{{\n    \"name\": \"Files ({Parsers.DomainMain(domain)})\",\n    \"short_name\": \"Files\",\n    \"start_url\": \"{pathPrefix}/\",\n    \"display\": \"minimal-ui\",\n    \"background_color\": \"#000000\",\n    \"theme_color\": \"#202024\",\n    \"orientation\": \"portrait-primary\",\n    \"icons\": [\n      {{\n        \"src\": \"{pathPrefix}/icon.svg\",\n        \"type\": \"image/svg+xml\",\n        \"sizes\": \"any\"\n      }},\n      {{\n        \"src\": \"{pathPrefix}/icon.png\",\n        \"type\": \"image/png\",\n        \"sizes\": \"512x512\"\n      }},\n      {{\n        \"src\": \"{pathPrefix}/icon.ico\",\n        \"type\": \"image/x-icon\",\n        \"sizes\": \"16x16 24x24 32x32 48x48 64x64 72x72 96x96 128x128 256x256\"\n      }}\n    ],\n    \"launch_handler\": {{\n      \"client_mode\": \"navigate-new\"\n    }},\n    \"related_applications\": [\n      {{\n        \"platform\": \"webapp\",\n        \"url\": \"{pathPrefix}/manifest.json\"\n      }}\n    ],\n    \"offline_enabled\": false,\n    \"omnibox\": {{\n      \"keyword\": \"files\"\n    }},\n    \"version\": \"0.1.0\"\n  }}\n  "),
            "/profiles.js" => (byte[]?)PackedFiles_ResourceManager.GetObject("File8"),
            "/query.js" => (byte[]?)PackedFiles_ResourceManager.GetObject("File9"),
            "/search.js" => (byte[]?)PackedFiles_ResourceManager.GetObject("File10"),
            "/share.js" => (byte[]?)PackedFiles_ResourceManager.GetObject("File11"),
            "/shares.js" => (byte[]?)PackedFiles_ResourceManager.GetObject("File12"),
            _ => null
        };
    
    public override string? GetFileVersion(string relPath)
        => relPath switch
        {
            "/add.js" => "638759273124903226",
            "/copy-move.js" => "638759648915490073",
            "/edit.js" => "638759566980274381",
            "/editor.js" => "638757692495386284",
            "/icon.ico" => "638502038964620984",
            "/icon.png" => "638502033677954432",
            "/icon.svg" => "638502033586954434",
            "/list.js" => "638759267583657396",
            "/manifest.json" => "638571678353306183",
            "/profiles.js" => "638759566717693259",
            "/query.js" => "638498535736727956",
            "/search.js" => "638759602611101449",
            "/share.js" => "638759566459461691",
            "/shares.js" => "638573656923778099",
            _ => null
        };
    
    private static readonly System.Resources.ResourceManager PackedFiles_ResourceManager = new("FilePlugin.Properties.PackedFiles", typeof(FilePlugin).Assembly);
}
