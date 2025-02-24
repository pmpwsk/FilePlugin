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
            // REDIRECT FROM HOME
            case "/":
            { CreatePage(req, "Files", out var page, out var e, out _);
                req.ForceLogin();
                req.Redirect($"list?u={req.User.Id}&p=");
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
            
            
            
            
            // FILE CHANGED EVENT
            case "/changed-event":
            { req.ForceGET();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                var segments = p.Split('/');
                CheckAccess(req, u, segments, false, out _, out _, out var directory, out var file, out _);
                if (directory == null && file == null)
                    throw new NotFoundSignal();
                string key = string.Join('/', [u, ..segments]);
                if (ChangeListeners.TryGetValue(key, out var set))
                    set.Add(req);
                else ChangeListeners[key] = [req];
                req.KeepEventAliveCancelled += RemoveChangeListener;
                await req.KeepEventAlive();
            } break;




            // VIEW MODE / 404
            default:
            {
                if (req.Path.StartsWith("/@"))
                { CreatePage(req, "Files", out var page, out var e, out var userProfile);

                    //view mode
                    string pathWithoutSlashAt = req.Path[2..];
                    bool directoryRequested;
                    if (directoryRequested = pathWithoutSlashAt.EndsWith('/'))
                        pathWithoutSlashAt = pathWithoutSlashAt[..^1];
                    string[] segments = pathWithoutSlashAt.Split('/', '\\');
                    
                    User user = req.UserTable.FindByUsername(segments[0]) ?? throw new NotFoundSignal();
                    segments[0] = "";
                    bool exists = CheckAccess(req, user.Id, segments, false, out var profile, out var parent, out var directory, out var file, out var name);
                    if (directory != null)
                    {
                        if (!directoryRequested)
                            throw new RedirectSignal($"{name}/");
                        //view mode > directory
                        if (directory.Files.TryGetValue("index.wfpg", out file))
                        {
                            //wfpg (index.wfpg)
                            page.Title = name;
                            
                            CheckAccess(req, user.Id, [..segments, "index.wfpg"], true, out _, out _, out _, out var editFile, out _);
                            if (editFile != null)
                                page.Navigation.Add(new Button("Edit", $"{req.PluginPathPrefix}/editor?u={user.Id}&p={HttpUtility.UrlEncode(string.Join('/', segments))}" + "/index.wfpg", "right"));
                            
                            Server.ParseIntoPage(req, page, File.ReadAllLines($"../FilePlugin.Profiles/{req.UserTable.Name}_{user.Id}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}/aW5kZXgud2ZwZw=="));
                            page.Title += " - Files";
                            if (profile == null || !profile.Trusted)
                                req.Context.Response.Headers.ContentSecurityPolicy = "sandbox allow-same-origin allow-popups allow-popups-to-escape-sandbox;";
                        }
                        else if (directory.Files.TryGetValue("index.html", out file))
                        {
                            //file (index.html)
                            req.Page = null;
                            if (profile == null || !profile.Trusted)
                                req.Context.Response.Headers.ContentSecurityPolicy = "sandbox allow-same-origin allow-popups allow-popups-to-escape-sandbox;";
                            req.Context.Response.ContentType = Server.Config.MimeTypes.GetValueOrDefault(".html");
                            if (Server.Config.BrowserCacheMaxAge.TryGetValue(".html", out int maxAge))
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
                            await req.WriteFile($"../FilePlugin.Profiles/{req.UserTable.Name}_{user.Id}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}/aW5kZXguaHRtbA==");
                        }
                        else
                        {
                            //list directories and files
                            page.Title = name + " - Files";
                            if (parent != null)
                            {
                                page.Navigation.Add(new Button("Back", "..", "right"));
                                page.Sidebar =
                                [
                                    new ButtonElement(null, "Go up a level", ".."),
                                    ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"../{HttpUtility.UrlPathEncode(dKV.Key)}/", dKV.Key == name ? "green" : null))
                                    //don't list files
                                ];
                            }
                            else page.Navigation.Add(new Button("Back", req.LoggedIn && req.User.Id == user.Id ? $"{req.PluginPathPrefix}/" : $"{req.PluginPathPrefix}/shares", "right"));
                            
                            CheckAccess(req, user.Id, segments, true, out _, out _, out var editDir, out _, out _);
                            if (editDir != null)
                                page.Navigation.Add(new Button("Edit", $"{req.PluginPathPrefix}/list?u={user.Id}&p={HttpUtility.UrlEncode(string.Join('/', segments))}", "right"));
                            
                            e.Add(new HeadingElement(name, "View mode"));
                            if (directory.Files.Count == 0 && directory.Directories.Count == 0)
                                e.Add(new ContainerElement("No items!", "", "red"));
                            else
                            {
                                foreach (var dKV in directory.Directories)
                                    e.Add(new ButtonElement(dKV.Key, null, $"{HttpUtility.UrlPathEncode(dKV.Key)}/"));
                                foreach (var fKV in directory.Files)
                                    e.Add(new ButtonElement(fKV.Key, $"{FileSizeString(fKV.Value.Size)} | {fKV.Value.ModifiedUtc.ToLongDateString()}", HttpUtility.UrlPathEncode(fKV.Key.EndsWith(".wfpg") ? fKV.Key[..^5] : fKV.Key)));
                            }
                        }
                    }
                    else if (file != null)
                    {
                        if (directoryRequested)
                            throw new RedirectSignal($"../{segments.Last()}");
                        //view mode > file
                        req.Page = null;
                        if (segments.Last() == "index.html")
                            throw new RedirectSignal(".");
                        if (profile == null || !profile.Trusted)
                            req.Context.Response.Headers.ContentSecurityPolicy = "sandbox allow-same-origin allow-popups allow-popups-to-escape-sandbox;";
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
                            if (directoryRequested)
                                throw new RedirectSignal($"../{segments.Last()[..^5]}");
                            if (segments[^1] == "index.wfpg")
                                throw new RedirectSignal(".");

                            //wfpg (not index.wfpg)
                            page.Title = segments[^1];
                            
                            CheckAccess(req, user.Id, segments, true, out _, out _, out _, out var editFile, out _);
                            if (editFile != null)
                                page.Navigation.Add(new Button("Edit", $"{req.PluginPathPrefix}/editor?u={user.Id}&p={HttpUtility.UrlEncode(string.Join('/', segments))}", "right"));
                            
                            Server.ParseIntoPage(req, page, File.ReadAllLines($"../FilePlugin.Profiles/{req.UserTable.Name}_{user.Id}{string.Join('/', segments.Select(Parsers.ToBase64PathSafe))}"));
                            page.Title += " - Files";
                            if (profile == null || !profile.Trusted)
                                req.Context.Response.Headers.ContentSecurityPolicy = "sandbox allow-same-origin allow-popups allow-popups-to-escape-sandbox;";
                        }
                        else MissingFileOrAccess(req, e);
                    }

                    void AddEditModeLink(bool test)
                    {
                        CheckAccess(req, user.Id, segments, true, out _, out _, out var editDir, out var editFile, out _);
                        if (editDir != null || editFile != null)
                            page.Navigation.Add(new Button("Edit", $"{req.PluginPathPrefix}/editor"));
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