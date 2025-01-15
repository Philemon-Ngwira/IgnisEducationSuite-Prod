// wwwroot/chartHelper.js

window.createChart = (chartId, chartType, chartData, chartOptions) => {
    const ctx = document.getElementById(chartId).getContext("2d");

    // Check if chart instance already exists
    if (window[chartId]) {
        window[chartId].destroy(); // Destroy previous chart instance if it exists
    }

    window[chartId] = new Chart(ctx, {
        type: chartType,
        data: chartData,
        options: chartOptions
    });
};

window.updateChart = (chartId, chartData) => {
    if (window[chartId]) {
        window[chartId].data = chartData;
        window[chartId].update();
    }
};
