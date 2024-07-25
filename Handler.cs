namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    public override async Task Handle(Request req)
    {
        switch (Parsers.GetFirstSegment(req.Path, out _))
        {
            // EDIT MODE
            case "edit":
                await HandleEdit(req);
                break;




            // EDIT MODE > EDITOR
            case "editor":
                await HandleEditor(req);
                break;




            // EDIT MODE > MORE
            case "more":
                await HandleMore(req);
                break;




            // MANAGE PROFILES
            case "profiles":
                await HandleProfiles(req);
                break;




            // EDIT MODE > SHARE
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