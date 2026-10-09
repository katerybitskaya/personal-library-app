(function () {
    const nativeFetch = window.fetch.bind(window);
    let redirecting = false;
    window.fetch = async (...args) => {
        const response = await nativeFetch(...args);
        if (response.status !== 401) return response;
        if (!redirecting) {
            redirecting = true;
            showToast(document.body.dataset.sessionExpired || 'Your session has expired. Please sign in again.', 'error');
            const returnUrl = location.pathname + location.search;
            setTimeout(() => { location.href = '/Account/Login?returnUrl=' + encodeURIComponent(returnUrl); }, 1800);
        }
        return new Promise(() => {});
    };
})();

function escapeHtml(value) {
    return String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

function networkErrorText() {
    return document.body.dataset.errorNetwork || 'Network error. Please try again.';
}

function genericErrorText() {
    return document.body.dataset.errorGeneric || 'Something went wrong. Please try again.';
}

function syncModalScrollLock() {
    document.body.classList.toggle('modal-open', !!document.querySelector('.modal-overlay.open'));
}

function openModal(id) {
    const overlay = document.getElementById(id);
    if (!overlay) return;
    if (overlay.parentElement !== document.body) { overlay.dataset.moved = '1'; document.body.appendChild(overlay); }
    overlay.scrollTop = 0;
    overlay.classList.add('open');
    syncModalScrollLock();
    setTimeout(() => {
        const first = overlay.querySelector('input:not([type=hidden]),textarea');
        if (first) first.focus();
    }, 260);
    overlay._bgHandler = (e) => { if (e.target === overlay) closeModal(id); };
    overlay.addEventListener('click', overlay._bgHandler);
}

function closeModal(id) {
    const overlay = document.getElementById(id);
    if (!overlay) return;
    overlay.classList.remove('open');
    if (overlay._bgHandler) overlay.removeEventListener('click', overlay._bgHandler);
    syncModalScrollLock();
}

document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        document.querySelectorAll('.modal-overlay.open').forEach(el => {
            el.classList.remove('open');
        });
        syncModalScrollLock();
    }
});

function showToast(message, type = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;
    const colors = { success: 'var(--gold)', error: '#e07070' };
    const icons  = { success: '✓', error: '✕' };
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.innerHTML = `<span style="font-weight:700;color:${colors[type]||colors.success}">${icons[type]||''}</span>${message}`;
    container.appendChild(toast);
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(20px)';
        toast.style.transition = 'all 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 3500);
}

function openConfirmModal(message, onConfirm) {
    const existing = document.getElementById('_confirmModal');
    if (existing) existing.remove();

    const b = document.body;
    const titleTxt  = b.dataset.confirmTitle  || 'Confirm';
    const cancelTxt = b.dataset.confirmCancel || 'Cancel';
    const okTxt     = b.dataset.confirmOk     || 'Confirm';

    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.id = '_confirmModal';
    overlay.innerHTML = `
        <div class="modal" style="max-width:420px">
            <h2 class="modal-title">${titleTxt}</h2>
            <p class="confirm-message">${message}</p>
            <div class="modal-footer">
                <button class="btn btn-ghost" onclick="closeModal('_confirmModal')">${cancelTxt}</button>
                <button class="btn btn-danger" id="_confirmBtn">${okTxt}</button>
            </div>
        </div>`;
    document.body.appendChild(overlay);
    requestAnimationFrame(() => { overlay.classList.add('open'); syncModalScrollLock(); });

    document.getElementById('_confirmBtn').addEventListener('click', async () => {
        closeModal('_confirmModal');
        await onConfirm();
    });
    overlay.addEventListener('click', e => { if (e.target === overlay) closeModal('_confirmModal'); });
}

function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

function initFilePicker(inputId, previewId, btnId) {
    const input   = document.getElementById(inputId);
    const preview = previewId ? document.getElementById(previewId) : null;
    const btn     = document.getElementById(btnId);
    if (!input) return;

    if (btn) btn.addEventListener('click', () => input.click());

    input.addEventListener('change', function () {
        const file = this.files[0];
        if (!file) return;
        if (btn) {
            btn.innerHTML = `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/></svg>${file.name}`;
            btn.style.color = 'var(--gold-light)';
            btn.style.borderColor = 'var(--gold-muted)';
        }
        if (preview) {
            const reader = new FileReader();
            reader.onload = e => {
                preview.src = e.target.result;
                preview.style.display = 'block';
                preview.classList.add('has-image');
            };
            reader.readAsDataURL(file);
        }
    });
}

function syncFlagEditor(formEl) {
    if (!formEl) return;
    const missingCb = formEl.querySelector('input[name="IsMissing"][type="checkbox"]');
    const missing = missingCb ? missingCb.checked : formEl.dataset.missing === 'true';
    formEl.querySelectorAll('.flag-chip').forEach(chip => {
        const ok = missing === (chip.dataset.missingOk === 'true');
        const cb = chip.querySelector('input');
        chip.hidden = !ok;
        cb.disabled = !ok;
        if (!ok) cb.checked = false;
    });
    const anyChecked = [...formEl.querySelectorAll('input[name="Flags"]')].some(cb => cb.checked);
    const note = formEl.querySelector('textarea[name="Note"]');
    if (note) note.disabled = !anyChecked;
}

function initFlagAutoSave(formEl) {
    if (!formEl) return;
    const status = formEl.querySelector('.flag-status');
    const err = formEl.querySelector('.flag-error');
    const note = formEl.querySelector('textarea[name="Note"]');
    let timer = null, pending = false, running = false, hideTimer = null, reloadAfter = false;
    const missingCb = formEl.querySelector('input[name="IsMissing"][type="checkbox"]');

    const setStatus = (text, cls) => {
        if (!status) return;
        clearTimeout(hideTimer);
        status.textContent = text;
        status.className = 'flag-status' + (cls ? ' ' + cls : '');
        if (cls === 'is-saved') hideTimer = setTimeout(() => { status.textContent = ''; status.className = 'flag-status'; }, 2000);
    };

    async function save() {
        if (running) { pending = true; return; }
        running = true;
        if (err) err.style.display = 'none';
        setStatus(formEl.dataset.savingMsg || 'Saving…', 'is-saving');
        try {
            const d = await (await fetch('/Book/UpdateFlags', { method: 'POST', body: new FormData(formEl) })).json();
            if (d.success) {
                setStatus(formEl.dataset.savedMsg || 'Saved!', 'is-saved');
                if (reloadAfter) { reloadAfter = false; softReload(); }
            } else {
                setStatus('', '');
                if (err) { err.textContent = d.message; err.style.display = 'block'; }
                if (reloadAfter && missingCb) { reloadAfter = false; missingCb.checked = !missingCb.checked; syncFlagEditor(formEl); }
            }
        } catch {
            setStatus('', '');
            if (err) { err.textContent = networkErrorText(); err.style.display = 'block'; }
        } finally {
            running = false;
            if (pending) { pending = false; save(); }
        }
    }
    const saveSoon = (ms) => { clearTimeout(timer); timer = setTimeout(() => { timer = null; save(); }, ms); };

    formEl.querySelectorAll('input[name="Flags"]').forEach(cb => cb.addEventListener('change', () => {
        syncFlagEditor(formEl);
        saveSoon(0);
    }));
    missingCb?.addEventListener('change', () => {
        syncFlagEditor(formEl);
        reloadAfter = true;
        saveSoon(0);
    });
    if (note) {
        note.addEventListener('input', () => saveSoon(800));
        note.addEventListener('blur', () => { if (timer) saveSoon(0); });
    }
    formEl.addEventListener('submit', e => { e.preventDefault(); saveSoon(0); });
    window.addEventListener('beforeunload', () => {
        if (!timer) return;
        clearTimeout(timer);
        navigator.sendBeacon('/Book/UpdateFlags', new FormData(formEl));
    });
    syncFlagEditor(formEl);
}


document.addEventListener('submit', async e => {
    const form = e.target.closest('.navbar .lang-switcher form');
    if (!form) return;
    e.preventDefault();
    try {
        await fetch(form.action, { method: 'POST', body: new FormData(form), redirect: 'manual' });
        softReload();
    } catch {
        showToast(networkErrorText(), 'error');
    }
});

let softReloadRunning = false;
async function softReload() {
    if (softReloadRunning) return;
    softReloadRunning = true;
    try {
        const r = await fetch(location.href, { cache: 'no-store', headers: { 'X-Soft-Reload': '1' } });
        if (!r.ok || new URL(r.url).pathname !== location.pathname) throw new Error('reload');
        const doc = new DOMParser().parseFromString(await r.text(), 'text/html');
        const oldMain = document.querySelector('main.main-content');
        const newMain = doc.querySelector('main.main-content');
        if (!oldMain || !newMain) throw new Error('reload');

        const openIds = [...oldMain.querySelectorAll('[id].open')].map(el => el.id);
        const y = window.scrollY;

        document.querySelectorAll('body > .modal-overlay[data-moved]').forEach(el => el.remove());
        oldMain.classList.add('no-anim');
        oldMain.innerHTML = newMain.innerHTML;
        ['.nav-links', '.drawer-links', '.navbar .lang-switcher'].forEach(sel => {
            const a = document.querySelector(sel), b = doc.querySelector(sel);
            if (a && b) a.innerHTML = b.innerHTML;
        });
        document.title = doc.title;
        document.documentElement.lang = doc.documentElement.lang;
        Object.assign(document.body.dataset, doc.body.dataset);
        const top = document.getElementById('backToTop'), newTop = doc.getElementById('backToTop');
        if (top && newTop) { top.title = newTop.title; top.setAttribute('aria-label', newTop.getAttribute('aria-label') || ''); }
        openIds.forEach(id => document.getElementById(id)?.classList.add('open'));

        doc.querySelectorAll('body > script:not([src])').forEach(s => {
            const el = document.createElement('script');
            el.textContent = '{\n' + s.textContent + '\n}';
            document.body.appendChild(el);
            el.remove();
        });
        window.scrollTo({ top: y, behavior: 'instant' });
        syncModalScrollLock();
    } catch {
        location.reload();
    } finally {
        softReloadRunning = false;
    }
}

function initSectionFilter() {
    const chips = document.querySelectorAll('.flag-filter-chip');
    const sections = document.querySelectorAll('.flag-section');
    if (!chips.length) return;

    function apply(filter) {
        if (![...chips].some(c => c.dataset.filter === filter)) filter = 'all';
        chips.forEach(c => {
            const on = c.dataset.filter === filter;
            c.classList.toggle('active', on);
            c.setAttribute('aria-pressed', on ? 'true' : 'false');
        });
        sections.forEach(s => { s.hidden = filter !== 'all' && s.dataset.section !== filter; });
    }

    chips.forEach(c => c.addEventListener('click', () => {
        const filter = c.dataset.filter;
        history.replaceState(null, '', filter === 'all' ? location.pathname : '#' + filter);
        apply(filter);
    }));

    const fromHash = () => apply(decodeURIComponent(location.hash.slice(1)) || 'all');
    if (window._sectionFilterHash) window.removeEventListener('hashchange', window._sectionFilterHash);
    window._sectionFilterHash = fromHash;
    window.addEventListener('hashchange', fromHash);
    fromHash();
}

async function toggleFavorite(btn) {
    const next = btn.getAttribute('aria-pressed') !== 'true';
    const fd = new FormData();
    fd.append('Kind', btn.dataset.kind);
    fd.append('Id', btn.dataset.id);
    fd.append('IsFavorite', next ? 'true' : 'false');
    fd.append('__RequestVerificationToken', getAntiForgeryToken());
    btn.disabled = true;
    try {
        const d = await (await fetch('/Favorites/Toggle', { method: 'POST', body: fd })).json();
        if (d.success) {
            btn.classList.toggle('is-on', next);
            btn.setAttribute('aria-pressed', next ? 'true' : 'false');
            const t = next ? btn.dataset.titleOn : btn.dataset.titleOff;
            btn.title = t; btn.setAttribute('aria-label', t);
            btn.closest('.fav-card')?.classList.toggle('is-removed', !next);
        } else showToast(d.message || genericErrorText(), 'error');
    } catch {
        showToast(networkErrorText(), 'error');
    } finally {
        btn.disabled = false;
    }
}

async function toggleSeriesOngoing(btn) {
    const next = btn.getAttribute('aria-pressed') !== 'true';
    const fd = new FormData();
    fd.append('Id', btn.dataset.seriesId);
    fd.append('IsOngoing', next ? 'true' : 'false');
    fd.append('__RequestVerificationToken', getAntiForgeryToken());
    btn.disabled = true;
    try {
        const d = await (await fetch('/Series/SetOngoing', { method: 'POST', body: fd })).json();
        if (d.success) {
            btn.classList.toggle('is-on', next);
            btn.setAttribute('aria-pressed', next ? 'true' : 'false');
        } else showToast(d.message || genericErrorText(), 'error');
    } catch {
        showToast(networkErrorText(), 'error');
    } finally {
        btn.disabled = false;
    }
}

function initSeriesSort(list, savedMsg) {
    if (!list) return;
    const rows = () => [...list.querySelectorAll('.series-part-row')];
    if (rows().length < 2) return;
    const orderOf = () => rows().map(r => r.dataset.bookId).join(',');

    async function save(before) {
        if (orderOf() === before) return;
        const fd = new FormData();
        fd.append('seriesId', list.dataset.seriesId);
        rows().forEach(r => fd.append('bookIds', r.dataset.bookId));
        fd.append('__RequestVerificationToken', getAntiForgeryToken());
        try {
            const d = await (await fetch('/Series/Reorder', { method: 'POST', body: fd })).json();
            if (d.success) showToast(savedMsg, 'success');
            else showToast(d.message || genericErrorText(), 'error');
        } catch {
            showToast(networkErrorText(), 'error');
        }
        softReload();
    }

    if (window.matchMedia('(hover: hover) and (pointer: fine)').matches) {
        list.classList.add('is-sortable');
        let dragged = null, before = '';
        rows().forEach(r => r.draggable = true);
        list.addEventListener('dragstart', e => {
            dragged = e.target.closest('.series-part-row');
            if (!dragged) return;
            before = orderOf();
            e.dataTransfer.effectAllowed = 'move';
            e.dataTransfer.setData('text/plain', dragged.dataset.bookId);
            requestAnimationFrame(() => dragged && dragged.classList.add('dragging'));
        });
        list.addEventListener('dragover', e => {
            if (!dragged) return;
            e.preventDefault();
            const next = rows().find(r => r !== dragged && e.clientY < r.getBoundingClientRect().top + r.offsetHeight / 2);
            if (next) { if (next !== dragged.nextElementSibling) list.insertBefore(dragged, next); }
            else if (list.lastElementChild !== dragged) list.appendChild(dragged);
        });
        list.addEventListener('drop', e => e.preventDefault());
        list.addEventListener('dragend', () => {
            if (!dragged) return;
            dragged.classList.remove('dragging');
            dragged = null;
            save(before);
        });
    }

    list.addEventListener('click', e => {
        const btn = e.target.closest('.part-move-btn');
        if (!btn) return;
        const row = btn.closest('.series-part-row');
        const before = orderOf();
        if (btn.dataset.move === '-1' && row.previousElementSibling) list.insertBefore(row, row.previousElementSibling);
        else if (btn.dataset.move === '1' && row.nextElementSibling) list.insertBefore(row.nextElementSibling, row);
        save(before);
    });
}

async function submitFormWithFile(formEl, url, errorElId, btnEl, successMsg, onSuccess) {
    const err = errorElId ? document.getElementById(errorElId) : null;
    if (err) err.style.display = 'none';
    const origHtml = btnEl.innerHTML;
    btnEl.disabled = true;
    btnEl.innerHTML = '<span class="spinner"></span>';

    try {
        const fd = new FormData(formEl);
        const r  = await fetch(url, { method: 'POST', body: fd });
        const d  = await r.json();
        if (d.success) {
            showToast(successMsg, 'success');
            if (onSuccess) onSuccess();
        } else {
            if (err) { err.textContent = d.message; err.style.display = 'block'; }
            else showToast(d.message || genericErrorText(), 'error');
        }
    } catch {
        const msg = networkErrorText();
        if (err) { err.textContent = msg; err.style.display = 'block'; }
        else showToast(msg, 'error');
    } finally {
        btnEl.disabled = false;
        btnEl.innerHTML = origHtml;
    }
}

function updateActiveAlphaBtn() {
    const sections = document.querySelectorAll('.author-section[id^="letter-"]');
    if (!sections.length) return;
    const scrollY = window.scrollY + 120;
    let active = null;
    sections.forEach(s => {
        if (s.offsetTop <= scrollY) active = s.id.replace('letter-', '');
    });
    document.querySelectorAll('.alpha-btn').forEach(btn => {
        btn.classList.toggle('current', btn.textContent.trim() === active);
    });
}

if (document.querySelector('.alphabet-nav')) {
    window.addEventListener('scroll', updateActiveAlphaBtn, { passive: true });
}

(function () {
    const navToggle = document.getElementById('navToggle');
    const drawer = document.getElementById('mobileDrawer');
    const overlay = document.getElementById('drawerOverlay');
    const drawerClose = document.getElementById('drawerClose');
    if (!navToggle || !drawer || !overlay) return;

    function openDrawer() {
        drawer.classList.add('open');
        overlay.classList.add('open');
        document.body.style.overflow = 'hidden';
    }
    function closeDrawer() {
        drawer.classList.remove('open');
        overlay.classList.remove('open');
        document.body.style.overflow = '';
    }

    navToggle.addEventListener('click', openDrawer);
    if (drawerClose) drawerClose.addEventListener('click', closeDrawer);
    overlay.addEventListener('click', closeDrawer);
    drawer.querySelectorAll('a').forEach(link => link.addEventListener('click', closeDrawer));
    document.addEventListener('keydown', (e) => { if (e.key === 'Escape') closeDrawer(); });
})();

(function () {
    const btn = document.getElementById('backToTop');
    if (!btn) return;
    const toggle = () => btn.classList.toggle('visible', window.scrollY > 400);
    window.addEventListener('scroll', toggle, { passive: true });
    toggle();
    btn.addEventListener('click', () => window.scrollTo({ top: 0, behavior: 'smooth' }));
})();
