using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace FitClub.Models
{
    [Table("group_training")]
    public class GroupTraining
    {
        [Key]
        [Column("training_id")]
        public int TrainingId { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("duration_minutes")]
        public int DurationMinutes { get; set; }

        [Column("intensity_id")]
        public int IntensityId { get; set; }

        [Column("type_id")]
        public int TypeId { get; set; }

        [Column("price")]
        public decimal Price { get; set; }

        [Column("max_participants")]
        public int MaxParticipants { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("image_path")]
        public string ImagePath { get; set; }

        [ForeignKey("IntensityId")]
        public IntensityLevel IntensityLevel { get; set; }

        [ForeignKey("TypeId")]
        public TrainingType TrainingType { get; set; }

        public virtual ICollection<TrainingTrainer> TrainingTrainers { get; set; } = new List<TrainingTrainer>();

        [NotMapped]
        public string DurationFormatted => $"{DurationMinutes} мин";

        [NotMapped]
        public string PriceFormatted => $"{Price:0} ₽";

        [NotMapped]
        public string ActiveStatusText => IsActive ? "Активна" : "Скрыта";

        [NotMapped]
        public IBrush ActiveStatusColor => IsActive ? Brush.Parse("#27AE60") : Brush.Parse("#E74C3C");

        [NotMapped]
        public IBrush TypeColor
        {
            get
            {
                return TrainingType?.Name switch
                {
                    "Кардио" => Brush.Parse("#E74C3C"),
                    "Силовая" => Brush.Parse("#34495E"),
                    "Йога" => Brush.Parse("#9B59B6"),
                    "Функциональная" => Brush.Parse("#F39C12"),
                    "Танцевальная" => Brush.Parse("#E67E22"),
                    "Единоборства" => Brush.Parse("#C0392B"),
                    _ => Brush.Parse("#95A5A6")
                };
            }
        }

        [NotMapped]
        public Bitmap TrainingImage
        {
            get
            {
                if (string.IsNullOrEmpty(ImagePath))
                    return null;

                try
                {
                    var imagePath = ImagePath.StartsWith("Assets/")
                        ? ImagePath
                        : $"Assets/{ImagePath}";

                    imagePath = imagePath.TrimStart('/');
                    var resourceUri = new Uri($"avares://FitClub/{imagePath}");
                    using var stream = AssetLoader.Open(resourceUri);
                    return new Bitmap(stream);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        [NotMapped]
        public string TrainerFullName => "Несколько тренеров";

        [NotMapped]
        public List<Trainer> AvailableTrainers { get; set; } = new List<Trainer>();

        [NotMapped]
        public Trainer SelectedTrainer { get; set; }

        [NotMapped]
        public string AssignedTrainersText => TrainingTrainers != null && TrainingTrainers.Any()
            ? string.Join(", ", TrainingTrainers.Select(tt => tt.Trainer != null ? tt.Trainer.LastName : ""))
            : "Тренер не назначен";

        [NotMapped]
        public IBrush TrainerStatusColor => TrainingTrainers != null && TrainingTrainers.Any()
            ? Brush.Parse("#3498DB")
            : Brush.Parse("#E74C3C");
    }
}