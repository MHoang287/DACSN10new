
AOS.init({
    duration: 1000,
    easing: 'ease-out-quart',
    once: true
});

// Initialize date range picker
$('#daterange').daterangepicker({
    autoUpdateInput: false,
    locale: {
        cancelLabel: 'Xóa',
        applyLabel: 'Áp dụng',
        format: 'DD/MM/YYYY'
    }
});

$('#daterange').on('apply.daterangepicker', function (ev, picker) {
    $(this).val(picker.startDate.format('DD/MM/YYYY') + ' - ' + picker.endDate.format('DD/MM/YYYY'));
});

$('#daterange').on('cancel.daterangepicker', function (ev, picker) {
    $(this).val('');
});

function viewPaymentDetails(paymentId) {
    // Load payment details in modal
    $('#paymentDetailsModal').modal('show');
    $('#paymentDetailsContent').html('<div class="text-center p-4"><i class="fas fa-spinner fa-spin fa-2x"></i><br>Đang tải...</div>');

    // Simulate loading payment details
    setTimeout(() => {
        $('#paymentDetailsContent').html(`
                    <div class="p-3">
                        <h6>Thông tin chi tiết giao dịch #${paymentId}</h6>
                        <p>Chi tiết giao dịch sẽ được hiển thị ở đây...</p>
                    </div>
                `);
    }, 1000);
}

function sharePayment(paymentId) {
    if (navigator.share) {
        navigator.share({
            title: 'Chia sẻ thông tin thanh toán',
            text: `Giao dịch #${paymentId} trên OnlineLearning`,
            url: window.location.href
        });
    } else {
        // Fallback for browsers that don't support Web Share API
        const url = window.location.href;
        navigator.clipboard.writeText(url).then(() => {
            toastr.success('Đã sao chép link vào clipboard!');
        });
    }
}

function exportToExcel() {
    // Get table data
    const data = [];
    const headers = ['Mã GD', 'Khóa học', 'Phương thức', 'Số tiền', 'Trạng thái', 'Thời gian'];
    data.push(headers);

    $('.payment-card').each(function () {
        const row = [];
        row.push($(this).find('.payment-id').text());
        row.push($(this).find('.course-title').text());
        row.push($(this).find('.info-value').eq(1).text());
        row.push($(this).find('.payment-amount').text());
        row.push($(this).find('.payment-status').text());
        row.push($(this).find('.info-value').eq(2).text());
        data.push(row);
    });

    // Create workbook and worksheet
    const wb = XLSX.utils.book_new();
    const ws = XLSX.utils.aoa_to_sheet(data);

    // Add worksheet to workbook
    XLSX.utils.book_append_sheet(wb, ws, 'Lịch sử thanh toán');

    // Save file
    XLSX.writeFile(wb, `LichSuThanhToan_${new Date().toISOString().split('T')[0]}.xlsx`);

    toastr.success('Đã xuất file Excel thành công!');
}

function printHistory() {
    const printContent = document.getElementById('paymentsContainer').outerHTML;
    const printWindow = window.open('', '_blank');
    printWindow.document.write(`
                <html>
                <head>
                    <title>Lịch sử thanh toán</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 20px; }
                        .payment-card { border: 1px solid #ddd; margin: 10px 0; padding: 15px; }
                        .payment-header { display: flex; justify-content: space-between; margin-bottom: 10px; }
                        .payment-info { display: grid; grid-template-columns: repeat(2, 1fr); gap: 10px; }
                        .info-item { margin-bottom: 5px; }
                        .info-label { font-weight: bold; }
                        .payment-actions { display: none; }
                    </style>
                </head>
                <body>
                    <h1>Lịch sử thanh toán</h1>
                    ${printContent}
                </body>
                </html>
            `);
    printWindow.document.close();
    printWindow.print();
}

// Configure toastr
toastr.options = {
    "closeButton": true,
    "progressBar": true,
    "positionClass": "toast-top-right",
    "timeOut": 3000
};

// Auto-refresh pending payments every 30 seconds
setInterval(() => {
    if ($('.status-waiting, .status-pending').length > 0) {
        console.log('Checking for payment updates...');
        // Could add AJAX call to check for updates
    }
}, 30000);