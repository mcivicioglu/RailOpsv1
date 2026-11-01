using Microsoft.AspNetCore.Mvc.Rendering;
using RailOps.Entities;
using System.ComponentModel.DataAnnotations;

namespace RailOps.ViewModels
{
    public class AdminUserEditViewModel
    {
        public string UserId { get; set; }
            = string.Empty;

        public string SicilNo { get; set; }
            = string.Empty;

        [Required(ErrorMessage =
            "Ad soyad zorunludur.")]
        [StringLength(100,
            ErrorMessage =
                "Ad soyad en fazla 100 karakter olabilir.")]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; }
            = string.Empty;

        public int? WorkTeamId { get; set; }

        public bool IsTechnicalStaff { get; set; }

        public List<SelectListItem> WorkTeamOptions { get; set; }
            = new List<SelectListItem>();
        public TechnicalJobTitle? JobTitle { get; set; }

    }
}
