
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RailOps.Context;
using RailOps.Entities;
using RailOps.ViewModels;

namespace RailOps.Controllers
{
    [Authorize]
    public class PasswordController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RailOpsContext _context;

        public PasswordController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RailOpsContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Change()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Change(
            ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Yeni ve eski parola aynı olmamalı
            if (model.CurrentPassword == model.NewPassword)
            {
                ModelState.AddModelError(
                    nameof(model.NewPassword),
                    "Yeni parola mevcut paroladan farklı olmalıdır.");

                return View(model);
            }

            // İşlemi yapan kullanıcı
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            // Parola ve zorunluluk bayrağı birlikte güncellensin
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "", error.Description);
                }

                await transaction.RollbackAsync();
                return View(model);
            }

            // Parola başarıyla değişti
            user.MustChangePassword = false;

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        "", error.Description);
                }

                await transaction.RollbackAsync();
                return View(model);
            }

            await transaction.CommitAsync();

            // Mevcut oturumu yeni Security Stamp ile yenile
            await _signInManager.RefreshSignInAsync(user);

            TempData["PasswordSuccess"] =
                "Parolanız başarıyla değiştirildi.";

            return RedirectToAction(nameof(Change));
        }
    }
}
