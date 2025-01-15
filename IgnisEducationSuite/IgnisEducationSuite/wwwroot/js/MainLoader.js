
document.addEventListener("DOMContentLoaded", function () {
    // Hide the loading screen after 2 seconds (or when the app is fully ready)
    setTimeout(function () {
        var loadingScreen = document.getElementById("loading-screen");
        loadingScreen.classList.add("hidden");
        setTimeout(function () {
            loadingScreen.style.display = "none";
        }, 500); // Wait for the fade-out transition
    }, 2000); // Adjust time as needed
});

