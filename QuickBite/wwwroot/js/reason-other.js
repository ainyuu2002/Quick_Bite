document.addEventListener('change', function (event) {
    var select = event.target;
    if (!select.matches || !select.matches('[data-reason-select]')) {
        return;
    }

    var group = select.closest('[data-reason-group]');
    if (!group) {
        return;
    }

    var other = group.querySelector('[data-reason-other]');
    if (!other) {
        return;
    }

    var isOther = select.value === '__other__';
    other.hidden = !isOther;
    other.required = isOther;
    if (!isOther) {
        other.value = '';
    }
});
