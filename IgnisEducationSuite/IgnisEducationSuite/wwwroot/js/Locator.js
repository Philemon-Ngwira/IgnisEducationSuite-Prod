window.getCurrentLocation = () => {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject("Geolocation not supported");
            return;
        }

        navigator.geolocation.getCurrentPosition(
            pos => {
                resolve({
                    latitude: pos.coords.latitude,
                    longitude: pos.coords.longitude,
                    accuracy: pos.coords.accuracy
                });
            },
            err => reject(err.message),
            {
                enableHighAccuracy: true,
                timeout: 10000
            }
        );
    });
};
window.renderParentDropoffMap = (mapElement, lat, lng) => {
    if (!window.google || !google.maps) {
        console.error("Google Maps not loaded");
        return;
    }

    const position = { lat: lat, lng: lng };
    const map = new google.maps.Map(mapElement, {
        zoom: 17,
        center: position,
        disableDefaultUI: true,
        zoomControl: true
    });

    new google.maps.Marker({
        position: position,
        map: map,
        title: "Child drop-off location"
    });
};



