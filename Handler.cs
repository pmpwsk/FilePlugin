namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    public override async Task Handle(Request req)
    {
        switch (Parsers.GetFirstSegment(req.Path, out _))
        {
            // LIST FILES
            case "list":
                await HandleList(req);
                break;
            
            // ADD FILES/DIRECTORIES
            case "add":
                await HandleAdd(req);
                break;
            
            // COPY/MOVE FILE/DIRECTORY
            case "copy":
            case "move":
                await HandleCopyOrMove(req, req.Path[1] == 'c');
                break;

            // EDIT FILE/DIRECTORY
            case "edit":
                await HandleEdit(req);
                break;

            // EDITOR
            case "editor":
                await HandleEditor(req);
                break;

            // MANAGE PROFILES
            case "profiles":
                await HandleProfiles(req);
                break;

            // SHARE
            case "share":
                await HandleShare(req);
                break;

            // SHARES
            case "shares":
                await HandleShares(req);
                break;

            // MAIN PAGE / DOWNLOAD / VIEW MODE / 404
            default:
                await HandleOther(req);
                break;
        }
    }
}