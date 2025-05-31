
// Initialize AOS
AOS.init({
    duration: 1000,
    easing: 'ease-out-quart',
    once: true
});

$(document).ready(function () {
    initializeSearch();
    initializeSorting();
});

function initializeSearch() {
    let searchTimeout;

    $('#teacherSearch').on('input', function () {
        const query = $(this).val().toLowerCase().trim();

        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => {
            filterTeachers(query);
        }, 300);
    });
}

function initializeSorting() {
    $('#sortTeachers').on('change', function () {
        const sortBy = $(this).val();
        sortTeachers(sortBy);
    });
}

function filterTeachers(query) {
    const teachers = $('.teacher-item');
    let visibleCount = 0;

    teachers.each(function () {
        const teacherName = $(this).data('name');
        const matches = !query || teacherName.includes(query);

        if (matches) {
            $(this).show();
            visibleCount++;
        } else {
            $(this).hide();
        }
    });

    // Show/hide no results message
    toggleNoResultsMessage(visibleCount === 0 && query);
}

function sortTeachers(sortBy) {
    const container = $('#teachersContainer');
    const teachers = $('.teacher-item').detach();

    teachers.sort(function (a, b) {
        switch (sortBy) {
            case 'name':
                return $(a).data('name').localeCompare($(b).data('name'), 'vi');
            case 'courses':
                return $(b).data('courses') - $(a).data('courses');
            case 'students':
                return $(b).data('students') - $(a).data('students');
            case 'recent':
                return $(b).data('recent') - $(a).data('recent');
            default:
                return 0;
        }
    });

    container.append(teachers);

    // Re-trigger AOS for repositioned elements
    AOS.refresh();
}

function toggleNoResultsMessage(show) {
    const existing = $('#noResultsMessage');

    if (show && existing.length === 0) {
        const message = $(`
                    <div class="col-12" id="noResultsMessage">
                        <div class="no-teachers">
                            <div class="no-teachers-icon">
                                <i class="fas fa-search"></i>
                            </div>
                            <h3>Không tìm thấy giảng viên nào</h3>
                            <p>Thử tìm kiếm với từ khóa khác</p>
                        </div>
                    </div>
                `);
        $('#teachersContainer').append(message);
    } else if (!show && existing.length > 0) {
        existing.remove();
    }
}

function unfollowTeacher(teacherId, button) {
    Swal.fire({
        title: 'Bỏ theo dõi giảng viên?',
        text: 'Bạn sẽ không nhận được thông báo về khóa học mới từ giảng viên này',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#ef4444',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Bỏ theo dõi',
        cancelButtonText: 'Hủy',
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {
            // Disable button during request
            $(button).prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>Đang xử lý...');

            $.post('@Url.Action("UnfollowTeacher")', {
                teacherId: teacherId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            })
                .done(function (response) {
                    if (response.success) {
                        // Animate card removal
                        const teacherCard = $(button).closest('.teacher-item');
                        teacherCard.addClass('animate__animated animate__fadeOutUp');

                        setTimeout(() => {
                            teacherCard.remove();
                            updateStatistics();

                            // Check if no teachers left
                            if ($('.teacher-item').length === 0) {
                                showNoTeachersState();
                            }
                        }, 500);

                        Swal.fire({
                            icon: 'success',
                            title: 'Thành công!',
                            text: response.message,
                            showConfirmButton: false,
                            timer: 2000,
                            toast: true,
                            position: 'top-end'
                        });
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Lỗi!',
                            text: response.message
                        });

                        // Re-enable button
                        $(button).prop('disabled', false).html('<i class="fas fa-user-minus me-1"></i>Bỏ theo dõi');
                    }
                })
                .fail(function () {
                    Swal.fire({
                        icon: 'error',
                        title: 'Lỗi!',
                        text: 'Có lỗi xảy ra, vui lòng thử lại!'
                    });

                    // Re-enable button
                    $(button).prop('disabled', false).html('<i class="fas fa-user-minus me-1"></i>Bỏ theo dõi');
                });
        }
    });
}

function toggleFavorite(courseId) {
    $.post('@Url.Action("AddToFavorite", "Course")', {
        courseId: courseId,
        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
    })
        .done(function (response) {
            if (response.success) {
                toastr.success(response.message);
            } else {
                toastr.error(response.message);
            }
        })
        .fail(function () {
            toastr.error('Có lỗi xảy ra!');
        });
}

function updateStatistics() {
    const remainingTeachers = $('.teacher-item').length;
    const totalCourses = $('.teacher-item').map(function () {
        return $(this).data('courses');
    }).get().reduce((a, b) => a + b, 0);
    const totalStudents = $('.teacher-item').map(function () {
        return $(this).data('students');
    }).get().reduce((a, b) => a + b, 0);
    const recentActivity = $('.teacher-item').map(function () {
        return $(this).data('recent');
    }).get().reduce((a, b) => a + b, 0);

    // Animate counter updates
    animateCounter($('.stats-bar .stat-item:eq(0) .stat-number'), remainingTeachers);
    animateCounter($('.stats-bar .stat-item:eq(1) .stat-number'), totalCourses);
    animateCounter($('.stats-bar .stat-item:eq(2) .stat-number'), totalStudents);
    animateCounter($('.stats-bar .stat-item:eq(3) .stat-number'), recentActivity);
}

function animateCounter(element, targetValue) {
    const currentValue = parseInt(element.text()) || 0;
    const increment = (targetValue - currentValue) / 20;
    let current = currentValue;

    const timer = setInterval(() => {
        current += increment;
        if ((increment > 0 && current >= targetValue) ||
            (increment < 0 && current <= targetValue)) {
            current = targetValue;
            clearInterval(timer);
        }
        element.text(Math.floor(current));
    }, 50);
}

function showNoTeachersState() {
    const container = $('#teachersContainer');
    container.html(`
                <div class="col-12">
                    <div class="no-teachers animate__animated animate__fadeInUp">
                        <div class="no-teachers-icon">
                            <i class="fas fa-user-friends"></i>
                        </div>
                        <h3>Không còn giảng viên nào</h3>
                        <p>Bạn đã bỏ theo dõi tất cả giảng viên. Hãy tìm kiếm và theo dõi những giảng viên mới!</p>
                        <a href="@Url.Action("SearchTeacher")" class="btn-find-teachers">
                            <i class="fas fa-search"></i>
                            Tìm kiếm giảng viên
                        </a>
                    </div>
                </div>
            `);
}

function showLoadingState() {
    const container = $('#teachersContainer');
    const loadingHtml = Array(6).fill().map(() =>
        '<div class="col-lg-6 col-xl-4 mb-4">' + $('#loadingTemplate').html() + '</div>'
    ).join('');

    container.html(loadingHtml);
}

// Infinite scroll (optional)
function initializeInfiniteScroll() {
    let loading = false;
    let page = @ViewBag.Page;
    const totalPages = @ViewBag.TotalPages;

    $(window).scroll(function () {
        if (loading || page >= totalPages) return;

        if ($(window).scrollTop() + $(window).height() > $(document).height() - 1000) {
            loading = true;
            page++;

            // Show loading indicator
            const loadingIndicator = $(`
                        <div class="text-center py-4" id="loadingMore">
                            <div class="spinner-border text-primary" role="status">
                                <span class="visually-hidden">Đang tải...</span>
                            </div>
                            <p class="mt-2 text-muted">Đang tải thêm giảng viên...</p>
                        </div>
                    `);
            $('.container-fluid').append(loadingIndicator);

            // Load more teachers
            $.get('@Url.Action("MyFollowedTeachers")', { page: page })
                .done(function (data) {
                    // Parse and append new teachers
                    const newContent = $(data).find('.teacher-item');
                    $('#teachersContainer').append(newContent);

                    // Re-initialize AOS for new elements
                    AOS.refresh();

                    loading = false;
                    $('#loadingMore').remove();
                })
                .fail(function () {
                    loading = false;
                    page--;
                    $('#loadingMore').remove();
                    toastr.error('Không thể tải thêm dữ liệu');
                });
        }
    });
}

// View mode toggle
function toggleViewMode() {
    const container = $('#teachersContainer');
    const isGridView = container.hasClass('row');

    if (isGridView) {
        // Switch to list view
        container.removeClass('row').addClass('list-view');
        $('.teacher-item').removeClass('col-lg-6 col-xl-4').addClass('col-12');
        $('.teacher-card').addClass('horizontal-layout');
    } else {
        // Switch to grid view
        container.removeClass('list-view').addClass('row');
        $('.teacher-item').removeClass('col-12').addClass('col-lg-6 col-xl-4');
        $('.teacher-card').removeClass('horizontal-layout');
    }
}

// Export functionality
function exportTeachersList() {
    const teachers = $('.teacher-item').map(function () {
        const card = $(this);
        return {
            name: card.find('.teacher-name').text(),
            email: card.find('.teacher-email').text(),
            courses: card.data('courses'),
            students: card.data('students')
        };
    }).get();

    const csv = [
        ['Tên giảng viên', 'Email', 'Số khóa học', 'Số học viên'],
        ...teachers.map(t => [t.name, t.email, t.courses, t.students])
    ].map(row => row.join(',')).join('\n');

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = 'danh-sach-giang-vien-theo-doi.csv';
    link.click();
}

// Keyboard shortcuts
$(document).keydown(function (e) {
    // Ctrl/Cmd + F: Focus search
    if ((e.ctrlKey || e.metaKey) && e.which === 70) {
        e.preventDefault();
        $('#teacherSearch').focus();
    }

    // Escape: Clear search
    if (e.which === 27) {
        $('#teacherSearch').val('').trigger('input');
    }
});

// Configure toastr
toastr.options = {
    "closeButton": true,
    "progressBar": true,
    "positionClass": "toast-top-right",
    "timeOut": 3000
};

// Add custom CSS for animations
$('<style>')
    .prop('type', 'text/css')
    .html(`
                .horizontal-layout {
                    display: flex !important;
                    flex-direction: row !important;
                }
                .horizontal-layout .teacher-header-card {
                    flex: 0 0 300px;
                }
                .horizontal-layout .teacher-content {
                    flex: 1;
                }
                .list-view .teacher-card {
                    margin-bottom: 1rem;
                }
        @media (max-width: 768px) {
                    .horizontal-layout {
                        flex-direction: column !important;
                    }
                    .horizontal-layout .teacher-header-card {
                        flex: none;
                    }
                }
            `)
    .appendTo('head');