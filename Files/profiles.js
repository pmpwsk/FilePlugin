async function SaveTrusted() {
    HideError();
    if (await SendRequest(`profiles/trusted?u=${GetQuery("u")}&v=${document.getElementById("trusted").checked}`, "POST", true) === 200)
        window.location.reload();
    else ShowError("Connection failed.");
}

async function SaveLimit() {
    HideError();
    var button = document.querySelector("#save-limit");
    var value = document.querySelector("#limit").value;
    if (value === "") {
        ShowError("Enter a limit.");
    } else try {
        switch (await SendRequest(`profiles/limit?u=${GetQuery("u")}&v=${encodeURIComponent(value)}`, "POST", true)) {
            case 200:
                button.className = "";
                button.innerText = "Saved!";
                window.location.reload();
                break;
            case 400:
                ShowError("Invalid value.");
                break;
            default:
                ShowError("Connection failed.");
                break;
        }
    } catch {
        ShowError("Connection failed.");
    }
}

function ChangedLimit() {
    HideError();
    var button = document.querySelector("#save-limit");
    button.className = "green";
    button.innerText = "Save";
}

async function Delete() {
    HideError();
    let deleteText = document.querySelector("#delete").firstElementChild;
    if (deleteText.textContent === "Delete")
        deleteText.textContent = "Delete everything?";
    else try {
        if (await SendRequest(`profiles/delete?u=${GetQuery("u")}`, "POST", true) === 200)
            window.location.assign("profiles");
        else ShowError("Connection failed.");
    } catch {
        ShowError("Connection failed.");
    } 
}