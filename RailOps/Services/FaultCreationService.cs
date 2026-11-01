
using System.Data;
using Microsoft.EntityFrameworkCore;
using RailOps.Context;
using RailOps.Entities;

namespace RailOps.Services
{
    public class FaultCreationService
    {
        private readonly RailOpsContext _context;

        public FaultCreationService(RailOpsContext context)
        {
            _context = context;
        }

        public async Task<int> CreateAsync(Fault fault)
        {
            var createdAt = DateTime.UtcNow;

            // Türkiye saatine göre kayıt yılı
            var timeZoneId = OperatingSystem.IsWindows()
                ? "Turkey Standard Time"
                : "Europe/Istanbul";

            var turkeyTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            var year = TimeZoneInfo
                .ConvertTimeFromUtc(createdAt, turkeyTimeZone)
                .Year;

            // Sayaç ve arızayı aynı transaction'da kaydet
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            var counter = await _context.FaultYearCounters
                .FromSqlInterpolated(
                    $@"SELECT [Year], [LastNumber]
                       FROM [dbo].[FaultYearCounters]
                       WITH (UPDLOCK, HOLDLOCK)
                       WHERE [Year] = {year}")
                .SingleOrDefaultAsync();

            if (counter == null)
            {
                counter = new FaultYearCounter
                {
                    Year = year,
                    LastNumber = 0
                };

                _context.FaultYearCounters.Add(counter);
            }

            counter.LastNumber++;

            // Arıza bilgilerini belirle
            fault.FaultNumber =
                $"ARZ-{year}-{counter.LastNumber:D4}";

            fault.CreatedAt = createdAt;
            fault.Status = FaultStatus.Open;


            if (string.IsNullOrWhiteSpace(fault.CreatedByUserId))
            {
                throw new InvalidOperationException(
                    "Arızayı oluşturan kullanıcı bilgisi bulunamadı.");
            }

            // 1. Arızayı kaydetmek üzere hazırla
            _context.Faults.Add(fault);

            // 2. İlk durum geçmişini oluştur
            var statusHistory = new FaultStatusHistory
            {
                Fault = fault,
                FromStatus = null,
                ToStatus = FaultStatus.Open,
                ChangedByUserId = fault.CreatedByUserId,
                ChangedAtUtc = createdAt
            };

            _context.FaultStatusHistories.Add(statusHistory);

            // 3. Sayaç, arıza ve geçmiş kaydını birlikte kaydet
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return fault.FaultId;

        }
    }
}
