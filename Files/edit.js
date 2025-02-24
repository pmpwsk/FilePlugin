async function Delete() {
    HideError();
    var del = document.getElementById("delete").children[0];
    if (del.innerText !== "Delete?") {
        del.innerText = "Delete?";
        return;
    }
    try {
        var u = GetQuery("u");
        var p = GetQuery("p");
        if (await SendRequest(`edit/delete?u=${u}&p=${encodeURIComponent(p)}`, "POST", true) === 200)
            window.location.assign(`edit?u=${u}&p=${encodeURIComponent(p.split("/").slice(0, -1).join("/"))}`);
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    }
}

async function SaveName() {
    HideError();
    var rename = document.getElementById("name-save");
    var name = document.getElementById("name").value;
    if (name === "") {
        ShowError("Enter a name.");
        return;
    }
    try {
        var u = GetQuery("u");
        var p = GetQuery("p");
        switch (await SendRequest(`edit/rename?u=${u}&p=${encodeURIComponent(p)}&n=${encodeURIComponent(name)}`, "POST", true)) {
            case 200:
                rename.className = "";
                rename.innerText = "Saved!";
                window.location.assign(`edit?u=${u}&p=${encodeURIComponent(p.split("/").slice(0, -1).join("/") + "/" + name)}`);
                break;
            case 302:
                ShowError("Another file or directory with this name already exists.");
                break;
            case 400:
                ShowError("Invalid name.");
                break;
            default:
                ShowError("Connection failed.");
                break;
        }
    } catch {
        ShowError("Connection failed.");
    }
}

function NameChanged() {
    var rename = document.getElementById("name-save");
    rename.className = "green";
    rename.innerText = "Save";
}

async function AddShare() {
    HideError();
    try {
        if (await SendRequest(`shares/add?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`, "POST", true) === 200)
            window.location.reload();
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    }
}

async function RemoveShare() {
    HideError();
    try {
        if (await SendRequest(`shares/remove?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`, "POST", true) === 200)
            window.location.reload();
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    }
}