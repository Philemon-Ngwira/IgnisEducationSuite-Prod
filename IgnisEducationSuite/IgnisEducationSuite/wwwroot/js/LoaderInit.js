function showLoader() {
    document.body.classList.add("loading");
    document.getElementById("loader-overlay").style.display = "flex";
}

function hideLoader() {
    document.body.classList.remove("loading");
    document.getElementById("loader-overlay").style.display = "none";
}
