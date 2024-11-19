let u = GetQuery("u"); let pEnc = encodeURIComponent(GetQuery("p"));
let ignoreChanges = false;
let changedEvent = new EventSource(`changed-event?u=${u}&p=${pEnc}`);
onbeforeunload = (event) => { changedEvent.close(); };
changedEvent.onmessage = async (event) => {
    if (event.data === "changed" && !ignoreChanges)
        window.location.reload();
};

async function AddNode(d) {
    HideError();
    try {
        var n = encodeURIComponent(document.querySelector("#name").value);
        if (n === "")
        {
            ShowError("Enter a name.");
            return;
        }
        ignoreChanges = true;
        switch (await SendRequest(`edit/add?u=${u}&p=${pEnc}&n=${n}&d=${d}`, "POST", true)) {
            case 200:
                window.location.assign(`edit?u=${u}&p=${pEnc}%2f${n}`)
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
        ignoreChanges = false;
    } catch {
        ShowError("Connection failed.");
    }
}

async function Upload() {
    HideError();
    var files = document.querySelector("#files").files;
    if (files.length === 0) {
        ShowError("No files selected!");
        return;
    }
    ignoreChanges = true;
    var form = new FormData();
    for (var f of files)
    form.append("file", f);
    var request = new XMLHttpRequest();
    request.open("POST", `edit/upload?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`);
    request.upload.addEventListener("progress", event => {
        document.querySelector("#upload").innerText = `${((event.loaded / event.total) * 100).toFixed(2)}%`;
    });
    request.onreadystatechange = () => {
        if (request.readyState === 4) {
            switch (request.status) {
                case 200:
                    document.querySelector('#upload').innerText = 'Done!';
                    window.location.reload();
                    break;
                case 302:
                    document.querySelector('#upload').innerText = 'Upload';
                    ShowError("You're trying to upload a file with a name that a folder is using!");
                    ignoreChanges = false;
                    break;
                case 413:
                    document.querySelector('#upload').innerText = 'Upload';
                    ShowError("At least one of the selected files is too large!");
                    ignoreChanges = false;
                    break;
                case 507:
                    document.querySelector('#upload').innerText = 'Upload';
                    ShowError("Uploading these files would exceed your account's storage limit. You can most likely obtain more storage space by contacting the administrator.");
                    ignoreChanges = false;
                    break;
                default:
                    document.querySelector('#upload').innerText = 'Upload';
                    ShowError("Connection failed. A possible cause is that at least one of the selected files might be too large.");
                    ignoreChanges = false;
                    break;
            }
        }
    };
    request.send(form);
}