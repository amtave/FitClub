using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("promotions")]
    public class Promotion
    {
        [Key]
        [Column("promotion_id")]
        public int PromotionId { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("discount_percent")]
        public int? DiscountPercent { get; set; }

        [Column("valid_until")]
        public DateTime? ValidUntil { get; set; }

        [Column("image_path")]
        public string ImagePath { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [NotMapped]
        public string ValidUntilFormatted => ValidUntil?.ToString("dd.MM.yyyy") ?? "Без ограничений";

        [NotMapped]
        public string DiscountInfo => DiscountPercent.HasValue ? $"-{DiscountPercent}%" : "Спецпредложение";

        [NotMapped]
        public Bitmap PromotionImage
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
    }
}