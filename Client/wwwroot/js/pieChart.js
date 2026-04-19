window.pieChartInterop = {
    _charts: {},

    render: function (canvasId, labelsJson, valuesJson, colorsJson, size) {
        var labels = JSON.parse(labelsJson);
        var values = JSON.parse(valuesJson);
        var colors = JSON.parse(colorsJson);

        var canvas = document.getElementById(canvasId);
        if (!canvas) return;

        canvas.width = size;
        canvas.height = size;

        // Destroy existing chart on this canvas
        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
            delete this._charts[canvasId];
        }

        var ctx = canvas.getContext('2d');
        this._charts[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: values,
                    backgroundColor: colors,
                    borderColor: 'rgba(30, 24, 18, 0.8)',
                    borderWidth: 2,
                    hoverOffset: 4
                }]
            },
            options: {
                responsive: false,
                maintainAspectRatio: true,
                cutout: '35%',
                plugins: {
                    legend: { display: false },
                    tooltip: { enabled: false }
                },
                animation: {
                    duration: 400,
                    easing: 'easeOutQuart'
                }
            }
        });
    },

    destroyAll: function () {
        for (var id in this._charts) {
            this._charts[id].destroy();
        }
        this._charts = {};
    }
};
