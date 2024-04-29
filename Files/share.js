async function AddAccess(uid, canEdit) {
    var un = document.querySelector("#name").value;
    if (un === "") {
        ShowError("Enter a username.");
        return;
    }
    var canEdit = document.querySelector("#edit").checked;
    try {
        switch ((await fetch(`/api[PATH_PREFIX]/share/set?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}&un=${un}&e=${canEdit}`)).status) {
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
    try {
        if ((await fetch(`/api[PATH_PREFIX]/share/set?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}&uid=${uid}&e=${canEdit}`)).status === 200)
            window.location.reload();
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    }
}

async function RemoveAccess(uid) {
    try {
        if ((await fetch(`/api[PATH_PREFIX]/share/set?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}&uid=${uid}`)).status === 200)
            window.location.reload();
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    }
}