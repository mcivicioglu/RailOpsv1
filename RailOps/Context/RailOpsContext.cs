using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RailOps.Entities;

namespace RailOps.Context
{
    public class RailOpsContext : IdentityDbContext<ApplicationUser>
    {
        public RailOpsContext(
            DbContextOptions<RailOpsContext> options)
            : base(options)
        {
        }

        public DbSet<Fault> Faults { get; set; } = null!;
        public DbSet<FaultYearCounter> FaultYearCounters { get; set; } = null!;
        public DbSet<FaultIntervention> FaultInterventions { get; set; } = null!;
        public DbSet<FaultRejection> FaultRejections { get; set; } = null!;
        public DbSet<FaultStatusHistory> FaultStatusHistories { get; set; } = null!;
        public DbSet<Directorate> Directorates { get; set; } = null!;
        public DbSet<Section> Sections { get; set; } = null!;
        public DbSet<LineGroup> LineGroups { get; set; } = null!;
        public DbSet<Line> Lines { get; set; } = null!;
        public DbSet<WorkTeam> WorkTeams { get; set; } = null!;
        public DbSet<FaultTransferHistory> FaultTransferHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Fault>()
                .HasOne(f => f.CreatedByUser)
                .WithMany()
                .HasForeignKey(f => f.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Fault>()
                .HasOne(f => f.AssignedToUser)
                .WithMany()
                .HasForeignKey(f => f.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Fault>()
                .HasOne(f => f.ClosedByUser)
                .WithMany()
                .HasForeignKey(f => f.ClosedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Fault>()
                .Property(f => f.FaultNumber)
                .HasMaxLength(20);

            builder.Entity<Fault>()
                .HasIndex(f => f.FaultNumber)
                .IsUnique()
                .HasFilter("[FaultNumber] IS NOT NULL");

            builder.Entity<FaultYearCounter>()
                .HasKey(c => c.Year);

            builder.Entity<FaultYearCounter>()
                .Property(c => c.Year)
                .ValueGeneratedNever();

            builder.Entity<FaultIntervention>(entity =>
            {
                entity.HasKey(x => x.FaultInterventionId);

                entity.Property(x => x.Description)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.HasOne(x => x.Fault)
                    .WithMany(f => f.Interventions)
                    .HasForeignKey(x => x.FaultId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.PerformedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.PerformedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FaultRejection>(entity =>
            {
                entity.HasKey(x => x.FaultRejectionId);

                entity.Property(x => x.Reason)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.HasOne(x => x.Fault)
                    .WithMany(f => f.Rejections)
                    .HasForeignKey(x => x.FaultId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.RejectedByUser)
                    .WithMany()
                    .HasForeignKey(x => x.RejectedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FaultStatusHistory>(entity =>
            {
                entity.HasKey(h => h.FaultStatusHistoryId);

                // Arıza ile ilişki
                entity.HasOne(h => h.Fault)
                    .WithMany(f => f.StatusHistory)
                    .HasForeignKey(h => h.FaultId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Değişikliği yapan personel ile ilişki
                entity.HasOne(h => h.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(h => h.ChangedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Arızanın geçmiş kayıtlarını hızlı listelemek için
                entity.HasIndex(h => new
                {
                    h.FaultId,
                    h.ChangedAtUtc
                });
            });

            builder.Entity<ApplicationUser>()
                .Property(u => u.IsActive)
                .HasDefaultValue(true);

            builder.Entity<ApplicationUser>()
                .Property(u => u.MustChangePassword)
                .HasDefaultValue(false);

            builder.Entity<Directorate>(entity =>
            {
                entity.HasKey(x => x.DirectorateId);

                entity.Property(x => x.DirectorateName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(x => x.DirectorateName)
                    .IsUnique();
            });

            builder.Entity<Section>(entity =>
            {
                entity.HasKey(x => x.SectionId);

                entity.Property(x => x.SectionName)
                    .IsRequired()
                    .HasMaxLength(120);

                entity.HasOne(x => x.Directorate)
                    .WithMany(x => x.Sections)
                    .HasForeignKey(x => x.DirectorateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new
                {
                    x.DirectorateId,
                    x.SectionName
                }).IsUnique();
            });

            builder.Entity<LineGroup>(entity =>
            {
                entity.HasKey(x => x.LineGroupId);

                entity.Property(x => x.LineGroupName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(x => x.LineGroupName)
                    .IsUnique();
            });

            builder.Entity<Line>(entity =>
            {
                entity.HasKey(x => x.LineId);

                entity.Property(x => x.LineCode)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.HasIndex(x => x.LineCode)
                    .IsUnique();

                entity.HasOne(x => x.LineGroup)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.LineGroupId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<WorkTeam>(entity =>
            {
                entity.HasKey(x => x.WorkTeamId);

                // Ekip -> Şeflik
                entity.HasOne(x => x.Section)
                    .WithMany(x => x.WorkTeams)
                    .HasForeignKey(x => x.SectionId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Ekip -> Operasyonel hat grubu
                entity.HasOne(x => x.LineGroup)
                    .WithMany(x => x.WorkTeams)
                    .HasForeignKey(x => x.LineGroupId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Aynı şeflik ve hat grubu için
                // ikinci bir ekip oluşturulamasın.
                entity.HasIndex(x => new
                {
                    x.SectionId,
                    x.LineGroupId
                }).IsUnique();
            });

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(x => x.WorkTeam)
                    .WithMany()
                    .HasForeignKey(x => x.WorkTeamId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<WorkTeam>()
                .HasOne(w => w.TeamLeader)
                .WithMany()
                .HasForeignKey(w => w.TeamLeaderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // TEST-13/A-2
            // Fault -> WorkTeam ilişkisi

            builder.Entity<Fault>()
                .HasOne(f => f.ResponsibleWorkTeam)
                .WithMany()
                .HasForeignKey(f => f.ResponsibleWorkTeamId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}