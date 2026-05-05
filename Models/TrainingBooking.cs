using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("training_booking")]
    public class TrainingBooking
    {
        [Key]
        [Column("booking_id")]
        public int BookingId { get; set; }

        [Column("client_id")]
        public int ClientId { get; set; }

        [Column("training_id")]
        public int TrainingId { get; set; }

        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        [Column("booking_date")]
        public DateTime BookingDate { get; set; }

        [Column("status")]
        public string Status { get; set; } = "confirmed";

        [Column("rating")]
        public int? Rating { get; set; }

        [Column("review")]
        public string? Review { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; }

        [ForeignKey("TrainingId")]
        public virtual GroupTraining GroupTraining { get; set; }

        [ForeignKey("ScheduleId")]
        public virtual TrainingSchedule TrainingSchedule { get; set; }
    }
}