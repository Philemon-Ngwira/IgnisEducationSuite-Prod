window.loadGoogleBook = function (bookId) {
    if (!window.google || !window.google.books) {
        console.error("Google Books API not loaded.");
        return;
    }

    var viewerContainer = document.getElementById('viewerContainer');
    if (!viewerContainer) {
        console.error("Viewer container element not found.");
        return;
    }

    var viewer = new google.books.DefaultViewer(viewerContainer);
    viewer.load(bookId, function (success) {
        if (!success) {
            viewerContainer.innerHTML = '<p style="color:red;">Book preview not available.</p>';
        }
    });
};

(function () {
    var script = document.createElement('script');
    script.src = 'https://www.google.com/books/jsapi.js';
    script.onload = function () {
        console.log("Google Books API script loaded.");
        google.books.load(function () {
            console.log("Google Books API loaded.");
        });
    };
    script.onerror = function () {
        console.error("Failed to load Google Books API script.");
    };
    document.head.appendChild(script);
})();
