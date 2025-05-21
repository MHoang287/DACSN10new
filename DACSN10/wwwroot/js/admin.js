// admin.js - Tệp JavaScript chính cho trang quản trị

document.addEventListener("DOMContentLoaded", function () {
    // Định nghĩa các hàm tiện ích
    const AdminApp = {
        // Hiển thị thông báo
        showMessage: function (message, type = 'info') {
            switch (type) {
                case 'success':
                    toastr.success(message);
                    break;
                case 'error':
                    toastr.error(message);
                    break;
                case 'warning':
                    toastr.warning(message);
                    break;
                default:
                    toastr.info(message);
                    break;
            }
        },

        // Hiển thị hộp thoại xác nhận sử dụng SweetAlert2
        confirmDialog: function (title, message, confirmButtonText, callback) {
            Swal.fire({
                title: title,
                text: message,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: confirmButtonText,
                cancelButtonText: 'Hủy bỏ'
            }).then((result) => {
                if (result.isConfirmed && typeof callback === 'function') {
                    callback();
                }
            });
        },

        // Khởi tạo DataTables với cấu hình tiếng Việt
        initDataTable: function (selector, options = {}) {
            const defaultOptions = {
                language: {
                    url: '//cdn.datatables.net/plug-ins/1.10.25/i18n/Vietnamese.json'
                },
                responsive: true,
                pageLength: 10,
                lengthMenu: [[10, 25, 50, -1], [10, 25, 50, "Tất cả"]]
            };

            const mergedOptions = { ...defaultOptions, ...options };
            return $(selector).DataTable(mergedOptions);
        },

        // Định dạng số tiền thành VND
        formatCurrency: function (amount) {
            return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
        },

        // Định dạng ngày tháng theo định dạng Việt Nam
        formatDate: function (dateString) {
            const date = new Date(dateString);
            return new Intl.DateTimeFormat('vi-VN', {
                day: '2-digit',
                month: '2-digit',
                year: 'numeric',
                hour: '2-digit',
                minute: '2-digit'
            }).format(date);
        },

        // Tạo biểu đồ cơ bản với Chart.js
        createChart: function (selector, type, data, options = {}) {
            const ctx = document.getElementById(selector).getContext('2d');
            return new Chart(ctx, {
                type: type,
                data: data,
                options: options
            });
        },

        // Hiển thị hướng dẫn với Intro.js
        showTutorial: function (steps) {
            introJs().setOptions({
                steps: steps,
                prevLabel: 'Trước',
                nextLabel: 'Tiếp',
                skipLabel: 'Bỏ qua',
                doneLabel: 'Xong'
            }).start();
        }
    };

    // Đưa AdminApp vào global window để các view có thể sử dụng
    window.AdminApp = AdminApp;

    // Auto initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Auto initialize popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
});