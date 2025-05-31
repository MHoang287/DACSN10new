
function approvePayment(paymentId) {
    Swal.fire({
        title: 'Duyệt thanh toán',
        html: `
                    <div class="text-start">
                        <p>Bạn có chắc chắn muốn <strong class="text-success">duyệt</strong> giao dịch này?</p>
                        <div class="alert alert-info small">
                            <i class="fas fa-info-circle me-2"></i>
                            Sau khi duyệt:
                            <ul class="mb-0 mt-2">
                                <li>Khóa học sẽ được kích hoạt cho học viên</li>
                                <li>Học viên sẽ nhận email thông báo</li>
                                <li>Hành động này không thể hoàn tác</li>
                            </ul>
                        </div>
                    </div>
                `,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: '<i class="fas fa-check me-2"></i>Duyệt thanh toán',
        cancelButtonText: '<i class="fas fa-times me-2"></i>Hủy',
        confirmButtonColor: '#27ae60',
        cancelButtonColor: '#6c757d',
        input: 'textarea',
        inputPlaceholder: 'Ghi chú cho học viên (tùy chọn)...',
        inputAttributes: {
            'aria-label': 'Ghi chú duyệt thanh toán',
            'style': 'margin-top: 15px;'
        },
        customClass: {
            popup: 'swal2-popup-large'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            const note = result.value || '';

            // Show loading
            Swal.fire({
                title: 'Đang xử lý...',
                text: 'Vui lòng chờ trong giây lát',
                icon: 'info',
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            fetch(`/Admin/Payment/Approve/${paymentId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                body: `note=${encodeURIComponent(note)}`
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        Swal.fire({
                            title: 'Thành công!',
                            text: data.message,
                            icon: 'success',
                            confirmButtonColor: '#27ae60'
                        }).then(() => {
                            location.reload();
                        });
                    } else {
                        Swal.fire({
                            title: 'Lỗi!',
                            text: data.message,
                            icon: 'error',
                            confirmButtonColor: '#e74c3c'
                        });
                    }
                })
                .catch(error => {
                    Swal.fire({
                        title: 'Lỗi!',
                        text: 'Có lỗi xảy ra khi duyệt thanh toán.',
                        icon: 'error',
                        confirmButtonColor: '#e74c3c'
                    });
                });
        }
    });
}

function rejectPayment(paymentId) {
    Swal.fire({
        title: 'Từ chối thanh toán',
        html: `
                    <div class="text-start">
                        <p>Bạn có chắc chắn muốn <strong class="text-danger">từ chối</strong> giao dịch này?</p>
                        <div class="alert alert-warning small">
                            <i class="fas fa-exclamation-triangle me-2"></i>
                            Vui lòng nhập lý do từ chối để thông báo cho học viên.
                        </div>
                    </div>
                `,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: '<i class="fas fa-times me-2"></i>Từ chối',
        cancelButtonText: '<i class="fas fa-arrow-left me-2"></i>Hủy',
        confirmButtonColor: '#e74c3c',
        cancelButtonColor: '#6c757d',
        input: 'textarea',
        inputPlaceholder: 'Lý do từ chối (bắt buộc)...',
        inputAttributes: {
            'aria-label': 'Lý do từ chối thanh toán',
            'style': 'margin-top: 15px;'
        },
        inputValidator: (value) => {
            if (!value || value.trim().length < 10) {
                return 'Vui lòng nhập lý do từ chối (ít nhất 10 ký tự)!'
            }
        },
        customClass: {
            popup: 'swal2-popup-large'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            const reason = result.value;

            // Show loading
            Swal.fire({
                title: 'Đang xử lý...',
                text: 'Vui lòng chờ trong giây lát',
                icon: 'info',
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            fetch(`/Admin/Payment/Reject/${paymentId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                body: `reason=${encodeURIComponent(reason)}`
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        Swal.fire({
                            title: 'Thành công!',
                            text: data.message,
                            icon: 'success',
                            confirmButtonColor: '#27ae60'
                        }).then(() => {
                            location.reload();
                        });
                    } else {
                        Swal.fire({
                            title: 'Lỗi!',
                            text: data.message,
                            icon: 'error',
                            confirmButtonColor: '#e74c3c'
                        });
                    }
                })
                .catch(error => {
                    Swal.fire({
                        title: 'Lỗi!',
                        text: 'Có lỗi xảy ra khi từ chối thanh toán.',
                        icon: 'error',
                        confirmButtonColor: '#e74c3c'
                    });
                });
        }
    });
}

function printDetails() {
    window.print();
}

// Add custom styles for SweetAlert
const style = document.createElement('style');
style.textContent = `
            .swal2-popup-large {
                width: 500px !important;
            }
            @media print {
                .btn, .timeline-icon.current {
                    display: none !important;
                }
                .payment-details-card {
                    box-shadow: none !important;
                    border: 1px solid #dee2e6 !important;
                }
            }
        `;
document.head.appendChild(style);