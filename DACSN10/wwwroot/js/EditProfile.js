// Edit Profile JavaScript
(function () {
    'use strict';

    // Initialize AOS
    AOS.init({
        duration: 1000,
        easing: 'ease-out-quart',
        once: true
    });

    // Configure toastr
    toastr.options = {
        "closeButton": true,
        "progressBar": true,
        "positionClass": "toast-top-right",
        "timeOut": 5000
    };

    // DOM Ready
    $(document).ready(function () {
        initializeFormHandlers();
        initializeValidation();
        initializeProgressAnimation();
        initializeInputAnimations();
        showTempDataMessages();
    });

    function initializeFormHandlers() {
        // Form validation and submission
        $('#profileForm').on('submit', function (e) {
            e.preventDefault();

            if (!validateForm()) {
                return false;
            }

            showLoadingState();

            // Submit form after delay
            setTimeout(() => {
                this.submit();
            }, 1000);
        });
    }

    function initializeValidation() {
        // Real-time validation
        $('input[required]').on('blur', function () {
            validateField($(this));
        });
    }

    function initializeProgressAnimation() {
        // Progress indicator animation
        setTimeout(() => {
            $('.progress-step').eq(1).addClass('active');
        }, 2000);

        setTimeout(() => {
            $('.progress-step').eq(2).addClass('active');
        }, 4000);
    }

    function initializeInputAnimations() {
        // Input focus animations
        $('.form-control-custom').on('focus', function () {
            $(this).parent().addClass('focused');
        }).on('blur', function () {
            $(this).parent().removeClass('focused');
        });
    }

    function showTempDataMessages() {
        // Show success/error messages from TempData
        if (window.EditProfileData) {
            if (window.EditProfileData.hasSuccess === 'true') {
                toastr.success(window.EditProfileData.successMessage);
            }

            if (window.EditProfileData.hasError === 'true') {
                toastr.error(window.EditProfileData.errorMessage);
            }
        }
    }

    function validateForm() {
        let isValid = true;

        // Validate required fields
        $('input[required]').each(function () {
            if (!validateField($(this))) {
                isValid = false;
            }
        });

        // Validate email format
        const email = $('input[type="email"]').val();
        if (email && !isValidEmail(email)) {
            showFieldError($('input[type="email"]'), 'Email không hợp lệ');
            isValid = false;
        }

        // Validate phone number
        const phone = $('input[type="tel"]').val();
        if (phone && !isValidPhone(phone)) {
            showFieldError($('input[type="tel"]'), 'Số điện thoại không hợp lệ');
            isValid = false;
        }

        return isValid;
    }

    function validateField(field) {
        const value = field.val().trim();

        if (field.prop('required') && !value) {
            showFieldError(field, 'Trường này không được để trống');
            return false;
        }

        hideFieldError(field);
        return true;
    }

    function showFieldError(field, message) {
        hideFieldError(field);
        field.addClass('is-invalid');
        field.after(`<div class="field-validation-error">${message}</div>`);
    }

    function hideFieldError(field) {
        field.removeClass('is-invalid');
        field.next('.field-validation-error').remove();
    }

    function isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    function isValidPhone(phone) {
        const phoneRegex = /^[0-9+\-\s()]{10,15}$/;
        return phoneRegex.test(phone);
    }

    function showLoadingState() {
        const saveBtn = $('#saveBtn');
        const saveSpinner = $('#saveSpinner');

        saveBtn.prop('disabled', true);
        saveBtn.find('span').text('Đang lưu...');
        saveSpinner.show();
    }

    // Avatar preview function (global scope for inline onclick)
    window.previewAvatar = function (event) {
        const file = event.target.files[0];
        if (file) {
            // Validate file type
            if (!file.type.startsWith('image/')) {
                Swal.fire({
                    icon: 'error',
                    title: 'Lỗi!',
                    text: 'Vui lòng chọn file hình ảnh!'
                });
                return;
            }

            // Validate file size (max 5MB)
            if (file.size > 5 * 1024 * 1024) {
                Swal.fire({
                    icon: 'error',
                    title: 'Lỗi!',
                    text: 'Kích thước file không được vượt quá 5MB!'
                });
                return;
            }

            const reader = new FileReader();
            reader.onload = function (e) {
                $('#avatarPreview').attr('src', e.target.result);

                // Show success message
                Swal.fire({
                    icon: 'success',
                    title: 'Avatar đã được tải lên!',
                    text: 'Hãy lưu thay đổi để cập nhật avatar',
                    showConfirmButton: false,
                    timer: 2000,
                    toast: true,
                    position: 'top-end'
                });
            };
            reader.onerror = function () {
                Swal.fire({
                    icon: 'error',
                    title: 'Lỗi!',
                    text: 'Không thể đọc file hình ảnh!'
                });
            };
            reader.readAsDataURL(file);
        }
    };

    // Additional utility functions
    function resetForm() {
        $('#profileForm')[0].reset();
        $('.field-validation-error').remove();
        $('.form-control-custom').removeClass('is-invalid');
    }

    function enableForm() {
        $('#profileForm input, #profileForm button').prop('disabled', false);
        $('#saveBtn span').text('Lưu thay đổi');
        $('#saveSpinner').hide();
    }

    function disableForm() {
        $('#profileForm input, #profileForm button').prop('disabled', true);
    }

    // Keyboard shortcuts
    $(document).keydown(function (e) {
        // Ctrl/Cmd + S: Save form
        if ((e.ctrlKey || e.metaKey) && e.which === 83) {
            e.preventDefault();
            $('#profileForm').submit();
        }

        // Ctrl/Cmd + R: Reset form
        if ((e.ctrlKey || e.metaKey) && e.which === 82) {
            e.preventDefault();
            if (confirm('Bạn có muốn reset form không?')) {
                resetForm();
            }
        }
    });

    // Form change detection
    let formChanged = false;
    $('#profileForm input').on('input change', function () {
        formChanged = true;
    });

    // Warn before leaving page if form has changes
    $(window).on('beforeunload', function (e) {
        if (formChanged) {
            const message = 'Bạn có thay đổi chưa được lưu. Bạn có chắc chắn muốn rời khỏi trang này?';
            e.returnValue = message;
            return message;
        }
    });

    // Remove warning when form is submitted
    $('#profileForm').on('submit', function () {
        formChanged = false;
    });

    // Auto-save draft (optional feature)
    let autoSaveTimer;
    function enableAutoSave() {
        $('#profileForm input').on('input', function () {
            clearTimeout(autoSaveTimer);
            autoSaveTimer = setTimeout(saveDraft, 2000); // Save after 2 seconds of inactivity
        });
    }

    function saveDraft() {
        const formData = {
            HoTen: $('input[name="HoTen"]').val(),
            Email: $('input[name="Email"]').val(),
            PhoneNumber: $('input[name="PhoneNumber"]').val(),
            timestamp: new Date().getTime()
        };

        try {
            localStorage.setItem('editProfileDraft', JSON.stringify(formData));
            showDraftSavedIndicator();
        } catch (e) {
            console.warn('Cannot save draft to localStorage:', e);
        }
    }

    function loadDraft() {
        try {
            const draft = localStorage.getItem('editProfileDraft');
            if (draft) {
                const draftData = JSON.parse(draft);

                // Check if draft is not too old (24 hours)
                const now = new Date().getTime();
                const draftAge = now - draftData.timestamp;
                const maxAge = 24 * 60 * 60 * 1000; // 24 hours in milliseconds

                if (draftAge < maxAge) {
                    Swal.fire({
                        title: 'Khôi phục bản nháp?',
                        text: 'Tìm thấy bản nháp đã lưu trước đó. Bạn có muốn khôi phục không?',
                        icon: 'question',
                        showCancelButton: true,
                        confirmButtonText: 'Khôi phục',
                        cancelButtonText: 'Bỏ qua'
                    }).then((result) => {
                        if (result.isConfirmed) {
                            restoreDraft(draftData);
                        }
                    });
                } else {
                    // Remove old draft
                    localStorage.removeItem('editProfileDraft');
                }
            }
        } catch (e) {
            console.warn('Cannot load draft from localStorage:', e);
        }
    }

    function restoreDraft(draftData) {
        $('input[name="HoTen"]').val(draftData.HoTen || '');
        $('input[name="Email"]').val(draftData.Email || '');
        $('input[name="PhoneNumber"]').val(draftData.PhoneNumber || '');

        toastr.success('Đã khôi phục bản nháp!');
        formChanged = true;
    }

    function clearDraft() {
        try {
            localStorage.removeItem('editProfileDraft');
        } catch (e) {
            console.warn('Cannot clear draft from localStorage:', e);
        }
    }

    function showDraftSavedIndicator() {
        const indicator = $('<div class="draft-indicator">Đã lưu nháp</div>');
        indicator.css({
            position: 'fixed',
            bottom: '20px',
            right: '20px',
            background: '#10b981',
            color: 'white',
            padding: '8px 16px',
            borderRadius: '20px',
            fontSize: '0.9rem',
            zIndex: 9999,
            opacity: 0
        });

        $('body').append(indicator);
        indicator.animate({ opacity: 1 }, 300);

        setTimeout(() => {
            indicator.animate({ opacity: 0 }, 300, function () {
                $(this).remove();
            });
        }, 2000);
    }

    // Form submission success handler
    $('#profileForm').on('submit', function () {
        clearDraft(); // Clear draft when form is successfully submitted
    });

    // Initialize features based on browser capabilities
    function initializeOptionalFeatures() {
        // Check if localStorage is available
        if (typeof (Storage) !== "undefined") {
            loadDraft();
            enableAutoSave();
        }

        // Check if geolocation is available (for timezone detection)
        if (navigator.geolocation) {
            // Could be used for timezone detection in profile
        }
    }

    // Accessibility enhancements
    function enhanceAccessibility() {
        // Add ARIA labels to form controls
        $('.form-control-custom').each(function () {
            const label = $(this).closest('.form-group').find('.form-label').text().trim();
            $(this).attr('aria-label', label);
        });

        // Add role attributes
        $('.validation-summary').attr('role', 'alert');
        $('.field-validation-error').attr('role', 'alert');

        // Keyboard navigation for custom elements
        $('.avatar-upload-btn').on('keydown', function (e) {
            if (e.which === 13 || e.which === 32) { // Enter or Space
                e.preventDefault();
                $('#avatarInput').click();
            }
        });
    }

    // Mobile-specific enhancements
    function enhanceMobileExperience() {
        // Add input type optimizations for mobile
        $('input[name="PhoneNumber"]').attr('inputmode', 'tel');
        $('input[name="Email"]').attr('inputmode', 'email');

        // Handle mobile keyboard events
        $('.form-control-custom').on('focus', function () {
            if (window.innerHeight < 500) { // Keyboard is likely open
                setTimeout(() => {
                    this.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }, 300);
            }
        });
    }

    // Theme detection and adaptation
    function adaptToTheme() {
        // Check for dark mode preference
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            // Could adjust colors for dark mode
            console.log('Dark mode detected');
        }

        // Listen for theme changes
        if (window.matchMedia) {
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
                if (e.matches) {
                    console.log('Switched to dark mode');
                } else {
                    console.log('Switched to light mode');
                }
            });
        }
    }

    // Performance optimizations
    function optimizePerformance() {
        // Debounce resize events
        let resizeTimer;
        $(window).on('resize', function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(handleResize, 250);
        });

        // Lazy load non-critical features
        setTimeout(() => {
            initializeOptionalFeatures();
            enhanceAccessibility();
            enhanceMobileExperience();
            adaptToTheme();
        }, 100);
    }

    function handleResize() {
        // Handle responsive adjustments if needed
        const isMobile = window.innerWidth < 768;

        if (isMobile) {
            // Mobile-specific adjustments
            $('.profile-card').css('padding', '2rem 1.5rem');
        } else {
            // Desktop adjustments
            $('.profile-card').css('padding', '3rem');
        }
    }

    // Error handling and fallbacks
    window.addEventListener('error', function (e) {
        console.error('JavaScript Error in edit-profile.js:', e);

        // Graceful degradation - ensure form still works
        if (e.message.includes('AOS') || e.message.includes('toastr') || e.message.includes('Swal')) {
            console.warn('Animation/notification library failed, continuing with basic functionality');
        }
    });

    // Initialize performance optimizations
    optimizePerformance();

    // Export functions for testing or external use
    window.EditProfileModule = {
        validateForm: validateForm,
        resetForm: resetForm,
        saveDraft: saveDraft,
        loadDraft: loadDraft,
        clearDraft: clearDraft
    };

})();