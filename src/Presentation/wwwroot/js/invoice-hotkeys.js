window.behsazanInvoiceHotkeys = (function () {
    let handler = null;
    let dotNetRef = null;

    function onKeyDown(e) {
        if (!e.altKey || e.ctrlKey || e.metaKey || e.shiftKey)
            return;

        let action = null;
        switch (e.code) {
            case 'KeyN':
                action = 'add';
                break;
            case 'KeyC':
                action = 'copy';
                break;
            case 'KeyV':
                action = 'paste';
                break;
            default:
                return;
        }

        e.preventDefault();
        e.stopPropagation();

        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('HandleInvoiceHotkey', action);
        }
    }

    return {
        register: function (ref) {
            this.unregister();
            dotNetRef = ref;
            handler = onKeyDown;
            document.addEventListener('keydown', handler, true);
        },
        unregister: function () {
            if (handler) {
                document.removeEventListener('keydown', handler, true);
                handler = null;
            }
            if (dotNetRef) {
                try { dotNetRef.dispose(); } catch (_) { /* ignore */ }
                dotNetRef = null;
            }
        }
    };
})();
