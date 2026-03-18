// ============================================================
// SmartGPON — Infrastructure/Data/DatabaseInitializer.cs
// SQL DDL idempotent — IF NOT EXISTS par colonne/table
// Zéro migration EF — appliqué au démarrage via Program.cs
// ============================================================
using Microsoft.EntityFrameworkCore;

namespace SmartGPON.Infrastructure.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext db)
        {
            await AddResourceMetadataColumns(db);
            await CreateDeletionRequestsTable(db);
        }

        private static async Task AddResourceMetadataColumns(ApplicationDbContext db)
        {
            // Colonne 1 — UploadedByUserId
            await db.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME='Resources' AND COLUMN_NAME='UploadedByUserId')
                BEGIN
                    ALTER TABLE Resources
                    ADD UploadedByUserId nvarchar(450) NOT NULL DEFAULT ''
                END
                """);

            // Colonne 2 — UploadedAt
            await db.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME='Resources' AND COLUMN_NAME='UploadedAt')
                BEGIN
                    ALTER TABLE Resources
                    ADD UploadedAt datetime2 NOT NULL DEFAULT GETUTCDATE()
                END
                """);

            // Colonne 3 — FileSize
            await db.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME='Resources' AND COLUMN_NAME='FileSize')
                BEGIN
                    ALTER TABLE Resources
                    ADD FileSize bigint NOT NULL DEFAULT 0
                END
                """);

            // Colonne 4 — FileExtension
            await db.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME='Resources' AND COLUMN_NAME='FileExtension')
                BEGIN
                    ALTER TABLE Resources
                    ADD FileExtension nvarchar(10) NOT NULL DEFAULT ''
                END
                """);

            // Colonne 5 — ContentType
            await db.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME='Resources' AND COLUMN_NAME='ContentType')
                BEGIN
                    ALTER TABLE Resources
                    ADD ContentType nvarchar(100) NOT NULL DEFAULT ''
                END
                """);
        }

        private static async Task CreateDeletionRequestsTable(ApplicationDbContext db)
        {
            await db.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME='DeletionRequests')
                BEGIN
                    CREATE TABLE DeletionRequests (
                        Id                int           IDENTITY(1,1) PRIMARY KEY,
                        ResourceId        int           NOT NULL,
                        RequestedByUserId nvarchar(450) NOT NULL,
                        ProjetId          int           NOT NULL,
                        Statut            tinyint       NOT NULL DEFAULT 0,
                        RequestedAt       datetime2     NOT NULL DEFAULT GETUTCDATE(),
                        ReviewedByUserId  nvarchar(450) NULL,
                        ReviewedAt        datetime2     NULL,
                        CommentaireRejet  nvarchar(500) NULL,
                        CONSTRAINT FK_DeletionRequests_Resources
                            FOREIGN KEY (ResourceId) REFERENCES Resources(Id)
                            ON DELETE NO ACTION,
                        CONSTRAINT FK_DeletionRequests_Projets
                            FOREIGN KEY (ProjetId) REFERENCES Projets(Id)
                            ON DELETE NO ACTION
                    )
                    CREATE INDEX IX_DeletionRequests_ResourceId
                        ON DeletionRequests(ResourceId)
                    CREATE INDEX IX_DeletionRequests_ProjetId_Statut
                        ON DeletionRequests(ProjetId, Statut)
                END
                """);
        }
    }
}
