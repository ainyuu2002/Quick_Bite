(() => {
    "use strict";

    const selector = "select[data-custom-select]";
    let openInstance = null;
    let generatedId = 0;

    function optionId(selectId, index) {
        return `${selectId}-custom-option-${index}`;
    }

    function enabledIndexes(select) {
        return Array.from(select.options)
            .map((option, index) => ({ option, index }))
            .filter(item => !item.option.disabled)
            .map(item => item.index);
    }

    function nextEnabledIndex(select, current, direction) {
        const indexes = enabledIndexes(select);
        if (indexes.length === 0) return -1;

        const position = indexes.indexOf(current);
        if (position === -1) {
            return direction > 0 ? indexes[0] : indexes[indexes.length - 1];
        }

        const next = Math.min(Math.max(position + direction, 0), indexes.length - 1);
        return indexes[next];
    }

    function restoreAttribute(element, name, value) {
        if (value === null) element.removeAttribute(name);
        else element.setAttribute(name, value);
    }

    function enhanceSelect(select) {
        if (select.dataset.customSelectEnhanced === "true") return;

        const originalParent = select.parentNode;
        const originalNextSibling = select.nextSibling;
        const originalId = select.getAttribute("id");
        const originalTabIndex = select.getAttribute("tabindex");
        const originalAriaHidden = select.getAttribute("aria-hidden");
        const originalEnhancedState = select.getAttribute("data-custom-select-enhanced");
        const hadNativeClass = select.classList.contains("custom-select__native");
        const selectId = select.id || `custom-select-${++generatedId}`;
        const listboxId = `${selectId}-custom-listbox`;
        const label = select.labels?.[0];

        const root = document.createElement("div");
        root.className = "custom-select";

        const trigger = document.createElement("button");
        trigger.type = "button";
        trigger.className = "custom-select__trigger";
        trigger.setAttribute("role", "combobox");
        trigger.setAttribute("aria-haspopup", "listbox");
        trigger.setAttribute("aria-expanded", "false");
        trigger.setAttribute("aria-controls", listboxId);
        trigger.setAttribute("aria-label", label?.textContent?.trim() || select.name || "Chọn giá trị");

        const valueText = document.createElement("span");
        valueText.className = "custom-select__value";

        const chevron = document.createElement("span");
        chevron.className = "custom-select__chevron";
        chevron.setAttribute("aria-hidden", "true");

        const listbox = document.createElement("div");
        listbox.id = listboxId;
        listbox.className = "custom-select__menu";
        listbox.setAttribute("role", "listbox");
        listbox.hidden = true;

        const optionElements = Array.from(select.options).map((option, index) => {
            const item = document.createElement("div");
            item.id = optionId(selectId, index);
            item.className = "custom-select__option";
            item.setAttribute("role", "option");
            item.setAttribute("aria-selected", option.selected ? "true" : "false");
            item.dataset.index = String(index);
            item.textContent = option.textContent;

            if (option.disabled) {
                item.classList.add("is-disabled");
                item.setAttribute("aria-disabled", "true");
            }

            listbox.appendChild(item);
            return item;
        });

        let activeIndex = select.selectedIndex;

        function syncFromNative() {
            const selected = select.options[select.selectedIndex];
            valueText.textContent = selected?.textContent || "";
            trigger.disabled = select.disabled;
            root.classList.toggle("is-disabled", select.disabled);

            optionElements.forEach((item, index) => {
                const isSelected = index === select.selectedIndex;
                item.setAttribute("aria-selected", isSelected ? "true" : "false");
                item.classList.toggle("is-selected", isSelected);
            });
        }

        function setActive(index) {
            if (index < 0 || select.options[index]?.disabled) return;
            activeIndex = index;
            optionElements.forEach((item, itemIndex) => {
                item.classList.toggle("is-active", itemIndex === activeIndex);
            });
            trigger.setAttribute("aria-activedescendant", optionId(selectId, activeIndex));
            optionElements[activeIndex]?.scrollIntoView({ block: "nearest" });
        }

        function close({ restoreFocus = false } = {}) {
            if (listbox.hidden) return;
            listbox.hidden = true;
            root.classList.remove("is-open");
            trigger.setAttribute("aria-expanded", "false");
            trigger.removeAttribute("aria-activedescendant");
            optionElements.forEach(item => item.classList.remove("is-active"));
            if (openInstance?.root === root) openInstance = null;
            if (restoreFocus) trigger.focus();
        }

        function open() {
            if (trigger.disabled) return;
            if (openInstance && openInstance.root !== root) openInstance.close();
            listbox.hidden = false;
            root.classList.add("is-open");
            trigger.setAttribute("aria-expanded", "true");
            activeIndex = select.selectedIndex;
            setActive(activeIndex);
            openInstance = { root, close };
        }

        function choose(index) {
            const option = select.options[index];
            if (!option || option.disabled) return;
            select.selectedIndex = index;
            select.dispatchEvent(new Event("change", { bubbles: true }));
            syncFromNative();
            close({ restoreFocus: true });
        }

        trigger.append(valueText, chevron);
        root.append(trigger, listbox);

        trigger.addEventListener("click", () => {
            if (listbox.hidden) open(); else close();
        });

        trigger.addEventListener("keydown", event => {
            switch (event.key) {
                case "ArrowDown":
                    event.preventDefault();
                    if (listbox.hidden) open();
                    else setActive(nextEnabledIndex(select, activeIndex, 1));
                    break;
                case "ArrowUp":
                    event.preventDefault();
                    if (listbox.hidden) open();
                    else setActive(nextEnabledIndex(select, activeIndex, -1));
                    break;
                case "Home":
                    if (!listbox.hidden) {
                        event.preventDefault();
                        setActive(enabledIndexes(select)[0] ?? -1);
                    }
                    break;
                case "End":
                    if (!listbox.hidden) {
                        event.preventDefault();
                        const indexes = enabledIndexes(select);
                        setActive(indexes[indexes.length - 1] ?? -1);
                    }
                    break;
                case "Enter":
                case " ":
                    event.preventDefault();
                    if (listbox.hidden) open(); else choose(activeIndex);
                    break;
                case "Escape":
                    if (!listbox.hidden) {
                        event.preventDefault();
                        close({ restoreFocus: true });
                    }
                    break;
                case "Tab":
                    close();
                    break;
            }
        });

        listbox.addEventListener("pointerdown", event => event.preventDefault());
        listbox.addEventListener("click", event => {
            const item = event.target.closest(".custom-select__option");
            if (!item || !listbox.contains(item)) return;
            choose(Number(item.dataset.index));
        });

        listbox.addEventListener("pointermove", event => {
            const item = event.target.closest(".custom-select__option");
            if (!item || !listbox.contains(item)) return;
            setActive(Number(item.dataset.index));
        });

        let changeListenerAttached = false;

        try {
            select.id = selectId;
            originalParent.insertBefore(root, select);
            root.prepend(select);
            select.classList.add("custom-select__native");
            select.tabIndex = -1;
            select.setAttribute("aria-hidden", "true");
            select.dataset.customSelectEnhanced = "true";
            select.addEventListener("change", syncFromNative);
            changeListenerAttached = true;
            root.classList.add("is-enhanced");
            syncFromNative();
        } catch (error) {
            if (changeListenerAttached) {
                select.removeEventListener("change", syncFromNative);
            }

            if (root.contains(select)) {
                const restoreBefore = originalNextSibling?.parentNode === originalParent
                    ? originalNextSibling
                    : null;
                originalParent.insertBefore(select, restoreBefore);
            }

            root.remove();
            if (!hadNativeClass) select.classList.remove("custom-select__native");
            restoreAttribute(select, "id", originalId);
            restoreAttribute(select, "tabindex", originalTabIndex);
            restoreAttribute(select, "aria-hidden", originalAriaHidden);
            restoreAttribute(select, "data-custom-select-enhanced", originalEnhancedState);
            throw error;
        }
    }

    document.addEventListener("pointerdown", event => {
        if (openInstance && !openInstance.root.contains(event.target)) {
            openInstance.close();
        }
    });

    document.addEventListener("DOMContentLoaded", () => {
        document.querySelectorAll(selector).forEach(select => {
            try {
                enhanceSelect(select);
            } catch (error) {
                console.error("QuickBite custom select initialization failed", error);
            }
        });
    });
})();
