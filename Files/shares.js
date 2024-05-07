async function RemoveShare() {
    HideError();
    try {
        if ((await fetch(`/api[PATH_PREFIX]/remove-share?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`)).status === 200)
            window.location.assign("[PATH_PREFIX]/shares");
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    }
}