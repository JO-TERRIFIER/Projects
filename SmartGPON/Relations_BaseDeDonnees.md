# Relations entre les Vues et la Base de Données - SmartGPON

Ce document décrit la façon dont chaque groupe de vues (géré par son contrôleur respectif) interagit avec la base de données via le contexte Entity Framework Core (`ApplicationDbContext`).

## Vues Principales GPON (Approche CRUD)

Ces vues interagissent directement avec leurs tables respectives pour la lecture (Select), la création (Insert), la modification (Update) et la suppression (Delete). Souvent, elles lisent aussi la table "parente" pour remplir les listes déroulantes (ViewBag).

*   **Vues `Clients`**
    *   **Table(s) lue(s) / écrite(s)** : `Db.Clients`

*   **Vues `Projets`**
    *   **Table(s) principale(s)** : `Db.Projets`
    *   **Tables lues (Lecture seule)** : `Db.Clients` (pour assigner un client au projet), `Db.Resources`, `Db.DeletionRequests` (pour l'affichage des fichiers rattachés dans les détails).

*   **Vues `Zones`**
    *   **Table(s) principale(s)** : `Db.Zones`
    *   **Tables lues (Lecture seule)** : `Db.Projets` (pour la liste déroulante des projets).

*   **Vues `Olts`**
    *   **Table(s) principale(s)** : `Db.Olts`
    *   **Tables lues (Lecture seule)** : `Db.Zones` (hiérarchie parent).

*   **Vues `Fdts`**
    *   **Table(s) principale(s)** : `Db.Fdts`
    *   **Tables lues (Lecture seule)** : `Db.Olts` (hiérarchie parent).

*   **Vues `Fats`**
    *   **Table(s) principale(s)** : `Db.Fats`
    *   **Tables lues (Lecture seule)** : `Db.Fdts` (hiérarchie parent).

*   **Vues `Bpis`**
    *   **Table(s) principale(s)** : `Db.Bpis`
    *   **Tables lues (Lecture seule)** : `Db.Fdts` (hiérarchie parent).

*   **Vues `BoitiersEtage`**
    *   **Table(s) principale(s)** : `Db.BoitiersEtage`
    *   **Tables lues (Lecture seule)** : `Db.Bpis` (hiérarchie parent).

## Vues d'Administration et Sécurité

*   **Vues `Users`** (Gestion des utilisateurs)
    *   **Tables lues / écrites** : `Db.Users` (Table Identity `AspNetUsers`), `Db.UserProjectAssignments` (pour gérer les droits RBAC par projet).
    *   **Tables lues** : `Db.Clients`, `Db.Projets` (pour assignement).

*   **Vues `AuditLogs`**
    *   **Table lue (Lecture seule)** : `Db.AuditLogs` (Affiche l'historique des modifications dans la base de données).

*   **Vues `Security`** (Dashboard et Simulations)
    *   **Tables lues / écrites** : `Db.AttackSimulations`, `Db.SecurityEvents`, `Db.NetworkAlerts`.
    *   **Tables lues (Lecture seule)** : `Db.Olts` (La cible des attaques simulées ou la source des événements).

*   **Vues `Notifications`**
    *   **Tables lues / mises à jour** : `Db.ApprovalRequests` (Requêtes de validation des schémas), `Db.DeletionRequests` (Requêtes de suppression de ressources), `Db.NetworkAlerts` (Alertes persistantes).

## Vues Transversales et Accueil

*   **Vues `Home`** (Accueil, ArchitectureGpon)
    *   **Tables lues (Lecture seule)** : Pratiquement l'ensemble de la topologie (`Db.Projets`, `Db.Zones`, `Db.Olts`, `Db.Fdts`, etc.) pour générer des statistiques globales (Count), des graphiques, ou construire l'arbre de dépendance GPON visible sur le tableau de bord.

*   **Vues Partielles `Resources`** (Fichiers joints)
    *   **Tables lues / écrites** : `Db.Resources` (Métadonnées des fichiers uploadés), `Db.DeletionRequests` (Processus de suppression).

---
*Note de sécurité (RBAC) :* La majorité des contrôleurs alimentant ces vues interrogent systématiquement la table `Db.UserProjectAssignments` (via la classe de base `RbacControllerBase`) pour s'assurer que l'utilisateur connecté `Db.Users` a le droit d'accéder aux données du projet demandé.
