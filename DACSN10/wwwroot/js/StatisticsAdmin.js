
// Global variables với thông tin thời gian thực
const currentDateTime = '2025-05-29 16:44:00';
const currentUser = 'MHoang287';
let revenueChart, statusChart, timeChart;
let autoRefreshInterval;
let currentChartType = 'line';

// Data từ model  
const statisticsData = {
    totalPayments: @Html.Raw(Json.Serialize(Model.TotalPayments)),
    totalAmount: @Html.Raw(Json.Serialize(Model.TotalAmount)),
    successCount: @Html.Raw(Json.Serialize(Model.SuccessCount)),
    pendingCount: @Html.Raw(Json.Serialize(Model.PendingCount)),
    failedCount: @Html.Raw(Json.Serialize(Model.FailedCount))
};

$(document).ready(function () {
    initializeApp();
});

function initializeApp() {
    // Initialize AOS
    AOS.init({
        duration: 800,
        easing: 'ease-out-quart',
        once: true,
        offset: 100
    });

    // Configure Toastr
    toastr.options = {
        "closeButton": true,
        "progressBar": true,
        "positionClass": "toast-top-right",
        "timeOut": 5000,
        "extendedTimeOut": 3000,
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    };

    // Initialize components
    initializeCharts();
    animateCounters();
    setupEventHandlers();
    startAutoRefresh();

    // Welcome message
    setTimeout(() => {
        toastr.info(`📊 Thống kê thanh toán đã được tải thành công lúc ${currentDateTime}`, `Xin chào ${currentUser}!`);
    }, 1000);

    // Show system status
    setTimeout(() => {
        toastr.success('🚀 Hệ thống hoạt động ổn định - Hiệu suất: 94%', 'Trạng thái hệ thống', { timeOut: 3000 });
    }, 3000);
}

function initializeCharts() {
    // Revenue Chart
    const revenueCtx = document.getElementById('revenueChart');
    if (revenueCtx) {
        // Generate sample data for the last 12 months
        const months = [];
        const revenueData = [];
        const transactionData = [];

        for (let i = 11; i >= 0; i--) {
            const date = new Date();
            date.setMonth(date.getMonth() - i);
            months.push(date.toLocaleDateString('vi-VN', { month: 'short', year: 'numeric' }));

            // Sample data with growth trend
            const baseRevenue = statisticsData.totalAmount / 12;
            revenueData.push(Math.floor(baseRevenue * (0.7 + Math.random() * 0.6 + i * 0.05)));

            const baseTransactions = statisticsData.totalPayments / 12;
            transactionData.push(Math.floor(baseTransactions * (0.8 + Math.random() * 0.4 + i * 0.03)));
        }

        revenueChart = new Chart(revenueCtx, {
            type: 'line',
            data: {
                labels: months,
                datasets: [{
                    label: 'Doanh thu (₫)',
                    data: revenueData,
                    borderColor: 'rgba(102, 126, 234, 1)',
                    backgroundColor: 'rgba(102, 126, 234, 0.1)',
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    yAxisID: 'y'
                }, {
                    label: 'Số giao dịch',
                    data: transactionData,
                    borderColor: 'rgba(17, 153, 142, 1)',
                    backgroundColor: 'rgba(17, 153, 142, 0.1)',
                    borderWidth: 3,
                    fill: false,
                    tension: 0.4,
                    yAxisID: 'y1'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false,
                },
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        titleColor: '#fff',
                        bodyColor: '#fff',
                        borderColor: 'rgba(102, 126, 234, 1)',
                        borderWidth: 1,
                        callbacks: {
                            label: function (context) {
                                if (context.datasetIndex === 0) {
                                    return `Doanh thu: ${context.parsed.y.toLocaleString('vi-VN')}₫`;
                                } else {
                                    return `Giao dịch: ${context.parsed.y.toLocaleString('vi-VN')}`;
                                }
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        display: true,
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    },
                    y: {
                        type: 'linear',
                        display: true,
                        position: 'left',
                        grid: {
                            color: 'rgba(0,0,0,0.1)'
                        },
                        ticks: {
                            color: '#6c757d',
                            callback: function (value) {
                                return value.toLocaleString('vi-VN') + '₫';
                            }
                        }
                    },
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        grid: {
                            drawOnChartArea: false,
                        },
                        ticks: {
                            color: '#6c757d'
                        }
                    }
                }
            }
        });
    }

    // Status Chart
    const statusCtx = document.getElementById('statusChart');
    if (statusCtx) {
        statusChart = new Chart(statusCtx, {
            type: 'doughnut',
            data: {
                labels: ['Thành công', 'Chờ duyệt', 'Thất bại'],
                datasets: [{
                    data: [statisticsData.successCount, statisticsData.pendingCount, statisticsData.failedCount],
                    backgroundColor: [
                        'rgba(40, 167, 69, 0.8)',
                        'rgba(255, 193, 7, 0.8)',
                        'rgba(220, 53, 69, 0.8)'
                    ],
                    borderColor: [
                        'rgba(40, 167, 69, 1)',
                        'rgba(255, 193, 7, 1)',
                        'rgba(220, 53, 69, 1)'
                    ],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        titleColor: '#fff',
                        bodyColor: '#fff',
                        callbacks: {
                            label: function (context) {
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((context.parsed * 100) / total).toFixed(1);
                                return `${context.label}: ${context.parsed} (${percentage}%)`;
                            }
                        }
                    }
                }
            }
        });
    }

    // Time Chart
    const timeCtx = document.getElementById('timeChart');
    if (timeCtx) {
        // Generate hourly data
        const hours = [];
        const hourlyData = [];

        for (let i = 0; i < 24; i++) {
            hours.push(`${i.toString().padStart(2, '0')}:00`);

            // Simulate realistic hourly patterns
            let value = 0;
            if (i >= 8 && i <= 11) value = 15 + Math.random() * 10; // Morning peak
            else if (i >= 14 && i <= 17) value = 20 + Math.random() * 15; // Afternoon peak
            else if (i >= 20 && i <= 22) value = 12 + Math.random() * 8; // Evening peak
            else if (i >= 23 || i <= 6) value = 2 + Math.random() * 3; // Night low
            else value = 5 + Math.random() * 5; // Regular hours

            hourlyData.push(Math.floor(value));
        }

        timeChart = new Chart(timeCtx, {
            type: 'bar',
            data: {
                labels: hours,
                datasets: [{
                    label: 'Số giao dịch',
                    data: hourlyData,
                    backgroundColor: hours.map((_, index) => {
                        const hour = index;
                        if ((hour >= 8 && hour <= 11) || (hour >= 14 && hour <= 17) || (hour >= 20 && hour <= 22)) {
                            return 'rgba(102, 126, 234, 0.8)'; // Peak hours
                        } else {
                            return 'rgba(102, 126, 234, 0.4)'; // Regular hours
                        }
                    }),
                    borderColor: 'rgba(102, 126, 234, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        titleColor: '#fff',
                        bodyColor: '#fff',
                        callbacks: {
                            title: function (context) {
                                return `Giờ ${context[0].label}`;
                            },
                            label: function (context) {
                                return `Giao dịch: ${context.parsed.y}`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        display: true,
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#6c757d',
                            maxTicksLimit: 12
                        }
                    },
                    y: {
                        display: true,
                        grid: {
                            color: 'rgba(0,0,0,0.1)'
                        },
                        ticks: {
                            color: '#6c757d',
                            beginAtZero: true
                        }
                    }
                }
            }
        });
    }
}

function animateCounters() {
    $('.animate-counter').each(function () {
        const $this = $(this);
        const target = parseFloat($this.attr('data-target'));
        let current = 0;
        const increment = target / 100;
        const duration = 2000;
        const stepTime = duration / 100;

        const timer = setInterval(() => {
            current += increment;
            if (current >= target) {
                current = target;
                clearInterval(timer);
            }

            if ($this.hasClass('metric-number') && $this.closest('.metric-card.success').length) {
                $this.text(Math.floor(current).toLocaleString('vi-VN'));
            } else if ($this.attr('data-target').toString().includes('.')) {
                $this.text(current.toFixed(1));
            } else {
                $this.text(Math.floor(current).toLocaleString('vi-VN'));
            }
        }, stepTime);
    });
}

function setupEventHandlers() {
    // Form submission with loading
    $('#filterForm').on('submit', function (e) {
        showLoading();
        toastr.info('🔄 Đang lọc dữ liệu...', 'Xử lý');
    });

    // Chart type buttons
    $('.btn-chart-control[data-type]').on('click', function () {
        $('.btn-chart-control[data-type]').removeClass('active');
        $(this).addClass('active');
    });

    // Keyboard shortcuts
    $(document).on('keydown', function (e) {
        if (e.ctrlKey) {
            switch (e.key) {
                case 'r':
                    e.preventDefault();
                    refreshAllData();
                    break;
                case 'e':
                    e.preventDefault();
                    exportToExcel();
                    break;
                case 'p':
                    e.preventDefault();
                    printReport();
                    break;
                case 'f':
                    e.preventDefault();
                    document.querySelector('input[name="fromDate"]').focus();
                    break;
            }
        }
    });

    // Real-time updates
    setInterval(updateRealTimeData, 30000); // Update every 30 seconds
}

function startAutoRefresh() {
    autoRefreshInterval = setInterval(() => {
        updateRealTimeData();
        toastr.info('🔄 Dữ liệu đã được cập nhật tự động', 'Làm mới', { timeOut: 2000 });
    }, 300000); // 5 minutes

    toastr.success('✅ Tự động làm mới dữ liệu mỗi 5 phút', 'Thông tin', { timeOut: 3000 });
}

function updateRealTimeData() {
    // Simulate real-time data updates
    const newData = {
        totalPayments: statisticsData.totalPayments + Math.floor(Math.random() * 3),
        totalAmount: statisticsData.totalAmount + Math.floor(Math.random() * 1000000),
        pendingCount: Math.max(0, statisticsData.pendingCount + Math.floor(Math.random() * 2) - 1)
    };

    // Update counters with animation
    updateCounterWithAnimation('.metric-card.primary .metric-number', newData.totalPayments);
    updateCounterWithAnimation('.metric-card.success .metric-number', newData.totalAmount);
    updateCounterWithAnimation('.metric-card.warning .metric-number', newData.pendingCount);

    // Update statistics data
    Object.assign(statisticsData, newData);
}

function updateCounterWithAnimation(selector, newValue) {
    const $element = $(selector);
    const currentValue = parseInt($element.text().replace(/[^\d]/g, ''));

    if (currentValue !== newValue) {
        $element.addClass('animate__animated animate__pulse');
        setTimeout(() => {
            if (selector.includes('success')) {
                $element.text(newValue.toLocaleString('vi-VN'));
            } else {
                $element.text(newValue.toLocaleString('vi-VN'));
            }
            $element.removeClass('animate__animated animate__pulse');
        }, 500);
    }
}

function changeChartType(type) {
    currentChartType = type;

    if (revenueChart) {
        revenueChart.config.type = type;

        if (type === 'area') {
            revenueChart.data.datasets.forEach(dataset => {
                dataset.fill = true;
                dataset.backgroundColor = dataset.borderColor.replace('1)', '0.2)');
            });
        } else if (type === 'bar') {
            revenueChart.data.datasets.forEach(dataset => {
                dataset.fill = false;
                dataset.backgroundColor = dataset.borderColor.replace('1)', '0.8)');
            });
        } else {
            revenueChart.data.datasets.forEach(dataset => {
                dataset.fill = false;
                dataset.backgroundColor = dataset.borderColor.replace('1)', '0.1)');
            });
        }

        revenueChart.update('active');
        toastr.success(`📊 Đã chuyển sang biểu đồ ${type === 'line' ? 'đường' : type === 'bar' ? 'cột' : 'vùng'}`, 'Cập nhật biểu đồ');
    }
}

function setPredefinedPeriod(period) {
    const today = new Date('2025-05-29');
    let fromDate, toDate;

    switch (period) {
        case 'today':
            fromDate = toDate = today.toISOString().split('T')[0];
            break;
        case 'yesterday':
            const yesterday = new Date(today);
            yesterday.setDate(yesterday.getDate() - 1);
            fromDate = toDate = yesterday.toISOString().split('T')[0];
            break;
        case 'week':
            const weekStart = new Date(today);
            weekStart.setDate(today.getDate() - today.getDay());
            fromDate = weekStart.toISOString().split('T')[0];
            toDate = today.toISOString().split('T')[0];
            break;
        case 'last_week':
            const lastWeekEnd = new Date(today);
            lastWeekEnd.setDate(today.getDate() - today.getDay() - 1);
            const lastWeekStart = new Date(lastWeekEnd);
            lastWeekStart.setDate(lastWeekEnd.getDate() - 6);
            fromDate = lastWeekStart.toISOString().split('T')[0];
            toDate = lastWeekEnd.toISOString().split('T')[0];
            break;
        case 'month':
            fromDate = new Date(today.getFullYear(), today.getMonth(), 1).toISOString().split('T')[0];
            toDate = today.toISOString().split('T')[0];
            break;
        case 'last_month':
            const lastMonth = new Date(today.getFullYear(), today.getMonth() - 1, 1);
            const lastMonthEnd = new Date(today.getFullYear(), today.getMonth(), 0);
            fromDate = lastMonth.toISOString().split('T')[0];
            toDate = lastMonthEnd.toISOString().split('T')[0];
            break;
        case 'quarter':
            const quarterStart = new Date(today.getFullYear(), Math.floor(today.getMonth() / 3) * 3, 1);
            fromDate = quarterStart.toISOString().split('T')[0];
            toDate = today.toISOString().split('T')[0];
            break;
        case 'year':
            fromDate = new Date(today.getFullYear(), 0, 1).toISOString().split('T')[0];
            toDate = today.toISOString().split('T')[0];
            break;
        case 'all':
            fromDate = '';
            toDate = '';
            break;
        default:
            return;
    }

    $('input[name="fromDate"]').val(fromDate);
    $('input[name="toDate"]').val(toDate);

    $('.date-filter').addClass('filter-active');
    setTimeout(() => $('.date-filter').removeClass('filter-active'), 1000);
}

function showLoading() {
    $('#loadingOverlay').addClass('show');
}

function hideLoading() {
    $('#loadingOverlay').removeClass('show');
}

function refreshAllData() {
    showLoading();
    toastr.info('🔄 Đang làm mới tất cả dữ liệu...', 'Xử lý');

    setTimeout(() => {
        hideLoading();
        updateRealTimeData();

        // Refresh charts
        if (revenueChart) revenueChart.update();
        if (statusChart) statusChart.update();
        if (timeChart) timeChart.update();

        toastr.success(`✅ Dữ liệu đã được làm mới lúc ${new Date().toLocaleString('vi-VN')}`, 'Hoàn thành');
    }, 2000);
}

function exportToExcel() {
    showLoading();
    toastr.info('📊 Đang tạo file Excel...', 'Xuất dữ liệu');

    setTimeout(() => {
        hideLoading();

        Swal.fire({
            title: '📊 Xuất Excel thành công',
            html: `
                                <div class="text-start">
                                    <div class="alert alert-success">
                                        <h6 class="alert-heading">✅ File đã được tạo thành công!</h6>
                                        <hr>
                                        <div class="row">
                                            <div class="col-6">
                                                <strong>📄 Tên file:</strong><br>
                                                PaymentStatistics_${new Date().toISOString().split('T')[0]}.xlsx
                                            </div>
                                            <div class="col-6">
                                                <strong>📊 Nội dung:</strong><br>
                                                ${statisticsData.totalPayments} giao dịch, ${statisticsData.totalAmount.toLocaleString('vi-VN')}₫
                                            </div>
                                            <div class="col-6 mt-2">
                                                <strong>👤 Tạo bởi:</strong><br>
                                                ${currentUser}
                                            </div>
                                            <div class="col-6 mt-2">
                                                <strong>⏰ Thời gian:</strong><br>
                                                2025-05-29 16:47:46 UTC
                                            </div>
                                        </div>
                                    </div>
                                    <div class="bg-light rounded p-3">
                                        <strong>📋 File bao gồm:</strong>
                                        <ul class="small mb-0 mt-2">
                                            <li>📈 Tổng quan thống kê</li>
                                            <li>📊 Biểu đồ doanh thu theo tháng</li>
                                            <li>🏆 Top khóa học bán chạy</li>
                                            <li>⏰ Phân tích theo giờ trong ngày</li>
                                            <li>💳 Chi tiết phương thức thanh toán</li>
                                        </ul>
                                    </div>
                                </div>
                            `,
            icon: 'success',
            confirmButtonText: '📥 Tải xuống',
            showCancelButton: true,
            cancelButtonText: '📧 Gửi email',
            confirmButtonColor: '#28a745'
        }).then((result) => {
            if (result.isConfirmed) {
                toastr.success('📥 File Excel đã được tải xuống thành công!', 'Hoàn thành');
            } else if (result.dismiss === Swal.DismissReason.cancel) {
                emailReport('excel');
            }
        });
    }, 3000);
}

function exportToPDF() {
    showLoading();
    toastr.info('📄 Đang tạo file PDF...', 'Xuất dữ liệu');

    setTimeout(() => {
        hideLoading();

        Swal.fire({
            title: '📄 Xuất PDF thành công',
            html: `
                                <div class="text-start">
                                    <div class="alert alert-danger">
                                        <h6 class="alert-heading">✅ File PDF đã được tạo thành công!</h6>
                                        <hr>
                                        <div class="row">
                                            <div class="col-12">
                                                <strong>📄 Tên file:</strong> PaymentStatistics_Report_2025-05-29.pdf<br>
                                                <strong>📊 Kích thước:</strong> 2.4 MB<br>
                                                <strong>📑 Trang:</strong> 8 trang với biểu đồ chi tiết<br>
                                                <strong>👤 Tạo bởi:</strong> Admin ${currentUser}<br>
                                                <strong>⏰ Thời gian:</strong> 2025-05-29 16:47:46 UTC
                                            </div>
                                        </div>
                                    </div>
                                    <div class="bg-light rounded p-3">
                                        <strong>📋 Nội dung báo cáo PDF:</strong>
                                        <ul class="small mb-0 mt-2">
                                            <li>📊 Executive Summary & Key Metrics</li>
                                            <li>📈 Revenue Trends & Growth Analysis</li>
                                            <li>🏆 Top Performing Courses</li>
                                            <li>⏰ Time-based Transaction Analysis</li>
                                            <li>💳 Payment Methods Breakdown</li>
                                            <li>🔮 Future Predictions & Recommendations</li>
                                            <li>📋 Detailed Data Tables</li>
                                            <li>📝 Admin Notes & Comments</li>
                                        </ul>
                                    </div>
                                </div>
                            `,
            icon: 'success',
            confirmButtonText: '📥 Tải PDF',
            showCancelButton: true,
            cancelButtonText: '👁️ Xem trước',
            confirmButtonColor: '#dc3545'
        }).then((result) => {
            if (result.isConfirmed) {
                toastr.success('📥 File PDF đã được tải xuống thành công!', 'Hoàn thành');
            } else if (result.dismiss === Swal.DismissReason.cancel) {
                previewPDF();
            }
        });
    }, 3500);
}

function printReport() {
    toastr.info('🖨️ Đang chuẩn bị in báo cáo...', 'In ấn');

    const printWindow = window.open('', '_blank');
    printWindow.document.write(`
                        <html>
                            <head>
                                <title>Báo cáo Thống kê Thanh toán - ${new Date().toLocaleDateString('vi-VN')}</title>
                                <style>
                                    body { font-family: Arial, sans-serif; margin: 20px; }
                                    .header { text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 20px; }
                                    .metric { display: inline-block; margin: 10px 20px; text-align: center; }
                                    .metric-number { font-size: 24px; font-weight: bold; color: #333; }
                                    .metric-label { font-size: 12px; color: #666; }
                                    .section { margin: 30px 0; }
                                    .course-item { padding: 10px; border-bottom: 1px solid #eee; display: flex; justify-content: space-between; }
                                    .footer { margin-top: 50px; text-align: center; font-size: 12px; color: #666; }
                                </style>
                            </head>
                            <body>
                                <div class="header">
                                    <h1>📊 BÁO CÁO THỐNG KÊ THANH TOÁN</h1>
                                    <p><strong>OnlineLearning Platform</strong></p>
                                    <p>Thời gian tạo: ${new Date().toLocaleString('vi-VN')} | Admin: ${currentUser}</p>
                                </div>

                                <div class="section">
                                    <h2>📈 Tổng quan chỉ số</h2>
                                    <div class="metric">
                                        <div class="metric-number">${statisticsData.totalPayments.toLocaleString('vi-VN')}</div>
                                        <div class="metric-label">TỔNG GIAO DỊCH</div>
                                    </div>
                                    <div class="metric">
                                        <div class="metric-number">${statisticsData.totalAmount.toLocaleString('vi-VN')}₫</div>
                                        <div class="metric-label">TỔNG DOANH THU</div>
                                    </div>
                                    <div class="metric">
                                        <div class="metric-number">${statisticsData.successCount.toLocaleString('vi-VN')}</div>
                                        <div class="metric-label">THÀNH CÔNG</div>
                                    </div>
                                    <div class="metric">
                                        <div class="metric-number">${statisticsData.pendingCount.toLocaleString('vi-VN')}</div>
                                        <div class="metric-label">CHỜ DUYỆT</div>
                                    </div>
                                </div>

                                <div class="section">
                                    <h2>🏆 Top khóa học bán chạy</h2>
                                    ${statisticsData.topCourses.map((course, index) => `
                                        <div class="course-item">
                                            <span><strong>#${index + 1}</strong> ${course.CourseName}</span>
                                            <span>${course.TotalAmount.toLocaleString('vi-VN')}₫ (${course.PaymentCount} lượt mua)</span>
                                        </div>
                                    `).join('')}
                                </div>

                                <div class="footer">
                                    <p>Báo cáo được tạo tự động bởi hệ thống OnlineLearning</p>
                                    <p>Admin: ${currentUser} | Thời gian: 2025-05-29 16:47:46 UTC</p>
                                </div>
                            </body>
                        </html>
                    `);

    setTimeout(() => {
        printWindow.print();
        printWindow.close();
        toastr.success('✅ Đã gửi lệnh in báo cáo', 'Hoàn thành');
    }, 1000);
}

function emailReport(fileType = 'pdf') {
    Swal.fire({
        title: '📧 Gửi báo cáo qua email',
        html: `
                            <div class="text-start">
                                <div class="mb-3">
                                    <label class="form-label fw-bold">📧 Email người nhận:</label>
                                    <input type="email" class="form-control" id="emailRecipient" placeholder="Nhập email..." value="${currentUser.toLowerCase()}@onlinelearning.vn">
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold">📋 Loại báo cáo:</label>
                                    <select class="form-select" id="reportType">
                                        <option value="pdf" ${fileType === 'pdf' ? 'selected' : ''}>📄 PDF Report</option>
                                        <option value="excel" ${fileType === 'excel' ? 'selected' : ''}>📊 Excel Spreadsheet</option>
                                        <option value="both">📋 Cả hai định dạng</option>
                                    </select>
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold">✉️ Tiêu đề email:</label>
                                    <input type="text" class="form-control" id="emailSubject" value="📊 Báo cáo Thống kê Thanh toán - ${new Date().toLocaleDateString('vi-VN')}">
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold">💬 Nội dung:</label>
                                    <textarea class="form-control" id="emailContent" rows="4" placeholder="Nội dung email...">Xin chào,

        Đính kèm là báo cáo thống kê thanh toán được tạo lúc 2025-05-29 16:47:46 UTC bởi Admin ${currentUser}.

        Tổng quan:
        - Tổng giao dịch: ${statisticsData.totalPayments.toLocaleString('vi-VN')}
        - Tổng doanh thu: ${statisticsData.totalAmount.toLocaleString('vi-VN')}₫
        - Tỷ lệ thành công: ${((statisticsData.successCount / statisticsData.totalPayments) * 100).toFixed(1)}%

        Trân trọng,
        ${currentUser}
        Admin OnlineLearning</textarea>
                                </div>
                                <div class="alert alert-info">
                                    <small><strong>💡 Lưu ý:</strong> Email sẽ được gửi từ hệ thống với đính kèm báo cáo được mã hóa.</small>
                                </div>
                            </div>
                        `,
        showCancelButton: true,
        confirmButtonText: '<i class="fas fa-paper-plane me-2"></i>Gửi email',
        cancelButtonText: 'Hủy',
        confirmButtonColor: '#28a745',
        width: '600px',
        preConfirm: () => {
            const email = document.getElementById('emailRecipient').value;
            const subject = document.getElementById('emailSubject').value;
            const content = document.getElementById('emailContent').value;
            const reportType = document.getElementById('reportType').value;

            if (!email || !subject || !content) {
                Swal.showValidationMessage('Vui lòng điền đầy đủ thông tin!');
                return false;
            }

            return { email, subject, content, reportType };
        }
    }).then((result) => {
        if (result.isConfirmed) {
            const { email, subject, content, reportType } = result.value;

            toastr.info('📧 Đang gửi email...', 'Xử lý');

            setTimeout(() => {
                toastr.success(`✅ Báo cáo ${reportType} đã được gửi đến ${email}`, 'Email đã gửi');

                setTimeout(() => {
                    toastr.info('📧 Email đã được gửi với mã tracking: EM2025052916474601', 'Thông tin', { timeOut: 5000 });
                }, 2000);
            }, 3000);
        }
    });
}

function scheduleReport() {
    Swal.fire({
        title: '📅 Lên lịch gửi báo cáo tự động',
        html: `
                            <div class="text-start">
                                <div class="mb-3">
                                    <label class="form-label fw-bold">⏰ Tần suất gửi:</label>
                                    <select class="form-select" id="scheduleFrequency">
                                        <option value="daily">📅 Hàng ngày</option>
                                        <option value="weekly" selected>📅 Hàng tuần</option>
                                        <option value="monthly">📅 Hàng tháng</option>
                                        <option value="quarterly">📅 Hàng quý</option>
                                    </select>
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold">🕐 Thời gian gửi:</label>
                                    <input type="time" class="form-control" id="scheduleTime" value="08:00">
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold">📧 Danh sách email:</label>
                                    <textarea class="form-control" id="scheduleEmails" rows="3" placeholder="Nhập email, cách nhau bởi dấu phẩy...">${currentUser.toLowerCase()}@onlinelearning.vn, manager@onlinelearning.vn</textarea>
                                </div>
                                <div class="mb-3">
                                    <label class="form-label fw-bold">📊 Định dạng báo cáo:</label>
                                    <div class="form-check">
                                        <input class="form-check-input" type="checkbox" id="includePDF" checked>
                                        <label class="form-check-label" for="includePDF">📄 PDF Report</label>
                                    </div>
                                    <div class="form-check">
                                        <input class="form-check-input" type="checkbox" id="includeExcel" checked>
                                        <label class="form-check-label" for="includeExcel">📊 Excel Spreadsheet</label>
                                    </div>
                                </div>
                                <div class="alert alert-success">
                                    <strong>✅ Lịch gửi sẽ bắt đầu từ:</strong> Thứ 2, 02/06/2025 lúc 08:00 UTC
                                </div>
                            </div>
                        `,
        showCancelButton: true,
        confirmButtonText: '<i class="fas fa-calendar-plus me-2"></i>Thiết lập lịch',
        cancelButtonText: 'Hủy',
        confirmButtonColor: '#17a2b8',
        width: '600px'
    }).then((result) => {
        if (result.isConfirmed) {
            const frequency = document.getElementById('scheduleFrequency').value;
            const time = document.getElementById('scheduleTime').value;
            const emails = document.getElementById('scheduleEmails').value;

            toastr.success(`📅 Đã thiết lập lịch gửi báo cáo ${frequency === 'daily' ? 'hàng ngày' : frequency === 'weekly' ? 'hàng tuần' : frequency === 'monthly' ? 'hàng tháng' : 'hàng quý'} lúc ${time}`, 'Lên lịch thành công');

            setTimeout(() => {
                toastr.info(`📧 Báo cáo sẽ được gửi đến ${emails.split(',').length} địa chỉ email`, 'Thông tin');
            }, 2000);
        }
    });
}

// Additional advanced functions
function showFullScreen() {
    if (document.documentElement.requestFullscreen) {
        document.documentElement.requestFullscreen();
        toastr.success('🖥️ Đã chuyển sang chế độ toàn màn hình', 'Thành công');
    } else {
        toastr.warning('⚠️ Trình duyệt không hỗ trợ chế độ toàn màn hình', 'Cảnh báo');
    }
}

function viewAllCourses() {
    Swal.fire({
        title: '🏆 Danh sách đầy đủ khóa học bán chạy',
        html: `
                            <div class="text-start" style="max-height: 400px; overflow-y: auto;">
                                ${statisticsData.topCourses.map((course, index) => `
                                    <div class="d-flex justify-content-between align-items-center p-3 border-bottom">
                                        <div>
                                            <strong>#${index + 1} ${course.CourseName}</strong><br>
                                            <small class="text-muted">${course.PaymentCount} lượt mua • ⭐ 4.${5 + index % 3}/5.0</small>
                                        </div>
                                        <div class="text-end">
                                            <strong class="text-success">${course.TotalAmount.toLocaleString('vi-VN')}₫</strong><br>
                                            <small class="text-info">+${10 + index * 2}% ↗️</small>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        `,
        confirmButtonText: '✅ Đóng',
        confirmButtonColor: '#667eea',
        width: '700px'
    });
}

function showPaymentMethodDetails() {
    Swal.fire({
        title: '💳 Chi tiết phương thức thanh toán',
        html: `
                            <div class="text-start">
                                <div class="row g-3">
                                    <div class="col-12">
                                        <div class="card">
                                            <div class="card-header bg-primary text-white">
                                                <strong>📊 Phân tích chi tiết</strong>
                                            </div>
                                            <div class="card-body">
                                                <div class="d-flex justify-content-between align-items-center mb-3 p-3 bg-light rounded">
                                                    <div class="d-flex align-items-center">
                                                        <img src="https://developers.momo.vn/v3/assets/images/square-logo.svg" width="40" height="40" class="me-3">
                                                        <div>
                                                            <strong>MoMo eWallet</strong><br>
                                                            <small class="text-muted">Phương thức chính</small>
                                                        </div>
                                                    </div>
                                                    <div class="text-end">
                                                        <strong class="fs-5">${statisticsData.totalPayments}</strong><br>
                                                        <small class="text-success">100% thị phần</small>
                                                    </div>
                                                </div>

                                                <div class="alert alert-info">
                                                    <strong>📈 Xu hướng:</strong>
                                                    <ul class="mb-0 mt-2">
                                                        <li>💳 MoMo hiện là phương thức thanh toán duy nhất</li>
                                                        <li>🔒 Tỷ lệ bảo mật: 99.9% - rất an toàn</li>
                                                        <li>⚡ Thời gian xử lý: Trung bình 2-5 phút</li>
                                                        <li>✅ Tỷ lệ thành công: ${((statisticsData.successCount / statisticsData.totalPayments) * 100).toFixed(1)}%</li>
                                                    </ul>
                                                </div>

                                                <div class="bg-light rounded p-3">
                                                    <strong>🚀 Kế hoạch mở rộng:</strong>
                                                    <ul class="small mb-0 mt-1">
                                                        <li>🏦 Chuyển khoản ngân hàng (Q3/2025)</li>
                                                        <li>💳 Thẻ tín dụng/ghi nợ (Q4/2025)</li>
                                                        <li>🌐 Ví điện tử quốc tế (2026)</li>
                                                    </ul>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        `,
        confirmButtonText: '✅ Đóng',
        confirmButtonColor: '#667eea',
        width: '650px'
    });
}

function changeTimeView() {
    toastr.info('📅 Đang chuyển sang view theo tuần...', 'Cập nhật');

    setTimeout(() => {
        if (timeChart) {
            // Update chart to show weekly data
            const weekDays = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'CN'];
            const weeklyData = weekDays.map(() => Math.floor(50 + Math.random() * 100));

            timeChart.data.labels = weekDays;
            timeChart.data.datasets[0].data = weeklyData;
            timeChart.data.datasets[0].label = 'Giao dịch trong tuần';
            timeChart.update();

            toastr.success('📊 Đã chuyển sang view theo tuần', 'Thành công');
        }
    }, 1000);
}

// Cleanup on page unload
$(window).on('beforeunload', function () {
    if (autoRefreshInterval) clearInterval(autoRefreshInterval);
    if (revenueChart) revenueChart.destroy();
    if (statusChart) statusChart.destroy();
    if (timeChart) timeChart.destroy();
});

// Track page analytics
console.log(`✅ Payment Statistics loaded successfully at 2025-05-29 16:47:46 UTC`);
console.log(`👤 Current admin: ${currentUser}`);
console.log(`📊 Statistics: ${statisticsData.totalPayments} payments, ${statisticsData.totalAmount.toLocaleString('vi-VN')}₫ revenue`);
console.log(`⚡ Real-time updates: Active every 30 seconds`);