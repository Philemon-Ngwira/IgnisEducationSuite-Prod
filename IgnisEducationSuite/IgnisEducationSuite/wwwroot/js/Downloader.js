window.BlazorDownloadFile = (fileName, byteArray) => {
    const blob = new Blob([new Uint8Array(byteArray)], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
window.downloadFileFromBytes = (fileName, byteArray) => {
    const blob = new Blob([new Uint8Array(byteArray)], { type: "application/pdf" });
    const url = window.URL.createObjectURL(blob);

    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    a.click();

    window.URL.revokeObjectURL(url);
};


window.BlazorDownloadConsolidatedReportFile = (fileName, base64Data) => {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = "data:application/octet-stream;base64," + base64Data;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
