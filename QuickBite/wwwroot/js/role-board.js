(function () {
    var board = document.querySelector('[data-role-board]');
    if (!board || !window.signalR) {
        return;
    }

    var group = board.getAttribute('data-role-board');
    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/orderHub')
        .withAutomaticReconnect()
        .build();

    var reload = function () {
        window.location.reload();
    };

    connection.on('NewOrder', reload);
    connection.on('OrderStatusChanged', reload);

    connection.start().then(function () {
        var method = group === 'kitchen'
            ? 'JoinKitchen'
            : (group === 'shipper' ? 'JoinShipper' : 'JoinStaff');
        return connection.invoke(method);
    }).catch(function () { });
})();
