using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace FitClub.Models
{
    [Table("training_plan")]
    public class TrainingPlan
    {
        [Key]
        [Column("plan_id")]
        public int PlanId { get; set; }

        [Column("client_id")]
        public int ClientId { get; set; }

        [Column("trainer_id")]
        public int TrainerId { get; set; }

        [Column("goal_id")]
        public int GoalId { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("notes")]
        public string Notes { get; set; } = "";

        [ForeignKey("ClientId")]
        public Client Client { get; set; }

        [ForeignKey("TrainerId")]
        public Trainer Trainer { get; set; }

        [ForeignKey("GoalId")]
        public TrainingPlanGoal Goal { get; set; }

        [InverseProperty("TrainingPlan")]
        public List<TrainingPlanDay> TrainingPlanDays { get; set; } = new List<TrainingPlanDay>();

        [NotMapped]
        public int CompletedDays => TrainingPlanDays?.Count(d => d.Exercises?.Count > 0) ?? 0;

        [NotMapped]
        public string StatusText => $"{CompletedDays}/12 дней заполнено";

        [NotMapped]
        public string StatusColor => CompletedDays == 12 ? "#27AE60" : 
                                   CompletedDays > 0 ? "#F39C12" : "#BDC3C7";

        // НОВОЕ СВОЙСТВО: проверяет, есть ли хотя бы один заполненный день
        [NotMapped]
        public bool HasAnyExercises => TrainingPlanDays?.Any(d => d.Exercises?.Count > 0) ?? false;
    }
}