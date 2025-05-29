$(document).ready(function () {
    // KendoEditor cho nội dung bài học
    if ($("#lessonContent").length && typeof $("#lessonContent").kendoEditor === 'function') {
        $("#lessonContent").kendoEditor({
            tools: [
                "bold", "italic", "underline", "strikethrough",
                "justifyLeft", "justifyCenter", "justifyRight", "justifyFull",
                "insertUnorderedList", "insertOrderedList", "indent", "outdent",
                "createLink", "unlink", "insertImage", "subscript", "superscript",
                "tableWizard", "createTable", "addRowAbove", "addRowBelow", "addColumnLeft",
                "addColumnRight", "deleteRow", "deleteColumn", "viewHtml", "formatting",
                "cleanFormatting"
            ],
            resizable: { content: true, toolbar: true }
        });
    }

    // Toggle giữa text content và file upload
    $('input[name="contentOption"]').on('change', function () {
        const selectedOption = $('input[name="contentOption"]:checked').val();
        if (selectedOption === 'text') {
            $('#textContentSection').show();
            $('#fileUploadSection').hide();
        } else {
            $('#textContentSection').hide();
            $('#fileUploadSection').show();
        }
    });

    // Preview video khi nhập URL
    let timeoutId;
    $('#VideoUrl').on('input', function () {
        clearTimeout(timeoutId);
        timeoutId = setTimeout(function () {
            const videoUrl = $('#VideoUrl').val().trim();
            if (videoUrl) {
                let embedUrl = '';
                if (videoUrl.includes('youtube.com') || videoUrl.includes('youtu.be')) {
                    let videoId = '';
                    if (videoUrl.includes('v=')) {
                        videoId = videoUrl.split('v=')[1].split('&')[0];
                    } else if (videoUrl.includes('youtu.be/')) {
                        videoId = videoUrl.split('youtu.be/')[1];
                    }
                    if (videoId) embedUrl = `https://www.youtube.com/embed/${videoId}`;
                }
                else if (videoUrl.includes('vimeo.com')) {
                    const vimeoId = videoUrl.split('/').pop();
                    embedUrl = `https://player.vimeo.com/video/${vimeoId}`;
                }
                else if (videoUrl.includes('drive.google.com')) {
                    let fileId = '';
                    let match = videoUrl.match(/d\/([^\/]+)/);
                    if (match) fileId = match[1];
                    else if (videoUrl.includes('id=')) fileId = videoUrl.split('id=')[1].split('&')[0];
                    if (fileId) embedUrl = `https://drive.google.com/file/d/${fileId}/preview`;
                }
                if (embedUrl) {
                    $('#videoPreviewFrame').attr('src', embedUrl);
                    $('#videoPreviewContainer').show();
                    $('#noVideoText').hide();
                } else {
                    $('#videoPreviewContainer').hide();
                    $('#noVideoText').show();
                }
            } else {
                $('#videoPreviewContainer').hide();
                $('#noVideoText').show();
            }
        }, 500);
    });

    // Nút xem thử nội dung
    $('#previewBtn').on('click', function () {
        const lessonTitle = $('#TenBaiHoc').val() || 'Untitled Lesson';
        let content = '';
        const selectedOption = $('input[name="contentOption"]:checked').val();
        if (selectedOption === 'text') {
            if ($('#lessonContent').data('kendoEditor')) {
                content = $('#lessonContent').data('kendoEditor').value();
            } else {
                content = $('#lessonContent').val();
            }
        } else {
            const fileInput = $('#DocumentFile')[0];
            if (fileInput && fileInput.files.length > 0) {
                content = `<div class="alert alert-info">
                            <i class="fas fa-file-alt me-2"></i>
                            Selected file: ${fileInput.files[0].name}
                        </div>`;
            } else {
                content = '<div class="alert alert-warning">No file selected</div>';
            }
        }
        $('#previewTitle').text(lessonTitle);
        $('#previewContent').html(content);

        if (typeof bootstrap !== "undefined" && bootstrap.Modal) {
            const previewModal = new bootstrap.Modal(document.getElementById('previewModal'));
            previewModal.show();
        } else {
            $('#previewModal').modal('show');
        }
    });

    // Validate form (jQuery validate hoặc tự kiểm tra)
    $("#createLessonForm, #editLessonForm").on('submit', function (e) {
        let valid = true;
        const selectedOption = $('input[name="contentOption"]:checked').val();

        // Kiểm tra tên bài học
        const title = $('#TenBaiHoc').val();
        if (!title || title.trim().length < 3) {
            toastr.error('Tên bài học tối thiểu 3 ký tự');
            $('#TenBaiHoc').addClass('is-invalid');
            valid = false;
        } else {
            $('#TenBaiHoc').removeClass('is-invalid');
        }

        // Kiểm tra thời lượng
        const duration = $('#ThoiLuong').val();
        if (!duration || Number(duration) < 1) {
            toastr.error('Thời lượng tối thiểu 1 phút');
            $('#ThoiLuong').addClass('is-invalid');
            valid = false;
        } else {
            $('#ThoiLuong').removeClass('is-invalid');
        }

        // Kiểm tra nội dung/file
        if (selectedOption === 'file') {
            const fileInput = $('#DocumentFile')[0];
            // Nếu là create hoặc chưa có file cũ thì bắt buộc phải chọn file
            if (fileInput && fileInput.files.length === 0 && !($('#fileUploadSection').find('a').length)) {
                toastr.error('Vui lòng chọn file để upload');
                $('#DocumentFile').addClass('is-invalid');
                valid = false;
            } else {
                $('#DocumentFile').removeClass('is-invalid');
            }
        } else {
            // Nếu là text, kiểm tra có nội dung không
            let textContent = '';
            if ($('#lessonContent').data('kendoEditor')) {
                textContent = $('#lessonContent').data('kendoEditor').value();
            } else {
                textContent = $('#lessonContent').val();
            }
            if (!textContent || textContent.trim().length === 0) {
                toastr.error('Vui lòng nhập nội dung bài học');
                $('#lessonContent').addClass('is-invalid');
                valid = false;
            } else {
                $('#lessonContent').removeClass('is-invalid');
            }
        }

        if (!valid) {
            e.preventDefault();
            return false;
        }
        return true;
    });

    // Tự động chuyển section khi load lại (Edit)
    const sectionToShow = $('input[name="contentOption"]:checked').val();
    if (sectionToShow === 'file') {
        $('#textContentSection').hide();
        $('#fileUploadSection').show();
    } else {
        $('#textContentSection').show();
        $('#fileUploadSection').hide();
    }
});