function openModal(id) {
    const overlay = document.getElementById(id);
    if (!overlay) return;
    overlay.classList.add('open');
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
}

document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        document.querySelectorAll('.modal-overlay.open').forEach(el => {
            el.classList.remove('open');
        });
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
    requestAnimationFrame(() => overlay.classList.add('open'));

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
            else showToast(d.message || 'Error.', 'error');
        }
    } catch {
        const msg = 'Network error. Please try again.';
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
