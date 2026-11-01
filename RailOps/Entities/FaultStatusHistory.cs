
namespace RailOps.Entities
{
    public class FaultStatusHistory
    {
        public int FaultStatusHistoryId { get; set; }

        // Hangi arızaya ait?
        public int FaultId { get; set; }

        public Fault Fault { get; set; } = null!;

        // Değişiklikten önceki durum
        // İlk oluşturulmada null olabilir.
        public FaultStatus? FromStatus { get; set; }

        // Değişiklikten sonraki durum
        public FaultStatus ToStatus { get; set; }

        // İşlemi gerçekleştiren kullanıcı
        public string ChangedByUserId { get; set; }
            = string.Empty;

        public ApplicationUser ChangedByUser { get; set; }
            = null!;

        // İşlemin gerçekleştiği zaman
        public DateTime ChangedAtUtc { get; set; }
    }
}
