
using System.ComponentModel.DataAnnotations;

namespace RailOps.Models
{
    public class FaultEditViewModel
    {
        public int FaultId { get; set; }

        [Required(ErrorMessage = "Başlık zorunludur.")]
        [StringLength(150)]
        public string FaultTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konum zorunludur.")]
        [StringLength(200)]
        public string FaultLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [StringLength(2000)]
        public string FaultDescription { get; set; } = string.Empty;
    }
}
