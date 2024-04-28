using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    public override Task Handle(AppRequest req, string path, string pathPrefix)
    {
        Presets.CreatePage(req, "Files", out var page, out var e);
        Presets.Navigation(req, page);

        string pluginHome = pathPrefix == "" ? "/" : pathPrefix;
        page.Navigation =
        [
            page.Navigation.Count != 0 ? page.Navigation.First() : new Button(req.Domain, "/"),
            new Button("Files", pluginHome)
        ];

        switch (path)
        {
            case "":
            {
                if (!req.LoggedIn)
                {
                    req.RedirectToLogin();
                    break;
                }

                var profile = GetOrCreateProfile(req);
                e.Add(new HeadingElement("Files"));
                e.Add(new ButtonElement("Edit mode", null, $"{pathPrefix}/edit?u={req.User.Id}&p="));
                e.Add(new ButtonElement("View mode", null, $"{pathPrefix}/@{req.User.Username}"));
                if (profile.SavedShares.Count == 0)
                    e.Add(new ButtonElement("Shared with me", null, pathPrefix + "/shares"));
            } break;
            default:
                req.Status = 404;
                break;
        }

        return Task.CompletedTask;
    }
}