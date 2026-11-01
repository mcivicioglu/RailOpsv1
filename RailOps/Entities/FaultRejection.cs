
namespace RailOps.Entities
{
    public class FaultRejection
    {
        public int FaultRejectionId { get; set; }

        public int FaultId { get; set; }

        public Fault Fault { get; set; } = null!;

        public string RejectedByUserId { get; set; }
            = string.Empty;

        public ApplicationUser RejectedByUser
        { get; set; } = null!;

        public string Reason { get; set; }
            = string.Empty;

        public DateTime RejectedAtUtc { get; set; }

        // Reddedilen teknik çözümün tamamlanma zamanı
        public DateTime? PreviousResolvedAtUtc
        { get; set; }
    }
}
