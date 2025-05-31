
AOS.init({
    duration: 800,
    easing: 'ease-out-quart',
    once: true
});

function cancelPayment(paymentId) {
    Swal.fire({
        title: 'Hủy giao dịch',
        text: 'Bạn có chắc chắn muốn hủy giao dịch này?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Có, hủy giao dịch',
        cancelButtonText: 'Không',
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        background: '#ffffff',
        color: '#2d3748'
    }).then((result) => {
        if (result.isConfirmed) {
            // Show loading
            Swal.fire({
                title: 'Đang xử lý...',
                text: 'Vui lòng chờ trong giây lát',
                icon: 'info',
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                background: '#ffffff',
                color: '#2d3748',
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            fetch(`/Payment/Cancel/${paymentId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                }
            })
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        Swal.fire({
                            title: 'Thành công!',
                            text: data.message,
                            icon: 'success',
                            background: '#ffffff',
                            color: '#2d3748',
                            confirmButtonColor: '#28a745'
                        }).then(() => {
                            location.reload();
                        });
                    } else {
                        Swal.fire({
                            title: 'Lỗi!',
                            text: data.message,
                            icon: 'error',
                            background: '#ffffff',
                            color: '#2d3748',
                            confirmButtonColor: '#dc3545'
                        });
                    }
                })
                .catch(error => {
                    Swal.fire({
                        title: 'Lỗi!',
                        text: 'Có lỗi xảy ra khi hủy giao dịch.',
                        icon: 'error',
                        background: '#ffffff',
                        color: '#2d3748',
                        confirmButtonColor: '#dc3545'
                    });
                });
        }
    });
}

function printInvoice() {
    // Custom print styles
    const printStyle = `
                <style>
        @media print {
                        body { background: white !important; }
                        .details-container { background: white !important; }
                        .btn, .alert { display: none !important; }
                        .details-card { box-shadow: none !important; border: 1px solid #ccc !important; }
                        .invoice-section { background: white !important; border: 1px solid #ccc !important; }
                    }
                </style>
            `;

    const originalContent = document.body.innerHTML;
    const printContent = document.querySelector('.details-card').outerHTML;

    document.head.insertAdjacentHTML('beforeend', printStyle);
    document.body.innerHTML = printContent;

    window.print();

    document.body.innerHTML = originalContent;
    location.reload(); // Reload to restore functionality
}

// Add hover effects
document.querySelectorAll('.btn').forEach(btn => {
    btn.addEventListener('mouseenter', function () {
        this.style.transform = 'translateY(-2px)';
    });

    btn.addEventListener('mouseleave', function () {
        this.style.transform = 'translateY(0)';
    });
});