// ============================================================
// SmartGPON — wwwroot/js/gpon-fileui.js
// P4 · 5 fonctions · Url.Action via data-* · CSRF · A2
// ============================================================

/**
 * USAGE dans les vues Razor:
 *   <div id="zone-container"
 *        data-upload-url="@Url.Action("UploadAjax","Resources")"
 *        data-upload-temp-url="@Url.Action("UploadTemp","Resources")"
 *        data-delete-url="@Url.Action("DeleteAjax","Resources")"
 *        data-delete-temp-url="@Url.Action("DeleteTemp","Resources")"
 *        data-getfiles-url="@Url.Action("GetFiles","Resources")"
 *        data-download-base="@Url.Action("Download","Resources")"
 *        data-session-guid="@ViewBag.SessionGuid"
 *        data-projet-id="@Model.ProjetId"
 *        data-zone-id="@(Model.ZoneId?.ToString() ?? "")"
 *        data-can-upload="@canUpload.ToString().ToLower()"
 *        data-can-delete="@canDelete.ToString().ToLower()">
 *   </div>
 */

// ── Helpers ─────────────────────────────────────────────────────────────────

function _getCsrf() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

function _formatSize(bytes) {
    if (!bytes) return '—';
    if (bytes >= 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    return (bytes / 1024).toFixed(1) + ' KB';
}

function _extIcon(ext) {
    const map = {
        '.pdf': 'bi-file-earmark-pdf', '.doc': 'bi-file-earmark-word',
        '.docx': 'bi-file-earmark-word', '.xls': 'bi-file-earmark-excel',
        '.xlsx': 'bi-file-earmark-excel', '.ppt': 'bi-file-earmark-ppt',
        '.pptx': 'bi-file-earmark-ppt', '.jpg': 'bi-file-earmark-image',
        '.jpeg': 'bi-file-earmark-image', '.png': 'bi-file-earmark-image',
        '.gif': 'bi-file-earmark-image', '.dwg': 'bi-file-earmark-code',
        '.svg': 'bi-file-earmark-image'
    };
    return map[(ext || '').toLowerCase()] || 'bi-file-earmark';
}

// ── 1. initFileZone ──────────────────────────────────────────────────────────
// mode: 'temp' (Create Projets+Zones) | 'ajax' (Edit Projets+Zones)
/**
 * @param {string} containerId  id du div conteneur
 * @param {'temp'|'ajax'} mode
 */
function initFileZone(containerId, mode) {
    const container = document.getElementById(containerId);
    if (!container) return;

    const urls = {
        upload:      container.dataset.uploadUrl,
        uploadTemp:  container.dataset.uploadTempUrl,
        delete_:     container.dataset.deleteUrl,
        deleteTemp:  container.dataset.deleteTempUrl,
        getFiles:    container.dataset.getfilesUrl,
        downloadBase: container.dataset.downloadBase
    };
    const projetId    = container.dataset.projetId;
    const zoneId      = container.dataset.zoneId || null;
    const sessionGuid = container.dataset.sessionGuid || null;
    const canDelete   = container.dataset.canDelete === 'true';

    // Zone drag&drop (l'élément avec class sg-file-zone dans le container)
    const zone = container.querySelector('.sg-file-zone');
    const input = container.querySelector('input[type=file]');
    const list  = container.querySelector('.sg-file-list');

    if (!zone || !input || !list) return;

    // Clic zone → trigger file input
    zone.addEventListener('click', () => input.click());
    zone.addEventListener('dragover', e => { e.preventDefault(); zone.classList.add('drag-over'); });
    zone.addEventListener('dragleave', () => zone.classList.remove('drag-over'));
    zone.addEventListener('drop', e => {
        e.preventDefault();
        zone.classList.remove('drag-over');
        Array.from(e.dataTransfer.files).forEach(f => _handleFile(f));
    });
    input.addEventListener('change', () => {
        Array.from(input.files).forEach(f => _handleFile(f));
        input.value = '';
    });

    async function _handleFile(file) {
        if (mode === 'temp') {
            const result = await uploadTempAjax(sessionGuid, file, urls);
            if (!result.success) { _showError(list, result.error); return; }
            _appendFileItem(list, { nomFichier: result.nomFichier, fileSize: result.fileSize,
                fileExtension: result.fileExtension, tempId: result.tempId }, mode, urls, canDelete);
        } else {
            const result = await uploadFileAjax(projetId, zoneId, file, urls);
            if (!result.success) { _showError(list, result.error); return; }
            _appendFileItem(list, { id: result.id, nomFichier: result.nomFichier,
                fileSize: result.fileSize, fileExtension: result.fileExtension }, mode, urls, canDelete);
        }
    }
}

function _showError(list, msg) {
    const div = document.createElement('div');
    div.className = 'alert alert-danger py-1 px-2 small mt-1';
    div.textContent = msg || 'Erreur upload.';
    list.appendChild(div);
    setTimeout(() => div.remove(), 5000);
}

function _appendFileItem(list, file, mode, urls, canDelete) {
    const item = document.createElement('div');
    item.className = 'sg-file-item';
    item.dataset.tempId = file.tempId || '';
    item.dataset.resourceId = file.id || '';
    item.innerHTML = `
        <i class="bi ${_extIcon(file.fileExtension)} sg-file-icon"></i>
        <span class="sg-file-name">${_esc(file.nomFichier)}${file.fileExtension || ''}</span>
        <span class="sg-file-meta">${_formatSize(file.fileSize)}</span>
        <button class="sg-btn-remove" title="Retirer" aria-label="Retirer">✕</button>`;

    item.querySelector('.sg-btn-remove').addEventListener('click', async () => {
        if (mode === 'temp') {
            if (file.tempId) await deleteFileAjax(null, urls, { tempId: file.tempId, sessionGuid: item.dataset.sessionGuid });
            item.remove();
        } else {
            if (!file.id) { item.remove(); return; }
            const r = await deleteFileAjax(file.id, urls);
            if (r.success) {
                if (r.requestedDeletion) {
                    item.querySelector('.sg-file-name').insertAdjacentHTML('afterend',
                        '<span class="sg-file-pending ms-1">Demande en cours</span>');
                    item.querySelector('.sg-btn-remove').remove();
                } else item.remove();
            }
        }
    });
    list.appendChild(item);
}

function _esc(s) {
    return (s || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
}

// ── 2. uploadTempAjax ───────────────────────────────────────────────────────
async function uploadTempAjax(sessionGuid, file, urls) {
    const fd = new FormData();
    fd.append('sessionGuid', sessionGuid);
    fd.append('file', file);
    fd.append('__RequestVerificationToken', _getCsrf());
    try {
        const r = await fetch(urls.uploadTemp, { method: 'POST', body: fd });
        return await r.json();
    } catch { return { success: false, error: 'Erreur réseau.' }; }
}

// ── 3. uploadFileAjax ───────────────────────────────────────────────────────
async function uploadFileAjax(projetId, zoneId, file, urls) {
    const fd = new FormData();
    fd.append('projetId', projetId);
    if (zoneId) fd.append('zoneId', zoneId);
    fd.append('file', file);
    fd.append('__RequestVerificationToken', _getCsrf());
    try {
        const r = await fetch(urls.upload, { method: 'POST', body: fd });
        return await r.json();
    } catch { return { success: false, error: 'Erreur réseau.' }; }
}

// ── 4. deleteFileAjax ───────────────────────────────────────────────────────
// Pour mode ajax (Edit): resourceId obligatoire
// Pour mode temp (Create): {tempId, sessionGuid}
async function deleteFileAjax(resourceId, urls, tempOpts) {
    const csrf = _getCsrf();
    try {
        if (tempOpts) {
            const u = `${urls.deleteTemp}?sessionGuid=${encodeURIComponent(tempOpts.sessionGuid)}&tempId=${encodeURIComponent(tempOpts.tempId)}`;
            const r = await fetch(u, { method: 'DELETE',
                headers: { 'RequestVerificationToken': csrf } });
            return await r.json();
        }
        const u = `${urls.delete_}/${resourceId}`;
        const r = await fetch(u, { method: 'DELETE',
            headers: { 'RequestVerificationToken': csrf } });
        return await r.json();
    } catch { return { success: false }; }
}

// ── 5. loadFileList ─────────────────────────────────────────────────────────
async function loadFileList(projetId, zoneId, containerId, urls) {
    const list = document.getElementById(containerId);
    if (!list) return;
    list.innerHTML = '<div class="sg-file-list-empty">Chargement…</div>';

    let url = `${urls.getFiles}?projetId=${projetId}`;
    if (zoneId) url += `&zoneId=${zoneId}`;

    try {
        const r    = await fetch(url);
        const data = await r.json();
        list.innerHTML = '';

        if (!data.length) {
            list.innerHTML = '<div class="sg-file-list-empty">Aucun fichier.</div>';
            return;
        }
        data.forEach(f => {
            const link = document.createElement('a');
            link.href     = `${urls.downloadBase}/${f.id}`;
            link.download = f.nomFichier + (f.fileExtension || '');
            link.className = 'sg-doc-file-item';
            link.innerHTML = `
                <i class="bi ${_extIcon(f.fileExtension)}"></i>
                <span>${_esc(f.nomFichier)}${_esc(f.fileExtension || '')}</span>
                ${f.hasPendingDeletion ? '<span class="sg-file-pending ms-1">Demande en cours</span>' : ''}
                <span class="sg-doc-file-meta">${_formatSize(f.fileSize)}</span>`;
            list.appendChild(link);
        });
    } catch {
        list.innerHTML = '<div class="sg-file-list-empty text-danger">Erreur de chargement.</div>';
    }
}

// ── 6. openDocPopup ─────────────────────────────────────────────────────────
function openDocPopup(projetId, zoneId, titre, urls) {
    // Overlay
    const overlay = document.createElement('div');
    overlay.className = 'sg-doc-overlay';
    overlay.setAttribute('role', 'dialog');
    overlay.setAttribute('aria-modal', 'true');

    const listId = 'sg-doc-list-' + Date.now();

    overlay.innerHTML = `
    <div class="sg-doc-popup">
      <div class="sg-doc-popup-header">
        <span><i class="bi bi-folder me-2"></i>${_esc(titre)}</span>
        <button class="sg-doc-popup-close" aria-label="Fermer">✕</button>
      </div>
      <div class="sg-doc-popup-body">
        <div id="${listId}" class="sg-file-list"></div>
      </div>
    </div>`;

    document.body.appendChild(overlay);
    loadFileList(projetId, zoneId, listId, urls);

    const close = () => { overlay.remove(); document.removeEventListener('keydown', onKey); };
    overlay.querySelector('.sg-doc-popup-close').addEventListener('click', close);
    overlay.addEventListener('click', e => { if (e.target === overlay) close(); });
    const onKey = e => { if (e.key === 'Escape') close(); };
    document.addEventListener('keydown', onKey);
}
