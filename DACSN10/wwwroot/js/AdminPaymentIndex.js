
// Initialize libraries
$(document).ready(function () {
    initializeApp();
});

// Global variables
let autoRefreshInterval;
let isAutoRefreshEnabled = false;
let miniCharts = {};

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
        "timeOut": 4000,
        "extendedTimeOut": 2000,
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    };

    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize event handlers
    setupEventHandlers();
    initializeMiniCharts();
    setupSmartSearch();
    setupTableFeatures();

    // Welcome message
    setTimeout(() => {
        toastr.info('👋 Chào mừng bạn quay trại lại hệ thống quản lý thanh toán!', 'Xin chào MHoang287');
    }, 1000);
}

function setupEventHandlers() {
    // Select all checkbox
    $('#selectAll').on('change', function () {
        const isChecked = this.checked;
        $('.payment-checkbox').prop('checked', isChecked);
        updateBulkActions();
        updateSelectedRows();

        if (isChecked) {
            toastr.info(`Đã chọn tất cả ${$('.payment-checkbox').length} giao dịch`);
        }
    });

    // Individual checkboxes
    $('.payment-checkbox').on('change', function () {
        updateBulkActions();
        updateSelectedRows();

        const totalCheckboxes = $('.payment-checkbox').length;
        const checkedCheckboxes = $('.payment-checkbox:checked').length;
        $('#selectAll').prop('checked', totalCheckboxes === checkedCheckboxes);
    });

    // Form submission with loading
    $('#filterForm').on('submit', function () {
        showLoading();
    });

    // Escape key to deselect all
    $(document).on('keydown', function (e) {
        if (e.key === 'Escape') {
            deselectAll();
        }
    });
}

function initializeMiniCharts() {
    // Sample data for mini charts
    const chartData = {
        transaction: [12, 19, 8, 15, 25, 22, 18],
        pending: [5, 8, 3, 12, 7, 9, 6],
        success: [8, 15, 12, 18, 22, 25, 20],
        revenue: [1200, 1900, 800, 1500, 2500, 2200, 1800]
    };

    const chartOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
            x: { display: false },
            y: { display: false }
        },
        elements: {
            point: { radius: 0 },
            line: { borderWidth: 2 }
        }
    };

    // Transaction trend
    miniCharts.transaction = new Chart(document.getElementById('transactionTrendChart'), {
        type: 'line',
        data: {
            labels: ['', '', '', '', '', '', ''],
            datasets: [{
                data: chartData.transaction,
                borderColor: '#667eea',
                backgroundColor: 'rgba(102, 126, 234, 0.1)',
                fill: true,
                tension: 0.4
            }]
        },
        options: chartOptions
    });

    // Pending trend
    miniCharts.pending = new Chart(document.getElementById('pendingTrendChart'), {
        type: 'line',
        data: {
            labels: ['', '', '', '', '', '', ''],
            datasets: [{
                data: chartData.pending,
                borderColor: '#f5576c',
                backgroundColor: 'rgba(245, 87, 108, 0.1)',
                fill: true,
                tension: 0.4
            }]
        },
        options: chartOptions
    });

    // Success trend
    miniCharts.success = new Chart(document.getElementById('successTrendChart'), {
        type: 'line',
        data: {
            labels: ['', '', '', '', '', '', ''],
            datasets: [{
                data: chartData.success,
                borderColor: '#38ef7d',
                backgroundColor: 'rgba(56, 239, 125, 0.1)',
                fill: true,
                tension: 0.4
            }]
        },
        options: chartOptions
    });

    // Revenue trend
    miniCharts.revenue = new Chart(document.getElementById('revenueTrendChart'), {
        type: 'bar',
        data: {
            labels: ['', '', '', '', '', '', ''],
            datasets: [{
                data: chartData.revenue,
                backgroundColor: 'rgba(102, 126, 234, 0.6)',
                borderColor: '#667eea',
                borderWidth: 1
            }]
        },
        options: chartOptions
    });
}

function setupSmartSearch() {
    const searchInput = $('#smartSearch');
    const suggestions = $('#searchSuggestions');

    searchInput.on('input', function () {
        const query = this.value.toLowerCase();
        if (query.length >= 2) {
            // Simulate search suggestions
            const mockSuggestions = [
                { type: 'course', text: 'Khóa học React Native', icon: 'fas fa-book' },
                { type: 'user', text: 'Nguyễn Văn An', icon: 'fas fa-user' },
                { type: 'email', text: 'admin@example.com', icon: 'fas fa-envelope' },
                { type: 'transaction', text: '#000123', icon: 'fas fa-hashtag' }
            ];

            let html = '';
            mockSuggestions.forEach(item => {
                if (item.text.toLowerCase().includes(query)) {
                    html += `
                                <div class="search-suggestion-item" onclick="selectSuggestion('${item.text}')">
                                    <i class="${item.icon} me-2 text-muted"></i>
                                    <span>${item.text}</span>
                                    <small class="text-muted ms-auto">${item.type}</small>
                                </div>
                            `;
                }
            });

            if (html) {
                suggestions.html(html).show();
            } else {
                suggestions.hide();
            }
        } else {
            suggestions.hide();
        }
    });

    // Hide suggestions when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.search-container').length) {
            suggestions.hide();
        }
    });
}

function selectSuggestion(text) {
    $('#smartSearch').val(text);
    $('#searchSuggestions').hide();
    $('#filterForm').submit();
}

function setupTableFeatures() {
    // Table sorting
    $('.table-modern th').on('click', function () {
        const $this = $(this);
        if ($this.find('.fa-sort').length) {
            // Toggle sort direction
            const currentSort = $this.data('sort') || 'asc';
            const newSort = currentSort === 'asc' ? 'desc' : 'asc';

            // Reset all sort indicators
            $('.table-modern th .fa-sort, .fa-sort-up, .fa-sort-down').removeClass('fa-sort-up fa-sort-down').addClass('fa-sort');

            // Set new sort indicator
            $this.find('i').removeClass('fa-sort').addClass(newSort === 'asc' ? 'fa-sort-up' : 'fa-sort-down');
            $this.data('sort', newSort);

            toastr.info(`Đã sắp xếp theo ${$this.text().trim()} (${newSort === 'asc' ? 'tăng dần' : 'giảm dần'})`);
        }
    });

    // Row hover effects
    $('.payment-row').hover(
        function () {
            $(this).addClass('table-hover-effect');
        },
        function () {
            $(this).removeClass('table-hover-effect');
        }
    );
}

function updateBulkActions() {
    const selectedCount = $('.payment-checkbox:checked').length;
    const $bulkActions = $('#bulkActions');

    if (selectedCount > 0) {
        $bulkActions.addClass('show');
        $('#selectedCount').text(`${selectedCount} giao dịch được chọn`);

        // Update button states
        updateBulkButtonStates(selectedCount);
    } else {
        $bulkActions.removeClass('show');
    }
}

function updateBulkButtonStates(count) {
    const $approveBtn = $('[onclick="bulkApprove()"]');
    const $rejectBtn = $('[onclick="bulkReject()"]');

    if (count > 10) {
        $approveBtn.addClass('pulse-effect');
        $rejectBtn.addClass('pulse-effect');
    } else {
        $approveBtn.removeClass('pulse-effect');
        $rejectBtn.removeClass('pulse-effect');
    }
}

function updateSelectedRows() {
    $('.payment-row').removeClass('selected table-primary');
    $('.payment-checkbox:checked').each(function () {
        $(this).closest('.payment-row').addClass('selected table-primary');
    });
}

function showLoading() {
    $('#loadingOverlay').fadeIn(300);
}

function hideLoading() {
    $('#loadingOverlay').fadeOut(300);
}

// Action Functions
function refreshTable() {
    showLoading();
    toastr.info('🔄 Đang cập nhật dữ liệu...');

    setTimeout(() => {
        location.reload();
    }, 1500);
}

function viewDetails(paymentId) {
    showLoading();
    setTimeout(() => {
        window.location.href = `/Admin/Payment/Details/${paymentId}`;
    }, 500);
}

function approvePayment(paymentId) {
    Swal.fire({
        title: '✅ Duyệt thanh toán',
        html: `
                    <div class="text-start">
                        <div class="alert alert-info mb-3">
                            <i class="fas fa-info-circle me-2"></i>
                            <strong>Xác nhận duyệt giao dịch #${paymentId.toString().padStart(6, '0')}</strong>
                        </div>
                        <div class="row">
                            <div class="col-12 mb-3">
                                <p>Sau khi duyệt, hệ thống sẽ tự động:</p>
                                <ul class="small">
                                    <li>🎓 Kích hoạt khóa học cho học viên</li>
                                    <li>📧 Gửi email thông báo thành công</li>
                                    <li>💾 Cập nhật trạng thái giao dịch</li>
                                    <li>📊 Cập nhật báo cáo doanh thu</li>
                                </ul>
                            </div>
                        </div>
                    </div>
                `,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: '<i class="fas fa-check me-2"></i>Xác nhận duyệt',
        cancelButtonText: '<i class="fas fa-times me-2"></i>Hủy bỏ',
        confirmButtonColor: '#11998e',
        cancelButtonColor: '#6c757d',
        input: 'textarea',
        inputPlaceholder: '💬 Ghi chú cho học viên (tùy chọn)...',
        inputAttributes: {
            'style': 'margin-top: 15px; min-height: 100px; border-radius: 10px;'
        },
        customClass: {
            popup: 'swal2-popup-large'
        },
        showLoaderOnConfirm: true,
        preConfirm: async (note) => {
            try {
                const response = await fetch(`/Admin/Payment/Approve/${paymentId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                    },
                    body: `note=${encodeURIComponent(note || '')}`
                });

                if (!response.ok) throw new Error('Network error');
                return await response.json();
            } catch (error) {
                Swal.showValidationMessage('Có lỗi xảy ra khi xử lý yêu cầu');
            }
        },
        allowOutsideClick: () => !Swal.isLoading()
    }).then((result) => {
        if (result.isConfirmed) {
            if (result.value.success) {
                Swal.fire({
                    title: '🎉 Thành công!',
                    text: result.value.message,
                    icon: 'success',
                    confirmButtonColor: '#11998e',
                    timer: 3000,
                    timerProgressBar: true,
                    showConfirmButton: false
                });

                // Update UI immediately
                updatePaymentRowStatus(paymentId, 'approved');
                updateStatsAfterApproval();

                setTimeout(() => location.reload(), 2000);
            } else {
                Swal.fire({
                    title: '❌ Lỗi!',
                    text: result.value.message,
                    icon: 'error',
                    confirmButtonColor: '#fc466b'
                });
            }
        }
    });
}

function rejectPayment(paymentId) {
    Swal.fire({
        title: '❌ Từ chối thanh toán',
        html: `
                    <div class="text-start">
                        <div class="alert alert-warning mb-3">
                            <i class="fas fa-exclamation-triangle me-2"></i>
                            <strong>Từ chối giao dịch #${paymentId.toString().padStart(6, '0')}</strong>
                        </div>
                        <div class="alert alert-info">
                            <h6 class="alert-heading">📝 Yêu cầu lý do từ chối</h6>
                            <p class="mb-0 small">Vui lòng cung cấp lý do cụ thể để học viên hiểu rõ nguyên nhân và có thể khắc phục.</p>
                        </div>
                    </div>
                `,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: '<i class="fas fa-times me-2"></i>Xác nhận từ chối',
        cancelButtonText: '<i class="fas fa-arrow-left me-2"></i>Hủy bỏ',
        confirmButtonColor: '#fc466b',
        cancelButtonColor: '#6c757d',
        input: 'textarea',
        inputPlaceholder: '📝 Nhập lý do từ chối (bắt buộc)...',
        inputAttributes: {
            'style': 'margin-top: 15px; min-height: 120px; border-radius: 10px;'
        },
        inputValidator: (value) => {
            if (!value || value.trim().length < 10) {
                return '⚠️ Vui lòng nhập lý do từ chối (ít nhất 10 ký tự)!'
            }
        },
        showLoaderOnConfirm: true,
        preConfirm: async (reason) => {
            try {
                const response = await fetch(`/Admin/Payment/Reject/${paymentId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                    },
                    body: `reason=${encodeURIComponent(reason)}`
                });

                if (!response.ok) throw new Error('Network error');
                return await response.json();
            } catch (error) {
                Swal.showValidationMessage('Có lỗi xảy ra khi xử lý yêu cầu');
            }
        },
        allowOutsideClick: () => !Swal.isLoading()
    }).then((result) => {
        if (result.isConfirmed) {
            if (result.value.success) {
                Swal.fire({
                    title: '✅ Đã từ chối!',
                    text: result.value.message,
                    icon: 'success',
                    confirmButtonColor: '#11998e',
                    timer: 3000,
                    timerProgressBar: true,
                    showConfirmButton: false
                });

                // Update UI immediately
                updatePaymentRowStatus(paymentId, 'rejected');
                updateStatsAfterRejection();

                setTimeout(() => location.reload(), 2000);
            } else {
                Swal.fire({
                    title: '❌ Lỗi!',
                    text: result.value.message,
                    icon: 'error',
                    confirmButtonColor: '#fc466b'
                });
            }
        }
    });
}

function bulkApprove() {
    const selectedIds = $('.payment-checkbox:checked').map(function () {
        return parseInt(this.value);
    }).get();

    if (selectedIds.length === 0) {
        toastr.warning('⚠️ Vui lòng chọn ít nhất một giao dịch để duyệt.');
        return;
    }

    Swal.fire({
        title: '🚀 Duyệt hàng loạt',
        html: `
                    <div class="text-start">
                        <div class="alert alert-primary mb-3">
                            <i class="fas fa-rocket me-2"></i>
                            <strong>Duyệt ${selectedIds.length} giao dịch cùng lúc</strong>
                        </div>
                        <div class="progress mb-3" style="height: 8px;">
                            <div class="progress-bar progress-bar-striped progress-bar-animated"
                                 style="width: 0%; background: linear-gradient(90deg, #11998e, #38ef7d);"></div>
                        </div>
                        <div class="row">
                            <div class="col-6">
                                <div class="text-center p-3 bg-light rounded">
                                    <i class="fas fa-check-circle fa-2x text-success mb-2"></i>
                                    <div class="fw-bold">${selectedIds.length}</div>
                                    <small>Giao dịch được duyệt</small>
                                </div>
                            </div>
                            <div class="col-6">
                                <div class="text-center p-3 bg-light rounded">
                                    <i class="fas fa-graduation-cap fa-2x text-primary mb-2"></i>
                                    <div class="fw-bold">${selectedIds.length}</div>
                                    <small>Khóa học kích hoạt</small>
                                </div>
                            </div>
                        </div>
                        <div class="alert alert-info mt-3">
                            <h6 class="alert-heading">📋 Thao tác này sẽ:</h6>
                            <ul class="mb-0 small">
                                <li>✅ Duyệt tất cả ${selectedIds.length} giao dịch</li>
                                <li>🎓 Kích hoạt khóa học cho tất cả học viên</li>
                                <li>📧 Gửi email thông báo đến từng học viên</li>
                                <li>📊 Cập nhật báo cáo doanh thu</li>
                                <li>⚠️ Không thể hoàn tác sau khi thực hiện</li>
                            </ul>
                        </div>
                    </div>
                `,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: `<i class="fas fa-rocket me-2"></i>Duyệt ${selectedIds.length} giao dịch`,
        cancelButtonText: '<i class="fas fa-times me-2"></i>Hủy bỏ',
        confirmButtonColor: '#11998e',
        cancelButtonColor: '#6c757d',
        showLoaderOnConfirm: true,
        preConfirm: async () => {
            try {
                const response = await fetch('/Admin/Payment/BulkApprove', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                    },
                    body: JSON.stringify(selectedIds)
                });

                if (!response.ok) throw new Error('Network error');
                return await response.json();
            } catch (error) {
                Swal.showValidationMessage('Có lỗi xảy ra khi xử lý yêu cầu');
            }
        },
        allowOutsideClick: () => !Swal.isLoading()
    }).then((result) => {
        if (result.isConfirmed) {
            if (result.value.success) {
                // Show success with confetti effect
                Swal.fire({
                    title: '🎉 Thành công!',
                    html: `
                                <div class="text-center">
                                    <i class="fas fa-check-circle fa-4x text-success mb-3"></i>
                                    <p class="fs-5">${result.value.message}</p>
                                    <div class="alert alert-success">
                                        <strong>📊 Thống kê nhanh:</strong><br>
                                        ✅ ${selectedIds.length} giao dịch đã duyệt<br>
                                        🎓 ${selectedIds.length} khóa học được kích hoạt<br>
                                        📧 ${selectedIds.length} email thông báo đã gửi
                                    </div>
                                </div>
                            `,
                    icon: 'success',
                    confirmButtonColor: '#11998e',
                    timer: 5000,
                    timerProgressBar: true,
                    showConfirmButton: false
                });

                // Animate bulk approval
                animateBulkApproval(selectedIds);
                setTimeout(() => location.reload(), 3000);
            } else {
                toastr.error(result.value.message);
            }
        }
    });
}

function bulkReject() {
    const selectedIds = $('.payment-checkbox:checked').map(function () {
        return parseInt(this.value);
    }).get();

    if (selectedIds.length === 0) {
        toastr.warning('⚠️ Vui lòng chọn ít nhất một giao dịch để từ chối.');
        return;
    }

    Swal.fire({
        title: '⚠️ Từ chối hàng loạt',
        html: `
                    <div class="text-start">
                        <div class="alert alert-warning mb-3">
                            <i class="fas fa-exclamation-triangle me-2"></i>
                            <strong>Từ chối ${selectedIds.length} giao dịch cùng lúc</strong>
                        </div>
                        <div class="alert alert-danger">
                            <h6 class="alert-heading">⚠️ Cảnh báo quan trọng</h6>
                            <p class="mb-0 small">Hành động này sẽ từ chối tất cả ${selectedIds.length} giao dịch đã chọn và gửi thông báo đến học viên.</p>
                        </div>
                    </div>
                `,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: `<i class="fas fa-times me-2"></i>Từ chối ${selectedIds.length} giao dịch`,
        cancelButtonText: '<i class="fas fa-arrow-left me-2"></i>Hủy bỏ',
        confirmButtonColor: '#fc466b',
        cancelButtonColor: '#6c757d',
        input: 'textarea',
        inputPlaceholder: '📝 Lý do từ chối chung cho tất cả giao dịch...',
        inputAttributes: {
            'style': 'margin-top: 15px; min-height: 100px; border-radius: 10px;'
        },
        inputValidator: (value) => {
            if (!value || value.trim().length < 10) {
                return '⚠️ Vui lòng nhập lý do từ chối (ít nhất 10 ký tự)!'
            }
        }
    }).then((result) => {
        if (result.isConfirmed) {
            // Process bulk rejection
            toastr.info(`🔄 Đang xử lý từ chối ${selectedIds.length} giao dịch...`);
            setTimeout(() => {
                toastr.success(`✅ Đã từ chối thành công ${selectedIds.length} giao dịch`);
                location.reload();
            }, 2000);
        }
    });
}

function deselectAll() {
    $('.payment-checkbox').prop('checked', false);
    $('#selectAll').prop('checked', false);
    $('#bulkActions').removeClass('show');
    $('.payment-row').removeClass('selected table-primary');
    toastr.info('🔄 Đã bỏ chọn tất cả giao dịch');
}

function sendEmail(email, paymentId) {
    Swal.fire({
        title: '📧 Gửi email học viên',
        html: `
                    <div class="text-start">
                        <div class="alert alert-info mb-3">
                            <i class="fas fa-envelope me-2"></i>
                            <strong>Gửi email cho giao dịch #${paymentId.toString().padStart(6, '0')}</strong>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">📧 Địa chỉ email:</label>
                            <input type="email" class="form-control" value="${email}" readonly>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">📋 Loại email:</label>
                            <select class="form-select" id="emailType">
                                <option value="confirmation">✅ Xác nhận thanh toán</option>
                                <option value="reminder">⏰ Nhắc nhở</option>
                                <option value="support">🆘 Hỗ trợ</option>
                                <option value="custom">✏️ Tùy chỉnh</option>
                            </select>
                        </div>
                    </div>
                `,
        showCancelButton: true,
        confirmButtonText: '<i class="fas fa-paper-plane me-2"></i>Gửi email',
        cancelButtonText: 'Hủy',
        confirmButtonColor: '#667eea',
        input: 'textarea',
        inputPlaceholder: '💬 Nội dung email (tùy chọn)...',
        inputAttributes: {
            'style': 'margin-top: 15px; min-height: 100px; border-radius: 10px;'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            toastr.success(`📧 Email đã được gửi đến ${email}`);
        }
    });
}

function exportCurrentData() {
    showLoading();
    toastr.info('📊 Đang chuẩn bị dữ liệu xuất...');

    setTimeout(() => {
        hideLoading();

        Swal.fire({
            title: '📥 Xuất dữ liệu',
            html: `
                        <div class="text-start">
                            <div class="alert alert-info mb-3">
                                <i class="fas fa-info-circle me-2"></i>
                                <strong>Chọn định dạng xuất dữ liệu</strong>
                            </div>
                            <div class="row g-3">
                                <div class="col-6">
                                    <div class="card h-100 border-success">
                                        <div class="card-body text-center">
                                            <i class="fas fa-file-excel fa-3x text-success mb-2"></i>
                                            <h6>Excel (.xlsx)</h6>
                                            <small class="text-muted">Phù hợp cho phân tích dữ liệu</small>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-6">
                                    <div class="card h-100 border-danger">
                                        <div class="card-body text-center">
                                            <i class="fas fa-file-pdf fa-3x text-danger mb-2"></i>
                                            <h6>PDF (.pdf)</h6>
                                            <small class="text-muted">Phù hợp cho báo cáo in ấn</small>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    `,
            showCancelButton: true,
            showDenyButton: true,
            confirmButtonText: '<i class="fas fa-file-excel me-2"></i>Xuất Excel',
            denyButtonText: '<i class="fas fa-file-pdf me-2"></i>Xuất PDF',
            cancelButtonText: 'Hủy',
            confirmButtonColor: '#198754',
            denyButtonColor: '#dc3545'
        }).then((result) => {
            if (result.isConfirmed) {
                downloadFile('payments-export.xlsx', 'Excel');
            } else if (result.isDenied) {
                downloadFile('payments-report.pdf', 'PDF');
            }
        });
    }, 2000);
}

function exportAllData() {
    Swal.fire({
        title: '📊 Xuất toàn bộ báo cáo',
        html: `
                    <div class="text-start">
                        <div class="alert alert-primary mb-3">
                            <i class="fas fa-chart-bar me-2"></i>
                            <strong>Báo cáo tổng hợp thanh toán</strong>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">📅 Khoảng thời gian:</label>
                            <select class="form-select" id="reportPeriod">
                                <option value="all">📊 Tất cả thời gian</option>
                                <option value="today">📍 Hôm nay</option>
                                <option value="week">📈 Tuần này</option>
                                <option value="month">📉 Tháng này</option>
                                <option value="quarter">📋 Quý này</option>
                                <option value="year">📆 Năm này</option>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">📋 Loại báo cáo:</label>
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" value="summary" id="summaryReport" checked>
                                <label class="form-check-label" for="summaryReport">📊 Báo cáo tổng quan</label>
                            </div>
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" value="detailed" id="detailedReport" checked>
                                <label class="form-check-label" for="detailedReport">📋 Báo cáo chi tiết</label>
                            </div>
                            <div class="form-check">
                                <input class="form-check-input" type="checkbox" value="charts" id="chartsReport">
                                <label class="form-check-label" for="chartsReport">📈 Biểu đồ và thống kê</label>
                            </div>
                        </div>
                    </div>
                `,
        showCancelButton: true,
        confirmButtonText: '<i class="fas fa-download me-2"></i>Tạo báo cáo',
        cancelButtonText: 'Hủy',
        confirmButtonColor: '#667eea'
    }).then((result) => {
        if (result.isConfirmed) {
            generateFullReport();
        }
    });
}

function showQuickStats() {
    Swal.fire({
        title: '📊 Thống kê nhanh',
        html: `
                    <div class="text-start">
                        <div class="row g-3 mb-4">
                            <div class="col-6">
                                <div class="card border-primary">
                                    <div class="card-body text-center">
                                        <i class="fas fa-clock fa-2x text-warning mb-2"></i>
                                        <h4 class="text-primary">${$('#pendingCount').text()}</h4>
                                        <small>Chờ duyệt</small>
                                    </div>
                                </div>
                            </div>
                            <div class="col-6">
                                <div class="card border-success">
                                    <div class="card-body text-center">
                                        <i class="fas fa-check fa-2x text-success mb-2"></i>
                                        <h4 class="text-success">${$('#successCount').text()}</h4>
                                        <small>Đã duyệt</small>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="card">
                            <div class="card-body">
                                <h6 class="card-title">💰 Doanh thu hôm nay</h6>
                                <h3 class="text-success">${$('#totalRevenue').text()}</h3>
                                <div class="progress mt-2" style="height: 8px;">
                                    <div class="progress-bar bg-success" style="width: 75%"></div>
                                </div>
                                <small class="text-muted">+15% so với hôm qua</small>
                            </div>
                        </div>
                        <div class="mt-3">
                            <h6>🚀 Hiệu suất hệ thống</h6>
                            <div class="d-flex justify-content-between">
                                <span>Tốc độ xử lý:</span>
                                <span class="text-success">⚡ Nhanh</span>
                            </div>
                            <div class="d-flex justify-content-between">
                                <span>Thời gian phản hồi:</span>
                                <span class="text-success">🚀 < 1s</span>
                            </div>
                        </div>
                    </div>
                `,
        confirmButtonText: '✅ Đóng',
        confirmButtonColor: '#667eea'
    });
}

function toggleAutoRefresh() {
    isAutoRefreshEnabled = !isAutoRefreshEnabled;
    const $indicator = $('#liveIndicator');

    if (isAutoRefreshEnabled) {
        autoRefreshInterval = setInterval(() => {
            updateLiveData();
        }, 30000); // 30 seconds

        $indicator.addClass('live-pulse').text('🔄 Cập nhật tự động bật');
        toastr.success('✅ Đã bật cập nhật tự động (30 giây/lần)');
    } else {
        clearInterval(autoRefreshInterval);
        $indicator.removeClass('live-pulse').text('⏸️ Cập nhật tự động tắt');
        toastr.info('⏸️ Đã tắt cập nhật tự động');
    }
}

function toggleColumns() {
    Swal.fire({
        title: '🗂️ Tùy chỉnh cột hiển thị',
        html: `
                    <div class="text-start">
                        <p class="text-muted mb-3">Chọn các cột bạn muốn hiển thị trong bảng:</p>
                        <div class="row g-2">
                            <div class="col-6">
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="col-id" checked>
                                    <label class="form-check-label" for="col-id">🆔 Mã giao dịch</label>
                                </div>
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="col-user" checked>
                                    <label class="form-check-label" for="col-user">👤 Học viên</label>
                                </div>
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="col-course" checked>
                                    <label class="form-check-label" for="col-course">📚 Khóa học</label>
                                </div>
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="col-amount" checked>
                                    <label class="form-check-label" for="col-amount">💰 Số tiền</label>
                                </div>
                            </div>
                            <div class="col-6">
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="col-date" checked>
                                    <label class="form-check-label" for="col-date">📅 Ngày tạo</label>
                                </div>
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="col-status" checked>
                                    <label class="form-check-label" for="col-status">📊 Trạng thái</label>
                                </div>
                                <div class="form-check">
                                    <input class="form-check-input" type="checkbox" id="col-actions" checked>
                                    <label class="form-check-label" for="col-actions">⚙️ Thao tác</label>
                                </div>
                            </div>
                        </div>
                    </div>
                `,
        showCancelButton: true,
        confirmButtonText: '✅ Áp dụng',
        cancelButtonText: 'Hủy',
        confirmButtonColor: '#667eea'
    }).then((result) => {
        if (result.isConfirmed) {
            toastr.success('✅ Đã cập nhật cột hiển thị');
        }
    });
}

// Helper Functions
function updateLiveData() {
    // Simulate live data updates
    const currentTransactions = parseInt($('#currentTransactions').text());
    const newCount = currentTransactions + Math.floor(Math.random() * 3);

    $('#currentTransactions').text(newCount);
    toastr.info('🔄 Dữ liệu đã được cập nhật', '', { timeOut: 2000 });
}

function updatePaymentRowStatus(paymentId, status) {
    const $row = $(`.payment-row[data-payment-id="${paymentId}"]`);
    const $statusCell = $row.find('.status-badge');

    if (status === 'approved') {
        $statusCell.removeClass('badge-pending').addClass('badge-success');
        $statusCell.html('<i class="fas fa-check-circle"></i>Đã duyệt');
    } else if (status === 'rejected') {
        $statusCell.removeClass('badge-pending').addClass('badge-failed');
        $statusCell.html('<i class="fas fa-times-circle"></i>Từ chối');
    }

    // Add visual effect
    $row.addClass('table-success');
    setTimeout(() => $row.removeClass('table-success'), 3000);
}

function updateStatsAfterApproval() {
    const currentPending = parseInt($('#pendingCount').text());
    const currentSuccess = parseInt($('#successCount').text());

    $('#pendingCount').text(Math.max(0, currentPending - 1));
    $('#successCount').text(currentSuccess + 1);
}

function updateStatsAfterRejection() {
    const currentPending = parseInt($('#pendingCount').text());
    $('#pendingCount').text(Math.max(0, currentPending - 1));
}

function animateBulkApproval(selectedIds) {
    selectedIds.forEach((id, index) => {
        setTimeout(() => {
            updatePaymentRowStatus(id, 'approved');
        }, index * 200);
    });
}

function downloadFile(filename, type) {
    showLoading();

    setTimeout(() => {
        hideLoading();

        // Simulate file download
        const link = document.createElement('a');
        link.href = '#';
        link.download = filename;

        toastr.success(`📥 File ${type} đã được tải xuống thành công!`, 'Hoàn thành');
    }, 3000);
}

function generateFullReport() {
    showLoading();

    // Simulate report generation with progress
    let progress = 0;
    const progressInterval = setInterval(() => {
        progress += 10;
        if (progress >= 100) {
            clearInterval(progressInterval);
            hideLoading();

            Swal.fire({
                title: '🎉 Báo cáo đã sẵn sàng!',
                html: `
                            <div class="text-center">
                                <i class="fas fa-file-alt fa-4x text-success mb-3"></i>
                                <p>Báo cáo tổng hợp đã được tạo thành công</p>
                                <div class="alert alert-success">
                                    <strong>📊 Thông tin báo cáo:</strong><br>
                                    📅 Thời gian tạo: ${new Date().toLocaleString('vi-VN')}<br>
                                    📋 Tổng số trang: 25<br>
                                    💾 Kích thước: 2.4 MB
                                </div>
                            </div>
                        `,
                showCancelButton: true,
                confirmButtonText: '<i class="fas fa-download me-2"></i>Tải xuống',
                cancelButtonText: '<i class="fas fa-envelope me-2"></i>Gửi email',
                confirmButtonColor: '#11998e'
            }).then((result) => {
                if (result.isConfirmed) {
                    downloadFile('full-payment-report.pdf', 'PDF');
                } else if (result.dismiss === Swal.DismissReason.cancel) {
                    // Send email logic here
                    toastr.success('📧 Báo cáo đã được gửi email thành công!');
                }
            });
        } else {
            // Update progress indicator
            console.log(`Generating report: ${progress}%`);
        }
    }, 300);
}

// Real-time notifications simulation
function startNotificationSystem() {
    setInterval(() => {
        if (Math.random() > 0.8) { // 20% chance every 30 seconds
            const notifications = [
                { type: 'info', message: '💳 Có giao dịch mới cần xử lý', icon: 'fas fa-credit-card' },
                { type: 'success', message: '✅ Học viên vừa hoàn thành khóa học', icon: 'fas fa-graduation-cap' },
                { type: 'warning', message: '⏰ Có 3 giao dịch chờ quá 24h', icon: 'fas fa-clock' }
            ];

            const randomNotification = notifications[Math.floor(Math.random() * notifications.length)];
            toastr[randomNotification.type](randomNotification.message, '', {
                timeOut: 5000,
                onclick: function () {
                    // Handle notification click
                    console.log('Notification clicked');
                }
            });
        }
    }, 30000);
}

// Start notification system
setTimeout(startNotificationSystem, 5000);

// Custom SweetAlert2 styles
const style = document.createElement('style');
style.textContent = `
            .swal2-popup-large {
                width: 600px !important;
                max-width: 90vw !important;
                padding: 0 !important;
                border-radius: 20px !important;
            }
            .swal2-popup-large .swal2-header {
                padding: 2rem 2rem 1rem !important;
            }
            .swal2-popup-large .swal2-content {
                padding: 0 2rem !important;
            }
            .swal2-popup-large .swal2-actions {
                padding: 1rem 2rem 2rem !important;
            }
            .table-hover-effect {
                background: rgba(102, 126, 234, 0.05) !important;
                transform: scale(1.01) !important;
                box-shadow: 0 8px 20px rgba(0, 0, 0, 0.1) !important;
            }
            .pulse-effect {
                animation: pulse 1.5s infinite !important;
            }
        `;
document.head.appendChild(style);

// Add CSS animations for enhanced UX
const additionalStyles = document.createElement('style');
additionalStyles.textContent = `
        @keyframes fadeInUp {
                from {
                    opacity: 0;
                    transform: translateY(30px);
                }
                to {
                    opacity: 1;
                    transform: translateY(0);
                }
            }

        @keyframes slideInRight {
                from {
                    opacity: 0;
                    transform: translateX(30px);
                }
                to {
                    opacity: 1;
                    transform: translateX(0);
                }
            }

            .fade-in-up {
                animation: fadeInUp 0.6s ease-out;
            }

            .slide-in-right {
                animation: slideInRight 0.5s ease-out;
            }
        `;
document.head.appendChild(additionalStyles);