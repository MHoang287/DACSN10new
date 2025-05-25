<script>
    $(document).ready(function() {
        // Initialize course statistics chart
        initializeCourseChart();

    // Search functionality
    setupSearchFunctionality();

    // Course interactions
    setupCourseInteractions();

    // Smooth scrolling for anchor links
    setupSmoothScrolling();

    // Counter animations
    animateCounters();
        });

    // Initialize Chart.js for course statistics
    function initializeCourseChart() {
            const ctx = document.getElementById('courseChart');
    if (ctx) {
        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Khóa học đang mở', 'Khóa học đã đóng'],
                datasets: [{
                    data: [@publishedCount, @closedCount],
                    backgroundColor: [
                        '#28a745',
                        '#dc3545'
                    ],
                    borderWidth: 2,
                    borderColor: '#fff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            padding: 20,
                            font: {
                                size: 14
                            }
                        }
                    }
                }
            }
        });
            }
        }

    // Setup search functionality
    function setupSearchFunctionality() {
            const searchInput = $('#searchCourse');
    const searchBtn = $('#searchBtn');
    const courseItems = $('.course-item');

    function performSearch() {
                const searchTerm = searchInput.val().toLowerCase().trim();

    if (searchTerm === '') {
        courseItems.show();
    return;
                }

    courseItems.each(function() {
                    const courseName = $(this).data('name') || '';
    const courseDescription = $(this).data('description') || '';

    if (courseName.includes(searchTerm) || courseDescription.includes(searchTerm)) {
        $(this).show();
                    } else {
        $(this).hide();
                    }
                });
            }

    searchBtn.on('click', performSearch);
    searchInput.on('keyup', function(e) {
                if (e.key === 'Enter') {
        performSearch();
                } else {
        // Real-time search with debounce
        clearTimeout(window.searchTimeout);
    window.searchTimeout = setTimeout(performSearch, 300);
                }
            });
        }

    // Setup course interactions
    function setupCourseInteractions() {
        // Course enrollment function
        window.enrollCourse = function (courseId, courseName) {
            Swal.fire({
                title: 'Xác nhận đăng ký',
                text: `Bạn có muốn đăng ký khóa học "${courseName}"?`,
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#28a745',
                cancelButtonColor: '#6c757d',
                confirmButtonText: 'Đăng ký',
                cancelButtonText: 'Hủy bỏ'
            }).then((result) => {
                if (result.isConfirmed) {
                    // Show loading
                    Swal.fire({
                        title: 'Đang xử lý...',
                        allowOutsideClick: false,
                        didOpen: () => {
                            Swal.showLoading();
                        }
                    });

                    // AJAX call to enroll
                    $.ajax({
                        url: '/Course/Enroll',
                        method: 'POST',
                        data: {
                            courseId: courseId,
                            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                        },
                        success: function (response) {
                            Swal.fire({
                                title: 'Thành công!',
                                text: 'Bạn đã đăng ký khóa học thành công.',
                                icon: 'success',
                                confirmButtonText: 'OK'
                            });
                        },
                        error: function (xhr) {
                            let errorMessage = 'Có lỗi xảy ra khi đăng ký khóa học.';
                            if (xhr.responseJSON && xhr.responseJSON.message) {
                                errorMessage = xhr.responseJSON.message;
                            }

                            Swal.fire({
                                title: 'Lỗi!',
                                text: errorMessage,
                                icon: 'error',
                                confirmButtonText: 'OK'
                            });
                        }
                    });
                }
            });
        };

    // Add to favorites function
    window.addToFavorite = function(courseId, courseName) {
        $.ajax({
            url: '/Course/AddToFavorite',
            method: 'POST',
            data: {
                courseId: courseId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                // Toggle heart icon
                const heartIcon = $(`.btn-favorite[onclick*="${courseId}"] i`);
                heartIcon.toggleClass('far fas');

                // Show toast notification
                const Toast = Swal.mixin({
                    toast: true,
                    position: 'top-end',
                    showConfirmButton: false,
                    timer: 3000,
                    timerProgressBar: true
                });

                Toast.fire({
                    icon: 'success',
                    title: `Đã thêm "${courseName}" vào danh sách yêu thích`
                });
            },
            error: function (xhr) {
                Swal.fire({
                    title: 'Lỗi!',
                    text: 'Không thể thêm vào danh sách yêu thích.',
                    icon: 'error',
                    confirmButtonText: 'OK'
                });
            }
        });
            };
        }

    // Setup smooth scrolling
    function setupSmoothScrolling() {
        $('a[href^="#"]').on('click', function (e) {
            e.preventDefault();
            const target = $(this.getAttribute('href'));

            if (target.length) {
                $('html, body').animate({
                    scrollTop: target.offset().top - 80
                }, 800, 'easeInOutCubic');
            }
        });
        }

    // Animate counters when they come into view
    function animateCounters() {
            const counters = $('.stat-number');
    let hasAnimated = false;

    function startCountAnimation() {
                if (hasAnimated) return;

    counters.each(function() {
                    const $this = $(this);
    const countTo = parseInt($this.text());

    $({countNum: 0 }).animate({
        countNum: countTo
                    }, {
        duration: 2000,
    easing: 'swing',
    step: function() {
        $this.text(Math.floor(this.countNum));
                        },
    complete: function() {
        $this.text(countTo);
                        }
                    });
                });

    hasAnimated = true;
            }

    // Trigger animation when hero section is in view
    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                setTimeout(startCountAnimation, 500);
            }
        });
            }, {threshold: 0.5 });

    const heroSection = document.querySelector('.hero-section');
    if (heroSection) {
        observer.observe(heroSection);
            }
        }

    // Newsletter subscription
    $(document).on('click', 'footer .btn-primary', function() {
            const email = $(this).siblings('input[type="email"]').val();

    if (!email) {
        Swal.fire({
            title: 'Lỗi!',
            text: 'Vui lòng nhập địa chỉ email.',
            icon: 'error',
            confirmButtonText: 'OK'
        });
    return;
            }

    // Simple email validation
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        Swal.fire({
            title: 'Lỗi!',
            text: 'Vui lòng nhập địa chỉ email hợp lệ.',
            icon: 'error',
            confirmButtonText: 'OK'
        });
    return;
            }

    // Show success message
    Swal.fire({
        title: 'Thành công!',
    text: 'Cảm ơn bạn đã đăng ký nhận tin từ OnlineLearning!',
    icon: 'success',
    confirmButtonText: 'OK'
            });

    // Clear input
    $(this).siblings('input[type="email"]').val('');
        });

    // Add loading states for course cards
    $('.course-card').on('click', '.btn-view', function() {
            const $btn = $(this);
    const originalText = $btn.html();

    $btn.html('<i class="fas fa-spinner fa-spin me-1"></i> Đang tải...');
    $btn.prop('disabled', true);

    // Re-enable after a short delay (in case navigation is slow)
    setTimeout(function() {
        $btn.html(originalText);
    $btn.prop('disabled', false);
            }, 3000);
        });

    // Parallax effect for floating elements
    $(window).on('scroll', function() {
            const scrolled = $(this).scrollTop();
    const parallax = $('.floating-element');

    parallax.each(function() {
                const speed = 0.5;
    const yPos = -(scrolled * speed);
    $(this).css('transform', `translateY(${yPos}px) rotate(${scrolled * 0.1}deg)`);
            });
        });

    // Custom easing for smooth scroll
    $.easing.easeInOutCubic = function (x, t, b, c, d) {
            if ((t/=d/2) < 1) return c/2*t*t*t + b;
    return c/2*((t-=2)*t*t + 2) + b;
        };
</script>
<!--Enhanced Core Functions-- >
    <script>
        $(document).ready(function () {
            // Initialize modern components
            initializeModernComponents();

        // Loading Spinner with modern animation
        window.addEventListener('beforeunload', function () {
            $('#loading-spinner').fadeIn(300);
            });

        // Modern Scroll to Top Button
        const scrollToTopBtn = $('#scrollToTop');
        $(window).on('scroll', function () {
                if (window.scrollY > 300) {
            scrollToTopBtn.addClass('show');
                } else {
            scrollToTopBtn.removeClass('show');
                }
            });

        scrollToTopBtn.on('click', function (e) {
            e.preventDefault();
        $('html, body').animate({
            scrollTop: 0
                }, 800, 'easeInOutCubic');
            });

        // Enhanced Progress Bar
        @if (User.Identity.IsAuthenticated)
        {
            <text>
                $.ajax({
                    url: '/Course/GetAverageProgress',
                method: 'GET',
                success: function (data) {
                    animateProgressBar(data);
                        },
                error: function () {
                    console.error('Failed to fetch progress data');
                        }
                    });
            </text>
        }

            // Modern Toastr Configuration
        toastr.options = {
            closeButton: true,
        progressBar: true,
        positionClass: 'toast-bottom-right',
        timeOut: 4000,
        extendedTimeOut: 2000,
        showEasing: "easeOutBounce",
        hideEasing: "easeInBack",
        showMethod: "slideDown",
        hideMethod: "slideUp",
        preventDuplicates: true
            };

        // Enhanced Toast Notification Function
        window.showToast = function (title, message, type = 'info') {
                const icons = {
            success: '✅',
        error: '❌',
        warning: '⚠️',
        info: 'ℹ️'
                };

        const iconHtml = `<span style="font-size: 1.2em; margin-right: 8px;">${icons[type] || icons.info}</span>`;

        switch (type) {
                    case 'success':
        toastr.success(iconHtml + message, title);
        break;
        case 'error':
        case 'danger':
        toastr.error(iconHtml + message, title);
        break;
        case 'warning':
        toastr.warning(iconHtml + message, title);
        break;
        default:
        toastr.info(iconHtml + message, title);
                }
            };

        // Initialize modern tooltips and popovers
        initializeTooltipsAndPopovers();

        // Enhanced Active link handling
        updateActiveNavLinks();

        // Modern smooth scrolling for anchor links
        setupSmoothScrolling();

        // Modern dropdown hover effects
        setupDropdownEffects();

            // Initialize particle background effect
            if (window.innerWidth > 768) {
            initializeParticleBackground();
            }
        });

        // Initialize modern components
        function initializeModernComponents() {
            // Add loading animation to buttons
            $('.btn-modern, .btn-outline-modern').on('click', function () {
                const $btn = $(this);
                const originalText = $btn.html();

                if (!$btn.hasClass('loading')) {
                    $btn.addClass('loading');
                    $btn.html('<i class="fas fa-spinner fa-spin me-2"></i>Đang xử lý...');

                    setTimeout(() => {
                        $btn.removeClass('loading');
                        $btn.html(originalText);
                    }, 2000);
                }
            });

        // Add ripple effect to buttons
        $('.btn-modern, .btn-outline-modern, .nav-link').on('click', function(e) {
                const ripple = $('<span class="ripple"></span>');
        const rect = this.getBoundingClientRect();
        const size = Math.max(rect.width, rect.height);
        const x = e.clientX - rect.left - size / 2;
        const y = e.clientY - rect.top - size / 2;

        ripple.css({
            width: size,
        height: size,
        left: x,
        top: y,
        position: 'absolute',
        borderRadius: '50%',
        background: 'rgba(255, 255, 255, 0.3)',
        transform: 'scale(0)',
        animation: 'ripple 0.6s linear',
        pointerEvents: 'none'
                });

        $(this).css('position', 'relative').css('overflow', 'hidden').append(ripple);
                
                setTimeout(() => ripple.remove(), 600);
            });
        }

        // Animate progress bar
        function animateProgressBar(targetWidth) {
            const progressBar = $('.progress-bar');
        progressBar.animate({
            width: targetWidth + '%'
            }, {
            duration: 1500,
        easing: 'easeOutQuart',
        step: function(now) {
            $(this).attr('aria-valuenow', Math.round(now));
                }
            });
        }

        // Initialize modern tooltips and popovers
        function initializeTooltipsAndPopovers() {
            // Enhanced tooltips
            var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
                return new bootstrap.Tooltip(tooltipTriggerEl, {
            animation: true,
        delay: {show: 300, hide: 100 },
        html: true,
        placement: 'auto'
                });
            });

        // Enhanced popovers
        var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
        popoverTriggerList.map(function (popoverTriggerEl) {
                return new bootstrap.Popover(popoverTriggerEl, {
            animation: true,
        html: true,
        trigger: 'hover focus',
        placement: 'auto'
                });
            });
        }

        // Update active navigation links
        function updateActiveNavLinks() {
            const currentUrl = window.location.pathname;
        $('.navbar-nav .nav-link').removeClass('active');

        $('.navbar-nav .nav-link').each(function() {
                const href = $(this).attr('href');
        if (href === currentUrl || (currentUrl.startsWith(href) && href !== '/')) {
            $(this).addClass('active');
                }
            });
        }

        // Setup modern smooth scrolling
        function setupSmoothScrolling() {
            $('a[href^="#"]').on('click', function (e) {
                const target = $(this.getAttribute('href'));
                if (target.length) {
                    e.preventDefault();
                    $('html, body').animate({
                        scrollTop: target.offset().top - 100
                    }, 800, 'easeInOutCubic');
                }
            });
        }

        // Setup modern dropdown effects
        function setupDropdownEffects() {
            if (window.innerWidth >= 992) {
            $('.navbar .dropdown').hover(
                function () {
                    $(this).find('.dropdown-menu').first().stop(true, true).fadeIn(200).addClass('show');
                },
                function () {
                    $(this).find('.dropdown-menu').first().stop(true, true).fadeOut(200).removeClass('show');
                }
            );
            }

        // Add entrance animation to dropdown items
        $('.dropdown-menu').on('show.bs.dropdown hidden.bs.dropdown', function() {
            $(this).find('.dropdown-item').each(function (index) {
                $(this).css({
                    'animation-delay': (index * 50) + 'ms',
                    'animation-duration': '0.3s',
                    'animation-fill-mode': 'both',
                    'animation-name': 'slideInDown'
                });
            });
            });
        }

        // Initialize particle background effect
        function initializeParticleBackground() {
            const canvas = $('<canvas id="particles-canvas"></canvas>').css({
            position: 'fixed',
        top: 0,
        left: 0,
        width: '100%',
        height: '100%',
        'z-index': -1,
        'pointer-events': 'none'
            });

        $('body').prepend(canvas);

        // Simple particle animation
        const ctx = canvas[0].getContext('2d');
        const particles = [];

        for (let i = 0; i < 50; i++) {
            particles.push({
                x: Math.random() * window.innerWidth,
                y: Math.random() * window.innerHeight,
                radius: Math.random() * 2 + 1,
                vx: (Math.random() - 0.5) * 0.5,
                vy: (Math.random() - 0.5) * 0.5,
                opacity: Math.random() * 0.3 + 0.1
            });
            }

        function animateParticles() {
            ctx.clearRect(0, 0, canvas[0].width, canvas[0].height);
                
                particles.forEach(particle => {
            particle.x += particle.vx;
        particle.y += particle.vy;

        if (particle.x < 0 || particle.x > canvas[0].width) particle.vx *= -1;
        if (particle.y < 0 || particle.y > canvas[0].height) particle.vy *= -1;

        ctx.beginPath();
        ctx.arc(particle.x, particle.y, particle.radius, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(102, 126, 234, ${particle.opacity})`;
        ctx.fill();
                });

        requestAnimationFrame(animateParticles);
            }

        canvas[0].width = window.innerWidth;
        canvas[0].height = window.innerHeight;
        animateParticles();
            
            $(window).resize(() => {
            canvas[0].width = window.innerWidth;
        canvas[0].height = window.innerHeight;
            });
        }

        // Global utility functions with modern enhancements
        window.AppUtils = {
            // Format currency to VND with modern styling
            formatCurrency: function(amount) {
                return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
        currency: 'VND',
        minimumFractionDigits: 0
                }).format(amount);
            },

        // Format date to Vietnamese format
        formatDate: function(dateString) {
                const date = new Date(dateString);
        return new Intl.DateTimeFormat('vi-VN', {
            day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        weekday: 'long'
                }).format(date);
            },

        // Modern alert with SweetAlert2
        showAlert: function(title, message, type = 'info') {
                return Swal.fire({
            title: title,
        html: message,
        icon: type,
        confirmButtonText: 'Đồng ý',
        buttonsStyling: false,
        customClass: {
            confirmButton: 'btn btn-modern mx-2'
                    },
        showClass: {
            popup: 'animate__animated animate__fadeInUp animate__faster'
                    },
        hideClass: {
            popup: 'animate__animated animate__fadeOutDown animate__faster'
                    }
                });
            },

        // Modern confirm dialog
        confirmDialog: function(title, message, callback) {
            Swal.fire({
                title: title,
                html: message,
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Xác nhận',
                cancelButtonText: 'Hủy bỏ',
                buttonsStyling: false,
                customClass: {
                    confirmButton: 'btn btn-modern mx-2',
                    cancelButton: 'btn btn-outline-modern mx-2'
                },
                showClass: {
                    popup: 'animate__animated animate__zoomIn animate__faster'
                },
                hideClass: {
                    popup: 'animate__animated animate__zoomOut animate__faster'
                }
            }).then((result) => {
                if (result.isConfirmed && typeof callback === 'function') {
                    callback();
                }
            });
            },

        // Show loading overlay
        showLoading: function(message = 'Đang xử lý...') {
            $('#loading-spinner').find('h5').text(message);
        $('#loading-spinner').fadeIn(300);
            },

        // Hide loading overlay
        hideLoading: function() {
            $('#loading-spinner').fadeOut(300);
            }
        };

        // Add CSS animations for ripple effect
        $('<style>').text(`
            @keyframes ripple {
                to {
                transform: scale(4);
            opacity: 0;
                }
            }

            @keyframes slideInDown {
                from {
                opacity: 0;
            transform: translate3d(0, -20px, 0);
                }
            to {
                opacity: 1;
            transform: translate3d(0, 0, 0);
                }
            }

            .loading {
                pointer - events: none;
            opacity: 0.7;
            }
            `).appendTo('head');

            // Custom easing functions
            $.easing.easeInOutCubic = function (x, t, b, c, d) {
            if ((t/=d/2) < 1) return c/2*t*t*t + b;
            return c/2*((t-=2)*t*t + 2) + b;
        };

            $.easing.easeOutQuart = function (x, t, b, c, d) {
            return -c * ((t=t/d-1)*t*t*t - 1) + b;
        };

            $.easing.easeOutBounce = function (x, t, b, c, d) {
            if ((t/=d) < (1/2.75)) {
                return c*(7.5625*t*t) + b;
            } else if (t < (2/2.75)) {
                return c*(7.5625*(t-=(1.5/2.75))*t + .75) + b;
            } else if (t < (2.5/2.75)) {
                return c*(7.5625*(t-=(2.25/2.75))*t + .9375) + b;
            } else {
                return c*(7.5625*(t-=(2.625/2.75))*t + .984375) + b;
            }
        };
    </script>