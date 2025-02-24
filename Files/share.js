async function AddAccess() {
    HideError();
    var un = document.getElementById("target-name").value;
    if (un === "") {
        ShowError("Enter a username.");
        return;
    }
    var canEdit = document.getElementById("edit").checked;
    try {
        switch (await SendRequest(`share/set?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}&un=${un}&e=${canEdit}`, "POST", true)) {
            case 200:
                window.location.reload();
                break;
            case 404:
                ShowError("No user with this username was found.");
                break;
            default:
                ShowError("Connection failed.");
                break;
        }
    } catch {
        ShowError("Connection failed.");
    }
}

async function SetAccess(uid, canEdit) {
    HideError();
    try {
        if (await SendRequest(`share/set?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}&uid=${uid}&e=${canEdit}`, "POST", true) === 200)
            window.location.reload();
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    }
}

async function RemoveAccess(uid) {
    HideError();
    try {
        if (await SendRequest(`share/set?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}&uid=${uid}`, "POST", true) === 200)
            window.location.reload();
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    }
}

async function CreateInvite() {
    HideError();
    var expiration = document.getElementById("expiration").value;
    if (expiration === "") {
        ShowError("Enter a number of days for the invite to expire after or 0 to disable expiration.");
        return;
    }
    try {
        switch (await SendRequest(`share/invite?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}&e=${expiration}`, "POST", true)) {
            case 200:
                window.location.reload();
                break;
            case 400:
                ShowError("Invalid expiration.");
                break;
            default:
                ShowError("Connection failed.");
                break;
        }
    } catch {
        ShowError("Connection failed.");
    }
}

async function DeleteInvite() {
    HideError();
    try {
        if (await SendRequest(`share/invite?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`, "POST", true) === 200)
            window.location.reload();
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    }
}