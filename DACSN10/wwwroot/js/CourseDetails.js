// Course Details JavaScript
document.addEventListener('DOMContentLoaded', function () {
    // Initialize AOS
    AOS.init({
        duration: 800,
        easing: 'ease-out-cubic',
        once: true,
        offset: 50
    });

    // Configure toastr
    if (typeof toastr !== 'undefined') {
        toastr.options = {
            "closeButton": true,
            "progressBar": true,
            "positionClass": "toast-top-right",
            "timeOut": 3000,
            "extendedTimeOut": 1000,
            "showEasing": "swing",
            "hideEasing": "linear",
            "showMethod": "fadeIn",
            "hideMethod": "fadeOut"
        };
    }

    // Smooth scroll for internal links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Add loading states to buttons
    document.querySelectorAll('.btn-payment, .btn-enroll').forEach(button => {
        button.addEventListener('click', function () {
            if (!this.disabled) {
                this.innerHTML = '<i class="bi bi-arrow-clockwise spin me-2"></i>Đang xử lý...';
                this.disabled = true;
            }
        });
    });

    // Parallax effect for hero section
    window.addEventListener('scroll', function () {
        const scrolled = window.pageYOffset;
        const hero = document.querySelector('.course-hero');
        if (hero) {
            hero.style.transform = `translateY(${scrolled * 0.5}px)`;
        }
    });

    // Add hover effects to cards
    document.querySelectorAll('.content-section, .course-info-card').forEach(card => {
        card.addEventListener('mouseenter', function () {
            this.style.transform = 'translateY(-5px)';
        });

        card.addEventListener('mouseleave', function () {
            this.style.transform = 'translateY(0)';
        });
    });
});

// Course interaction functions
function toggleFavorite(courseId) {
    if (!checkAuthentication()) return;

    const isFavorite = document.querySelector('[onclick*="toggleFavorite"]').innerHTML.includes('Đã yêu thích');
    const action = isFavorite ? 'RemoveFromFavorite' : 'AddToFavorite';

    showLoadingToast('Đang xử lý...');

    fetch(`/Course/${action}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: `courseId=${courseId}`
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showSuccessToast(data.message);
                setTimeout(() => location.reload(), 1500);
            } else {
                showErrorToast(data.message);
            }
        })
        .catch(error => {
            showErrorToast('Có lỗi xảy ra. Vui lòng thử lại!');
            console.error('Error:', error);
        });
}

function toggleFollow(courseId) {
    if (!checkAuthentication()) return;

    const isFollowed = document.querySelector('[onclick*="toggleFollow"]').innerHTML.includes('Đang theo dõi');
    const action = isFollowed ? 'UnfollowCourse' : 'FollowCourse';

    showLoadingToast('Đang xử lý...');

    fetch(`/Course/${action}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: `courseId=${courseId}`
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showSuccessToast(data.message);
                setTimeout(() => location.reload(), 1500);
            } else {
                showErrorToast(data.message);
            }
        })
        .catch(error => {
            showErrorToast('Có lỗi xảy ra. Vui lòng thử lại!');
            console.error('Error:', error);
        });
}

function quickEnroll(courseId) {
    if (!checkAuthentication()) return;

    Swal.fire({
        title: 'Đăng ký khóa học',
        text: 'Bạn có chắc chắn muốn đăng ký khóa học này?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: '<i class="bi bi-check-circle me-2"></i>Đăng ký ngay',
        cancelButtonText: '<i class="bi bi-x-circle me-2"></i>Hủy bỏ',
        confirmButtonColor: '#667eea',
        cancelButtonColor: '#6c757d',
        background: '#fff',
        backdrop: `rgba(102, 126, 234, 0.1)`,
        allowOutsideClick: false,
        allowEscapeKey: false,
        customClass: {
            popup: 'animated fadeInDown',
            confirmButton: 'btn-modern',
            cancelButton: 'btn-modern'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            const form = document.querySelector('form[asp-action="Enroll"]');
            if (form) {
                form.submit();
            }
        }
    });
}

function watchLesson(lessonId) {
    showLoadingToast('Đang tải bài học...');
    setTimeout(() => {
        window.location.href = `/Lesson/Watch/${lessonId}`;
    }, 1000);
}

function takeQuiz(quizId) {
    Swal.fire({
        title: 'Bắt đầu kiểm tra',
        text: 'Bạn sẵn sàng làm bài kiểm tra này?',
        icon: 'info',
        showCancelButton: true,
        confirmButtonText: '<i class="bi bi-play-circle me-2"></i>Bắt đầu',
        cancelButtonText: 'Hủy',
        confirmButtonColor: '#ffc107',
        customClass: {
            popup: 'animated zoomIn'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = `/Quiz/Take/${quizId}`;
        }
    });
}

function viewAssignment(assignmentId) {
    showLoadingToast('Đang tải bài tập...');
    setTimeout(() => {
        window.location.href = `/Assignment/View/${assignmentId}`;
    }, 1000);
}

function followTeacher(teacherId) {
    if (!checkAuthentication()) return;

    showLoadingToast('Đang xử lý...');

    fetch('/User/FollowTeacher', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': getAntiForgeryToken()
        },
        body: `teacherId=${teacherId}`
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showSuccessToast(data.message);
            } else {
                showErrorToast(data.message);
            }
        })
        .catch(error => {
            showErrorToast('Có lỗi xảy ra. Vui lòng thử lại!');
            console.error('Error:', error);
        });
}

function showLoginAlert(action) {
    const messages = {
        'payment': 'Vui lòng đăng nhập để thanh toán khóa học',
        'enroll': 'Vui lòng đăng nhập để đăng ký khóa học',
        'favorite': 'Vui lòng đăng nhập để thêm vào yêu thích',
        'follow': 'Vui lòng đăng nhập để theo dõi'
    };

    Swal.fire({
        title: 'Yêu cầu đăng nhập',
        text: messages[action] || 'Vui lòng đăng nhập để tiếp tục',
        icon: 'info',
        showCancelButton: true,
        confirmButtonText: '<i class="bi bi-box-arrow-in-right me-2"></i>Đăng nhập',
        cancelButtonText: 'Hủy',
        confirmButtonColor: '#667eea',
        background: '#fff',
        customClass: {
            popup: 'animated slideInDown'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = '/Identity/Account/Login';
        }
    });
}

// Utility functions
function checkAuthentication() {
    return document.querySelector('input[name="__RequestVerificationToken"]') !== null;
}

function getAntiForgeryToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]');
    return token ? token.value : '';
}

function showLoadingToast(message) {
    if (typeof toastr !== 'undefined') {
        toastr.info(message, '', {
            timeOut: 0,
            extendedTimeOut: 0,
            closeButton: false,
            tapToDismiss: false
        });
    }
}

function showSuccessToast(message) {
    if (typeof toastr !== 'undefined') {
        toastr.clear();
        toastr.success(message);
    }
}

function showErrorToast(message) {
    if (typeof toastr !== 'undefined') {
        toastr.clear();
        toastr.error(message);
    }
}

// Add CSS for spinner animation
const style = document.createElement('style');
style.textContent = `
    .spin {
        animation: spin 1s linear infinite;
    }
    
    @keyframes spin {
        from { transform: rotate(0deg); }
        to { transform: rotate(360deg); }
    }
    
    .btn-modern {
        border-radius: 25px !important;
        padding: 12px 30px !important;
        font-weight: 600 !important;
        text-transform: uppercase !important;
        letter-spacing: 1px !important;
    }
`;
document.head.appendChild(style);