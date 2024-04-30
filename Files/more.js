async function Delete() {
    var del = document.querySelector("#delete").children[0];
    if (del.innerText !== "Delete?") {
        del.innerText = "Delete?";
        return;
    }
    try {
        var u = GetQuery("u");
        var p = GetQuery("p");
        if ((await fetch(`/api[PATH_PREFIX]/delete?u=${u}&p=${encodeURIComponent(p)}`)).status === 200)
            window.location.assign(`[PATH_PREFIX]/edit?u=${u}&p=${encodeURIComponent(p.split("/").slice(0, -1).join("/"))}`);
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    }
}