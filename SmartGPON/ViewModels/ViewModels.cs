// SmartGPON v3 - Web/ViewModels/ViewModels.cs
using System;
using System.Collections.Generic;
using SmartGPON.Core.Entities;
using SmartGPON.Core.Enums;

namespace SmartGPON.Web.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalProjets { get; set; }
        public int ProjetsTermines { get; set; }
        public int ProjetsEnCours { get; set; }
        public int ProjetsSuspendus { get; set; }
    }


    public class SecurityDashboardViewModel
    {
        public int AlertesCritiques { get; set; }
        public int AlertesWarning { get; set; }
        public int RogueOltsActifs { get; set; }
        public int SimulationsEnCours { get; set; }
        public int EvenementsAujourdhui { get; set; }
        public PagedResult<NetworkAlert> AlertesPaginees { get; set; } = new PagedResult<NetworkAlert>();
        public List<MaliciousOlt> RogueOlts { get; set; } = new();
        public List<AttackSimulation> SimulationsRecentes { get; set; } = new();
        public List<SecurityEvent> EvenementsRecents { get; set; } = new();
    }

    public class OltCapacityViewModel
    {
        public int OltId { get; set; }
        public string OltNom { get; set; } = string.Empty;
        public int NbrePorts { get; set; }
        public int NbreFdts { get; set; }
        public int NbreFats { get; set; }
        public int NbreBpis { get; set; }
        public decimal TauxActivite { get; set; }
        public int CapaciteRestante => NbrePorts - NbreFdts;
    }

    public class NetworkTreeViewModel
    {
        public int ZoneId { get; set; }
        public string ZoneNom { get; set; } = string.Empty;
        public List<OltTreeNode> Olts { get; set; } = new();
    }

    public class OltTreeNode
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public StatutEquipement Statut { get; set; }
        public List<FdtTreeNode> Fdts { get; set; } = new();
    }

    public class FdtTreeNode
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public int NbSplitters1x8 { get; set; }
        public int NbSplitters1x64 { get; set; }
        public List<FatTreeNode> Fats { get; set; } = new();
        public List<BpiTreeNode> Bpis { get; set; } = new();
    }

    public class FatTreeNode
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public int Capacite { get; set; }
    }

    public class BpiTreeNode
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public int Capacite { get; set; }
        public int NbSplitters1x8 { get; set; }
    }

    public class SimulationFormViewModel
    {
        public int? OltId { get; set; }
        public string TypeAttaque { get; set; } = string.Empty;
        public NiveauRisque NiveauRisque { get; set; } = NiveauRisque.Moyen;
        public string? Parametres { get; set; }
        public List<Olt> Olts { get; set; } = new();
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPrev => Page > 1;
        public bool HasNext => Page < TotalPages;
    }
}
