using System.Web;
using uwap.WebFramework.Accounts;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    private async Task HandleOther(Request req)
    {
        switch (req.Path)
        {
            // MAIN FILES PAGE
            case "/":
            { CreatePage(req, "Files", out var page, out var e, out var userProfile);
                req.ForceLogin();
                userProfile ??= GetOrCreateProfile(req);
                e.Add(new HeadingElement("Files", $"{FileSizeString(userProfile.SizeUsed)} / {FileSizeString(userProfile.SizeLimit)} used"));
                e.Add(new ButtonElement("Edit mode", null, $"edit?u={req.User.Id}&p="));
                e.Add(new ButtonElement("View mode", null, $"@{req.User.Username}"));
                if (userProfile.SavedShares.Count != 0)
                    e.Add(new ButtonElement("Shared with me", null, "shares"));
                if (req.IsAdmin)
                    e.Add(new ButtonElement("Manage profiles", null, "profiles"));
            } break;




            // DOWNLOAD
            case "/download":
            { req.ForceGET();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                var segments = p.Split('/');
                CheckAccess(req, u, segments, false, out var profile, out var parent, out var directory, out var file, out var name);
                if (directory != null)
                    throw new BadRequestSignal();
                if (profile == null)
                    throw new ServerErrorSignal();
                if (file == null)
                    throw new NotFoundSignal();
                await req.WriteFileAsDownload($"../FilePlugin.Profiles/{req.UserTable.Name}_{u}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}", name);
            } break;




            // VIEW MODE / 404
            default:
            {
                if (req.Path.StartsWith("/@"))
                { CreatePage(req, "Files", out var page, out var e, out var userProfile);

                    //view mode
                    string[] segments = req.Path[2..].Split('/', '\\').Select(x => HttpUtility.UrlDecode(x)).ToArray();
                    
                    User user = req.UserTable.FindByUsername(segments[0]) ?? throw new NotFoundSignal();
                    segments[0] = "";
                    bool exists = CheckAccess(req, user.Id, segments, false, out var profile, out var parent, out var directory, out var file, out var name);
                    if (directory != null)
                    {
                        //view mode > directory
                        if (directory.Files.TryGetValue("index.wfpg", out file))
                        {
                            //wfpg (index.wfpg)
                            page.Title = name;
                            Server.ParseIntoPage(req, page, File.ReadAllLines($"../FilePlugin.Profiles/{req.UserTable.Name}_{user.Id}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}/aW5kZXgud2ZwZw=="));
                            page.Title += " - Files";
                            if (profile == null || !profile.Trusted)
                                req.Context.Response.Headers.ContentSecurityPolicy = "sandbox allow-same-origin;";
                        }
                        else
                        {
                            //list directories and files
                            page.Title = name + " - Files";
                            if (parent != null)
                            {
                                string parentUrl = $"{req.PluginPathPrefix}/@{user.Username}{string.Join('/', segments.SkipLast(1).Select(HttpUtility.UrlEncode))}";
                                page.Navigation.Add(new Button("Back", parentUrl, "right"));
                                page.Sidebar =
                                [
                                    new ButtonElement(null, "Go up a level", parentUrl),
                                    ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"{parentUrl}/{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null))
                                    //don't list files
                                ];
                            }
                            else
                            {
                                page.Navigation.Add(new Button("Back", req.LoggedIn && req.User.Id == user.Id ? $"{req.PluginPathPrefix}/" : $"{req.PluginPathPrefix}/shares", "right"));
                                if (req.LoggedIn && user.Id == req.User.Id)
                                    page.Sidebar =
                                    [
                                        new ButtonElement("Menu:", null, $"{req.PluginPathPrefix}/"),
                                        new ButtonElement(null, "Edit mode", $"{req.PluginPathPrefix}/edit?u={req.User.Id}&p="),
                                        new ButtonElement(null, "View mode", $"{req.PluginPathPrefix}/@{req.User.Username}", "green"),
                                        new ButtonElement(null, "Shares", $"{req.PluginPathPrefix}/shares")
                                    ];
                            }
                            e.Add(new HeadingElement(name, "View mode"));
                            if (directory.Files.Count == 0 && directory.Directories.Count == 0)
                                e.Add(new ContainerElement("No items!", "", "red"));
                            else
                            {
                                foreach (var dKV in directory.Directories)
                                    e.Add(new ButtonElement(dKV.Key, null, $"{req.PluginPathPrefix}{req.Path}/{HttpUtility.UrlEncode(dKV.Key)}"));
                                foreach (var fKV in directory.Files)
                                    e.Add(new ButtonElement(fKV.Key, $"{FileSizeString(fKV.Value.Size)} | {fKV.Value.ModifiedUtc.ToLongDateString()}", $"{req.PluginPathPrefix}{req.Path}/{HttpUtility.UrlEncode(fKV.Key.EndsWith(".wfpg") ? fKV.Key[..^5] : fKV.Key)}"));
                            }
                        }
                    }
                    else if (file != null)
                    {
                        //view mode > file
                        req.Page = null;
                        if (profile == null || !profile.Trusted)
                            req.Context.Response.Headers.ContentSecurityPolicy = "sandbox allow-same-origin;";
                        if (segments.Last().SplitAtLast('.', out _, out var extension))
                        {
                            req.Context.Response.ContentType = Server.Config.MimeTypes.TryGetValue('.' + extension, out var contentType) ? contentType : null;
                            if (Server.Config.BrowserCacheMaxAge.TryGetValue('.' + extension, out int maxAge))
                            {
                                if (maxAge == 0)
                                    req.Context.Response.Headers.CacheControl = "no-cache, private";
                                else
                                {
                                    string timestamp = file.ModifiedUtc.Ticks.ToString();
                                    req.Context.Response.Headers.CacheControl = "public, max-age=" + maxAge;
                                    if (req.Context.Request.Headers.TryGetValue("If-None-Match", out var oldTag) && oldTag == timestamp)
                                        throw new HttpStatusSignal(304);
                                    else req.Context.Response.Headers.ETag = timestamp;
                                }
                            }
                        }
                        await req.WriteFile($"../FilePlugin.Profiles/{req.UserTable.Name}_{user.Id}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}");
                    }
                    else if (exists)
                        MissingFileOrAccess(req, e);
                    else
                    {
                        segments[^1] += ".wfpg";
                        CheckAccess(req, user.Id, segments, false, out _, out _, out _, out file, out _);
                        if (file != null)
                        {
                            //wfpg (not index.wfpg)
                            page.Title = segments[^1];
                            Server.ParseIntoPage(req, page, File.ReadAllLines($"../FilePlugin.Profiles/{req.UserTable.Name}_{user.Id}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}"));
                            page.Title += " - Files";
                            if (profile == null || !profile.Trusted)
                                req.Context.Response.Headers.ContentSecurityPolicy = "sandbox allow-same-origin;";
                        }
                        else MissingFileOrAccess(req, e);
                    }
                }
                else
                {
                    req.CreatePage("Error");
                    req.Status = 404;
                }
            } break;
        }
    }
}