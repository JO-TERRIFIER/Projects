// SmartGPON v3 – Core/Enums/Enums.cs
namespace SmartGPON.Core.Enums
{
    public enum StatutEquipement   : byte { Inactif = 0, Actif = 1, EnPanne = 2, EnMaintenance = 3 }
    public enum ProjetStatut       : byte { EnCours = 0, Termine = 1, Suspendu = 2 }
    public enum ResultatTest       : byte { EnCours = 0, Reussi = 1, Echoue = 2 }
    public enum StatutValidation   : byte { EnAttente = 0, Valide = 1, Rejete = 2 }
    public enum SimulationStatut   : byte { EnAttente = 0, EnCours = 1, Termine = 2, Echoue = 3 }
    public enum NiveauRisque       : byte { Faible = 1, Moyen = 2, Eleve = 3, Critique = 4 }
    public enum AlertSeverite      : byte { Info = 1, Warning = 2, Critical = 3 }
    public enum StatutMaliciousOlt : byte { Actif = 0, Resolu = 1, FauxPositif = 2 }

    public static class UserRoles
    {
        public const string Admin      = "Admin";
        public const string Superviseur = "Superviseur";
        public const string Technicien = "Technicien";
        public const string Readonly   = "Readonly";
    }
}
