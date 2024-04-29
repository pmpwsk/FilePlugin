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
        let response = await fetch(`/api[PATH_PREFIX]/load?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`);
        switch (response.status) {
            case 200:
                ta.value = await response.text();
                ta.placeholder = "Enter something...";
                break;
            case 201:
                ta.value = "";
                ta.placeholder = "Enter something...";
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
    save.innerText = "Save";
    save.className = "green";
}

async function Save() {
    back.innerText = "Back";
    save.innerText = "Saving...";
    save.className = "green";
    try {
        if ((await fetch(`[PATH_PREFIX]/save?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`, { method: "POST", body: ta.value })).status === 200) {
            save.innerText = "Saved!";
            save.className = "";
        } else {
            save.innerText = "Error!";
            save.className = "red";
        }
    } catch {
        save.innerText = "Error!";
        save.className = "red";
    }
}

function GoTo(url) {
    if (save.innerText === "Save")
        save.className = "red";
    else window.location.assign(url);
}

function GoBack(url) {
    if (save.innerText === "Save" && back.innerText == "Back")
        back.innerText = "Discard?";
    else window.location.assign(url);
}