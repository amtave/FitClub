using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("tariff")]
    public class Tariff
    {
        [Key]
        [Column("tariff_id")]
        public int TariffId { get; set; }

        [Required]
        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Required]
        [Column("price")]
        public decimal Price { get; set; }

        [Required]
        [Column("duration_days")]
        public int DurationDays { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("image_path")]
        public string ImagePath { get; set; } = string.Empty; // Инициализация по умолчанию

        [ForeignKey("CategoryId")]
        public TariffCategory Category { get; set; }

        [NotMapped]
        public Bitmap TariffImage
        {
            get
            {
                try
                {
                    var defaultImage = CategoryId switch
                    {
                        1 => "Assets/gym_tariff.png",
                        2 => "Assets/group_tariff.png", 
                        3 => "Assets/individual_tariff.png",
                        4 => "Assets/combo_tariff.png",
                        _ => "Assets/default_tariff.png"
                    };

                    var imagePath = !string.IsNullOrWhiteSpace(ImagePath) ? ImagePath : defaultImage;

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
        public string CategoryIconName
        {
            get
            {
                return CategoryId switch
                {
                    1 => "🏋️ Тренажерный зал",
                    2 => "👥 Групповые тренировки",
                    3 => "💪 Индивидуальные тренировки",
                    4 => "🚀 Комбинированные",
                    _ => "📦 Тариф"
                };
            }
        }

        [NotMapped]
        public string CategoryColor
        {
            get
            {
                return CategoryId switch
                {
                    1 => "#3498DB", // Синий - тренажерный зал
                    2 => "#27AE60", // Зеленый - групповые
                    3 => "#F39C12", // Оранжевый - индивидуальные
                    4 => "#9B59B6", // Фиолетовый - комбинированные
                    _ => "#95A5A6"  // Серый - по умолчанию
                };
            }
        }

        [NotMapped]
        public string ActiveStatusText => IsActive ? "Активен" : "Неактивен";

        [NotMapped]
        public string ActiveStatusColor => IsActive ? "#27AE60" : "#E74C3C";

        [NotMapped]
        public string CategoryIcon
        {
            get
            {
                return CategoryId switch
                {
                    1 => "🏋️",
                    2 => "👥",
                    3 => "💪",
                    4 => "🚀",
                    _ => "📦"
                };
            }
        }

        [NotMapped]
        public string ShortDescription
        {
            get
            {
                if (string.IsNullOrEmpty(Description))
                    return "Нет описания";
                
                return Description.Length > 100 
                    ? Description.Substring(0, 97) + "..." 
                    : Description;
            }
        }

        [NotMapped]
        public string FormattedPrice
        {
            get
            {
                return $"{Price:N0} ₽";
            }
        }

        [NotMapped]
        public string CreatedAtFormatted
        {
            get
            {
                return CreatedAt.ToString("dd.MM.yyyy");
            }
        }
    }
}