
namespace RailOps.Entities
{
    public class FaultIntervention
    {
        public int FaultInterventionId { get; set; }

        public int FaultId { get; set; }

        public Fault Fault { get; set; } = null!;

        public string PerformedByUserId { get; set; }
            = string.Empty;

        public ApplicationUser PerformedByUser { get; set; }
            = null!;

        public string Description { get; set; }
            = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
            = DateTime.UtcNow;
    }
}
