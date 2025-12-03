//window.ZoomInterop = {
//    init: function (signature, meetingNumber, userName, sdkKey, password) {
//        if (!window.ZoomMtg) {
//            console.error("ZoomMtg is not loaded yet!");
//            return;
//        }

//        console.log("ZoomInterop.init called");

//        ZoomMtg.setZoomJSLib('https://source.zoom.us/4.0.0/lib', '/av');
//        ZoomMtg.preLoadWasm();
//        ZoomMtg.prepareWebSDK();

//        ZoomMtg.init({
//            leaveUrl: window.location.href,
//            isSupportAV: true,
//            patchJsMedia: true,
//            success: function () {
//                ZoomMtg.join({
//                    signature: signature,
//                    meetingNumber: meetingNumber,
//                    userName: userName,
//                    sdkKey: sdkKey,
//                    passWord: password,
//                    success: function () {
//                        console.log("Joined meeting successfully");

//                        // Attach the meeting status listener AFTER join succeeds
//                        ZoomMtg.inMeetingServiceListener('onMeetingStatus', function (data) {
//                            console.log("Meeting status changed:", data);
//                            if (data.meetingStatus === ZoomMtg.MEETING_STATUS.ENDED) {
//                                console.log("Meeting ended by host.");
//                                DotNet.invokeMethodAsync(
//                                    "IgnisEducationSuite.Client",
//                                    "OnZoomMeetingEnded",
//                                    meetingNumber // pass meeting number
//                                );
//                            }
//                        });
//                    },
//                    error: function (err) {
//                        console.error("Join error:", err);
//                    }
//                });
//            },
//            error: function (err) {
//                console.error("Init error:", err);
//            }
//        });

//    }
//};

//window.HideIgnisFooter = () => {
//    const footer = document.getElementById("ignis-footer");
//    if (footer) footer.style.display = "none";
//};

//window.ShowIgnisFooter = () => {
//    const footer = document.getElementById("ignis-footer");
//    if (footer) footer.style.display = "block";
//};



// Zoom.js

window.ZoomInterop = {
    init: async function (
        signature,
        meetingNumber,
        userName,
        sdkKey,
        password,
        isHost
    ) {
        if (!window.ZoomMtgEmbedded) {
            console.error("Zoom Embedded SDK not loaded yet");
            return;
        }

        const client = ZoomMtgEmbedded.createClient();
        const zoomContainer = document.getElementById("meetingSDKElement");

        if (!zoomContainer) {
            console.error("Could not find meetingSDKElement container");
            return;
        }

        try {
            // Initialize Embedded SDK
            await client.init({
                zoomAppRoot: zoomContainer,
                language: "en-US",
                customize: {
                    video: {
                        isResizable: true,
                        viewSizes: {
                            default: {
                                width: 1200,
                                height: 600
                            },
                            ribbon: {
                                width: 300,
                                height: 700
                            }
                        }
                    }
                }
            });

            console.log("Embedded SDK init success");

            // Join the meeting
            await client.join({
                signature: signature,
                sdkKey: sdkKey,
                meetingNumber: meetingNumber,
                password: password,
                userName: userName
            });

            console.log("Joined meeting");

            client.on("connection-change", (payload) => {
                if (payload.state === "Closed") {
                    console.log("Meeting closed. Redirecting to end page...");
                    // Replace `meeting-ended` with your Razor page route
                    window.location.href = `/meeting-ended/${meetingNumber}`;
                }
            });


            // Save client globally so leave() can access it
            window.currentZoomClient = client;

        } catch (err) {
            console.error("Zoom join/init error:", err);
        }
    },

    leave: function () {
        if (window.currentZoomClient) {
            window.currentZoomClient
                .leave()
                .then(() => console.log("Left meeting"))
                .catch((err) => console.error("Leave meeting error:", err));
        }
    }
};

// Footer controls
window.HideIgnisFooter = () => {
    const footer = document.getElementById("ignis-footer");
    if (footer) footer.style.display = "none";
};

window.ShowIgnisFooter = () => {
    const footer = document.getElementById("ignis-footer");
    if (footer) footer.style.display = "block";
};


