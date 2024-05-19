let changedAlready = false;
let savedValue = null;
let ch = 0;
let ta = document.querySelector('#text');
let editor = document.querySelector('#editor');
let sidebar = document.querySelector('.sidebar');
let full = document.querySelector('.full');
let save = document.querySelector('#save');
let back = document.querySelector('#back');
window.onresize = Resize;
ta.onclick = Refocus;
Resize();
Load();
document.addEventListener('keydown', e => {
    if (e.ctrlKey && e.key === 's') {
        e.preventDefault();
        Save();
    }
});
window.addEventListener("beforeunload", e => {
    if (save.innerText === "Save" && back.innerText == "Back") {
        var confirmationMessage = "You have unsaved changes!";
        (e || window.event).returnValue = confirmationMessage;
        return confirmationMessage;
    }
});

function Resize() {
    let fullComp = window.getComputedStyle(full);
    let editorComp = window.getComputedStyle(editor);
    let newHeight = Math.floor(window.visualViewport.height - parseFloat(editorComp['marginTop']) - parseFloat(fullComp['paddingTop']) - parseFloat(fullComp['paddingBottom']));
    editor.style.flex = '1';
    editor.style.height = newHeight + 'px';
    Refocus();
}

function Refocus() {
    let nh = ta.clientHeight;
    if (ch > nh && document.activeElement === ta) {
        ta.blur();
        ta.focus();
    }
    ch = nh;
}

async function Load() {
    try {
        let response = await fetch(`/api[PATH_PREFIX]/load?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`, {cache:"no-store"});
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
        switch ((await fetch(`[PATH_PREFIX]/save?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`, { method: "POST", body: ta.value })).status) {
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
    if (save.innerText === "Save" && back.innerText == "Back")
        back.innerText = "Discard?";
    else window.location.assign(`[PATH_PREFIX]/edit?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`);
}