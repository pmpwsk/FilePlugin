using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    private Task HandleProfiles(Request req)
    {
        switch (req.Path)
        {
            case "/profiles":
            { CreatePage(req, "Profiles - Files", out var page, out var e, out var userProfile);
                req.ForceAdmin();
                if (req.Query.TryGetValue("u", out var u))
                {
                    //manage given profile
                    if (!Table.TryGetValue($"{req.UserTable.Name}_{u}", out var profile))
                        throw new NotFoundSignal();
                    page.Scripts.Add(Presets.SendRequestScript);
                    page.Scripts.Add(new Script("profiles.js"));
                    page.Scripts.Add(new Script("query.js"));
                    string userTablePrefix = req.UserTable.Name + '_';
                    page.Sidebar.Add(new ButtonElement("Profiles:", null, "profiles"));
                    List<ButtonElement> sidebarItems = [];
                    foreach (var kv in Table)
                        if (kv.Key.StartsWith(userTablePrefix))
                        {
                            string userId = kv.Key[userTablePrefix.Length..];
                            sidebarItems.Add(new ButtonElement(null, req.UserTable.TryGetValue(userId, out var otherUser) ? otherUser.Username : $"[{userId}]", $"profiles?u={userId}", userId == u ? "green" : null));
                        }
                    page.Sidebar.AddRange(sidebarItems.OrderBy(x => x.Text));
                    var user = req.UserTable.TryGet(u);
                    e.Add(new HeadingElement("Manage profiles", $"{(user != null ? user.Username : $"[{u}]")} ({FileSizeString(profile.SizeUsed)} / {FileSizeString(profile.SizeLimit)})"));
                    page.AddError();
                    var limit = profile.SizeLimit;
                    byte exp = 0;
                    while (limit % 1024 == 0 && exp++ < 6)
                        limit /= 1024;
                    e.Add(new ContainerElement("Size limit", new TextBox("Enter a limit...", $"{limit}{exp switch {0=>"", 1=>"k", 2=>"m", 3=>"g", 4=>"t", 5=>"p", _=>"e"}}", "limit", onEnter: "SaveLimit()", onInput: "ChangedLimit()")) {Button = new ButtonJS("Saved!", "SaveLimit()", id: "save-limit")});
                    e.Add(new ButtonElementJS("Delete", null, "Delete()", "red", id: "delete"));
                }
                else
                {
                    //list profiles
                    e.Add(new HeadingElement("Manage profiles"));
                    string userTablePrefix = req.UserTable.Name + '_';
                    List<ButtonElement> items = [];
                    foreach (var kv in Table)
                        if (kv.Key.StartsWith(userTablePrefix))
                        {
                            string userId = kv.Key[userTablePrefix.Length..];
                            items.Add(new ButtonElement(req.UserTable.TryGetValue(userId, out var user) ? user.Username : $"[{userId}]", $"{FileSizeString(kv.Value.SizeUsed)} / {FileSizeString(kv.Value.SizeLimit)}", $"profiles?u={userId}"));
                        }
                    if (items.Count != 0)
                        e.AddRange(items.OrderBy(x => x.Title));
                    else e.Add(new ContainerElement("No items!", "", "red"));
                }
            } break;

            case "/profiles/limit":
            { req.ForcePOST();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("v", out var v) && v != ""))
                    throw new BadRequestSignal();
                if (!req.IsAdmin)
                    throw new ForbiddenSignal();
                if (!Table.TryGetValue($"{req.UserTable.Name}_{u}", out var profile))
                    throw new NotFoundSignal();
                char unit;
                if ("bkmgtpe".Contains(v[^1]))
                {
                    unit = v[^1];
                    v = v[..^1];
                }
                else unit = 'b';
                if (!long.TryParse(v, out var limit))
                    throw new BadRequestSignal();
                limit *= unit switch
                {
                    'b' => 1,
                    'k' => 1024,
                    'm' => 1048576,
                    'g' => 1073741824,
                    't' => 1099511627776,
                    'p' => 1125899906842624,
                    _ => 1152921504606846976
                };
                var checkLimit = limit;
                byte exp = 0;
                while (checkLimit % 1024 == 0 && "bkmgtpe"[exp] != unit)
                {
                    checkLimit /= 1024;
                    exp++;
                }
                if (checkLimit.ToString() != v || "bkmgtpe"[exp] != unit)
                    throw new BadRequestSignal();
                profile.Lock();
                profile.SizeLimit = limit;
                profile.UnlockSave();
            } break;

            case "/profiles/delete":
            { req.ForcePOST();
                if (!req.Query.TryGetValue("u", out var u))
                    throw new BadRequestSignal();
                if (!req.IsAdmin)
                    throw new ForbiddenSignal();
                string key = $"{req.UserTable.Name}_{u}";
                if (!Table.TryGetValue(key, out var profile))
                    throw new NotFoundSignal();
                Directory.Delete("../FilePlugin.Profiles/" + key, true);
                Table.Delete(key);
            } break;




            // 404
            default:
                req.CreatePage("Error");
                req.Status = 404;
                break;
        }

        return Task.CompletedTask;
    }
}