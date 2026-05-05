using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("trainer")]
    public class Trainer
    {
        [Key]
        [Column("trainer_id")]
        public int TrainerId { get; set; }

        [Required]
        [Column("last_name")]
        public string LastName { get; set; }

        [Required]
        [Column("first_name")]
        public string FirstName { get; set; }

        [Required]
        [Column("middle_name")]
        public string MiddleName { get; set; }

        [Column("phone")]
        public string Phone { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("specialization")]
        public string Specialization { get; set; }

        [Column("bio")]
        public string Bio { get; set; }

        [Column("experience_years")]
        public int ExperienceYears { get; set; }

        [Column("achievements")]
        public string Achievements { get; set; }

        [Column("photo_path")]
        public string PhotoPath { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // НОВОЕ ПОЛЕ: График работы из БД
        [Column("work_schedule")]
        public string WorkSchedule { get; set; } = "mon-fri";

        [Column("individual_training_price")]
        public decimal IndividualTrainingPrice { get; set; } = 2000;

        // Вычисляемые свойства
        [NotMapped]
        public string FullName => $"{LastName} {FirstName} {MiddleName}";

        [NotMapped]
        public string ExperienceInfo => ExperienceYears switch
        {
            1 => $"{ExperienceYears} год опыта",
            >= 2 and <= 4 => $"{ExperienceYears} года опыта",
            _ => $"{ExperienceYears} лет опыта"
        };

        [NotMapped]
        public Bitmap PhotoBitmap
        {
            get
            {
                if (string.IsNullOrEmpty(PhotoPath))
                {
                    // Возвращаем заглушку, если фото не указано
                    try
                    {
                        var resourceUri = new Uri($"avares://FitClub/Assets/default_avatar.png");
                        using var stream = AssetLoader.Open(resourceUri);
                        return new Bitmap(stream);
                    }
                    catch
                    {
                        return null;
                    }
                }

                try
                {
                    var imagePath = PhotoPath.StartsWith("Assets/")
                        ? PhotoPath
                        : $"Assets/{PhotoPath}";

                    imagePath = imagePath.TrimStart('/');
                    var resourceUri = new Uri($"avares://FitClub/{imagePath}");
                    using var stream = AssetLoader.Open(resourceUri);
                    return new Bitmap(stream);
                }
                catch (Exception)
                {
                    // В случае ошибки загрузки фото, возвращаем заглушку
                    try
                    {
                        var resourceUri = new Uri($"avares://FitClub/Assets/default_avatar.png");
                        using var stream = AssetLoader.Open(resourceUri);
                        return new Bitmap(stream);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
        }

        [NotMapped]
        public bool IsSelected { get; set; }

        // ИСПРАВЛЕННЫЕ ВЫЧИСЛЯЕМЫЕ СВОЙСТВА - теперь используют данные из БД
        [NotMapped]
        public string WorkScheduleDescription
        {
            get
            {
                return WorkSchedule switch
                {
                    "mon-fri" => "Работает с понедельника по пятницу",
                    "wed-sun" => "Работает со среды по воскресенье",
                    _ => "График не указан"
                };
            }
        }

        // Дополнительное свойство для удобства
        [NotMapped]
        public bool IsMonFriSchedule => WorkSchedule == "mon-fri";

        [NotMapped]
        public bool IsWedSunSchedule => WorkSchedule == "wed-sun";

        // НОВЫЕ СВОЙСТВА: Для отображения статуса в админке
        [NotMapped]
        public string StatusText
        {
            get
            {
                return IsActive ? "Активен" : "Неактивен";
            }
        }

        [NotMapped]
        public string StatusColor
        {
            get
            {
                return IsActive ? "#27AE60" : "#E74C3C";
            }
        }

        // НОВОЕ СВОЙСТВО: Короткая информация для отображения в списках
        [NotMapped]
        public string ShortInfo
        {
            get
            {
                if (!string.IsNullOrEmpty(Specialization))
                    return $"{Specialization} • {ExperienceInfo}";
                return ExperienceInfo;
            }
        }

        [NotMapped]
        public string GroupTrainingsList { get; set; } = "Нет групповых тренировок";
    }
    
}