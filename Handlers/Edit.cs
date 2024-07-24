using System.Web;
using uwap.WebFramework.Elements;

namespace uwap.WebFramework.Plugins;

public partial class FilePlugin : Plugin
{
    private Task Edit(Request req)
    {
        switch (req.Path)
        {
            case "/edit":
            { CreatePage(req, "Files", out var page, out var e, out var userProfile);
                //edit mode
                req.ForceLogin();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                string pEnc = HttpUtility.UrlEncode(p);
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out _, out var parent, out var directory, out var file, out var name);
                if (directory != null)
                {
                    //edit mode > directory
                    page.Title = name + " - Files";
                    page.Scripts.Add(Presets.SendRequestScript);
                    page.Scripts.Add(new Script("query.js"));
                    page.Scripts.Add(new Script("edit-d.js"));
                    if (parent != null)
                    {
                        string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                        string backUrl = $"edit?u={u}&p={parentEnc}";
                        page.Navigation.Add(new Button("Back", backUrl, "right"));
                        page.Sidebar =
                        [
                            new ButtonElement(null, "Go up a level", backUrl),
                            ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null)),
                            ..parent.Files.Select(fKV => new ButtonElement(null, fKV.Key, $"edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}", fKV.Key == name ? "green" : null))
                        ];
                    }
                    else
                    {
                        page.Navigation.Add(new Button("Back", req.LoggedIn && req.User.Id == u ? "." : "shares", "right"));
                        if (u == req.User.Id)
                            page.Sidebar =
                            [
                                new ButtonElement("Menu:", null, "."),
                                new ButtonElement(null, "Edit mode", $"edit?u={req.User.Id}&p=", "green"),
                                new ButtonElement(null, "View mode", $"@{req.User.Username}"),
                                new ButtonElement(null, "Shares", "shares")
                            ];
                    }
                    page.Navigation.Add(new Button("More", $"more?u={u}&p={pEnc}", "right"));
                    e.Add(new HeadingElement(name, "Edit mode"));
                    e.Add(new ContainerElement("New", new TextBox("Enter a name...", null, "name", onEnter: "AddNode('false')", autofocus: true))
                    { Buttons = [
                        new ButtonJS("Text file", "AddNode('false')", "green"),
                        new ButtonJS("Folder", "AddNode('true')", "green")
                    ]});
                    e.Add(new ContainerElement("Upload", new FileSelector("files", true)) { Button = new ButtonJS("Upload", "Upload()", "green", id: "upload")});
                    page.AddError();
                    if (directory.Files.Count == 0 && directory.Directories.Count == 0)
                        e.Add(new ContainerElement("No items!", "", "red"));
                    else
                    {
                        foreach (var dKV in directory.Directories)
                            e.Add(new ButtonElement(dKV.Key, null, $"edit?u={u}&p={pEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}"));
                        foreach (var fKV in directory.Files)
                            e.Add(new ButtonElement(fKV.Key, $"{FileSizeString(fKV.Value.Size)} | {fKV.Value.ModifiedUtc.ToLongDateString()}", $"edit?u={u}&p={pEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}"));
                    }
                }
                else if (file != null)
                {
                    //edit mode > file
                    page.Title = name + " - Files";
                    if (parent != null)
                    {
                        string parentEnc = HttpUtility.UrlEncode(string.Join('/', segments.SkipLast(1)));
                        string backUrl = $"edit?u={u}&p={parentEnc}";
                        page.Navigation.Add(new Button("Back", backUrl, "right"));
                        page.Sidebar =
                        [
                            new ButtonElement(null, "Go up a level", backUrl),
                            ..parent.Directories.Select(dKV => new ButtonElement(null, dKV.Key, $"edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(dKV.Key)}", dKV.Key == name ? "green" : null)),
                            ..parent.Files.Select(fKV => new ButtonElement(null, fKV.Key, $"edit?u={u}&p={parentEnc}%2f{HttpUtility.UrlEncode(fKV.Key)}", fKV.Key == name ? "green" : null))
                        ];
                    }
                    else
                    {
                        page.Navigation.Add(new Button("Back", "shares", "right"));
                        if (u == req.User.Id)
                            page.Sidebar =
                            [
                                new ButtonElement("Menu:", null, "."),
                                new ButtonElement(null, "Edit mode", $"edit?u={req.User.Id}&p=", "green"),
                                new ButtonElement(null, "View mode", $"@{req.User.Username}"),
                                new ButtonElement(null, "Shares", "shares")
                            ];
                    }
                    if (parent != null || req.LoggedIn)
                        page.Navigation.Add(new Button("More", $"more?u={u}&p={pEnc}", "right"));
                    e.Add(new HeadingElement(name, "Edit mode"));
                    string username = u == req.User.Id ? req.User.Username : req.UserTable[u].Username;
                    if (segments.Last().EndsWith(".wfpg"))
                        e.Add(new ButtonElement("View page", null, $"@{username}{(segments.Last() == "index.wfpg" ? string.Join('/', segments.SkipLast(1).Select(HttpUtility.UrlEncode)) : string.Join('/', p[..^5].Split('/').Select(HttpUtility.UrlEncode)))}"));
                    e.Add(new ButtonElement("View in browser", null, $"@{username}{string.Join('/', p.Split('/').Select(HttpUtility.UrlEncode))}"));
                    e.Add(new ButtonElement("Download", null, $"download?u={u}&p={pEnc}", newTab: true));
                    e.Add(new ButtonElement("Edit as text", null, $"editor?u={u}&p={pEnc}"));
                }
                else MissingFileOrAccess(req, e);
            } break;

            case "/edit/add":
            { req.ForcePOST();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p) && req.Query.TryGetValue("n", out var n) && NameOkay(n) && req.Query.TryGetValue("d", out bool d)))
                    throw new BadRequestSignal();
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (directory == null)
                    throw new NotFoundSignal();
                if (directory.Directories.ContainsKey(n) || directory.Files.ContainsKey(n))
                    throw new HttpStatusSignal(302);
                if (profile == null)
                    throw new ServerErrorSignal();
                profile.Lock();
                string target = $"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', ((IEnumerable<string>)[..segments, n]).Select(Parsers.ToBase64PathSafe))}";
                if (d)
                {
                    Directory.CreateDirectory(target);
                    directory.Directories.Add(n, new());
                }
                else
                {
                    File.WriteAllText(target, "");
                    directory.Files.Add(n, new(DateTime.UtcNow, 0));
                }
                profile.UnlockSave();
            } break;

            case "/edit/upload":
            { req.ForcePOST();
                if (!(req.Query.TryGetValue("u", out var u) && req.Query.TryGetValue("p", out var p)))
                    throw new BadRequestSignal();
                var segments = p.Split('/');
                CheckAccess(req, u, segments, true, out var profile, out var parent, out var directory, out var file, out var name);
                if (directory == null)
                    throw new NotFoundSignal();
                if (profile == null)
                    throw new ServerErrorSignal();
                long limit = req.IsAdmin ? long.MaxValue : UploadSizeLimit;
                req.BodySizeLimit = limit;
                if ((!req.IsForm) || req.Files.Count == 0)
                    req.Status = 400;
                profile.Lock();
                foreach (var uploadedFile in req.Files)
                {
                    if (!NameOkay(uploadedFile.FileName))
                        continue;
                    string loc = $"../FilePlugin/{req.UserTable.Name}_{u}{string.Join('/', ((IEnumerable<string>)[..segments, uploadedFile.FileName]).Select(Parsers.ToBase64PathSafe))}";
                    long oldSize;
                    if (directory.Files.TryGetValue(uploadedFile.FileName, out var f))
                    {
                        f.ModifiedUtc = DateTime.UtcNow;
                        oldSize = f.Size;
                    }
                    else
                    {
                        f = new(DateTime.UtcNow, 0);
                        directory.Files[uploadedFile.FileName] = f;
                        oldSize = 0;
                    }
                    try
                    {
                        if (!uploadedFile.Download(loc, limit))
                        {
                            profile.SizeUsed -= oldSize;
                            directory.Files.Remove(uploadedFile.FileName);
                            profile.UnlockSave();
                            throw new HttpStatusSignal(413);
                        }
                        f.Size = new FileInfo(loc).Length;
                        if (profile.SizeUsed + f.Size - oldSize > profile.SizeLimit && !req.IsAdmin)
                        {
                            profile.SizeUsed -= oldSize;
                            directory.Files.Remove(uploadedFile.FileName);
                            profile.UnlockSave();
                            throw new HttpStatusSignal(507);
                        }
                        profile.SizeUsed += f.Size - oldSize;
                    }
                    catch
                    {
                        try { File.Delete(loc); } catch { }
                        profile.SizeUsed -= oldSize;
                        directory.Files.Remove(uploadedFile.FileName);
                        profile.UnlockSave();
                        throw;
                    }
                }
                profile.UnlockSave();
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