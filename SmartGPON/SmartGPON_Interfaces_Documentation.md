# Architecture des Interfaces et Design System - SmartGPON

Ce document est conçu comme une base de connaissance technique pour l'assistance IA (notamment Claude 3.7 Sonnet) intervenant sur le frontend de l'application **SmartGPON**.

## 1. Philosophie et Design System (NOC v3)

L'application SmartGPON utilise un design très spécifique, inspiré des interfaces de supervision télécom (Network Operations Center).

### 1.1 Palette de Couleurs (Custom CSS Variables)
Le fichier `site.css` définit les variables suivantes (mode clair par défaut) :
- `--color-bg-body` : `#f8fafc` (Gris très clair, fond général)
- `--color-bg-surface`: `#ffffff` (Blanc pur, fond des cartes/panneaux)
- `--color-text-main` : `#1e293b` (Gris très sombre, texte principal)
- `--color-text-muted`: `#64748b` (Gris moyen, texte secondaire/descriptions)
- `--color-border`    : `#e2e8f0` (Bordures légères)
- `--color-telecom-blue`: `#2563eb` (Couleur primaire, boutons d'action principale)
- `--color-telecom-blue-hover`: `#1d4ed8`
- **Couleurs de statut (Critiques)** :
  - `--color-status-online`: `#10b981` (Vert - Actif/En ligne)
  - `--color-status-offline`: `#ef4444` (Rouge - Hors ligne/Erreur/Suppression)
  - `--color-status-warning`: `#f59e0b` (Orange - Avertissement/En attente)

### 1.2 Typographie et Icônes
- **Police native** : `Inter` (Google Fonts).
- **Icônes** : `Bootstrap Icons` (préfixe `bi bi-*`).

### 1.3 Classes Utilitaires Personnalisées (`site.css`)
Au lieu d'utiliser uniquement Bootstrap, le projet possède ses propres classes métier :
- `.sg-card` : Conteneur principal avec ombre légère (`box-shadow`) et bordures arrondies. Supporte les états hover.
- `.sg-table` : Tableau métier. Ne pas utiliser `.table` standard de Bootstrap seul. En-têtes grisés, lignes épurées.
- `.sg-btn`, `.sg-btn-primary`, `.sg-btn-danger`, `.sg-btn-outline` : Boutons avec transitions lisses.
- `.badge-status` : Pillules colorées pour les états (Online/Offline/Pending).
- `.tree-node-group`, `.tree-node` : Classes spécifiques pour l'affichage arborescent de l'architecture GPON.

---

## 2. Structure Générale (`_Layout.cshtml`)

Toute l'application est encapsulée dans `Views/Shared/_Layout.cshtml`.

### 2.1 Topbar (`<header id="topbar">`)
- **Logo / Titre** : SmartGPON (avec indicateur NOC v3).
- **Bouton Kiosk Shutdown** : Appelle la fonction JS `shutdownApp()` qui POST sur `/Shutdown?token=...` (mode kiosk).
- **Theme Toggle** : Bascule Light/Dark mode via localStorage.
- **Notifications** : Bouton cloche pointant vers `/Notifications/Index`.
- **User Dropdown** : Affiche les initiales, le nom, le rôle, et le bouton de déconnexion.

### 2.2 Sidebar (`<nav id="sidebar">`)
Divisée en trois sections :
1. **Supervision** : Tableau de bord (`/Home`), Architecture GPON (`/Home/ArchitectureGpon`), Notifications.
2. **Gestion** : Le cœur CRUD du métier (Clients, Projets, Zones, OLTs, FDTs, FATs, BPIs, Boîtiers d'étage).
3. **Sécurité** : Centre sécurité, Simulations, Rogue OLTs.
4. **Footer** : Admin (Utilisateurs), Demandes (Approvals), Audit.
*Note : La hiérarchie physique GPON est strictement : Zone -> OLT -> FDT -> FAT -> BPI -> BoitierEtage.*

### 2.3 Mécanismes JS Globaux
- Gestion automatique des Toasts (succès/erreur).
- Interception des formulaires de suppression (`/Delete` dans l'action) pour forcer un `prompt()` demandant une "Raison obligatoire pour cette suppression" (très important pour l'audit et le RBAC).

---

## 3. Cartographie des Vues par Contrôleur

Les vues sont organisées classiquement selon le modèle MVC d'ASP.NET Core.

### 3.1 Vues de Gestion (CRUD)
Pour chaque entité réseau (Client, Projet, Zone, Olt, Fdt, Fat, Bpi, BoitierEtage), la structure est standardisée :
- `/Index` : Liste paginée/filtrée sous forme de `.sg-table` ou de grille de `.sg-card`. Contient les boutons de création et d'actions (Edit, Delete).
- `/Create` et `/Edit` : Formulaires utilisant Bootstrap Grid (`row`, `col-md-6`), avec validation (`asp-validation-for`). Tous les champs obligatoires sont marqués avec `<span class="text-danger">*</span>`. Utilisation des select natifs avec `asp-items`.

### 3.2 Vues de Supervision et Métier
- **`Home/Index.cshtml`** : Dashboard principal. Contient des métriques clés (cartes colorées) et des graphiques Chart.js (état des abonnés, OLTs, alertes).
- **`Home/ArchitectureGpon.cshtml`** : Affiche l'arborescence visuelle complète du réseau GPON. C'est la vue la plus complexe techniquement côté UI (générée récursivement ou via JS interactif).
- **`Notifications/Index.cshtml`** : Liste chronologique des alertes de sécurité et événements système.

### 3.3 Sécurité et Audit
- **`Security/Index.cshtml`** : Dashboard dédié à la sécurité (tentatives d'intrusion, anomalies).
- **`Security/Simulations.cshtml` & `LancerSimulation.cshtml`** : Outils de test de pénétration/simulation d'attaques.
- **`Security/RogueOlts.cshtml`** : Vue spécialisée pour la détection d'équipements non autorisés sur le réseau.
- **`AuditLogs/Index.cshtml`** : Tableau brut et inaltérable des journaux d'audit (accès complet avec sélecteurs de filtre pour les Admins/Superviseurs).

### 3.4 Approbations (RBAC Workflow)
- **`Approvals/Index.cshtml`** : Interface où les `Superviseurs` peuvent valider ou rejeter les actions destructrices (Suppressions) demandées par les rôles inférieurs (`Membres`).

---

## 4. Règles strictes pour la Modification des Vues (Prompting Guidelines)

Lorsque vous, LLM, devez modifier ou créer une vue dans ce projet, respectez **absolument** les points suivants :

1. **Aucun composant Bootstrap standard non stylisé** : Ne pas utiliser `<div class="card">` mais `<div class="sg-card">`. Ne pas utiliser `<button class="btn btn-primary">` mais `<button class="btn sg-btn sg-btn-primary">`.
2. **Icons obligatoires** : Chaque titre de page (H1/H2) et chaque bouton d'action doit être accompagné de son icône `bi-*` pertinente.
3. **Responsive Design** : Utilisez les grilles Bootstrap (`row`, `col-lg-6`, etc.) pour s'assurer que les formulaires s'affichent bien sur écrans réduits.
4. **Cohérence du modèle de données (ViewModels)** : Ne pas lier directement aux entités de la base de données (`Core.Entities.*`) dans les vues. Utilisez toujours les ViewModels passés par les contrôleurs.
5. **RBAC UI Elements** : Les vues doivent respecter la vue en lecture seule. Par exemple, cacher les boutons d'édition/suppression si `User.IsInRole("Visiteur")` (souvent géré globalement dans le Layout ou via des variables ViewBag, mais à garder en tête).
6. **Pas de `window.close()`** : L'app fonctionne potentiellement en mode Kiosk (Edge). L'extinction passe exclusivement par l'appel de `shutdownApp()` du `_Layout`.
7. **Boutons de suppression (Delete)** : Ils ne doivent **jamais** être de simples liens `<a>`. Ce doit toujours être des formulaires `<form action=".../Delete" method="post">` contenant un bouton de type `submit` et le tag `@Html.AntiForgeryToken()`, afin que l'intercepteur JS du Layout puisse demander la raison d'audit.

---
*Ce fichier est généré pour servir de contexte partagé et permettre à l'IA d'intervenir plus efficacement et avec une meilleure cohérence graphique et fonctionnelle sur la base de code SmartGPON.*
