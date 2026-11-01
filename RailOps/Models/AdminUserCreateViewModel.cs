using Microsoft.AspNetCore.Mvc.Rendering;
using RailOps.Entities;
using System.ComponentModel.DataAnnotations;

namespace RailOps.ViewModels
{
    public class AdminUserCreateViewModel
    {
        [Required(ErrorMessage = "Sicil numarası zorunludur.")]
        [StringLength(32, ErrorMessage =
            "Sicil numarası en fazla 32 karakter olabilir.")]
        [Display(Name = "Sicil Numarası")]
        public string SicilNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [StringLength(100, ErrorMessage =
            "Ad soyad en fazla 100 karakter olabilir.")]
        [Display(Name = "Ad Soyad")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rol seçimi zorunludur.")]
        [Display(Name = "Personel Rolü")]
        public string Role { get; set; } = string.Empty;
        public int? WorkTeamId { get; set; }

        public List<SelectListItem> WorkTeamOptions { get; set; }
            = new List<SelectListItem>();

        [Required(ErrorMessage = "Parola zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Başlangıç Parolası")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parola tekrarı zorunludur.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password),
            ErrorMessage = "Parolalar eşleşmiyor.")]
        [Display(Name = "Parola Tekrarı")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public TechnicalJobTitle? JobTitle { get; set; }

    }
}
