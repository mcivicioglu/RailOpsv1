
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RailOps.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        // Kayıt sayfasını görüntülemeyi engelle
        public IActionResult OnGet()
        {
            return NotFound();
        }

        // Kullanıcı oluşturma isteğini engelle
        public IActionResult OnPost()
        {
            return NotFound();
        }
    }
}
