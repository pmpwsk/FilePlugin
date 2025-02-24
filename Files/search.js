let u = GetQuery("u"); let pEnc = encodeURIComponent(GetQuery("p"));
function Search() {
    var elem = document.getElementById("search");
    if (elem.value !== "")
        window.location.assign(`search?u=${u}&p=${pEnc}&q=${encodeURIComponent(elem.value)}`);
}