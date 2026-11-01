using System.ComponentModel.DataAnnotations;

namespace RailOps.Entities
{
    public class FaultTransferHistory
    {
        public int FaultTransferHistoryId { get; set; }

        public int FaultId { get; set; }

        public int FromWorkTeamId { get; set; }

        public int ToWorkTeamId { get; set; }

        public string TransferredByUserId { get; set; }
            = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; }
            = string.Empty;

        public DateTime TransferredAtUtc { get; set; }

        public Fault Fault { get; set; } = null!;
    }
}