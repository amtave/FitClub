using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("training_schedule")]
    public class TrainingSchedule
    {
        [Key]
        [Column("schedule_id")]
        public int ScheduleId { get; set; }

        [Column("training_id")]
        public int TrainingId { get; set; }

        [Column("trainer_id")]
        public int TrainerId { get; set; }

        [Column("training_date")]
        public DateTime TrainingDate { get; set; }

        [Column("training_time")]
        public TimeSpan TrainingTime { get; set; }

        [Column("max_participants")]
        public int MaxParticipants { get; set; }

        [Column("current_participants")]
        public int CurrentParticipants { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [ForeignKey("TrainingId")]
        public GroupTraining GroupTraining { get; set; }

        [ForeignKey("TrainerId")]
        public Trainer Trainer { get; set; }

        [NotMapped]
        public string TimeFormatted => TrainingTime.ToString(@"hh\:mm");

        [NotMapped]
        public string TimeRangeFormatted
        {
            get
            {
                int duration = GroupTraining?.DurationMinutes ?? 60;
                var endTime = TrainingTime.Add(TimeSpan.FromMinutes(duration));
                return $"{TrainingTime:hh\\:mm} - {endTime:hh\\:mm}";
            }
        }

        [NotMapped]
        public string OccupiedSlotsFormatted => $"{CurrentParticipants}/{MaxParticipants} записано";

        [NotMapped]
        public string DateFormatted => TrainingDate.ToString("dd.MM.yyyy");

        public virtual ICollection<TrainingBooking> TrainingBookings { get; set; } = new List<TrainingBooking>();
    }
}