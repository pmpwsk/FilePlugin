let u = GetQuery("u"); let pEnc = encodeURIComponent(GetQuery("p"));

async function AddNode(d) {
    HideError();
    try {
        var n = encodeURIComponent(document.getElementById(d ? "dir-name" : "file-name").value);
        if (n === "")
        {
            ShowError("Enter a name.");
            return;
        }
        switch (await SendRequest(`add/create?u=${u}&p=${pEnc}&n=${n}&d=${d}`, "POST", true)) {
            case 200:
                window.location.assign(`${d ? "list" : "editor"}?u=${u}&p=${pEnc}%2f${n}`)
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
    } catch {
        ShowError("Connection failed.");
    }
}

async function Upload() {
    HideError();
    var files = document.getElementById("upload-files").files;
    if (files.length === 0) {
        ShowError("No files selected!");
        return;
    }
    var form = new FormData();
    for (var f of files)
        form.append("file", f);
    var request = new XMLHttpRequest();
    request.open("POST", `add/upload?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`);
    request.upload.addEventListener("progress", event => {
        document.getElementById("upload-button").innerText = `${((event.loaded / event.total) * 100).toFixed(2)}%`;
    });
    request.onreadystatechange = () => {
        if (request.readyState === 4) {
            switch (request.status) {
                case 200:
                    document.getElementById("upload-button").innerText = 'Done!';
                    window.location.assign(`list?u=${u}&p=${pEnc}`);
                    break;
                case 302:
                    document.getElementById("upload-button").innerText = 'Upload';
                    ShowError("You're trying to upload a file with a name that a folder is using!");
                    break;
                case 413:
                    document.getElementById("upload-button").innerText = 'Upload';
                    ShowError("At least one of the selected files is too large!");
                    break;
                case 507:
                    document.getElementById("upload-button").innerText = 'Upload';
                    ShowError("Uploading these files would exceed your account's storage limit. You can most likely obtain more storage space by contacting the administrator.");
                    break;
                default:
                    document.getElementById("upload-button").innerText = 'Upload';
                    ShowError("Connection failed. A possible cause is that at least one of the selected files might be too large.");
                    break;
            }
        }
    };
    request.send(form);
}