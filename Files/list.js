let u = GetQuery("u"); let pEnc = encodeURIComponent(GetQuery("p"));
let changedEvent = new EventSource(`changed-event?u=${u}&p=${pEnc}`);
onbeforeunload = (event) => { changedEvent.close(); };
changedEvent.onmessage = async (event) => {
    if (event.data === "changed")
        window.location.reload();
};