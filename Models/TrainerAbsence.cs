using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("trainer_absence")]
    public class TrainerAbsence
    {
        [Key]
        [Column("absence_id")]
        public int AbsenceId { get; set; }

        [Column("trainer_id")]
        public int TrainerId { get; set; }

        [Column("absence_date")]
        public DateTime AbsenceDate { get; set; }

        [Column("reason_type")]
        public string ReasonType { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("document_photo_path")]
        public string? DocumentPhotoPath { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("TrainerId")]
        public virtual Trainer Trainer { get; set; }
    }
}