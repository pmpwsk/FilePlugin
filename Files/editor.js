let u = GetQuery("u");
let pEnc = encodeURIComponent(GetQuery("p"));
let changedAlready = false;
let savedValue = null;

let ta, save, back;

window.onload = async () => {
    ta = document.getElementById("editor");
    save = document.getElementById("save");
    back = document.getElementById("back");
    
    await Load();
    
    document.addEventListener('keydown', e => {
        if (e.ctrlKey && e.key === 's') {
            e.preventDefault();
            Save();
        }
    });
    window.addEventListener("beforeunload", e => {
        if (save.innerText === "Save" && back.innerText === "Back") {
            var confirmationMessage = "You have unsaved changes!";
            (e || window.event).returnValue = confirmationMessage;
            return confirmationMessage;
        }
    });
    let changedEvent = new EventSource(`changed-event?u=${u}&p=${pEnc}`);
    onbeforeunload = (event) => {
        changedEvent.close();
    };
    changedEvent.onmessage = async (event) => {
        if (event.data === "changed" && save.innerText === "Saved!")
            await Load();
    };
}

async function Load() {
    try {
        let response = await fetch(`editor/load?u=${u}&p=${pEnc}`, {cache:"no-store"});
        switch (response.status) {
            case 200:
            case 201:
                savedValue = response.status === 201 ? "" : await response.text();
                if (save.innerText === "Save") {
                    if (ta.value === savedValue) {
                        save.innerText = "Saved!";
                        save.className = "";
                    }
                } else ta.value = savedValue;
                ta.placeholder = "Enter something...";
                if (ta.value === "")
                    ta.focus();
                break;
            default:
                ta.value = "";
                ta.placeholder = "Error loading this file's content! Try reloading the page.";
                save.innerText = "Error!";
                save.className = "red";
        }
    } catch {
        ta.value = "";
        ta.placeholder = "Error loading this file's content! Try reloading the page.";
        save.innerText = "Error!";
        save.className = "red";
    }
}

function TextChanged() {
    if (changedAlready || ta.value !== savedValue) {
        save.innerText = "Save";
        save.className = "green";
        changedAlready = true;
        savedValue = null;
    }
}

async function Save() {
    back.innerText = "Back";
    save.innerText = "Saving...";
    save.className = "green";
    try {
        switch ((await fetch(`editor/save?u=${u}&p=${pEnc}`, { method: "POST", body: ta.value })).status) {
            case 200:
                save.innerText = "Saved!";
                save.className = "";
                break;
            case 507:
                save.innerText = "Too long!";
                save.className = "red";
                alert("Saving this file would exceed your account's storage limit. Try shortening it or copy the text to save it somewhere else for the time being. You can most likely obtain more storage space by contacting the administrator.");
                break;
            default:
                save.innerText = "Error!";
                save.className = "red";
                break;
        }
    } catch {
        save.innerText = "Error!";
        save.className = "red";
    }
}

function GoBack() {
    if (save.innerText === "Save" && back.innerText === "Back")
        back.innerText = "Discard?";
    else window.location.assign(`edit?u=${u}&p=${pEnc}`);
}