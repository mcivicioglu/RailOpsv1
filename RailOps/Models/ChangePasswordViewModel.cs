
using System.ComponentModel.DataAnnotations;

namespace RailOps.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mevcut parola zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mevcut Parola")]
        public string CurrentPassword { get; set; }
            = string.Empty;

        [Required(ErrorMessage = "Yeni parola zorunludur.")]
        [StringLength(128, MinimumLength = 8,
            ErrorMessage = "Yeni parola 8-128 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Parola")]
        public string NewPassword { get; set; }
            = string.Empty;

        [Required(ErrorMessage = "Parola tekrarı zorunludur.")]
        [Compare(nameof(NewPassword),
            ErrorMessage = "Yeni parolalar eşleşmiyor.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Parola Tekrarı")]
        public string ConfirmNewPassword { get; set; }
            = string.Empty;
    }
}
