window.ZoomInterop = {
    init: function (signature, meetingNumber, userName, sdkKey, password) {

        if (!window.ZoomMtg) {
            console.error("ZoomMtg is not loaded yet!");
            return;
        }

        console.log("ZoomInterop.init called");

        ZoomMtg.setZoomJSLib('https://source.zoom.us/4.0.0/lib', '/av');
        ZoomMtg.preLoadWasm();
        ZoomMtg.prepareWebSDK();

        ZoomMtg.init({
            leaveUrl: window.location.href,
            isSupportAV: true,
            patchJsMedia: true,
            success: function () {
                console.log("ZoomMtg.init success");

                ZoomMtg.join({
                    signature: signature,
                    meetingNumber: meetingNumber,
                    userName: userName,
                    sdkKey: sdkKey,
                    passWord: password, // <--- FIXED
                    success: function () {
                        console.log("Joined meeting successfully");
                    },
                    error: function (err) {
                        console.error("Join error:", err);
                    }
                });
            },
            error: function (err) {
                console.error("Init error:", err);
            }
        });
    }
};
