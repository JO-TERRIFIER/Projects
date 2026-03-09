-- ============================================================
-- SmartGPON v3 - Stored Procedures & Seed Data
-- ============================================================
USE SmartGPON;
GO

-- Dashboard KPIs
CREATE OR ALTER PROCEDURE sp_GetDashboardStats
    @ClientId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(1) FROM Olts o JOIN Zones z ON z.Id=o.ZoneId JOIN Projets p ON p.Id=z.ProjetId WHERE (@ClientId IS NULL OR p.ClientId=@ClientId)) AS TotalOlts,
        (SELECT COUNT(1) FROM Olts o JOIN Zones z ON z.Id=o.ZoneId JOIN Projets p ON p.Id=z.ProjetId WHERE o.Statut=1 AND (@ClientId IS NULL OR p.ClientId=@ClientId)) AS OltsActifs,
        (SELECT COUNT(1) FROM NetworkAlerts WHERE IsRead=0) AS AlertesNonLues,
        (SELECT COUNT(1) FROM AttackSimulations WHERE Statut IN (0,1)) AS SimulationsActives,
        (SELECT COUNT(1) FROM MaliciousOlts WHERE Statut=0) AS RogueOltsActifs;
END
GO

-- Network tree (hierarchy for a zone)
CREATE OR ALTER PROCEDURE sp_GetNetworkTree
    @ZoneId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        o.Id AS OltId, o.Nom AS OltNom, o.IpAddress, o.Statut AS OltStatut,
        fd.Id AS FdtId, fd.Nom AS FdtNom, fd.NbSplitters1x8, fd.NbSplitters1x64,
        b.Id AS BpiId, b.Nom AS BpiNom, b.Capacite AS BpiCapacite,
        fa.Id AS FatId, fa.Nom AS FatNom, fa.Capacite AS FatCapacite
    FROM Olts o
    JOIN Fdts fd ON fd.OltId = o.Id
    LEFT JOIN Bpis b ON b.FdtId = fd.Id
    LEFT JOIN Fats fa ON fa.FdtId = fd.Id
    WHERE o.ZoneId = @ZoneId
    ORDER BY o.Nom, fd.Nom, b.Nom, fa.Nom;
END
GO

-- Paginated alerts
CREATE OR ALTER PROCEDURE sp_GetAlertsPaged
    @Page INT = 1, @PageSize INT = 20, @Severite TINYINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        Id, Titre, Description, Severite, Type, OltId, IsRead, DateAlerte,
        COUNT(1) OVER() AS TotalCount
    FROM NetworkAlerts
    WHERE (@Severite IS NULL OR Severite = @Severite)
    ORDER BY DateAlerte DESC
    OFFSET (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- Capacity report per OLT (sans ONT)
CREATE OR ALTER PROCEDURE sp_GetOltCapacity
    @OltId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        o.Id, o.Nom, o.NbrePorts,
        COUNT(DISTINCT fd.Id) AS NbreFdts,
        COUNT(DISTINCT fa.Id) AS NbreFats,
        COUNT(DISTINCT b.Id) AS NbreBpis
    FROM Olts o
    LEFT JOIN Fdts fd ON fd.OltId=o.Id
    LEFT JOIN Fats fa ON fa.FdtId=fd.Id
    LEFT JOIN Bpis b ON b.FdtId=fd.Id
    WHERE o.Id=@OltId
    GROUP BY o.Id, o.Nom, o.NbrePorts;
END
GO

-- SEED DATA
INSERT INTO Clients (Nom, Code, Adresse, Email) VALUES
('Tunisie Telecom', 'TT', 'Avenue Mohamed V, Tunis', 'admin@tunisietelecom.tn'),
('Ooredoo Tunisie', 'OO', 'Rue du Lac Biwa, Tunis', 'admin@ooredoo.tn');

INSERT INTO Projets (ClientId, Nom, Statut, DateDebut) VALUES
(1, N'Déploiement GPON Grand Tunis', 1, '2023-01-15'),
(1, 'Extension FTTH Sfax', 0, '2024-03-01'),
(2, 'FTTH Ooredoo Sousse', 0, '2024-06-01');

INSERT INTO Zones (ProjetId, Nom, Latitude, Longitude) VALUES
(1, 'Zone Nord Tunis', 36.8065, 10.1815),
(1, 'Zone Centre Tunis', 36.7965, 10.1815),
(2, 'Zone Sfax Centre', 34.7400, 10.7600);

INSERT INTO Olts (ZoneId, Nom, Marque, Modele, IpAddress, NbrePorts, Statut) VALUES
(1, 'OLT-NORD-01', 'Huawei', 'MA5800-X7', '10.10.1.1', 32, 1),
(1, 'OLT-NORD-02', 'ZTE', 'C650', '10.10.1.2', 16, 1),
(2, 'OLT-CTR-01', 'Huawei', 'MA5680T', '10.10.2.1', 16, 1);

INSERT INTO Fdts (OltId, Nom, Capacite, NbSplitters1x8, NbSplitters1x64) VALUES
(1, 'FDT-01-A', 8, 2, 1),
(1, 'FDT-01-B', 8, 1, 1),
(2, 'FDT-02-A', 8, 1, 1),
(3, 'FDT-03-A', 8, 0, 1);

INSERT INTO Fats (FdtId, Nom, Capacite) VALUES
(1,'FAT-01-A-1',8),(1,'FAT-01-A-2',8),(2,'FAT-01-B-1',8),(3,'FAT-02-A-1',8),(4,'FAT-03-A-1',8);

-- BPIs: capacité 24 (1x1:8) ou 48 (2x1:8)
INSERT INTO Bpis (FdtId, Nom, Capacite, NbSplitters1x8) VALUES
(1, 'BPI-01-A', 24, 1),(1, 'BPI-01-B', 48, 2),(2, 'BPI-02-A', 24, 1),(3, 'BPI-03-A', 48, 2);

-- Techniciens affectés par projet
INSERT INTO Techniciens (ProjetId, Nom, Prenom, Email, Specialite) VALUES
(1,'Ben Ali','Karim','k.benali@sotetel.tn','GPON Splicing'),
(1,'Trabelsi','Sami','s.trabelsi@sotetel.tn','OLT Configuration'),
(2,'Khalil','Ines','i.khalil@sotetel.tn','Fiber Testing');

-- Security seed
INSERT INTO NetworkAlerts (Titre, Description, Severite, Type, OltId) VALUES
(N'Signal faible détecté','Anomalie signal sur branche de distribution',2,'SignalDegradation',1),
(N'Tentative accès non autorisé','Login échoué x5 sur OLT-NORD-01',3,'Security',1),
(N'Taux charge élevé','OLT-NORD-01 CPU > 85%',2,'Performance',1);

INSERT INTO SecurityEvents (Type, Description, IpSource, Utilisateur, Niveau) VALUES
('LoginFailed',N'5 tentatives échouées','10.10.1.100','unknown',3),
(N'PortScan',N'Scan ports détecté','192.168.99.1','unknown',2),
(N'ConfigChange',N'Modification config OLT','10.10.1.1','admin',1);

PRINT 'Procedures and seed data OK.';
