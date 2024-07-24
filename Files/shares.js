async function RemoveShare() {
    HideError();
    try {
        if (await SendRequest(`shares/remove?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`, "POST", true) === 200)
            window.location.assign("shares");
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    }
}