using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    public override async Task Handle(Request req)
    {
        switch (Parsers.GetFirstSegment(req.Path, out _))
        {
            // EDIT MODE
            case "edit":
                await Edit(req);
                break;




            // EDIT MODE > EDITOR
            case "editor":
                await Editor(req);
                break;




            // EDIT MODE > MORE
            case "more":
                await More(req);
                break;




            // MANAGE PROFILES
            case "profiles":
                await Profiles(req);
                break;




            // EDIT MODE > SHARE
            case "share":
                await Share(req);
                break;




            // SHARES
            case "shares":
                await Shares(req);
                break;


            

            // MAIN PAGE / DOWNLOAD / VIEW MODE / 404
            default:
                await Other(req);
                break;
        }
    }
}