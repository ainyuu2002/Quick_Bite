// admin-orders.js — SignalR client phía admin/staff (Dev D — Tùng)
// Cách dùng (trang /Pages/Admin/Orders của A, hoặc trang test /Staff/Board):
//   <script src=".../signalr.min.js"></script>
//   <script src="~/js/admin-orders.js"></script>
// Phần tử trang có thì dùng, không có thì bỏ qua (không lỗi):
//   #orderList       — <ul>/<tbody> chứa danh sách đơn
//   #staffOnline     — chỗ hiện số màn hình staff online
//   #newOrderBadge   — badge đếm đơn mới (FR-17)
//   #newOrderSound   — <audio> âm báo đơn mới (FR-17)
(function () {
    const conn = new signalR.HubConnectionBuilder()
        .withUrl("/orderHub")
        .withAutomaticReconnect()   // FR-10
        .build();

    // FR-17: đơn mới chèn đầu danh sách
    function prependOrderRow(o) {
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

    // FR-17: âm báo
    function playSound() {
        try {
            const ctx = new (window.AudioContext || window.webkitAudioContext)();
            const osc = ctx.createOscillator();
            osc.type = "sine";
            osc.frequency.value = 880;
            osc.connect(ctx.destination);
            osc.start();
            osc.stop(ctx.currentTime + 0.15);
        } catch (e) { /* trình duyệt chặn autoplay → bỏ qua, không lỗi */ }
    }
    conn.on("NewOrder", function (o) {
        prependOrderRow(o);
        bumpBadge();
        playSound();
    });

    // SDS 3.3: OrderStatusChanged gửi tới CẢ group "staff" — để dòng đơn nhảy tab bên admin.
    // TODO (A + D, sáng ngày 2): thay phần thân bằng logic chuyển tab thật của /Pages/Admin/Orders.
    conn.on("OrderStatusChanged", function (p) {
        const li = document.getElementById("order-" + p.orderId);
        if (!li) return;
        if (!li.dataset.base) li.dataset.base = li.textContent;
        li.textContent = li.dataset.base + " — [" + (p.statusText || p.status) + "]";
        li.style.textDecoration =
            (p.status === "Cancelled" || p.status === 5) ? "line-through" : "";
    });

    // Bổ sung của D (đã ghi SDS 3.3): số màn hình staff online
    conn.on("StaffOnlineChanged", function (count) {
        const el = document.getElementById("staffOnline");
        if (el) el.textContent = count;
    });

    conn.start()
        .then(function () { return conn.invoke("JoinStaff"); })
        .then(function () { console.log("SignalR: đã kết nối + JoinStaff"); })
        .catch(function (err) { console.error("SignalR:", err); });
})();
