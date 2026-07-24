// admin-orders.js — SignalR client phía admin/staff (Dev D — Tùng; mở rộng bảng admin: Dev A — Dũng)
// Cách dùng (trang /Pages/Admin/Orders của A, hoặc trang test /Staff/Board):
//   <script src=".../signalr.min.js"></script>
//   <script src="~/js/admin-orders.js"></script>
// Phần tử trang có thì dùng, không có thì bỏ qua (không lỗi):
//   #ordersTableBody — <tbody> bảng đơn của /Admin/Orders, có data-current-status = tab đang xem
//   #orderList       — <ul> danh sách đơn (trang test /Staff/Board của D)
//   #liveBanner      — băng "có đơn mới / đơn vừa đổi" + nút Tải lại
//   #staffOnline     — chỗ hiện số màn hình staff online
//   #newOrderBadge   — badge đếm đơn mới (FR-17)
//   #liveDot / #liveLabel — chỉ báo realtime đang sống
(function () {
    const STATUS_PENDING = 0;
    const STATUS_CANCELLED = 5;

    const conn = new signalR.HubConnectionBuilder()
        .withUrl("/orderHub")
        .withAutomaticReconnect()   // FR-10
        .build();

    let tableBody = document.getElementById("ordersTableBody");

    // Tab đang xem. Trang admin lọc theo trạng thái nên không phải đơn nào cũng thuộc
    // bảng đang hiển thị — chỗ này quyết định "chèn thẳng" hay "báo rồi để user tải lại".
    const currentStatus = tableBody
        ? parseInt(tableBody.dataset.currentStatus, 10)
        : null;

    // ---- Băng thông báo (chỉ dùng khi không làm mới được bảng) ----
    let bannerCount = 0;
    function showBanner(message) {
        const banner = document.getElementById("liveBanner");
        if (!banner) return;
        bannerCount++;
        const text = document.getElementById("liveBannerText");
        if (text) {
            text.textContent = bannerCount === 1
                ? message
                : `${message} (và ${bannerCount - 1} thay đổi khác)`;
        }
        banner.hidden = false;
    }

    // ---- Làm mới bảng đơn ----
    // Cố ý KHÔNG dựng HTML dòng đơn bằng JS: dòng đơn có form đổi trạng thái, form hủy,
    // token chống CSRF, bảng món, địa chỉ... — dựng lại ở client là chép đôi markup của
    // Index.cshtml và chắc chắn lệch. Thay vào đó tải chính trang đang xem rồi thay <tbody>:
    // server đã lo đúng bộ lọc tab, tìm kiếm, sắp xếp, phân trang.
    let refreshing = false;
    let refreshQueued = false;

    async function refreshTable(highlightOrderId) {
        if (!tableBody) return false;
        if (refreshing) { refreshQueued = true; return true; }   // gộp nhiều sự kiện dồn dập làm một
        refreshing = true;

        try {
            const response = await fetch(location.href, { credentials: "same-origin" });
            if (!response.ok) return false;

            const doc = new DOMParser().parseFromString(await response.text(), "text/html");
            const freshBody = doc.getElementById("ordersTableBody");
            if (!freshBody) return false;   // phiên hết hạn → trả về trang đăng nhập

            // Giữ lại những đơn đang mở <details>, nếu không nhân viên đang đọc dở sẽ bị sập hết.
            const openIds = Array.from(document.querySelectorAll(".order-details[open]"))
                .map(d => d.closest("tr[data-details-for]")?.dataset.detailsFor)
                .filter(Boolean);

            tableBody.replaceWith(freshBody);
            tableBody = freshBody;

            openIds.forEach(id => {
                const details = document.querySelector(
                    `tr[data-details-for="${id}"] .order-details`);
                if (details) details.open = true;
            });

            const freshCount = doc.getElementById("resultCount");
            const count = document.getElementById("resultCount");
            if (freshCount && count) count.textContent = freshCount.textContent;

            if (highlightOrderId) {
                const row = document.querySelector(`tr[data-order-id="${highlightOrderId}"]`);
                if (row) row.classList.add("row--new");
            }
            return true;
        } catch (err) {
            console.error("Làm mới bảng đơn thất bại:", err);
            return false;
        } finally {
            refreshing = false;
            if (refreshQueued) { refreshQueued = false; refreshTable(); }
        }
    }

    // Trang test /Staff/Board của Dev D — không có bảng, vẫn chèn <li> như cũ.
    function prependOrderListItem(o) {
        const list = document.getElementById("orderList");
        if (!list) return;
        const li = document.createElement("li");
        li.id = "order-" + o.id;
        li.dataset.base = "Đơn #" + o.id + " — " + o.customerName + " — " + o.total + "đ";
        li.textContent = li.dataset.base;
        list.prepend(li);
    }

    // FR-17: badge đếm đơn mới
    function bumpBadge() {
        const badge = document.getElementById("newOrderBadge");
        if (!badge) return;
        badge.textContent = (parseInt(badge.textContent) || 0) + 1;
    }

    // FR-17: âm báo — AudioContext phải được "mở khóa" bằng cú click đầu tiên (chính sách autoplay)
    let audioCtx = null;
    document.addEventListener("click", function () {
        if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)();
    }, { once: true });

    function playSound() {
        if (!audioCtx) return;                                 // chưa ai click vào trang → đành im
        if (audioCtx.state === "suspended") audioCtx.resume(); // context ngủ → đánh thức
        const osc = audioCtx.createOscillator();
        osc.type = "sine";
        osc.frequency.value = 880;
        osc.connect(audioCtx.destination);
        osc.start();
        osc.stop(audioCtx.currentTime + 0.15);
    }

    conn.on("NewOrder", async function (o) {
        // Để lúc gỡ lỗi nhìn được payload thật thay vì phải đoán.
        console.debug("SignalR NewOrder:", o, "| tab đang xem:", currentStatus);
        bumpBadge();
        playSound();

        if (!tableBody) { prependOrderListItem(o); return; }

        // Đơn mới luôn là Pending: chỉ tab đó mới có gì để hiện thêm.
        // Tab khác vẫn báo, vì con số ở các tab kia không đổi nhưng người trực cần biết.
        if (currentStatus === STATUS_PENDING) {
            if (!await refreshTable(o.id)) showBanner(`Có đơn mới #${o.id} vừa vào.`);
        } else {
            showBanner(`Có đơn mới #${o.id} vừa vào (ở tab Chờ xác nhận).`);
        }
    });

    // SDS 3.3: OrderStatusChanged gửi tới CẢ group "staff" — để dòng đơn nhảy tab bên admin.
    conn.on("OrderStatusChanged", async function (p) {
        if (tableBody) {
            const onThisPage = !!document.querySelector(`tr[data-order-id="${p.orderId}"]`);
            const belongsHere = p.status === currentStatus;

            // Đơn vừa rời khỏi tab này (onThisPage) hoặc vừa rơi vào tab này (belongsHere)
            // — cả hai đều làm danh sách sai, phải nạp lại. Ngoài ra thì mặc kệ.
            if (!onThisPage && !belongsHere) return;

            // Đơn vừa vào tab này thì làm nổi lên cho dễ thấy.
            if (!await refreshTable(belongsHere && !onThisPage ? p.orderId : null)) {
                showBanner(`Đơn #${p.orderId} đã chuyển sang "${p.statusText}".`);
            }
            return;
        }

        // Trang test /Staff/Board của Dev D
        const li = document.getElementById("order-" + p.orderId);
        if (!li) return;
        if (!li.dataset.base) li.dataset.base = li.textContent;
        li.textContent = li.dataset.base + " — [" + (p.statusText || p.status) + "]";
        li.style.textDecoration =
            (p.status === "Cancelled" || p.status === STATUS_CANCELLED) ? "line-through" : "";
    });

    // Bổ sung của D (đã ghi SDS 3.3): số màn hình staff online
    conn.on("StaffOnlineChanged", function (count) {
        const el = document.getElementById("staffOnline");
        if (el) el.textContent = count;
    });

    // Chỉ báo trạng thái kết nối — lúc demo nhìn là biết realtime còn sống hay không.
    function setLive(on) {
        const dot = document.getElementById("liveDot");
        const label = document.getElementById("liveLabel");
        if (dot) { dot.hidden = false; dot.classList.toggle("is-off", !on); }
        if (label) { label.hidden = false; label.textContent = on ? "Trực tuyến" : "Mất kết nối"; }
    }
    conn.onreconnecting(() => setLive(false));
    conn.onreconnected(function () {
        setLive(true);
        // Reconnect tạo connectionId MỚI: nó không còn trong group "staff" và chưa được
        // chấm công. Phải gọi lại JoinStaff để vào lại group và mở ca cho kết nối mới.
        conn.invoke("JoinStaff").catch(err => console.error("JoinStaff (reconnect):", err));
    });
    conn.onclose(() => setLive(false));

    conn.start()
        .then(function () { return conn.invoke("JoinStaff"); })
        .then(function () { setLive(true); console.log("SignalR: đã kết nối + JoinStaff"); })
        .catch(function (err) { setLive(false); console.error("SignalR:", err); });
})();
