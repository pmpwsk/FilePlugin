async function AddNode(d) {
    try {
        var n = encodeURIComponent(document.querySelector("#name").value);
        if (n === "")
        {
            ShowError("Enter a name.");
            return;
        }
        var u = GetQuery("u");
        var p = encodeURIComponent(GetQuery("p"));
        switch ((await fetch(`/api[PATH_PREFIX]/add?u=${u}&p=${p}&n=${n}&d=${d}`)).status) {
            case 200:
                window.location.assign(`[PATH_PREFIX]/edit?u=${u}&p=${p}%2f${n}`)
                break;
            case 302:
                ShowError("Another file or directory with this name already exists.");
                break;
            default:
                ShowError("Connection failed.");
                break;
        }
    } catch {
        ShowError("Connection failed.");
    }
}