
using System.ComponentModel.DataAnnotations;

namespace RailOps.ViewModels
{
    public class AdminPasswordResetViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public string SicilNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Geçici parola zorunludur.")]
        [StringLength(128, MinimumLength = 8,
            ErrorMessage = "Parola 8-128 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Geçici Parola")]
        public string TemporaryPassword { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(TemporaryPassword),
            ErrorMessage = "Parolalar eşleşmiyor.")]
        [DataType(DataType.Password)]
        [Display(Name = "Geçici Parola Tekrarı")]
        public string ConfirmTemporaryPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yönetici parolanızı giriniz.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yönetici Parolanız")]
        public string AdminPassword { get; set; } = string.Empty;
    }
}
