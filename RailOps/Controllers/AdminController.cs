using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RailOps.Context;
using RailOps.Entities;
using RailOps.ViewModels;
using RailOps.ViewModels.Admin;

namespace RailOps.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RailOpsContext _context;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RailOpsContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .AsNoTracking()
                .OrderBy(u => u.UserName)
                .ToListAsync();

            var model = new List<AdminUserListItemViewModel>();

            foreach (var user in users)
            {
                var roles =
                    await _userManager.GetRolesAsync(user);

                model.Add(new AdminUserListItemViewModel
                {
                    UserId = user.Id,
                    SicilNo = user.UserName ?? "-",
                    FullName = user.FullName ?? "-",
                    Roles = roles.ToList(),
                    IsActive = user.IsActive
                });
            }

            return View(model);
        }

        private async Task<List<SelectListItem>>
            GetWorkTeamOptionsAsync()
        {
            var teams = await _context.WorkTeams
                .AsNoTracking()
                .OrderBy(x =>
                    x.Section.Directorate.DirectorateName)
                .ThenBy(x => x.Section.SectionName)
                .ThenBy(x => x.LineGroup.LineGroupName)
                .Select(x => new
                {
                    x.WorkTeamId,
                    x.Section.Directorate.DirectorateName,
                    x.Section.SectionName,
                    x.LineGroup.LineGroupName
                })
                .ToListAsync();

            return teams.Select(x => new SelectListItem
            {
                Value = x.WorkTeamId.ToString(),

                Text = $"{x.DirectorateName} / " +
                       $"{x.SectionName} / " +
                       $"{x.LineGroupName}"

            }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new AdminUserCreateViewModel
            {
                WorkTeamOptions =
                    await GetWorkTeamOptionsAsync()
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AdminUserCreateViewModel model)
        {
            // 1. Form hata ile geri dönerse
            // ekip listesinin boş kalmaması için yeniden yükle.
            model.WorkTeamOptions =
                await GetWorkTeamOptionsAsync();

            // 2. Yalnızca izin verilen roller
            if (model.Role is not
                ("TechnicalStaff" or "OperationsStaff"))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Geçersiz personel rolü.");
            }

            // 3. İşletme personeline teknik ekip atanamaz.
            // Bu kontrol JavaScript'ten bağımsızdır.
            if (model.Role == "OperationsStaff"
                && model.WorkTeamId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "İşletme personeline teknik ekip atanamaz.");
            }

            // 4. Teknik personelin seçtiği ekip mevcut mu?
            if (model.Role == "TechnicalStaff"
                && model.WorkTeamId.HasValue)
            {
                bool teamExists = await _context.WorkTeams
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.WorkTeamId == model.WorkTeamId.Value);

                if (!teamExists)
                {
                    ModelState.AddModelError(
                        nameof(model.WorkTeamId),
                        "Seçilen teknik ekip bulunamadı.");
                }
            }

            // İşletme personeline teknik görev unvanı atanamaz
            if (model.Role == "OperationsStaff" &&
                model.JobTitle.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.JobTitle),
                    "İşletme personeline teknik görev unvanı atanamaz.");
            }

            // Teknik personel için gönderilen unvan geçerli mi?
            if (model.Role == "TechnicalStaff" &&
                model.JobTitle.HasValue)
            {
                bool isValidJobTitle = Enum.IsDefined(
                    typeof(TechnicalJobTitle),
                    model.JobTitle.Value);

                if (!isValidJobTitle)
                {
                    ModelState.AddModelError(
                        nameof(model.JobTitle),
                        "Geçersiz görev unvanı seçildi.");
                }
            }

            // TEST-12/D-5D-4
            // Yeni oluşturulacak aktif teknik personelin
            // mevcut ekip sorumlusundan daha yüksek unvana
            // sahip olup olmadığını kontrol et.

            if (model.Role == "TechnicalStaff" &&
                model.WorkTeamId.HasValue &&
                model.JobTitle.HasValue &&
                (model.JobTitle.Value is
                    TechnicalJobTitle.Chief or
                    TechnicalJobTitle.Engineer or
                    TechnicalJobTitle.Foreman))
            {
                // Seçilen ekibin mevcut sorumlusunun unvanını getir.
                var leaderJobTitle = await _context.WorkTeams
                    .AsNoTracking()
                    .Where(w =>
                        w.WorkTeamId == model.WorkTeamId.Value &&
                        w.TeamLeaderUserId != null)
                    .Select(w => w.TeamLeader!.JobTitle)
                    .SingleOrDefaultAsync();

                // Enum sıralaması:
                // Şef = 1, Mühendis = 2, Formen = 3
                // Küçük sayı = daha yüksek öncelik.

                if (leaderJobTitle.HasValue &&
                    (int)model.JobTitle.Value <
                    (int)leaderJobTitle.Value)
                {
                    ModelState.AddModelError(
                        nameof(model.JobTitle),
                        "Bu teknik ekipte daha düşük öncelikli " +
                        "unvana sahip bir personel ekip sorumlusu " +
                        "olarak atanmış. Yeni personeli oluşturmadan " +
                        "önce mevcut ekip sorumluluğunu kaldırmalı " +
                        "veya yeniden düzenlemelisiniz.");
                }
            }

            // 5. Form doğrulaması
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 6. Sicil ve ad soyad bilgilerini düzenle
            var sicilNo =
                model.SicilNo?.Trim() ?? string.Empty;

            var fullName =
                model.FullName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(sicilNo)
                || string.IsNullOrWhiteSpace(fullName))
            {
                ModelState.AddModelError(
                    "",
                    "Sicil numarası ve ad soyad boş olamaz.");

                return View(model);
            }

            // 7. Sicil numarası daha önce kullanılmış mı?
            var existingUser =
                await _userManager.FindByNameAsync(sicilNo);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.SicilNo),
                    "Bu sicil numarası zaten kayıtlı.");

                return View(model);
            }

            // 8. Kullanıcı ve rol atamasını
            // aynı transaction'da gerçekleştir
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var user = new ApplicationUser
            {
                UserName = sicilNo,
                FullName = fullName,
                Email = null,
                IsActive = true,
                MustChangePassword = true,

                // YENİ: Teknik personelin ekip ataması
                WorkTeamId = model.Role == "TechnicalStaff"
                    ? model.WorkTeamId
                    : null,

                JobTitle = model.Role == "TechnicalStaff"
                    ? model.JobTitle
                    : null
            };

            // 9. Identity ile kullanıcı oluştur
            var createResult =
                await _userManager.CreateAsync(
                    user, model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        "", error.Description);
                }

                await transaction.RollbackAsync();
                return View(model);
            }

            // 10. Seçilen rolü kullanıcıya ata
            var roleResult =
                await _userManager.AddToRoleAsync(
                    user, model.Role);

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        "", error.Description);
                }

                await transaction.RollbackAsync();
                return View(model);
            }

            // 11. Kullanıcı, ekip bilgisi ve rol
            // birlikte kaydedildi
            await transaction.CommitAsync();

            TempData["AdminSuccess"] =
                "Personel hesabı başarıyla oluşturuldu.";

            return RedirectToAction(nameof(Index));
        }



        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            // 1. Kullanıcı kimliğini kontrol et
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            // 2. Kullanıcıyı bul
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            // 3. Admin hesapları bu ekrandan düzenlenemez
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return Forbid();

            // 4. Kullanıcının gerçek rollerini kontrol et
            bool isTechnicalStaff = await _userManager
                .IsInRoleAsync(user, "TechnicalStaff");

            bool isOperationsStaff = await _userManager
                .IsInRoleAsync(user, "OperationsStaff");

            // Sadece teknik role sahip kullanıcı için
            // ekip seçimi gösterilecek.
            bool canAssignTeam =
                isTechnicalStaff && !isOperationsStaff;

            // 5. ViewModel'i hazırla
            var model = new AdminUserEditViewModel
            {
                UserId = user.Id,

                SicilNo = user.UserName ?? string.Empty,

                FullName = user.FullName ?? string.Empty,

                IsTechnicalStaff = canAssignTeam,

                WorkTeamId = canAssignTeam
                    ? user.WorkTeamId
                    : null,
                JobTitle = canAssignTeam
                    ? user.JobTitle
                    : null
            };

            // 6. Teknik personelse ekip seçeneklerini yükle
            if (canAssignTeam)
            {
                model.WorkTeamOptions =
                    await GetWorkTeamOptionsAsync();
            }

            // 7. Düzenleme ekranını aç
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            AdminUserEditViewModel model)
        {

            if (string.IsNullOrWhiteSpace(model.UserId))
                return BadRequest();

            var user = await _userManager
                .FindByIdAsync(model.UserId);

            if (user == null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return Forbid();

            bool isTechnicalStaff = await _userManager
                .IsInRoleAsync(user, "TechnicalStaff");

            bool isOperationsStaff = await _userManager
                .IsInRoleAsync(user, "OperationsStaff");

            if (isTechnicalStaff == isOperationsStaff)
            {
                return BadRequest(
                    "Kullanıcının personel rolü geçersiz.");
            }

            bool canAssignTeam = isTechnicalStaff;

            model.SicilNo = user.UserName ?? string.Empty;

            ModelState.Remove(nameof(model.SicilNo));

            model.IsTechnicalStaff = canAssignTeam;

            model.WorkTeamOptions = canAssignTeam
                ? await GetWorkTeamOptionsAsync()
                : new List<SelectListItem>();

            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError(
                    nameof(model.FullName),
                    "Ad soyad boş olamaz.");
            }

            if (canAssignTeam)
            {
                if (model.WorkTeamId.HasValue)
                {
                    bool teamExists = await _context.WorkTeams
                        .AsNoTracking()
                        .AnyAsync(x =>
                            x.WorkTeamId == model.WorkTeamId.Value);

                    if (!teamExists)
                    {
                        ModelState.AddModelError(
                            nameof(model.WorkTeamId),
                            "Seçilen teknik ekip bulunamadı.");
                    }
                }
            }
            else
            {
                if (model.WorkTeamId.HasValue)
                {
                    ModelState.AddModelError(
                        "",
                        "İşletme personeline teknik ekip atanamaz.");
                }
            }


            if (canAssignTeam)
            {
                if (model.JobTitle.HasValue)
                {
                    bool isValidJobTitle = Enum.IsDefined(
                        typeof(TechnicalJobTitle),
                        model.JobTitle.Value);

                    if (!isValidJobTitle)
                    {
                        ModelState.AddModelError(
                            nameof(model.JobTitle),
                            "Geçersiz görev unvanı seçildi.");
                    }
                }
            }
            else
            {
                if (model.JobTitle.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.JobTitle),
                        "İşletme personeline teknik görev unvanı atanamaz.");
                }
            }

            // TEST-12/D-5B
            // Personel başka bir teknik ekibe geçirilecekse
            // mevcut ekip sorumluluğunu kontrol et.

            if (canAssignTeam &&
                user.WorkTeamId != model.WorkTeamId)
            {
                bool isTeamLeader = await _context.WorkTeams
                    .AsNoTracking()
                    .AnyAsync(w =>
                        w.TeamLeaderUserId == user.Id);

                if (isTeamLeader)
                {
                    ModelState.AddModelError(
                        nameof(model.WorkTeamId),
                        "Bu personel bir teknik ekibin sorumlusu. " +
                        "Başka ekibe aktarılmadan önce mevcut " +
                        "ekip sorumluluğunu kaldırmalı veya " +
                        "başka bir personele devretmelisiniz.");
                }
            }

            // TEST-12/D-5C
            // Ekip sorumlusu olan personelin görev unvanını koru.

            if (canAssignTeam &&
                user.JobTitle != model.JobTitle)
            {
                bool isTeamLeader = await _context.WorkTeams
                    .AsNoTracking()
                    .AnyAsync(w => w.TeamLeaderUserId == user.Id);

                if (isTeamLeader)
                {
                    ModelState.AddModelError(
                        nameof(model.JobTitle),
                        "Bu personel bir teknik ekibin sorumlusu. " +
                        "Görev unvanını değiştirmeden önce mevcut " +
                        "ekip sorumluluğunu kaldırmalı veya " +
                        "başka bir personele devretmelisiniz.");
                }
            }

            // TEST-12/D-5D-1
            // Aktif teknik personelin yeni unvanı veya ekibi,
            // kayıtlı ekip sorumlusundan daha yüksek öncelikli
            // hale geliyorsa değişikliği engelle.

            if (canAssignTeam &&
                user.IsActive &&
                model.WorkTeamId.HasValue &&
                model.JobTitle.HasValue &&
                (user.WorkTeamId != model.WorkTeamId ||
                 user.JobTitle != model.JobTitle) &&
                (model.JobTitle.Value is
                    TechnicalJobTitle.Chief or
                    TechnicalJobTitle.Engineer or
                    TechnicalJobTitle.Foreman))
            {
                var leaderJobTitle = await _context.WorkTeams
                    .AsNoTracking()
                    .Where(w =>
                        w.WorkTeamId == model.WorkTeamId.Value &&
                        w.TeamLeaderUserId != null &&
                        w.TeamLeaderUserId != user.Id)
                    .Select(w => w.TeamLeader!.JobTitle)
                    .SingleOrDefaultAsync();

                if (leaderJobTitle.HasValue &&
                    (int)model.JobTitle.Value < (int)leaderJobTitle.Value)
                {
                    ModelState.AddModelError(
                        nameof(model.JobTitle),
                        "Bu ekipte daha düşük öncelikli unvana sahip " +
                        "bir personel ekip sorumlusu olarak atanmış. " +
                        "Önce mevcut ekip sorumluluğunu kaldırmalı " +
                        "veya gerekli sorumlu değişikliğini yapmalısınız.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user.FullName = model.FullName.Trim();

            user.WorkTeamId = canAssignTeam
                ? model.WorkTeamId
                : null;

            user.JobTitle = canAssignTeam
                ? model.JobTitle
                : null;

            try
            {
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(
                            "", error.Description);
                    }

                    return View(model);
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "Personel bilgileri kaydedilemedi. " +
                    "Lütfen ekip seçimini kontrol edin.");

                return View(model);
            }

            TempData["AdminSuccess"] =
                "Personel bilgileri ve ekip ataması güncellendi.";

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(
            string id,
            [FromForm] bool? isActive)
        {
            // 1. Geçerli kullanıcı ve durum bilgisi gerekli
            if (string.IsNullOrWhiteSpace(id) ||
                !isActive.HasValue)
            {
                return BadRequest();
            }

            // 2. Hedef personeli getir
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            // 3. Yöneticinin kendi hesabını kapatmasını engelle
            if (user.Id == _userManager.GetUserId(User))
                return Forbid();

            // 4. Admin hesapları bu işlemden korunur
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return Forbid();

            // 5. Durum zaten aynıysa güncelleme yapma
            if (user.IsActive == isActive.Value)
            {
                TempData["AdminSuccess"] =
                    "Personelin durumu zaten güncel.";

                return RedirectToAction(nameof(Index));
            }

            // TEST-12/D-5A
            // Personel pasifleştirilecekse ekip sorumluluğunu kontrol et.
            if (!isActive.Value)
            {
                bool isTeamLeader = await _context.WorkTeams
                    .AsNoTracking()
                    .AnyAsync(w => w.TeamLeaderUserId == user.Id);

                if (isTeamLeader)
                {
                    TempData["AdminError"] =
                        "Bu personel bir teknik ekibin sorumlusu olarak atanmış. " +
                        "Personeli pasifleştirmeden önce ekip sorumluluğunu " +
                        "kaldırmalı veya başka bir personele devretmelisiniz.";

                    return RedirectToAction(nameof(Index));
                }
            }

            // TEST-12/D-5D-2
            // Pasif personel aktifleştirilirken,
            // mevcut ekip sorumlusunun unvan önceliğini koru.

            if (isActive.Value &&
                user.WorkTeamId.HasValue &&
                user.JobTitle.HasValue &&
                (user.JobTitle.Value is
                    TechnicalJobTitle.Chief or
                    TechnicalJobTitle.Engineer or
                    TechnicalJobTitle.Foreman))
            {
                bool isTechnicalStaff = await _userManager
                    .IsInRoleAsync(user, "TechnicalStaff");

                if (isTechnicalStaff)
                {
                    // Personelin bağlı olduğu ekibin mevcut
                    // sorumlusunun görev unvanını getir.
                    var leaderJobTitle = await _context.WorkTeams
                        .AsNoTracking()
                        .Where(w =>
                            w.WorkTeamId == user.WorkTeamId.Value &&
                            w.TeamLeaderUserId != null &&
                            w.TeamLeaderUserId != user.Id)
                        .Select(w => w.TeamLeader!.JobTitle)
                        .SingleOrDefaultAsync();

                    // Enum önceliği:
                    // Şef = 1, Mühendis = 2, Formen = 3
                    // Küçük değer = daha yüksek öncelik.
                    if (leaderJobTitle.HasValue &&
                        (int)user.JobTitle.Value <
                        (int)leaderJobTitle.Value)
                    {
                        TempData["AdminError"] =
                            "Bu personel aktifleştirilemez. " +
                            "Bağlı olduğu teknik ekipte daha düşük " +
                            "öncelikli unvana sahip bir personel " +
                            "ekip sorumlusu olarak atanmış. " +
                            "Önce mevcut sorumluluğu kaldırmalı " +
                            "veya yeniden düzenlemelisiniz.";

                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            // 6. Aktiflik durumunu değiştir
            user.IsActive = isActive.Value;

            // 7. Durumu kaydet ve eski oturumları geçersiz kıl
            var result =
                await _userManager.UpdateSecurityStampAsync(user);

            if (!result.Succeeded)
            {
                TempData["AdminError"] =
                    "Personel durumu güncellenemedi.";

                return RedirectToAction(nameof(Index));
            }

            TempData["AdminSuccess"] = user.IsActive
                ? "Personel hesabı aktifleştirildi."
                : "Personel hesabı pasifleştirildi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return Forbid();

            var model = new AdminPasswordResetViewModel
            {
                UserId = user.Id,
                SicilNo = user.UserName ?? string.Empty
            };

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            AdminPasswordResetViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.UserId))
                return BadRequest();

            var user = await _userManager
                .FindByIdAsync(model.UserId);

            if (user == null)
                return NotFound();

            // Yönetici hesapları bu ekrandan sıfırlanamaz
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return Forbid();

            // Sicil numarasını veritabanından al
            model.SicilNo = user.UserName ?? string.Empty;

            if (!ModelState.IsValid)
                return View(model);

            // İşlemi yapan yöneticinin parolasını doğrula
            var admin = await _userManager.GetUserAsync(User);

            if (admin == null ||
                !await _userManager.CheckPasswordAsync(
                    admin, model.AdminPassword))
            {
                ModelState.AddModelError(
                    nameof(model.AdminPassword),
                    "Yönetici parolası doğrulanamadı.");

                return View(model);
            }

            // Token yalnızca sunucuda oluşturulur ve kullanılır
            var token = await _userManager
                .GeneratePasswordResetTokenAsync(user);
            user.MustChangePassword = true;
            var result = await _userManager
                .ResetPasswordAsync(
                    user, token, model.TemporaryPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        "", error.Description);
                }

                return View(model);
            }

            TempData["AdminSuccess"] =
                "Personelin geçici parolası güncellendi.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> WorkTeamLeaders()
        {
            var teams = await _context.WorkTeams
                .AsNoTracking()
                .OrderBy(x => x.Section.Directorate.DirectorateName)
                .ThenBy(x => x.Section.SectionName)
                .ThenBy(x => x.LineGroup.LineGroupName)
                .Select(x => new WorkTeamLeaderListItemViewModel
                {
                    WorkTeamId = x.WorkTeamId,

                    DirectorateName =
                        x.Section.Directorate.DirectorateName,

                    SectionName =
                        x.Section.SectionName,

                    LineGroupName =
                        x.LineGroup.LineGroupName,

                    TeamLeaderName = x.TeamLeader != null
                        ? x.TeamLeader.FullName
                        : null
                })
                .ToListAsync();

            return View(teams);
        }

        // Ekip sorumlusu adaylarını göster (yalnızca Admin).
        [HttpGet]
        public async Task<IActionResult> AssignTeamLeader(int id)
        {
            if (id <= 0)
                return NotFound();

            var team = await _context.WorkTeams
                .AsNoTracking()
                .Where(x => x.WorkTeamId == id)
                .Select(x => new
                {
                    x.WorkTeamId,
                    x.TeamLeaderUserId,
                    x.Section.Directorate.DirectorateName,
                    x.Section.SectionName,
                    x.LineGroup.LineGroupName
                })
                .SingleOrDefaultAsync();

            if (team == null)
                return NotFound();

            // GET ve POST aynı aday belirleme kurallarını kullanır.
            var candidates = await GetTeamLeaderCandidatesAsync(team.WorkTeamId);

            var model = new WorkTeamLeaderAssignViewModel
            {
                WorkTeamId = team.WorkTeamId,
                DirectorateName = team.DirectorateName,
                SectionName = team.SectionName,
                LineGroupName = team.LineGroupName,
                TeamLeaderUserId = team.TeamLeaderUserId,
                CandidateOptions = candidates
            };

            return View(model);
        }

        // Ekip sorumlusu atamasını kaydet.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTeamLeader(
            int workTeamId,
            string? teamLeaderUserId)
        {
            if (workTeamId <= 0)
                return NotFound();

            var team = await _context.WorkTeams
                .Include(x => x.Section)
                    .ThenInclude(x => x.Directorate)
                .Include(x => x.LineGroup)
                .SingleOrDefaultAsync(x => x.WorkTeamId == workTeamId);

            if (team == null)
                return NotFound();

            // Formdan gelen kullanıcı ID'sine güvenmeyiz:
            // adayları her POST isteğinde tekrar hesaplarız.
            var candidates = await GetTeamLeaderCandidatesAsync(team.WorkTeamId);

            var model = new WorkTeamLeaderAssignViewModel
            {
                WorkTeamId = team.WorkTeamId,
                DirectorateName = team.Section.Directorate.DirectorateName,
                SectionName = team.Section.SectionName,
                LineGroupName = team.LineGroup.LineGroupName,
                TeamLeaderUserId = teamLeaderUserId,
                CandidateOptions = candidates
            };

            // TEST-12/D-4A
            // Sorumlu seçimi boş bırakılmışsa atamayı kaldır.
            if (string.IsNullOrWhiteSpace(teamLeaderUserId))
            {
                team.TeamLeaderUserId = null;

                await _context.SaveChangesAsync();

                TempData["AdminSuccess"] =
                    "Ekip sorumlusu ataması kaldırıldı.";

                return RedirectToAction(
                    nameof(WorkTeamLeaders));
            }

            // Boş seçim, farklı ekip, pasif kullanıcı, Teknik Personel
            // olmayan kullanıcı veya unvan önceliğini bozan aday reddedilir.
            bool isValidCandidate =
                !string.IsNullOrWhiteSpace(teamLeaderUserId) &&
                candidates.Any(x => x.Value == teamLeaderUserId);


            if (!isValidCandidate)
            {
                ModelState.AddModelError(
                    nameof(model.TeamLeaderUserId),
                    "Seçilen personel ekip sorumlusu olarak atanmaya uygun değil.");

                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            team.TeamLeaderUserId = teamLeaderUserId;
            await _context.SaveChangesAsync();

            TempData["AdminSuccess"] =
                "Ekip sorumlusu başarıyla atandı.";

            return RedirectToAction(nameof(WorkTeamLeaders));
        }

        private async Task<List<SelectListItem>>
            GetTeamLeaderCandidatesAsync(int workTeamId)
        {
            var technicalUsers = await _userManager
                .GetUsersInRoleAsync("TechnicalStaff");

            // Aynı ekipteki aktif teknik personeller
            var eligibleUsers = technicalUsers
                .Where(u =>
                    u.IsActive &&
                    u.WorkTeamId == workTeamId)
                .ToList();

            TechnicalJobTitle? requiredTitle = null;

            // Unvan önceliği
            if (eligibleUsers.Any(u =>
                u.JobTitle == TechnicalJobTitle.Chief))
            {
                requiredTitle = TechnicalJobTitle.Chief;
            }
            else if (eligibleUsers.Any(u =>
                u.JobTitle == TechnicalJobTitle.Engineer))
            {
                requiredTitle = TechnicalJobTitle.Engineer;
            }
            else if (eligibleUsers.Any(u =>
                u.JobTitle == TechnicalJobTitle.Foreman))
            {
                requiredTitle = TechnicalJobTitle.Foreman;
            }

            if (!requiredTitle.HasValue)
                return new List<SelectListItem>();

            return eligibleUsers
                .Where(u => u.JobTitle == requiredTitle.Value)
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.UserName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FullName} ({u.UserName})"
                })
                .ToList();
        }

    }
}
