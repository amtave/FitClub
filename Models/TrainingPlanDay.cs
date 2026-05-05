using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization; // ДОБАВЬТЕ ЭТУ СТРОКУ

namespace FitClub.Models
{
    [Table("training_plan_day")]
    public class TrainingPlanDay
    {
        [Key]
        [Column("day_id")]
        public int DayId { get; set; }

        [Column("plan_id")]
        public int PlanId { get; set; }

        [Column("day_number")]
        public int DayNumber { get; set; }

        [Column("exercises_json")]
        public string ExercisesJson { get; set; } = "[]";

        [Column("notes")]
        public string Notes { get; set; } = "";

        [ForeignKey("PlanId")]
        public TrainingPlan TrainingPlan { get; set; }

        [NotMapped]
        public List<Exercise> Exercises
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(ExercisesJson) || ExercisesJson == "[]")
                        return new List<Exercise>();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    return JsonSerializer.Deserialize<List<Exercise>>(ExercisesJson, options) ?? new List<Exercise>();
                }
                catch
                {
                    return new List<Exercise>();
                }
            }
            set
            {
                try
                {
                    // УПРОЩЕННАЯ СЕРИАЛИЗАЦИЯ БЕЗ JsonIgnoreCondition
                    ExercisesJson = JsonSerializer.Serialize(value ?? new List<Exercise>());
                }
                catch
                {
                    ExercisesJson = "[]";
                }
            }
        }

        [NotMapped]
        public bool IsCompleted => Exercises?.Count > 0;

        [NotMapped]
        public string BorderColor => IsCompleted ? "#27AE60" : "#BDC3C7";

        [NotMapped]
        public string DayBackground => IsCompleted ? "#27AE60" : "#3498DB";

        [NotMapped]
        public string NumberBackground => IsCompleted ? "#27AE60" : "#3498DB";

        [NotMapped]
        public string DayBackgroundView => IsCompleted ? "#E8F6F3" : "#F8F9FA";

        public void AddExercise(Exercise exercise)
        {
            var exercises = Exercises;
            exercises.Add(exercise);
            Exercises = exercises;
        }

        public void RemoveExercise(Exercise exercise)
        {
            var exercises = Exercises;
            exercises.Remove(exercise);
            Exercises = exercises;
        }
    }
}