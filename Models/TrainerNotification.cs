using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("trainer_notification")]
    public class TrainerNotification
    {
        [Key]
        [Column("notification_id")]
        public int NotificationId { get; set; }

        [Column("trainer_id")]
        public int TrainerId { get; set; }

        [Required]
        [Column("message")]
        public string Message { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("TrainerId")]
        public Trainer Trainer { get; set; }
    }
}