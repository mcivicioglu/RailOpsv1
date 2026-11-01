
using System.ComponentModel.DataAnnotations;

namespace RailOps.ViewModels
{
    public class FaultRejectViewModel
    {
        public int FaultId { get; set; }

        [Required(ErrorMessage =
            "Ret gerekçesi zorunludur.")]
        [StringLength(1000,
            ErrorMessage =
                "Ret gerekçesi en fazla 1000 karakter olabilir.")]
        [Display(Name = "Ret Gerekçesi")]
        public string Reason { get; set; }
            = string.Empty;
    }
}
