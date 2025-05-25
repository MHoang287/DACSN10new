$(document).ready(function () {
    // Animate counters
    animateCounters();

    // Search functionality
    $('#searchCourse, #searchBtn').on('keyup click', function (e) {
        if (e.type === 'click' || e.keyCode === 13) {
            performSearch();
        }
    });

    function performSearch() {
        const searchTerm = $('#searchCourse').val().toLowerCase();
        $('.course-item').each(function () {
            const courseName = $(this).data('name') || '';
            const courseDesc = $(this).data('description') || '';

            if (courseName.includes(searchTerm) || courseDesc.includes(searchTerm)) {
                $(this).show().addClass('animate__fadeIn');
            } else {
                $(this).hide();
            }
        });
    }

    // Initialize Chart.js
    const ctx = document.getElementById('courseChart');
    if (ctx) {
        const publishedCount = @(Model?.Count(c => c.TrangThai == "Published") ?? 0);
        const closedCount = @(Model?.Count(c => c.TrangThai != "Published") ?? 0);

        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Đang mở', 'Đã đóng'],
                datasets: [{
                    data: [publishedCount, closedCount],
                    backgroundColor: ['#4fc3f7', '#adb5bd'],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        display: true,
                        position: 'bottom',
                        labels: {
                            padding: 20,
                            usePointStyle: true
                        }
                    }
                }
            }
        });
    }

    // Smooth scrolling
    $('a[href^="#"]').on('click', function (e) {
        const target = $(this.getAttribute('href'));
        if (target.length) {
            e.preventDefault();
            $('html, body').animate({
                scrollTop: target.offset().top - 70
            }, 500);
        }
    });

    // Card hover animations
    $('.course-card').hover(
        function () { $(this).addClass('animate__pulse'); },
        function () { $(this).removeClass('animate__pulse'); }
    );
});

// Counter animation
function animateCounters() {
    animateCounter('#totalStudents', @(Model?.Sum(c => c.Enrollments?.Count ?? 0) ?? 0));
    animateCounter('#totalCourses', @(Model?.Count ?? 0));
    animateCounter('#totalLessons', @(Model?.Sum(c => c.Lessons?.Count ?? 0) ?? 0));
}

function animateCounter(selector, target) {
    const element = $(selector);
    const increment = target / 100;
    let current = 0;

    const timer = setInterval(() => {
        current += increment;
        if (current >= target) {
            element.text(target.toLocaleString());
            clearInterval(timer);
        } else {
            element.text(Math.floor(current).toLocaleString());
        }
    }, 20);
}

// Course enrollment function
function enrollCourse(courseId, courseName) {
    @if (User.Identity.IsAuthenticated) {
        <text>
            $.ajax({
                url: '@Url.Action("Enroll", "Course")',
            type: 'POST',
            data: {
                courseId: courseId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                    },
            success: function(response) {
                Swal.fire({
                    icon: 'success',
                    title: 'Đăng ký thành công!',
                    text: `Bạn đã đăng ký khóa học: ${courseName}`,
                    confirmButtonText: 'Xem khóa học của tôi',
                    showCancelButton: true,
                    cancelButtonText: 'Tiếp tục khám phá'
                }).then((result) => {
                    if (result.isConfirmed) {
                        window.location.href = '@Url.Action("MyCourses", "Course")';
                    }
                });
                    },
            error: function(xhr) {
                Swal.fire({
                    icon: 'error',
                    title: 'Lỗi',
                    text: 'Có lỗi xảy ra khi đăng ký khóa học.',
                    confirmButtonText: 'Thử lại'
                });
                    }
                });
        </text>
    }
    else {
        <text>
            Swal.fire({
                icon: 'info',
            title: 'Cần đăng nhập',
            text: 'Bạn cần đăng nhập để đăng ký khóa học.',
            confirmButtonText: 'Đăng nhập',
            showCancelButton: true,
            cancelButtonText: 'Hủy'
                }).then((result) => {
                    if (result.isConfirmed) {
                window.location.href = '/Identity/Account/Login';
                    }
                });
        </text>
    }
}

// Add to favorite function
function addToFavorite(courseId, courseName) {
    @if (User.Identity.IsAuthenticated) {
        <text>
            $.ajax({
                url: '@Url.Action("AddToFavorite", "Course")',
            type: 'POST',
            data: {
                courseId: courseId,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                    },
            success: function(response) {
                Swal.fire({
                    icon: 'success',
                    title: 'Thành công!',
                    text: `Đã thêm "${courseName}" vào danh sách yêu thích`,
                    timer: 2000,
                    showConfirmButton: false
                });
                    },
            error: function(xhr) {
                Swal.fire({
                    icon: 'error',
                    title: 'Lỗi',
                    text: 'Có lỗi xảy ra khi thêm vào yêu thích.',
                    confirmButtonText: 'Thử lại'
                });
                    }
                });
        </text>
    }
}