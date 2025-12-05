window.renderPieChart = (canvasId, labels, data, colors) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const ctx = canvas.getContext('2d');

    // Destroy existing chart if it exists
    if (canvas.chart) {
        canvas.chart.destroy();
    }

    canvas.chart = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: colors,
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            let label = context.label || '';
                            if (label) {
                                label += ': ';
                            }
                            label += '$ ' + context.parsed.toFixed(2);
                            return label;
                        }
                    }
                }
            }
        }
    });
};

window.renderLineChart = (canvasId, labels, datasets) => {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const ctx = canvas.getContext('2d');

    // Destroy existing chart if it exists
    if (canvas.chart) {
        canvas.chart.destroy();
    }

    canvas.chart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            plugins: {
                legend: {
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            let label = context.dataset.label || '';
                            if (label) {
                                label += ': ';
                            }
                            label += '$ ' + context.parsed.y.toFixed(2);
                            return label;
                        }
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function (value) {
                            return '$ ' + value.toFixed(0);
                        }
                    }
                }
            }
        }
    });
};

window.renderBarChart = (canvasId, labels, datasets, horizontal = true) => {
    console.log('renderBarChart called with labels:', labels);

    // Convert labels to array if it's an object (Blazor serialization issue)
    const labelsArray = Array.isArray(labels) ? labels : Object.values(labels);
    console.log('Labels as array:', labelsArray);
    console.log('Horizontal?', horizontal);

    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        console.error('Canvas not found:', canvasId);
        return;
    }

    const ctx = canvas.getContext('2d');

    // Destroy existing chart if it exists
    if (canvas.chart) {
        canvas.chart.destroy();
    }

    const chartConfig = {
        type: 'bar',
        data: {
            labels: labelsArray,
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: true,
            indexAxis: horizontal ? 'y' : 'x',
            plugins: {
                legend: {
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            let label = context.dataset.label || '';
                            if (label) {
                                label += ': ';
                            }
                            // Use correct axis based on orientation
                            const value = horizontal ? context.parsed.x : context.parsed.y;
                            label += '$ ' + value.toFixed(2);
                            return label;
                        }
                    }
                }
            },
            scales: horizontal ? {
                // Horizontal bar chart (values on X, categories on Y)
                x: {
                    type: 'linear',
                    beginAtZero: true,
                    ticks: {
                        callback: function (value) {
                            return '$ ' + value.toFixed(0);
                        }
                    }
                },
                y: {
                    type: 'category'
                }
            } : {
                // Vertical bar chart (categories on X, values on Y)
                x: {
                    type: 'category',
                    ticks: {
                        autoSkip: false,
                        maxRotation: 45,
                        minRotation: 0,
                        callback: function (value, index, ticks) {
                            console.log('X-axis tick callback:', value, index, labelsArray[index]);
                            return labelsArray[index] || value;
                        }
                    }
                },
                y: {
                    type: 'linear',
                    beginAtZero: true,
                    ticks: {
                        callback: function (value) {
                            return '$ ' + value.toFixed(0);
                        }
                    }
                }
            }
        }
    };

    console.log('Creating chart with config:', chartConfig);
    canvas.chart = new Chart(ctx, chartConfig);
    console.log('Chart created successfully');
};

// Generic render function for full control from Blazor
window.renderChart = (canvasId, config) => {
    try {
        console.log('renderChart called for:', canvasId);
        console.log('Config:', config);

        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error('Canvas not found:', canvasId);
            return;
        }

        const ctx = canvas.getContext('2d');
        if (!ctx) {
            console.error('Could not get 2D context for:', canvasId);
            return;
        }

        // Destroy existing chart if it exists
        if (canvas.chart) {
            console.log('Destroying existing chart');
            canvas.chart.destroy();
        }

        console.log('Creating new chart...');
        canvas.chart = new Chart(ctx, config);
        console.log('Chart created successfully');
    } catch (error) {
        console.error('Error in renderChart:', error);
    }
};
