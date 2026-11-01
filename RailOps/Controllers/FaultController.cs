using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RailOps.Context;
using RailOps.Entities;
using RailOps.Models;
using RailOps.Services;
using RailOps.ViewModels;

namespace RailOps.Controllers
{
    [Authorize]
    public class FaultController : Controller
    {
        private readonly RailOpsContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FaultCreationService _faultCreationService;

        public FaultController(
            RailOpsContext context,
            UserManager<ApplicationUser> userManager,
            FaultCreationService faultCreationService)
        {
            _context = context;
            _userManager = userManager;
            _faultCreationService = faultCreationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // TEST-13/D-3
            // 1. Giriş yapan kullanıcıyı getir.
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            // 2. Pasif kullanıcı listeyi görüntüleyemez.
            if (!currentUser.IsActive)
            {
                return Forbid();
            }

            // 3. Kullanıcının rollerini belirle.
            bool isAdmin = User.IsInRole("Admin");

            bool isOperationsStaff =
                User.IsInRole("OperationsStaff");

            bool isTechnicalStaff =
                User.IsInRole("TechnicalStaff");

            // 4. Kullanıcının kimlik ve ekip bilgileri.
            var userId = currentUser.Id;
            var workTeamId = currentUser.WorkTeamId;

            bool canViewTeamFaults =
                isTechnicalStaff && workTeamId.HasValue;

            // 5. Sorguyu oluştur.
            // Henüz veritabanından kayıt çekmiyoruz.
            var query = _context.Faults
                .AsNoTracking();

            // 6. Admin ve İşletme dışındaki kullanıcılar
            // yalnızca yetkili oldukları arızaları görür.
            if (!isAdmin && !isOperationsStaff)
            {
                query = query.Where(f =>

                    // Kullanıcının oluşturduğu arızalar
                    f.CreatedByUserId == userId ||

                    // Kendi teknik ekibindeki arızalar
                    (
                        canViewTeamFaults &&
                        f.ResponsibleWorkTeamId == workTeamId
                    ) ||

                    // Hâlen kendisine atanmış arızalar
                    (
                        isTechnicalStaff &&
                        f.AssignedToUserId == userId
                    )
                );
            }

            // 7. Filtrelenmiş arızaları listele.
            var faults = await query
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(faults);
        }

        private async Task<List<SelectListItem>>
            GetResponsibleWorkTeamOptionsAsync()
        {
            // 1. Teknik ekipleri veritabanından getir
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

            // 2. Dropdown seçeneklerine dönüştür
            return teams.Select(x => new SelectListItem
            {
                Value = x.WorkTeamId.ToString(),

                Text = $"{x.DirectorateName} / " +
                       $"{x.SectionName} / " +
                       $"{x.LineGroupName}"
            }).ToList();
        }

        [HttpGet]
        [Authorize(Roles = "OperationsStaff")]
        public async Task<IActionResult> Create()
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            if (!currentUser.IsActive)
            {
                return Forbid();
            }

            var model = new FaultCreateViewModel
            {
                WorkTeamOptions =
                    await GetResponsibleWorkTeamOptionsAsync()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "OperationsStaff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            FaultCreateViewModel model)
        {
            if (!model.ResponsibleWorkTeamId.HasValue ||
                model.ResponsibleWorkTeamId.Value <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.ResponsibleWorkTeamId),
                    "Sorumlu teknik ekip seçiniz.");
            }
            else
            {
                bool teamExists = await _context.WorkTeams
                    .AsNoTracking()
                    .AnyAsync(w =>
                        w.WorkTeamId ==
                        model.ResponsibleWorkTeamId.Value);

                if (!teamExists)
                {
                    ModelState.AddModelError(
                        nameof(model.ResponsibleWorkTeamId),
                        "Seçilen teknik ekip bulunamadı.");
                }
            }
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            if (!currentUser.IsActive)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                model.WorkTeamOptions =
                    await GetResponsibleWorkTeamOptionsAsync();

                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var fault = new Fault
            {
                FaultTitle = model.FaultTitle.Trim(),
                FaultLocation = model.FaultLocation.Trim(),
                FaultDescription = model.FaultDescription.Trim(),

                CreatedByUserId = userId,

                ResponsibleWorkTeamId =
                    model.ResponsibleWorkTeamId.Value
            };

            var faultId =
                await _faultCreationService.CreateAsync(fault);

            return RedirectToAction(
                nameof(Details),
                new { id = faultId });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var fault = await _context.Faults
                .AsNoTracking()
                .Include(f => f.CreatedByUser)
                .Include(f => f.AssignedToUser)
                .Include(f => f.ClosedByUser)
                .Include(f => f.ResponsibleWorkTeam)
                    .ThenInclude(w => w!.Section)
                    .ThenInclude(s => s.Directorate)
                .Include(f => f.ResponsibleWorkTeam)
                    .ThenInclude(w => w!.LineGroup)
                .Include(f => f.Interventions)
                    .ThenInclude(i => i.PerformedByUser)
                .Include(f => f.Rejections)
                    .ThenInclude(r => r.RejectedByUser)
                .Include(f => f.StatusHistory)
                    .ThenInclude(h => h.ChangedByUser)
                .AsSplitQuery()
                .FirstOrDefaultAsync(f => f.FaultId == id);

            if (fault == null)
            {
                return NotFound();
            }

            // TEST-13/D-1
            // Giriş yapan kullanıcının güncel bilgilerini getir.
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            // Pasif kullanıcının detay erişimini engelle.
            if (!currentUser.IsActive)
            {
                return Forbid();
            }

            // Kullanıcının rollerini belirle.
            bool isAdmin = User.IsInRole("Admin");
            bool isTechnicalStaff = User.IsInRole("TechnicalStaff");
            bool isOperationsStaff = User.IsInRole("OperationsStaff");

            // 1. Arızayı oluşturan kişi mi?
            bool isFaultCreator =
                fault.CreatedByUserId == currentUser.Id;

            // 2. Arızanın sorumlu teknik ekibinde mi?
            bool isResponsibleTeamMember =
                isTechnicalStaff &&
                currentUser.WorkTeamId.HasValue &&
                fault.ResponsibleWorkTeamId.HasValue &&
                currentUser.WorkTeamId.Value ==
                    fault.ResponsibleWorkTeamId.Value;

            // 3. Arızayı daha önce üstlenen teknik personel mi?
            // Sonradan ekip değişse bile mevcut müdahalesini
            // görüntüleyebilmesini koruyoruz.
            bool isAssignedTechnicalStaff =
                isTechnicalStaff &&
                fault.AssignedToUserId == currentUser.Id;

            // 4. Temel görüntüleme yetkisi
            bool canViewFault =
                isAdmin ||
                isFaultCreator ||
                isResponsibleTeamMember ||
                isAssignedTechnicalStaff ||
                isOperationsStaff;

            if (!canViewFault)
            {
                return Forbid();
            }

            bool canClaimFault =
                isTechnicalStaff &&
                currentUser.WorkTeamId.HasValue &&
                fault.ResponsibleWorkTeamId.HasValue &&
                currentUser.WorkTeamId.Value ==
                    fault.ResponsibleWorkTeamId.Value &&
                fault.Status == FaultStatus.Open &&
                fault.AssignedToUserId == null;

            ViewData["CanClaimFault"] = canClaimFault;

            return View(fault);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var fault = await _context.Faults
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FaultId == id);

            if (fault == null)
                return NotFound();

            // Arızayı yalnızca oluşturan kişi düzenleyebilir
            if (fault.CreatedByUserId != userId)
                return Forbid();

            // Yalnızca Açık durumdaki arızalar düzenlenebilir
            if (fault.Status != FaultStatus.Open)
            {
                return Conflict(
                    "Yalnızca açık durumdaki arızalar düzenlenebilir.");
            }

            var model = new FaultEditViewModel
            {
                FaultId = fault.FaultId,
                FaultTitle = fault.FaultTitle,
                FaultLocation = fault.FaultLocation,
                FaultDescription = fault.FaultDescription
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, FaultEditViewModel model)
        {
            // URL ve formdaki arıza kimliği aynı olmalı
            if (id != model.FaultId)
                return BadRequest();

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            // Güncel kayıt ve yetki kontrolü
            var fault = await _context.Faults
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FaultId == id);

            if (fault == null)
                return NotFound();

            if (fault.CreatedByUserId != userId)
                return Forbid();

            if (fault.Status != FaultStatus.Open)
            {
                return Conflict(
                    "Bu arıza artık düzenlenebilir durumda değil.");
            }

            if (!ModelState.IsValid)
                return View(model);

            // Formdaki değerleri hazırla
            var title = model.FaultTitle.Trim();
            var location = model.FaultLocation.Trim();
            var description = model.FaultDescription.Trim();

            // Kayıt, kontrolümüzden sonra üstlenilmiş olabilir.
            // Bu yüzden koşulları SQL UPDATE sırasında tekrar uygula.
            var affectedRows = await _context.Faults
                .Where(f =>
                    f.FaultId == id &&
                    f.CreatedByUserId == userId &&
                    f.Status == FaultStatus.Open)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(f => f.FaultTitle, title)
                    .SetProperty(f => f.FaultLocation, location)
                    .SetProperty(f => f.FaultDescription, description));

            if (affectedRows != 1)
            {
                return Conflict(
                    "Arızanın durumu değişmiş olabilir. " +
                    "Lütfen kaydı yeniden kontrol edin.");
            }

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        [HttpPost]
        [Authorize(Roles = "TechnicalStaff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Claim(int id)
        {
            // TEST-13/C-1
            // Giriş yapan teknik personeli getir.
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return Challenge();
            }

            // Personel aktif olmalı ve bir teknik ekibe bağlı olmalı.
            if (!currentUser.IsActive ||
                !currentUser.WorkTeamId.HasValue)
            {
                TempData["ClaimError"] =
                    "Arıza üstlenilemedi. Aktif bir teknik personel " +
                    "olmalı ve bir teknik ekibe bağlı bulunmalısınız.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var userId = currentUser.Id;
            var workTeamId = currentUser.WorkTeamId.Value;

            var now = DateTime.UtcNow;

            // Durum değişikliği ve geçmiş kaydı
            // birlikte başarılı olmalı.
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // Yalnızca açık ve henüz üstlenilmemiş arızayı güncelle
            var affectedRows = await _context.Faults
                .Where(f =>
                    f.FaultId == id &&
                    f.Status == FaultStatus.Open &&
                    f.AssignedToUserId == null &&
                    f.ResponsibleWorkTeamId == workTeamId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        f => f.Status,
                        FaultStatus.InProgress)
                    .SetProperty(
                        f => f.AssignedToUserId,
                        userId)
                    .SetProperty(
                        f => f.AssignedAtUtc,
                        (DateTime?)now));

            if (affectedRows == 0)
            {
                TempData["ClaimError"] =
                    "Arıza üstlenilemedi. Arıza sizin teknik ekibinize " +
                    "ait olmayabilir, başka bir personel tarafından " +
                    "üstlenilmiş olabilir veya durumu değişmiş olabilir.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // Başarılı üstlenme için durum geçmişi oluştur
            var history = new FaultStatusHistory
            {
                FaultId = id,
                FromStatus = FaultStatus.Open,
                ToStatus = FaultStatus.InProgress,
                ChangedByUserId = userId,
                ChangedAtUtc = now
            };

            _context.FaultStatusHistories.Add(history);

            await _context.SaveChangesAsync();

            // İki işlem de başarılıysa transaction tamamlanır
            await transaction.CommitAsync();

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        [HttpGet]
        [Authorize(Roles = "TechnicalStaff")]
        public async Task<IActionResult> AddIntervention(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var fault = await _context.Faults
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FaultId == id);

            if (fault == null)
                return NotFound();

            // Yalnızca üstlenen personel işlem yapabilir
            if (fault.Status != FaultStatus.InProgress ||
                fault.AssignedToUserId != userId)
            {
                return Forbid();
            }

            ViewBag.FaultNumber =
                fault.FaultNumber ?? $"Eski #{fault.FaultId}";

            return View(new FaultInterventionCreateViewModel
            {
                FaultId = fault.FaultId
            });
        }

        [HttpPost]
        [Authorize(Roles = "TechnicalStaff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIntervention(
            FaultInterventionCreateViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var fault = await _context.Faults
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    f => f.FaultId == model.FaultId);

            if (fault == null)
                return NotFound();

            // Yetkiyi POST sırasında tekrar kontrol ediyoruz
            if (fault.Status != FaultStatus.InProgress ||
                fault.AssignedToUserId != userId)
            {
                return Forbid();
            }

            ViewBag.FaultNumber =
                fault.FaultNumber ?? $"Eski #{fault.FaultId}";

            if (!ModelState.IsValid)
                return View(model);

            var intervention = new FaultIntervention
            {
                FaultId = fault.FaultId,
                PerformedByUserId = userId,
                Description = model.Description.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.FaultInterventions.Add(intervention);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Details),
                new { id = fault.FaultId });
        }

        [HttpPost]
        [Authorize(Roles = "TechnicalStaff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            // 1. Giriş yapan kullanıcıyı al
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var now = DateTime.UtcNow;

            // 2. Durum değişikliği ve geçmiş kaydı
            // aynı transaction içinde gerçekleşecek
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // 3. Uygun durumdaki arızayı güncelle
            var affectedRows = await _context.Faults
                .Where(f =>
                    f.FaultId == id &&
                    f.Status == FaultStatus.InProgress &&
                    f.AssignedToUserId == userId &&

                    // Son ret işleminden sonra yeni müdahale
                    // kaydı bulunması zorunludur
                    f.Interventions.Any(i =>
                        !f.Rejections.Any(r =>
                            r.RejectedAtUtc >= i.CreatedAtUtc))
                )
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        f => f.Status,
                        FaultStatus.Resolved)
                    .SetProperty(
                        f => f.ResolvedAtUtc,
                        (DateTime?)now));

            // 4. Güncelleme başarısızsa geçmiş kaydı oluşturma
            if (affectedRows == 0)
            {
                await transaction.RollbackAsync();

                TempData["ResolveError"] =
                    "Arıza tamamlanamadı. Son ret işleminden " +
                    "sonra yeni müdahale notu eklenmesi " +
                    "gerekebilir. Arızanın durumunu ve " +
                    "sorumlusunu da kontrol edin.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // 5. Başarılı durum değişikliğini geçmişe kaydet
            var history = new FaultStatusHistory
            {
                FaultId = id,
                FromStatus = FaultStatus.InProgress,
                ToStatus = FaultStatus.Resolved,
                ChangedByUserId = userId,
                ChangedAtUtc = now
            };

            _context.FaultStatusHistories.Add(history);

            // 6. Geçmiş kaydını veritabanına yaz
            await _context.SaveChangesAsync();

            // 7. Her iki işlem de başarılıysa onayla
            await transaction.CommitAsync();

            TempData["ResolveSuccess"] =
                "Teknik müdahale tamamlandı. " +
                "Arıza İşletme doğrulamasını bekliyor.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        [HttpPost]
        [Authorize(Roles = "OperationsStaff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            // 1. Giriş yapan İşletme personelini belirle
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var now = DateTime.UtcNow;

            // 2. Durum güncellemesi ve geçmiş kaydı
            // aynı transaction içinde gerçekleşecek
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // 3. Yalnızca doğrulama bekleyen arızayı kapat
            var affectedRows = await _context.Faults
                .Where(f =>
                    f.FaultId == id &&
                    f.Status == FaultStatus.Resolved &&
                    f.AssignedToUserId != null &&
                    f.AssignedToUserId != userId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        f => f.Status,
                        FaultStatus.Closed)
                    .SetProperty(
                        f => f.ClosedAtUtc,
                        (DateTime?)now)
                    .SetProperty(
                        f => f.ClosedByUserId,
                        userId));

            // 4. Güncelleme başarısızsa geçmiş kaydı ekleme
            if (affectedRows == 0)
            {
                await transaction.RollbackAsync();

                TempData["ApprovalError"] =
                    "Arıza onaylanamadı. Kayıt uygun durumda " +
                    "olmayabilir veya kendi teknik müdahalenizi " +
                    "onaylamaya çalışıyor olabilirsiniz.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            // 5. İşletme onayını durum geçmişine ekle
            var history = new FaultStatusHistory
            {
                FaultId = id,
                FromStatus = FaultStatus.Resolved,
                ToStatus = FaultStatus.Closed,
                ChangedByUserId = userId,
                ChangedAtUtc = now
            };

            _context.FaultStatusHistories.Add(history);

            // 6. Geçmiş kaydını kaydet
            await _context.SaveChangesAsync();

            // 7. İki işlem de başarılıysa onayla
            await transaction.CommitAsync();

            TempData["ApprovalSuccess"] =
                "Arıza İşletme tarafından onaylandı ve kapatıldı.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        [HttpGet]
        [Authorize(Roles = "OperationsStaff")]
        public async Task<IActionResult> Reject(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var fault = await _context.Faults
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FaultId == id);

            if (fault == null)
                return NotFound();

            if (fault.Status != FaultStatus.Resolved ||
                fault.AssignedToUserId == null ||
                fault.AssignedToUserId == userId)
            {
                TempData["RejectionError"] =
                    "Bu arıza için ret işlemi yapılamıyor.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            ViewBag.FaultNumber =
                fault.FaultNumber ?? $"Eski #{fault.FaultId}";

            return View(new FaultRejectViewModel
            {
                FaultId = fault.FaultId
            });
        }

        [HttpPost]
        [Authorize(Roles = "OperationsStaff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            FaultRejectViewModel model)
        {
            // 1. Giriş yapan kullanıcı
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            // 2. Arızayı bul
            var fault = await _context.Faults
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    f => f.FaultId == model.FaultId);

            if (fault == null)
            {
                return NotFound();
            }

            // 3. İş kurallarını kontrol et
            if (fault.Status != FaultStatus.Resolved ||
                fault.AssignedToUserId == null ||
                fault.AssignedToUserId == userId)
            {
                TempData["RejectionError"] =
                    "Arıza uygun durumda değil veya " +
                    "ret yetkiniz yok.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = model.FaultId });
            }

            ViewBag.FaultNumber =
                fault.FaultNumber ?? $"Eski #{fault.FaultId}";

            // 4. Ret gerekçesini doğrula
            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                ModelState.AddModelError(
                    nameof(model.Reason),
                    "Ret gerekçesi zorunludur.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var now = DateTime.UtcNow;

            var previousResolvedAt = fault.ResolvedAtUtc;

            // 5. Transaction başlat
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            // 6. Arızayı yeniden InProgress durumuna getir
            var affectedRows = await _context.Faults
                .Where(f =>
                    f.FaultId == model.FaultId &&
                    f.Status == FaultStatus.Resolved &&
                    f.AssignedToUserId != null &&
                    f.AssignedToUserId != userId &&
                    f.ResolvedAtUtc == previousResolvedAt)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        f => f.Status,
                        FaultStatus.InProgress)
                    .SetProperty(
                        f => f.ResolvedAtUtc,
                        (DateTime?)null));

            // 7. Durum değişikliği başarısızsa durdur
            if (affectedRows == 0)
            {
                await transaction.RollbackAsync();

                TempData["RejectionError"] =
                    "Ret işlemi gerçekleştirilemedi. " +
                    "Arızanın durumu değişmiş olabilir.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = model.FaultId });
            }

            // 8. Ret gerekçesini kaydet
            var rejection = new FaultRejection
            {
                FaultId = model.FaultId,
                RejectedByUserId = userId,
                Reason = model.Reason.Trim(),
                RejectedAtUtc = now,
                PreviousResolvedAtUtc = previousResolvedAt
            };

            _context.FaultRejections.Add(rejection);

            // 9. Durum geçmişini kaydet
            var history = new FaultStatusHistory
            {
                FaultId = model.FaultId,
                FromStatus = FaultStatus.Resolved,
                ToStatus = FaultStatus.InProgress,
                ChangedByUserId = userId,
                ChangedAtUtc = now
            };

            _context.FaultStatusHistories.Add(history);

            // 10. Ret ve geçmiş kayıtlarını veritabanına yaz
            await _context.SaveChangesAsync();

            // 11. İşlemi tamamla
            await transaction.CommitAsync();

            TempData["RejectionSuccess"] =
                "Çözüm reddedildi. Arıza yeniden işleme alındı.";

            // 12. Detay sayfasına yönlendir
            return RedirectToAction(
                nameof(Details),
                new { id = model.FaultId });
        }

        [HttpPost]
        [Authorize(Roles = "TechnicalStaff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(
            int id,
            int toWorkTeamId,
            string? reason)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Challenge();

            if (!currentUser.IsActive)
                return Forbid();

            if (string.IsNullOrWhiteSpace(reason) ||
                reason.Trim().Length > 500)
            {
                TempData["TransferError"] =
                    "Aktarım gerekçesi 1-500 karakter arasında olmalıdır.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            var fault = await _context.Faults
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FaultId == id);

            if (fault == null)
                return NotFound();

            if (fault.Status != FaultStatus.InProgress ||
                fault.AssignedToUserId != currentUser.Id)
            {
                return Forbid();
            }

            if (!fault.ResponsibleWorkTeamId.HasValue)
                return BadRequest("Arızanın sorumlu ekibi bulunamadı.");

            int fromWorkTeamId =
                fault.ResponsibleWorkTeamId.Value;

            if (toWorkTeamId <= 0 ||
                toWorkTeamId == fromWorkTeamId)
            {
                TempData["TransferError"] =
                    "Farklı bir hedef teknik ekip seçmelisiniz.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            bool targetTeamExists =
                await _context.WorkTeams
                    .AsNoTracking()
                    .AnyAsync(w =>
                        w.WorkTeamId == toWorkTeamId);

            if (!targetTeamExists)
            {
                TempData["TransferError"] =
                    "Seçilen hedef teknik ekip bulunamadı.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            var now = DateTime.UtcNow;

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            int affectedRows = await _context.Faults
                .Where(f =>
                    f.FaultId == id &&
                    f.Status == FaultStatus.InProgress &&
                    f.AssignedToUserId == currentUser.Id &&
                    f.ResponsibleWorkTeamId == fromWorkTeamId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(
                        f => f.ResponsibleWorkTeamId,
                        toWorkTeamId)
                    .SetProperty(
                        f => f.AssignedToUserId,
                        (string?)null)
                    .SetProperty(
                        f => f.AssignedAtUtc,
                        (DateTime?)null)
                    .SetProperty(
                        f => f.Status,
                        FaultStatus.Open)
                );

            if (affectedRows != 1)
            {
                await transaction.RollbackAsync();

                TempData["TransferError"] =
                    "Arıza başka bir işlem nedeniyle değişmiş olabilir.";

                return RedirectToAction(nameof(Index));
            }

            _context.FaultTransferHistories.Add(
                new FaultTransferHistory
                {
                    FaultId = id,
                    FromWorkTeamId = fromWorkTeamId,
                    ToWorkTeamId = toWorkTeamId,
                    TransferredByUserId = currentUser.Id,
                    Reason = reason.Trim(),
                    TransferredAtUtc = now
                });

            _context.FaultStatusHistories.Add(
                new FaultStatusHistory
                {
                    FaultId = id,
                    FromStatus = FaultStatus.InProgress,
                    ToStatus = FaultStatus.Open,
                    ChangedByUserId = currentUser.Id,
                    ChangedAtUtc = now
                });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["TransferSuccess"] =
                "Arıza yeni teknik ekibe aktarıldı.";
            return RedirectToAction(nameof(Index));
        }
    }
}