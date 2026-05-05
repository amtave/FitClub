using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace FitClub.Models
{
    [Table("client")]
    public class Client
    {
        [Key]
        [Column("client_id")]
        public int ClientId { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }

        [Column("first_name")]
        public string FirstName { get; set; }

        [Column("middle_name")]
        public string MiddleName { get; set; }

        [Column("passport_series")]
        public string PassportSeries { get; set; } = "";  // Заполняется ТОЛЬКО после подтверждения

        [Column("passport_number")]
        public string PassportNumber { get; set; } = "";  // Заполняется ТОЛЬКО после подтверждения

        [Column("phone")]
        public string Phone { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("avatar_path")]
        public string AvatarPath { get; set; } = "";

        // Дополнительная информация
        [Column("gender")]
        public string? Gender { get; set; }

        [Column("height_cm")]
        public int? HeightCm { get; set; }

        [Column("weight_kg")]
        public decimal? WeightKg { get; set; }

        [Column("goal_id")]
        public int? GoalId { get; set; }

        [Column("additional_info_updated_at")]
        public DateTime? AdditionalInfoUpdatedAt { get; set; }

        // Навигационные свойства
        [ForeignKey("GoalId")]
        public virtual TrainingPlanGoal Goal { get; set; }

        // Связь с запросами на верификацию
        public virtual ICollection<PassportVerificationRequest> PassportVerificationRequests { get; set; }

        [NotMapped]
        public bool HasPassportData => !string.IsNullOrEmpty(PassportSeries) && !string.IsNullOrEmpty(PassportNumber);

        [NotMapped]
        public bool HasAdditionalInfo => 
            !string.IsNullOrEmpty(Gender) || 
            HeightCm.HasValue || 
            WeightKg.HasValue || 
            GoalId.HasValue;

        [NotMapped]
        public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();

        [NotMapped]
        public string GenderDisplay => Gender switch
        {
            "Мужской" => "Мужской",
            "Женский" => "Женский",
            null => "Не указан",
            "" => "Не указан",
            _ => "Не указан"
        };

        [NotMapped]
        public string GoalDisplay => Goal?.Name ?? "Не указана";

        [NotMapped]
        public string HeightDisplay => HeightCm.HasValue ? $"{HeightCm} см" : "Не указан";

        [NotMapped]
        public string WeightDisplay => WeightKg.HasValue ? $"{WeightKg} кг" : "Не указан";

        [NotMapped]
        public Bitmap AvatarBitmap { get; set; }

        [NotMapped]
        public BonusCard BonusCard { get; set; }
        
        [NotMapped]
        public PassportVerificationRequest PendingRequest { get; set; }
    }
}