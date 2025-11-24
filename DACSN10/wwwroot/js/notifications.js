// wwwroot/js/notifications.js
$(function () {

    const apiBase = '/Api/Notifications';

    const $bell = $('#notificationBell');          // nút chuông
    const $badge = $('#notificationBadge');        // số đỏ trên chuông
    const $dropdown = $('#notificationDropdown');  // khung popup
    const $list = $('#notificationList');          // <ul> chứa các item
    const $loading = $('#notificationLoading');    // vòng loading
    const $markAllBtn = $('#markAllNotificationsRead'); // nút "Đánh dấu tất cả đã đọc"

    function setLoading(isLoading) {
        if (isLoading) {
            $loading.show();
        } else {
            $loading.hide();
        }
    }

    // --------- Badge (số trên chuông) ---------
    function loadBadge() {
        $.get(apiBase + '/GetUnread')
            .done(function (res) {
                if (res && res.success && res.count > 0) {
                    $badge.text(res.count).show();
                } else {
                    $badge.hide();
                }
            })
            .fail(function () {
                // có thể log lỗi nếu muốn
            });
    }

    // --------- Danh sách thông báo ---------
    function renderNotifications(notifications) {
        $list.empty();

        if (!notifications || notifications.length === 0) {
            $list.append(
                '<li class="notification-empty">Không có thông báo nào.</li>'
            );
            return;
        }

        notifications.forEach(function (n) {
            const readClass = n.isRead ? 'notification-item read' : 'notification-item';
            const linkStart = n.link ? `<a href="${n.link}" class="notification-link">` : '<div class="notification-link">';
            const linkEnd = n.link ? '</a>' : '</div>';

            const html =
                `<li class="${readClass}" data-id="${n.notificationID}">
                    ${linkStart}
                        <div class="notification-title">${n.title}</div>
                        <div class="notification-message">${n.message}</div>
                        <div class="notification-meta">
                            <span class="notification-time">${n.timeAgo}</span>
                        </div>
                    ${linkEnd}
                </li>`;

            $list.append(html);
        });
    }

    function loadNotifications() {
        setLoading(true);

        $.get(apiBase + '/GetNotifications', { pageSize: 10, pageNumber: 1 })
            .done(function (res) {
                if (res && res.success) {
                    renderNotifications(res.data || []);
                } else {
                    renderNotifications([]);
                }
            })
            .fail(function () {
                renderNotifications([]);
            })
            .always(function () {
                setLoading(false);
            });
    }

    // --------- Sự kiện ---------

    // click chuông -> mở popup + load data
    $bell.on('click', function () {
        // bạn có thể toggle class show/hide ở đây, ví dụ:
        $dropdown.toggleClass('open');
        if ($dropdown.hasClass('open')) {
            loadNotifications();
        }
    });

    // click từng item -> mark as read
    $list.on('click', '.notification-item', function () {
        const id = $(this).data('id');
        if (!id) return;

        const $item = $(this);

        $.ajax({
            url: apiBase + '/MarkAsRead',
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(id)
        }).done(function (res) {
            if (res && res.success) {
                $item.addClass('read');
                loadBadge();
            }
        });
    });

    // nút "Đánh dấu tất cả đã đọc"
    $markAllBtn.on('click', function () {   
        $.ajax({
            url: apiBase + '/MarkAllAsRead',
            method: 'POST'
        }).done(function (res) {
            if (res && res.success) {
                $list.find('.notification-item').addClass('read');
                loadBadge();
            }
        });
    });

    // load badge ngay khi vào trang
    loadBadge();
});
