// order-track.js — SignalR client phía khách theo dõi đơn (Dev D — Tùng)
// Cách dùng (trang Track của C):
//   <div id="orderTrack" data-order-id="@Model.OrderId">...</div>
//   <script src=".../signalr.min.js"></script>
//   <script src="~/js/order-track.js"></script>
// TODO (C + D, sáng ngày 2): thay updateProgressBar bằng cập nhật thanh tiến trình 5 bước thật (FR-09).
(function () {
    const root = document.getElementById("orderTrack");
    if (!root) return;                              // trang không có khối track → không làm gì
    const orderId = Number(root.dataset.orderId);
    if (!orderId) return;

    const conn = new signalR.HubConnectionBuilder()
        .withUrl("/orderHub")
        .withAutomaticReconnect()                   // FR-10 / NFR-06
        .build();

    function updateProgressBar(status, statusText) {
        const el = document.getElementById("status");
        if (el) el.textContent = statusText || status;
    }

    // FR-09: trạng thái cập nhật ≤ 2 giây, không reload
    conn.on("OrderStatusChanged", function (p) {
        updateProgressBar(p.status, p.statusText);
    });

    // FR-10: sau khi tự reconnect, đồng bộ lại trạng thái mới nhất (có thể đã đổi lúc rớt mạng)
    // Endpoint /api/orders/{id}/status theo SDS 3.4 — chốt với C ai làm.
    conn.onreconnected(function () {
        fetch("/api/orders/" + orderId + "/status")
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (p) { if (p) updateProgressBar(p.status, p.statusText); })
            .catch(function () { });
    });

    conn.start()
        .then(function () { return conn.invoke("WatchOrder", orderId); })
        .then(function () { console.log("SignalR: đang theo dõi đơn #" + orderId); })
        .catch(function (err) { console.error("SignalR:", err); });
})();
