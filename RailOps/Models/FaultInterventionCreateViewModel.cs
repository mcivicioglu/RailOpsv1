using System.ComponentModel.DataAnnotations;

namespace RailOps.ViewModels
{
    public class FaultInterventionCreateViewModel
    {
        public int FaultId { get; set; }

        [Required(ErrorMessage =
            "Müdahale açıklaması zorunludur.")]
        [StringLength(2000,
            ErrorMessage =
                "Açıklama en fazla 2000 karakter olabilir.")]
        [Display(Name = "Müdahale Açıklaması")]
        public string Description { get; set; }
            = string.Empty;
    }
}
