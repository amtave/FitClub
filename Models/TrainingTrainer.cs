using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("training_trainers")]
    public class TrainingTrainer
    {
        [Column("training_id")]
        public int TrainingId { get; set; }

        [Column("trainer_id")]
        public int TrainerId { get; set; }

        [Column("is_primary")]
        public bool IsPrimary { get; set; }

        // Навигационные свойства
        [ForeignKey("TrainingId")]
        public virtual GroupTraining Training { get; set; }

        [ForeignKey("TrainerId")]
        public virtual Trainer Trainer { get; set; }
    }
}