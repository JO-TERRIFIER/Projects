// ============================================================
// SmartGPON — Core/Enums/Enums.cs — FRESH START
// ============================================================
namespace SmartGPON.Core.Enums
{
    /// <summary>Project-level assignment type (NOT an Identity role).</summary>
    public enum AssignmentType : byte
    {
        ChefProjet = 1,
        TechTerrain = 2,
        TechDessin = 3
    }

    public enum EquipementType : byte
    {
        OLT = 1,
        FDT = 2,
        FAT = 3,
        BPI = 4,
        Chambre = 5,
        Fibre = 6,
        BoitierEtage = 7
    }

    public enum TypeFibre : byte
    {
        FO_4 = 1,
        FO_6 = 2,
        FO_12 = 3,
        FO_24 = 4,
        FO_48 = 5,
        FO_72 = 6,
        FO_96 = 7,
        FO_144 = 8
    }

    public enum TypeSplitter : byte
    {
        x8 = 1,   // immeubles
        x64 = 2   // villas
    }

    public enum ValidationStatut : byte
    {
        EnAttente = 1,
        Valide = 2,
        Rejete = 3
    }

    public enum ProjetStatut : byte
    {
        EnCours = 0,
        Termine = 1,
        Suspendu = 2
    }

    public enum SimulationStatut : byte
    {
        EnAttente = 0,
        EnCours = 1,
        Termine = 2,
        Echoue = 3
    }

    public enum NiveauRisque : byte
    {
        Faible = 1,
        Moyen = 2,
        Eleve = 3,
        Critique = 4
    }

    public enum AlertSeverite : byte
    {
        Info = 1,
        Warning = 2,
        Critical = 3
    }

    /// <summary>ASP.NET Identity roles (AspNetRoles table). Distinct from AssignmentType.</summary>
    public static class UserRoles
    {
        public const string Superviseur = "Superviseur";
        public const string Visiteur = "Visiteur";
        public const string Membre = "Membre";
    }
}
