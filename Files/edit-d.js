async function AddNode(d) {
    try {
        var n = encodeURIComponent(document.querySelector("#name").value);
        if (n === "")
        {
            ShowError("Enter a name.");
            return;
        }
        var u = GetQuery("u");
        var p = encodeURIComponent(GetQuery("p"));
        switch ((await fetch(`/api[PATH_PREFIX]/add?u=${u}&p=${p}&n=${n}&d=${d}`)).status) {
            case 200:
                window.location.assign(`[PATH_PREFIX]/edit?u=${u}&p=${p}%2f${n}`)
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
    var files = document.querySelector("#files").files;
    if (files.length === 0) {
        ShowError("No files selected!");
        return;
    }
    var form = new FormData();
    for (var f of files)
    form.append("file", f);
    var request = new XMLHttpRequest();
    request.open("POST", `[PATH_PREFIX]/upload?u=${GetQuery("u")}&p=${encodeURIComponent(GetQuery("p"))}`);
    request.upload.addEventListener("progress", event => {
        document.querySelector("#upload").innerText = `${((event.loaded / event.total) * 100).toFixed(2)}%`;
    });
    request.onreadystatechange = () => {
        if (request.readyState == 4) {
            switch (request.status) {
                case 200:
                    document.querySelector('#upload').innerText = 'Done!';
                    window.location.reload();
                    break;
                case 413:
                    document.querySelector('#upload').innerText = 'Upload';
                    ShowError("At least one of the selected files is too large!");
                    break;
                default:
                    document.querySelector('#upload').innerText = 'Upload';
                    ShowError("Connection failed.");
                    break;
            }
        }
    };
    request.send(form);
}