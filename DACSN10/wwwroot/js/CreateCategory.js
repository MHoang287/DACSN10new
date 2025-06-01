
let selectedIconClass = 'fa-folder';
let selectedColorValue = '#3b82f6';

$(document).ready(function () {
    // Initialize default selections
    $('.icon-option[data-icon="fa-folder"]').addClass('selected');
    $('.color-option[data-color="#3b82f6"]').addClass('selected');

    // Real-time preview updates
    $('#Name').on('input', function () {
        const name = $(this).val() || 'Tên danh mục';
        $('#previewName').text(name);
        updateSlugPreview(name);
        checkSuggestions(name);
    });

    $('#Description').on('input', function () {
        const description = $(this).val() || 'Mô tả danh mục sẽ hiển thị ở đây';
        $('#previewDescription').text(description.substring(0, 100) + (description.length > 100 ? '...' : ''));
    });

    // Form validation
    $('#categoryForm').on('submit', function (e) {
        if (!validateForm()) {
            e.preventDefault();
            return false;
        }

        showLoading();
    });

    // Hide suggestions when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('#Name, #categorySuggestions').length) {
            $('#categorySuggestions').hide();
        }
    });

    // Initialize preview for edit mode
    @if (isEdit) {
        updatePreview();
    }
});

function selectIcon(element) {
    $('.icon-option').removeClass('selected');
    $(element).addClass('selected');

    selectedIconClass = $(element).data('icon');
    $('#selectedIcon').val(selectedIconClass);
    $('#previewIcon i').removeClass().addClass(`fas ${selectedIconClass}`);

    updatePreviewBackground();
}

function selectColor(element) {
    $('.color-option').removeClass('selected');
    $(element).addClass('selected');

    selectedColorValue = $(element).data('color');
    $('#selectedColor').val(selectedColorValue);

    updatePreviewBackground();
}

function updatePreviewBackground() {
    const preview = $('#categoryPreview');
    preview.css('background', `linear-gradient(135deg, ${selectedColorValue} 0%, ${adjustBrightness(selectedColorValue, -20)} 100%)`);
}

function adjustBrightness(hex, percent) {
    // Convert hex to RGB
    const num = parseInt(hex.replace("#", ""), 16);
    const amt = Math.round(2.55 * percent);
    const R = (num >> 16) + amt;
    const G = (num >> 8 & 0x00FF) + amt;
    const B = (num & 0x0000FF) + amt;

    return "#" + (0x1000000 + (R < 255 ? R < 1 ? 0 : R : 255) * 0x10000 +
        (G < 255 ? G < 1 ? 0 : G : 255) * 0x100 +
        (B < 255 ? B < 1 ? 0 : B : 255))
        .toString(16).slice(1);
}

function updateSlugPreview(name) {
    const slug = name.toLowerCase()
        .replace(/[^a-z0-9\s-]/g, '')
        .replace(/\s+/g, '-')
        .replace(/-+/g, '-')
        .trim('-');

    $('#slugPreview').text(`/category/${slug || 'ten-danh-muc'}`);
}

function checkSuggestions(name) {
    if (name.length >= 2) {
        $('#categorySuggestions').show();
    } else {
        $('#categorySuggestions').hide();
    }
}

function selectSuggestion(suggestion) {
    $('#Name').val(suggestion).trigger('input');
    $('#categorySuggestions').hide();
}

function validateForm() {
    const name = $('#Name').val().trim();

    if (!name) {
        showError('Vui lòng nhập tên danh mục!');
        $('#Name').focus();
        return false;
    }

    if (name.length < 2) {
        showError('Tên danh mục phải có ít nhất 2 ký tự!');
        $('#Name').focus();
        return false;
    }

    return true;
}

function resetForm() {
    Swal.fire({
        title: 'Đặt lại form?',
        text: 'Tất cả thông tin đã nhập sẽ bị xóa.',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Đặt lại',
        cancelButtonText: 'Hủy'
    }).then((result) => {
        if (result.isConfirmed) {
            document.getElementById('categoryForm').reset();

            // Reset selections
            $('.icon-option').removeClass('selected');
            $('.color-option').removeClass('selected');
            $('.icon-option[data-icon="fa-folder"]').addClass('selected');
            $('.color-option[data-color="#3b82f6"]').addClass('selected');

            // Reset preview
            $('#previewName').text('Tên danh mục');
            $('#previewDescription').text('Mô tả danh mục sẽ hiển thị ở đây');
            $('#previewIcon i').removeClass().addClass('fas fa-folder');
            $('#slugPreview').text('/category/ten-danh-muc');

            selectedIconClass = 'fa-folder';
            selectedColorValue = '#3b82f6';
            updatePreviewBackground();

            showSuccess('Đã đặt lại form!');
        }
    });
}

function previewCategory() {
    const name = $('#Name').val() || 'Tên danh mục';
    const description = $('#Description').val() || 'Chưa có mô tả';

    Swal.fire({
        title: 'Xem trước danh mục',
        html: `
                    <div class="text-start">
                        <div class="card border-0 mb-3" style="background: linear-gradient(135deg, ${selectedColorValue} 0%, ${adjustBrightness(selectedColorValue, -20)} 100%); color: white;">
                            <div class="card-body text-center">
                                <div style="font-size: 3rem; margin-bottom: 1rem;">
                                    <i class="fas ${selectedIconClass}"></i>
                                </div>
                                <h5>${name}</h5>
                                <p>${description}</p>
                            </div>
                        </div>
                        <div class="alert alert-info">
                            <i class="fas fa-info-circle me-2"></i>
                            Đây là cách danh mục sẽ hiển thị trên website
                        </div>
                    </div>
                `,
        confirmButtonText: 'Đóng',
        width: 500
    });
}

function updatePreview() {
    const name = $('#Name').val() || 'Tên danh mục';
    const description = $('#Description').val() || 'Mô tả danh mục sẽ hiển thị ở đây';

    $('#previewName').text(name);
    $('#previewDescription').text(description);
    updateSlugPreview(name);
}