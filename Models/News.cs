using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace FitClub.Models
{
    [Table("news")]
    public class News
    {
        [Key]
        [Column("news_id")]
        public int NewsId { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("image_path")]
        public string ImagePath { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [NotMapped]
        public string CreatedAtFormatted => CreatedAt.ToString("dd.MM.yyyy");

        [NotMapped]
        public Bitmap NewsImage
        {
            get
            {
                if (string.IsNullOrEmpty(ImagePath))
                    return null;

                try
                {
                    // Определяем путь к файлу относительно корня проекта
                    string fullPath;
                    
                    // Если путь уже абсолютный
                    if (Path.IsPathRooted(ImagePath))
                    {
                        fullPath = ImagePath;
                    }
                    // Если путь начинается с Assets/ (старые новости)
                    else if (ImagePath.StartsWith("Assets/"))
                    {
                        fullPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ImagePath);
                    }
                    // Если путь начинается с News/ (новые новости)
                    else if (ImagePath.StartsWith("News/"))
                    {
                        fullPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ImagePath);
                    }
                    // Другой относительный путь
                    else
                    {
                        fullPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ImagePath);
                    }
                    
                    fullPath = Path.GetFullPath(fullPath);
                    
                    // Проверяем существование файла
                    if (File.Exists(fullPath))
                    {
                        return new Bitmap(fullPath);
                    }
                    
                    // Если файл не найден, пробуем альтернативные пути
                    string fileName = Path.GetFileName(ImagePath);
                    string[] possiblePaths = new[]
                    {
                        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", fileName),
                        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "News", fileName),
                        Path.Combine(AppContext.BaseDirectory, "Assets", fileName),
                        Path.Combine(AppContext.BaseDirectory, "News", fileName)
                    };
                    
                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            return new Bitmap(path);
                        }
                    }
                    
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки изображения новости: {ex.Message}");
                    return null;
                }
            }
        }
    }
}