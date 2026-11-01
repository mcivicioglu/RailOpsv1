namespace RailOps.Entities
{
    public class Fault
    {
        public int FaultId { get; set; }
        public string? FaultNumber { get; set; }
        public string FaultTitle { get; set; } = string.Empty;
        public string FaultDescription { get; set; } = string.Empty;
        public string FaultLocation { get; set; } = string.Empty;
        public FaultStatus Status { get; set; } = FaultStatus.Open;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }
        public string? AssignedToUserId { get; set; }
        public ApplicationUser? AssignedToUser { get; set; }
        public DateTime? AssignedAtUtc { get; set; }
        public ICollection<FaultIntervention> Interventions { get; set; } = new List<FaultIntervention>();
        public DateTime? ResolvedAtUtc { get; set; }
        public DateTime? ClosedAtUtc { get; set; }
        public string? ClosedByUserId { get; set; }
        public ApplicationUser? ClosedByUser { get; set; }
        public ICollection<FaultRejection> Rejections { get; set; } = new List<FaultRejection>();
        public ICollection<FaultStatusHistory> StatusHistory { get; set; } = new List<FaultStatusHistory>();
        public int? ResponsibleWorkTeamId { get; set; }
        public WorkTeam? ResponsibleWorkTeam { get; set; }

    }

    public enum FaultStatus
    {
        Open = 1,
        InProgress = 2,
        Resolved = 3,
        Closed = 4  
    }
}
