/* ============================================================
   SmartGPON — gpon-architecture-filters.js
   Filtres en cascade: Client → Projet → Zone
   Architecture GPON · Plan v3 · A1+A2+M1+M2 intégrés
   ============================================================ */

'use strict';

// ─── Helpers ──────────────────────────────────────────────────

function buildLi(item) {
  // M1: camelCase garanti côté controller (.Select(x => new { id, nom }))
  const li = document.createElement('li');
  li.dataset.id = item.id;
  li.textContent = item.nom;
  return li;
}

function fetchAndPopulate(url, cbId, prefixOption) {
  // A2: retourne une Promise → await-able, séquence stricte garantie
  return fetch(url)
    .then(function (r) { return r.json(); })
    .then(function (data) {
      const list = document.querySelector('#' + cbId + ' .sg-combobox-list');
      list.innerHTML = '';
      if (prefixOption) { list.append(buildLi(prefixOption)); }
      data.forEach(function (item) { list.append(buildLi(item)); });
    });
}

function setComboValue(cbId, id, nom) {
  document.querySelector('#' + cbId + ' .sg-combobox-value').value = id;
  document.querySelector('#' + cbId + ' .sg-combobox-input').value = nom;
}

function getComboText(cbId, id) {
  const li = document.querySelector('#' + cbId + ' li[data-id="' + id + '"]');
  return li ? li.textContent : '';
}

function enable(cbId) {
  document.getElementById(cbId).classList.remove('sg-combobox--disabled');
}

function disable(cbId) {
  document.getElementById(cbId).classList.add('sg-combobox--disabled');
}

function resetCombo(cbId) {
  document.querySelector('#' + cbId + ' .sg-combobox-value').value = '';
  document.querySelector('#' + cbId + ' .sg-combobox-input').value = '';
  const list = document.querySelector('#' + cbId + ' .sg-combobox-list');
  if (list) { list.setAttribute('hidden', ''); }
}

function hideList(cbId) {
  const list = document.querySelector('#' + cbId + ' .sg-combobox-list');
  if (list) { list.setAttribute('hidden', ''); }
}

// ─── INIT (A2: séquence await chaîné strict) ──────────────────

async function initFilters() {
  // M2: prefixOption OBLIGATOIRE pour cb-client uniquement
  await fetchAndPopulate('/Home/GetClients', 'cb-client',
    { id: 0, nom: 'Tous les clients' });

  if (window.GPON_ACTIVE_ZONE_ID > 0) {
    // Rechargement page avec zone active → remonter les 3 filtres
    await fetchAndPopulate(
      '/Home/GetProjets?clientId=' + window.GPON_ACTIVE_CLIENT_ID,
      'cb-projet'
    );
    await fetchAndPopulate(
      '/Home/GetZones?projetId=' + window.GPON_ACTIVE_PROJET_ID,
      'cb-zone'
    );
    // Sélectionner silencieusement dans cet ordre exact
    setComboValue('cb-client',
      window.GPON_ACTIVE_CLIENT_ID,
      getComboText('cb-client', window.GPON_ACTIVE_CLIENT_ID));
    setComboValue('cb-projet',
      window.GPON_ACTIVE_PROJET_ID,
      getComboText('cb-projet', window.GPON_ACTIVE_PROJET_ID));
    setComboValue('cb-zone',
      window.GPON_ACTIVE_ZONE_ID,
      getComboText('cb-zone', window.GPON_ACTIVE_ZONE_ID));
    enable('cb-zone');
    document.getElementById('zone-hint').hidden = true; // M2
  } else {
    // Première visite → tous projets, zone désactivée
    await fetchAndPopulate('/Home/GetProjets', 'cb-projet');
    disable('cb-zone');
  }
}

// ─── ON CLIENT SELECT ──────────────────────────────────────────

document.addEventListener('DOMContentLoaded', function () {
  document.querySelector('#cb-client .sg-combobox-list')
    .addEventListener('click', async function (e) {
      const li = e.target.closest('li');
      if (!li) { return; }
      const id = parseInt(li.dataset.id, 10);
      setComboValue('cb-client', id, li.textContent);
      resetCombo('cb-projet');
      resetCombo('cb-zone');
      disable('cb-zone');
      document.getElementById('zone-hint').hidden = false;
      // M2: id==0 = "Tous les clients" → pas de filtre clientId
      const url = id === 0
        ? '/Home/GetProjets'
        : '/Home/GetProjets?clientId=' + id;
      await fetchAndPopulate(url, 'cb-projet');
      hideList('cb-client');
    });

  // ─── ON PROJET SELECT ────────────────────────────────────────

  document.querySelector('#cb-projet .sg-combobox-list')
    .addEventListener('click', async function (e) {
      const li = e.target.closest('li');
      if (!li) { return; }
      const id = parseInt(li.dataset.id, 10);
      setComboValue('cb-projet', id, li.textContent);
      resetCombo('cb-zone');
      await fetchAndPopulate('/Home/GetZones?projetId=' + id, 'cb-zone');
      enable('cb-zone');
      document.getElementById('zone-hint').hidden = true; // M2
      hideList('cb-projet');
    });

  // ─── ON ZONE SELECT ──────────────────────────────────────────

  document.querySelector('#cb-zone .sg-combobox-list')
    .addEventListener('click', function (e) {
      const li = e.target.closest('li');
      if (!li) { return; }
      window.location = '/Home/ArchitectureGpon?zoneId=' + li.dataset.id;
    });

  // ─── FILTRE TEXTE TEMPS RÉEL (les 3 combobox) ────────────────

  ['cb-client', 'cb-projet', 'cb-zone'].forEach(function (cbId) {
    const cb    = document.getElementById(cbId);
    const input = cb.querySelector('.sg-combobox-input');
    const list  = cb.querySelector('.sg-combobox-list');

    // Filtre en temps réel
    input.addEventListener('input', function (e) {
      const q = e.target.value.toLowerCase();
      list.querySelectorAll('li').forEach(function (li) {
        li.hidden = !li.textContent.toLowerCase().includes(q);
      });
      list.removeAttribute('hidden');
    });

    // Ouvrir la liste au focus
    input.addEventListener('focus', function () {
      list.removeAttribute('hidden');
    });

    // Fermer si clic hors combobox
    document.addEventListener('click', function (e) {
      if (!cb.contains(e.target)) {
        list.setAttribute('hidden', '');
      }
    });
  });

  // ─── Démarrer l'initialisation ────────────────────────────────
  initFilters();
});
