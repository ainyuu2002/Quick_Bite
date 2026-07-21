(function () {
    "use strict";

    const root = document.querySelector("[data-orders-track]");
    if (!root) {
        return;
    }

    const statusUrl = root.dataset.statusUrl;
    const cancelledStatus = 5;
    const historyToggle = document.querySelector("[data-toggle-order-history]");
    let stopped = false;

    if (historyToggle) {
        historyToggle.addEventListener("click", function () {
            const isExpanded = historyToggle.getAttribute("aria-expanded") === "true";
            root.querySelectorAll('[data-order-history-extra="true"]').forEach(function (element) {
                element.classList.toggle("d-none", isExpanded);
            });

            historyToggle.setAttribute("aria-expanded", String(!isExpanded));
            historyToggle.textContent = isExpanded
                ? `Hiển thị tất cả (${historyToggle.dataset.totalOrders})`
                : "Thu gọn";
        });
    }

    root.querySelectorAll("[data-order-track]").forEach(function (orderElement) {
        const detailsToggle = orderElement.querySelector("[data-toggle-order-details]");
        const details = orderElement.querySelector("[data-order-details]");
        if (!detailsToggle || !details) {
            return;
        }

        detailsToggle.addEventListener("click", function () {
            const isExpanded = detailsToggle.getAttribute("aria-expanded") === "true";
            details.classList.toggle("d-none", isExpanded);
            detailsToggle.setAttribute("aria-expanded", String(!isExpanded));
            detailsToggle.textContent = isExpanded ? "Xem chi tiết" : "Thu gọn chi tiết";
        });
    });

    function updateProgress(orderElement, status, statusText) {
        const numericStatus = Number(status);
        if (!Number.isInteger(numericStatus)) {
            return;
        }

        const isCancelled = numericStatus === cancelledStatus;
        orderElement.querySelectorAll("[data-order-status]").forEach(function (step) {
            const stepStatus = Number(step.dataset.orderStatus);
            const isCurrent = !isCancelled && stepStatus === numericStatus;
            const isComplete = !isCancelled && stepStatus < numericStatus;

            step.classList.toggle("is-current", isCurrent);
            step.classList.toggle("is-complete", isComplete);
            if (isCurrent) {
                step.setAttribute("aria-current", "step");
            } else {
                step.removeAttribute("aria-current");
            }
        });

        orderElement.querySelectorAll("[data-current-status-text]").forEach(function (element) {
            element.textContent = statusText || "";
        });

        orderElement.querySelectorAll("[data-order-cancelled]").forEach(function (element) {
            element.classList.toggle("d-none", !isCancelled);
        });

        orderElement.querySelectorAll("[data-cancel-order-form]").forEach(function (element) {
            element.classList.toggle("d-none", numericStatus !== 0);
        });

        orderElement.querySelectorAll("[data-last-updated]").forEach(function (element) {
            element.textContent = new Date().toLocaleTimeString("vi-VN");
        });

        orderElement.dataset.currentStatus = String(numericStatus);
    }

    function updateStoppedState() {
        const orderElements = Array.from(root.querySelectorAll("[data-order-track]"));
        stopped = orderElements.length > 0 && orderElements.every(function (element) {
            const status = Number(element.dataset.currentStatus);
            return status === 4 || status === cancelledStatus;
        });
    }

    async function refreshStatus() {
        if (stopped || !statusUrl) {
            return;
        }

        try {
            const response = await fetch(statusUrl, {
                headers: { "Accept": "application/json" },
                cache: "no-store"
            });

            if (response.ok) {
                const payload = await response.json();
                (payload.orders || []).forEach(function (order) {
                    const orderElement = root.querySelector(
                        `[data-order-track][data-order-id="${Number(order.id)}"]`
                    );
                    if (orderElement) {
                        updateProgress(orderElement, order.status, order.statusText);
                    }
                });
                updateStoppedState();
            }
        } catch (error) {
            console.error("QuickBite: không thể cập nhật trạng thái đơn hàng.", error);
        }

        if (!stopped) {
            window.setTimeout(refreshStatus, 5000);
        }
    }

    root.querySelectorAll("[data-order-track]").forEach(function (orderElement) {
        updateProgress(
            orderElement,
            orderElement.dataset.currentStatus,
            orderElement.querySelector("[data-current-status-text]")?.textContent
        );
    });
    updateStoppedState();
    if (!stopped) {
        window.setTimeout(refreshStatus, 5000);
    }
}());
