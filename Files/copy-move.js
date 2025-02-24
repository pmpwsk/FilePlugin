async function CopyOrMove() {
    var isCopy = window.location.pathname.endsWith("y");
    if (await SendRequest(`${(isCopy?"copy":"move")}/do?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}&l=${encodeURIComponent(GetQuery("l"))}`, "POST", true) === 200)
        window.location.assign(`list?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("l"))}`);
    else ShowError("Connection failed.");
}