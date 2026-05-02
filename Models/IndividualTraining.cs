using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("individual_training")]
    public class IndividualTraining
    {
        [Key]
        [Column("individual_training_id")]
        public int IndividualTrainingId { get; set; }

        [Column("client_id")]
        public int? ClientId { get; set; }

        [Column("trainer_id")]
        public int TrainerId { get; set; }

        [Column("training_date")]
        public DateTime TrainingDate { get; set; }

        [Column("start_time")]
        public TimeSpan StartTime { get; set; }

        [Column("end_time")]
        public TimeSpan EndTime { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("price")]
        public decimal Price { get; set; } = 2000;

        [Column("rating")]
        public int? Rating { get; set; }

        [Column("review")]
        public string? Review { get; set; }

        [ForeignKey("ClientId")]
        public Client Client { get; set; }

        [ForeignKey("TrainerId")]
        public Trainer Trainer { get; set; }

        [NotMapped]
        public string StartTimeFormatted => StartTime.ToString(@"hh\:mm");

        [NotMapped]
        public string EndTimeFormatted => EndTime.ToString(@"hh\:mm");

        [NotMapped]
        public string TimeRangeFormatted => $"{StartTimeFormatted} - {EndTimeFormatted}";

        [NotMapped]
        public string DateFormatted => TrainingDate.ToString("dd.MM.yyyy");

        [NotMapped]
        public string PriceFormatted => $"{Price:N0} ₽";

        [NotMapped]
        public string ClientFullName => Client != null ? $"{Client.LastName} {Client.FirstName} {Client.MiddleName}" : "Свободное окно";

        [NotMapped]
        public string ClientPhone => Client?.Phone ?? "-";

        [NotMapped]
        public string ClientAvatarPath => Client?.AvatarPath ?? "Assets/default_avatar.png";

        [NotMapped]
        public bool HasClient => Client != null;
    }
}