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
