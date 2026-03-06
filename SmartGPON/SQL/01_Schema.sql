-- ============================================================
-- SmartGPON v3 - Complete Database Schema
-- SQL Server 2019+ | Optimized with indexes & FK constraints
-- ============================================================
USE master;
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'SmartGPON')
    CREATE DATABASE SmartGPON COLLATE French_CI_AS;
GO
USE SmartGPON;
GO

-- ── CORE TABLES ──────────────────────────────────────────────

CREATE TABLE Clients (
    Id          INT IDENTITY PRIMARY KEY,
    Nom         NVARCHAR(100) NOT NULL,
    Code        NVARCHAR(20)  NOT NULL UNIQUE,
    Adresse     NVARCHAR(200),
    Telephone   NVARCHAR(20),
    Email       NVARCHAR(100),
    Logo        NVARCHAR(300),
    DateCreation DATETIME2 DEFAULT SYSDATETIME(),
    IsActive    BIT DEFAULT 1
);

CREATE TABLE Projets (
    Id          INT IDENTITY PRIMARY KEY,
    ClientId    INT NOT NULL REFERENCES Clients(Id) ON DELETE CASCADE,
    Nom         NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500),
    Statut      TINYINT DEFAULT 0,   -- 0=EnCours 1=Termine 2=Suspendu
    DateDebut   DATE,
    DateFin     DATE,
    DateCreation DATETIME2 DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Projets_ClientId ON Projets(ClientId);

CREATE TABLE Zones (
    Id          INT IDENTITY PRIMARY KEY,
    ProjetId    INT NOT NULL REFERENCES Projets(Id) ON DELETE CASCADE,
    Nom         NVARCHAR(100) NOT NULL,
    Description NVARCHAR(300),
    Latitude    DECIMAL(10,7),
    Longitude   DECIMAL(10,7)
);
CREATE INDEX IX_Zones_ProjetId ON Zones(ProjetId);

CREATE TABLE Olts (
    Id          INT IDENTITY PRIMARY KEY,
    ZoneId      INT NOT NULL REFERENCES Zones(Id) ON DELETE CASCADE,
    Nom         NVARCHAR(100) NOT NULL,
    Marque      NVARCHAR(50),
    Modele      NVARCHAR(50),
    IpAddress   NVARCHAR(45),
    NbrePorts   INT DEFAULT 16,
    Statut      TINYINT DEFAULT 1,
    DateInstall DATE,
    DateCreation DATETIME2 DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Olts_ZoneId ON Olts(ZoneId);

CREATE TABLE Fdts (
    Id          INT IDENTITY PRIMARY KEY,
    OltId       INT NOT NULL REFERENCES Olts(Id) ON DELETE CASCADE,
    Nom         NVARCHAR(100) NOT NULL,
    Capacite    INT DEFAULT 8,
    Latitude    DECIMAL(10,7),
    Longitude   DECIMAL(10,7),
    Statut      TINYINT DEFAULT 1
);
CREATE INDEX IX_Fdts_OltId ON Fdts(OltId);

CREATE TABLE Fats (
    Id          INT IDENTITY PRIMARY KEY,
    FdtId       INT NOT NULL REFERENCES Fdts(Id) ON DELETE CASCADE,
    Nom         NVARCHAR(100) NOT NULL,
    Capacite    INT DEFAULT 8,
    Latitude    DECIMAL(10,7),
    Longitude   DECIMAL(10,7),
    Statut      TINYINT DEFAULT 1
);
CREATE INDEX IX_Fats_FdtId ON Fats(FdtId);

CREATE TABLE Splitters (
    Id          INT IDENTITY PRIMARY KEY,
    FatId       INT NOT NULL REFERENCES Fats(Id) ON DELETE CASCADE,
    Nom         NVARCHAR(100) NOT NULL,
    Ratio       NVARCHAR(10) DEFAULT '1:8',
    NombreSorties INT DEFAULT 8,
    Statut      TINYINT DEFAULT 1
);
CREATE INDEX IX_Splitters_FatId ON Splitters(FatId);

CREATE TABLE Onts (
    Id          INT IDENTITY PRIMARY KEY,
    SplitterId  INT NOT NULL REFERENCES Splitters(Id) ON DELETE CASCADE,
    SerialNumber NVARCHAR(50) NOT NULL,
    Nom         NVARCHAR(100),
    MacAddress  NVARCHAR(17),
    IpAddress   NVARCHAR(45),
    Modele      NVARCHAR(50),
    Statut      TINYINT DEFAULT 1,
    SignalRx    DECIMAL(6,2),
    SignalTx    DECIMAL(6,2),
    DateInstall DATE,
    DateCreation DATETIME2 DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Onts_SplitterId ON Onts(SplitterId);
CREATE INDEX IX_Onts_SerialNumber ON Onts(SerialNumber);

CREATE TABLE Fibres (
    Id          INT IDENTITY PRIMARY KEY,
    ZoneId      INT NOT NULL REFERENCES Zones(Id),
    Nom         NVARCHAR(100) NOT NULL,
    Longueur    DECIMAL(8,2),
    Type        NVARCHAR(30),
    Statut      TINYINT DEFAULT 1
);

CREATE TABLE Chambres (
    Id          INT IDENTITY PRIMARY KEY,
    ZoneId      INT NOT NULL REFERENCES Zones(Id),
    Nom         NVARCHAR(100) NOT NULL,
    Type        NVARCHAR(30),
    Latitude    DECIMAL(10,7),
    Longitude   DECIMAL(10,7),
    Statut      TINYINT DEFAULT 1
);

CREATE TABLE Techniciens (
    Id          INT IDENTITY PRIMARY KEY,
    ClientId    INT NOT NULL REFERENCES Clients(Id),
    Nom         NVARCHAR(100) NOT NULL,
    Prenom      NVARCHAR(100),
    Email       NVARCHAR(150),
    Telephone   NVARCHAR(20),
    Specialite  NVARCHAR(100),
    IsActive    BIT DEFAULT 1
);

CREATE TABLE Tests (
    Id          INT IDENTITY PRIMARY KEY,
    OntId       INT NOT NULL REFERENCES Onts(Id),
    TechnicienId INT REFERENCES Techniciens(Id),
    TypeTest    NVARCHAR(50),
    Resultat    TINYINT DEFAULT 0,   -- 0=EnCours 1=Reussi 2=Echoue
    Notes       NVARCHAR(500),
    DateTest    DATETIME2 DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Tests_OntId ON Tests(OntId);

CREATE TABLE Validations (
    Id          INT IDENTITY PRIMARY KEY,
    ProjetId    INT NOT NULL REFERENCES Projets(Id),
    TechnicienId INT REFERENCES Techniciens(Id),
    Statut      TINYINT DEFAULT 0,
    Commentaire NVARCHAR(500),
    DateValidation DATETIME2 DEFAULT SYSDATETIME()
);

-- ── SECURITY TABLES ──────────────────────────────────────────

CREATE TABLE AttackSimulations (
    Id              INT IDENTITY PRIMARY KEY,
    OltId           INT REFERENCES Olts(Id),
    TypeAttaque     NVARCHAR(50) NOT NULL,  -- MITM, DoS, Replay, Rogue
    Parametres      NVARCHAR(MAX),          -- JSON config
    Statut          TINYINT DEFAULT 0,      -- 0=EnAttente 1=EnCours 2=Termine 3=Echoue
    NiveauRisque    TINYINT DEFAULT 2,      -- 1=Faible 2=Moyen 3=Eleve 4=Critique
    ResultatDetails NVARCHAR(MAX),
    LancePar        NVARCHAR(100),
    DateLancement   DATETIME2 DEFAULT SYSDATETIME(),
    DateFin         DATETIME2
);
CREATE INDEX IX_AttackSim_OltId ON AttackSimulations(OltId);
CREATE INDEX IX_AttackSim_Date  ON AttackSimulations(DateLancement DESC);

CREATE TABLE MaliciousOlts (
    Id              INT IDENTITY PRIMARY KEY,
    OltId           INT REFERENCES Olts(Id),
    IpSuspecte      NVARCHAR(45),
    MacSuspecte     NVARCHAR(17),
    RaisonDetection NVARCHAR(200),
    NiveauConfiance INT DEFAULT 70,          -- % 0-100
    Statut          TINYINT DEFAULT 0,       -- 0=Actif 1=Resolu 2=FauxPositif
    DateDetection   DATETIME2 DEFAULT SYSDATETIME(),
    DateResolution  DATETIME2
);
CREATE INDEX IX_MalOlts_Date ON MaliciousOlts(DateDetection DESC);

CREATE TABLE TrafficCaptures (
    Id              INT IDENTITY PRIMARY KEY,
    OltId           INT REFERENCES Olts(Id),
    TailleOctets    BIGINT DEFAULT 0,
    NombrePaquets   INT DEFAULT 0,
    Protocole       NVARCHAR(30),
    IpSource        NVARCHAR(45),
    IpDestination   NVARCHAR(45),
    AnomalieDetectee BIT DEFAULT 0,
    TypeAnomalie    NVARCHAR(100),
    DateCapture     DATETIME2 DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Traffic_OltId ON TrafficCaptures(OltId);
CREATE INDEX IX_Traffic_Date  ON TrafficCaptures(DateCapture DESC);

CREATE TABLE NetworkAlerts (
    Id              INT IDENTITY PRIMARY KEY,
    Titre           NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(1000),
    Severite        TINYINT DEFAULT 2,       -- 1=Info 2=Warning 3=Critical
    Type            NVARCHAR(50),
    OltId           INT REFERENCES Olts(Id),
    IsRead          BIT DEFAULT 0,
    DateAlerte      DATETIME2 DEFAULT SYSDATETIME()
);
CREATE INDEX IX_Alerts_Date    ON NetworkAlerts(DateAlerte DESC);
CREATE INDEX IX_Alerts_IsRead  ON NetworkAlerts(IsRead);

CREATE TABLE SecurityEvents (
    Id              INT IDENTITY PRIMARY KEY,
    Type            NVARCHAR(50) NOT NULL,
    Description     NVARCHAR(500),
    IpSource        NVARCHAR(45),
    Utilisateur     NVARCHAR(100),
    Niveau          TINYINT DEFAULT 1,
    DateEvenement   DATETIME2 DEFAULT SYSDATETIME()
);
CREATE INDEX IX_SecEvents_Date ON SecurityEvents(DateEvenement DESC);

-- ── ASP.NET IDENTITY ─────────────────────────────────────────

CREATE TABLE AspNetRoles (
    Id              NVARCHAR(450) PRIMARY KEY,
    Name            NVARCHAR(256),
    NormalizedName  NVARCHAR(256),
    ConcurrencyStamp NVARCHAR(MAX)
);

CREATE TABLE AspNetUsers (
    Id                   NVARCHAR(450) PRIMARY KEY,
    ClientId             INT REFERENCES Clients(Id),
    UserName             NVARCHAR(256),
    NormalizedUserName   NVARCHAR(256),
    Email                NVARCHAR(256),
    NormalizedEmail      NVARCHAR(256),
    EmailConfirmed       BIT DEFAULT 0,
    PasswordHash         NVARCHAR(MAX),
    SecurityStamp        NVARCHAR(MAX),
    ConcurrencyStamp     NVARCHAR(MAX),
    PhoneNumber          NVARCHAR(MAX),
    PhoneNumberConfirmed BIT DEFAULT 0,
    TwoFactorEnabled     BIT DEFAULT 0,
    LockoutEnd           DATETIMEOFFSET,
    LockoutEnabled       BIT DEFAULT 1,
    AccessFailedCount    INT DEFAULT 0
);

CREATE TABLE AspNetUserRoles (
    UserId  NVARCHAR(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    RoleId  NVARCHAR(450) NOT NULL REFERENCES AspNetRoles(Id) ON DELETE CASCADE,
    PRIMARY KEY (UserId, RoleId)
);

CREATE TABLE AspNetUserClaims (
    Id         INT IDENTITY PRIMARY KEY,
    UserId     NVARCHAR(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    ClaimType  NVARCHAR(MAX),
    ClaimValue NVARCHAR(MAX)
);

CREATE TABLE AspNetUserLogins (
    LoginProvider       NVARCHAR(450) NOT NULL,
    ProviderKey         NVARCHAR(450) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX),
    UserId              NVARCHAR(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    PRIMARY KEY (LoginProvider, ProviderKey)
);

CREATE TABLE AspNetUserTokens (
    UserId        NVARCHAR(450) NOT NULL REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    LoginProvider NVARCHAR(450) NOT NULL,
    Name          NVARCHAR(450) NOT NULL,
    Value         NVARCHAR(MAX),
    PRIMARY KEY (UserId, LoginProvider, Name)
);

CREATE TABLE AspNetRoleClaims (
    Id         INT IDENTITY PRIMARY KEY,
    RoleId     NVARCHAR(450) NOT NULL REFERENCES AspNetRoles(Id) ON DELETE CASCADE,
    ClaimType  NVARCHAR(MAX),
    ClaimValue NVARCHAR(MAX)
);
GO
PRINT 'Schema created successfully.';
