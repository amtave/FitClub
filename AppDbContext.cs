using Microsoft.EntityFrameworkCore;
using FitClub.Models;
using Npgsql;
using System.Linq;
using System;

namespace FitClub
{
    public class AppDbContext : DbContext
    {
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Models.Client> Clients { get; set; }
        public DbSet<Models.Trainer> Trainers { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }
        public DbSet<ClientSubscription> ClientSubscriptions { get; set; }
        public DbSet<IntensityLevel> IntensityLevels { get; set; }
        public DbSet<TrainingType> TrainingTypes { get; set; }
        public DbSet<GroupTraining> GroupTrainings { get; set; }
        public DbSet<TrainingBooking> TrainingBookings { get; set; }
        public DbSet<TrainingSchedule> TrainingSchedules { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<SingleGymVisit> SingleGymVisits { get; set; }
        public DbSet<IndividualTraining> IndividualTrainings { get; set; }
        public DbSet<TariffCategory> TariffCategories { get; set; }
        public DbSet<TrainingTrainer> TrainingTrainers { get; set; }
        public DbSet<TrainingPlanGoal> TrainingPlanGoals { get; set; }
        public DbSet<TrainingPlan> TrainingPlans { get; set; }
        public DbSet<TrainingPlanDay> TrainingPlanDays { get; set; }
        public DbSet<BonusCard> BonusCards { get; set; }
        public DbSet<PaymentCard> PaymentCards { get; set; }
        public DbSet<PassportVerificationRequest> PassportVerificationRequests { get; set; }
        public DbSet<ClubInfo> ClubInfos { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<JobApplicationRating> JobApplicationRatings { get; set; }
        public DbSet<TrainerAbsence> TrainerAbsences { get; set; }
        public DbSet<ClientNotification> ClientNotifications { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=FitClub;Username=postgres;Password=1234");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().ToTable("role");
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Models.Client>().ToTable("client");
            modelBuilder.Entity<Models.Trainer>().ToTable("trainer");
            modelBuilder.Entity<Tariff>().ToTable("tariff");
            modelBuilder.Entity<ClientSubscription>().ToTable("client_subscription");
            modelBuilder.Entity<IntensityLevel>().ToTable("intensity_level");
            modelBuilder.Entity<TrainingType>().ToTable("training_type");
            modelBuilder.Entity<GroupTraining>().ToTable("group_training");
            modelBuilder.Entity<TrainingBooking>().ToTable("training_booking");
            modelBuilder.Entity<TrainingSchedule>().ToTable("training_schedule");
            modelBuilder.Entity<News>().ToTable("news");
            modelBuilder.Entity<Promotion>().ToTable("promotions");
            modelBuilder.Entity<IndividualTraining>().ToTable("individual_training");
            modelBuilder.Entity<TariffCategory>().ToTable("tariff_category");
            modelBuilder.Entity<TrainingTrainer>().ToTable("training_trainers");
            modelBuilder.Entity<SingleGymVisit>().ToTable("single_gym_visit");
            modelBuilder.Entity<TrainingPlanGoal>().ToTable("training_plan_goal");
            modelBuilder.Entity<TrainingPlan>().ToTable("training_plan");
            modelBuilder.Entity<TrainingPlanDay>().ToTable("training_plan_day");
            modelBuilder.Entity<BonusCard>().ToTable("bonus_card");
            modelBuilder.Entity<PaymentCard>().ToTable("payment_card");
            modelBuilder.Entity<PassportVerificationRequest>().ToTable("passport_verification_request");
            modelBuilder.Entity<ClubInfo>().ToTable("club_info");
            modelBuilder.Entity<TrainerAbsence>().ToTable("trainer_absence");

            modelBuilder.Entity<ClientNotification>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
                entity.ToTable("client_notification");
                entity.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.IsRead).HasDefaultValue(false);
            });

            modelBuilder.Entity<JobApplication>(entity =>
            {
                entity.HasKey(e => e.ApplicationId);
                entity.ToTable("job_application");
                entity.HasOne(e => e.Client).WithMany().HasForeignKey(e => e.ClientId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Reviewer).WithMany().HasForeignKey(e => e.ReviewedBy).OnDelete(DeleteBehavior.SetNull);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.Status).HasDefaultValue("pending");
            });

            modelBuilder.Entity<JobApplicationRating>(entity =>
            {
                entity.HasKey(e => e.RatingId);
                entity.ToTable("job_application_ratings");
                entity.HasOne(e => e.JobApplication).WithMany(j => j.Ratings).HasForeignKey(e => e.ApplicationId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TrainingTrainer>().HasKey(tt => new { tt.TrainingId, tt.TrainerId });

            modelBuilder.Entity<TrainingPlan>().HasOne(tp => tp.Client).WithMany().HasForeignKey(tp => tp.ClientId);
            modelBuilder.Entity<TrainingPlan>().HasOne(tp => tp.Trainer).WithMany().HasForeignKey(tp => tp.TrainerId);
            modelBuilder.Entity<TrainingPlan>().HasOne(tp => tp.Goal).WithMany().HasForeignKey(tp => tp.GoalId);
            modelBuilder.Entity<TrainingPlanDay>().HasOne(tpd => tpd.TrainingPlan).WithMany(tp => tp.TrainingPlanDays).HasForeignKey(tpd => tpd.PlanId);

            modelBuilder.Entity<TrainingTrainer>().HasOne(tt => tt.Training).WithMany(t => t.TrainingTrainers).HasForeignKey(tt => tt.TrainingId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TrainingTrainer>().HasOne(tt => tt.Trainer).WithMany().HasForeignKey(tt => tt.TrainerId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>().HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<ClientSubscription>(entity =>
            {
                entity.HasKey(e => e.SubscriptionId);
                entity.HasOne(cs => cs.Client).WithMany().HasForeignKey(cs => cs.ClientId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(cs => cs.Tariff).WithMany().HasForeignKey(cs => cs.TariffId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(cs => cs.SelectedTrainingType).WithMany().HasForeignKey(cs => cs.SelectedTrainingTypeId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(cs => cs.SelectedTrainer).WithMany().HasForeignKey(cs => cs.SelectedTrainerId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<GroupTraining>().HasOne(gt => gt.IntensityLevel).WithMany().HasForeignKey(gt => gt.IntensityId);
            modelBuilder.Entity<GroupTraining>().HasOne(gt => gt.TrainingType).WithMany().HasForeignKey(gt => gt.TypeId);

            modelBuilder.Entity<TrainingSchedule>().HasOne(ts => ts.GroupTraining).WithMany().HasForeignKey(ts => ts.TrainingId);
            modelBuilder.Entity<TrainingSchedule>().HasOne(ts => ts.Trainer).WithMany().HasForeignKey(ts => ts.TrainerId);

            modelBuilder.Entity<TrainingBooking>().HasOne(tb => tb.Client).WithMany().HasForeignKey(tb => tb.ClientId);
            modelBuilder.Entity<TrainingBooking>().HasOne(tb => tb.GroupTraining).WithMany().HasForeignKey(tb => tb.TrainingId);
            modelBuilder.Entity<TrainingBooking>().HasOne(tb => tb.TrainingSchedule).WithMany(ts => ts.TrainingBookings).HasForeignKey(tb => tb.ScheduleId);

            modelBuilder.Entity<IndividualTraining>().HasOne(it => it.Client).WithMany().HasForeignKey(it => it.ClientId);
            modelBuilder.Entity<IndividualTraining>().HasOne(it => it.Trainer).WithMany().HasForeignKey(it => it.TrainerId);

            modelBuilder.Entity<BonusCard>(entity =>
            {
                entity.HasKey(e => e.CardId);
                entity.HasOne(bc => bc.Client).WithMany().HasForeignKey(bc => bc.ClientId).OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.CardNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PointsBalance).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.IssueDate).HasDefaultValueSql("CURRENT_DATE");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<PaymentCard>(entity =>
            {
                entity.HasKey(e => e.CardId);
                entity.HasOne(pc => pc.Client).WithMany().HasForeignKey(pc => pc.ClientId).OnDelete(DeleteBehavior.Cascade);
                entity.Property(e => e.CardNumber).IsRequired().HasMaxLength(19);
                entity.Property(e => e.CardHolderName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CVV).IsRequired().HasMaxLength(3);
                entity.Property(e => e.IsDefault).HasDefaultValue(false);
                entity.Property(e => e.IsVerified).HasDefaultValue(false);
                entity.Property(e => e.VerificationCode).HasMaxLength(6);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<TariffCategory>(entity =>
            {
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Icon).HasMaxLength(50);
            });

            modelBuilder.Entity<Tariff>(entity =>
            {
                entity.HasKey(e => e.TariffId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
                entity.Property(e => e.DurationDays).IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.ImagePath).HasDefaultValue(string.Empty).IsRequired(false);
                entity.HasOne(t => t.Category).WithMany().HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Models.Client>(entity =>
            {
                entity.HasKey(e => e.ClientId);
                entity.HasOne(c => c.Goal).WithMany().HasForeignKey(c => c.GoalId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<PassportVerificationRequest>(entity =>
            {
                entity.HasKey(e => e.RequestId);
                entity.ToTable("passport_verification_request");
            });
        }

        public Models.Client GetClientByEmail(string email)
        {
            return Clients.FirstOrDefault(c => c.Email == email);
        }
    }
}