# Relations entre les Vues (Interfaces Utilisateur) - SmartGPON

Ce document décrit la navigation et les relations entre les différentes vues (interfaces) de l'application SmartGPON.

## 1. Menu Principal (`_Layout.cshtml`)
La barre de navigation (Topbar/Sidebar) est le point d'entrée central. Elle permet d'accéder aux vues principales :
- **Accueil** (`Home/Index`)
- **Architecture GPON** (`Home/ArchitectureGpon`)
- **Notifications** (`Notifications/Index`) : Accès aux alertes et requêtes en attente.
- **Référentiel GPON** :
  - **Clients** (`Clients/Index`)
  - **Projets** (`Projets/Index`)
  - **Zones** (`Zones/Index`)
  - **OLTs** (`Olts/Index`)
  - **FDTs** (`Fdts/Index`)
  - **FATs** (`Fats/Index`)
  - **BPIs** (`Bpis/Index`)
  - **Boîtiers d'Étage** (`BoitiersEtage/Index`)
- **Centre de Sécurité** :
  - **Tableau de Bord** (`Security/Index`)
  - **Simulations d'Attaque** (`Security/Simulations`)
- **Administration** (Réservé aux Superviseurs) :
  - **Utilisateurs** (`Users/Index`)
  - **Audit Logs** (`AuditLogs/Index`)

## 2. Navigation Hiérarchique (Vues CRUD)
Pour chaque entité GPON listée ci-dessus (Clients, Projets, Zones, etc.), le modèle de navigation standard est le suivant :

*   **`Index` (Liste principale)**
    *   Lien vers -> **`Create`** (Formulaire modal ou page pour ajouter un élément)
    *   Lien vers -> **`Edit`** (Formulaire modal ou page pour modifier un élément spécifique)
    *   Lien vers -> **`Details`** (Page détaillant l'élément et ses dépendances)
    *   Action -> **`Delete`** (Suppression de l'élément)

### Dépendances directes "Enfant" depuis l'Index ou les Détails :
*   Depuis **`Projets/Index`** ou **`Projets/Details`** -> Lien vers **`Zones/Index`** (filtré par `projetId`)
*   Depuis **`Zones/Index`** ou **`Zones/Details`** -> Lien vers **`Olts/Index`** (filtré par `zoneId`)
*   Depuis **`Olts/Index`** -> Lien vers **`Fdts/Index`** (filtré par `oltId`)
*   Depuis **`Fdts/Index`** -> Lien vers **`Fats/Index`** ou **`Bpis/Index`** (filtré par `fdtId`)
*   Depuis **`Bpis/Index`** -> Lien vers **`BoitiersEtage/Index`** (filtré par `bpiId`)

## 3. Détails d'un Projet (`Projets/Details.cshtml`)
La vue de détail d'un projet sert de hub pour ce projet spécifique.
*   **Affiche** : Informations générales du projet.
*   **Intègre** : La vue partielle des **Ressources** (`Resources/_FileList.cshtml` et `Resources/_UploadModal.cshtml`).
*   **Gère** : Les demandes de suppression de fichiers (`Resources/_PendingDeletions.cshtml`).

## 4. Centre de Sécurité (`Security`)
*   **`Security/Index`** : Tableau de bord de sécurité.
    *   Contient des onglets ou liens vers : **Alertes Réseau** (`Security/Alerts.cshtml`) et **OLTs Malveillants** (`Security/RogueOlts.cshtml`).
*   **`Security/Simulations`** : Liste des simulations passées et en cours.
    *   Lien vers -> **`Security/LancerSimulation`** (Formulaire pour exécuter une nouvelle attaque simulée).

## 5. Accès et Authentification (`Account`)
*   **`Account/Login`** -> Redirige vers `Home/Index` après succès.
*   **`Account/AccessDenied`** -> Page d'erreur pour les droits insuffisants (RBAC).
*   **Déconnexion** (Depuis le Layout) -> Redirige vers `Login`.
