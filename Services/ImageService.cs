using System;
using System.IO;
using System.Reflection;

namespace FitClub.Services
{
    public static class ImageService
    {
        public static string GetTrainerPhotoPath(string photoFileName)
        {
            if (string.IsNullOrEmpty(photoFileName))
            {
                return GetDefaultPhotoPath();
            }

            // Пробуем разные варианты путей
            var paths = new[]
            {
                // 1. Прямой путь к файлу
                photoFileName,
                
                // 2. Относительный путь из Assets
                Path.Combine("Assets", photoFileName),
                
                // 3. Полный путь от текущей директории
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", photoFileName),
                
                // 4. Путь от базовой директории приложения
                Path.Combine(AppContext.BaseDirectory, "Assets", photoFileName),
                
                // 5. Embedded resource
                $"avares://FitClub/Assets/{photoFileName}"
            };

            foreach (var path in paths)
            {
                if (IsValidImagePath(path))
                {
                    Console.WriteLine($"Фото найдено: {path}");
                    return path;
                }
            }

            Console.WriteLine($"Фото {photoFileName} не найдено, используем стандартное");
            return GetDefaultPhotoPath();
        }

        private static bool IsValidImagePath(string path)
        {
            try
            {
                if (path.StartsWith("avares://"))
                {
                    // Для URI проверяем существование через AssetLoader
                    var assets = Avalonia.Platform.AssetLoader.GetAssets(new Uri("avares://FitClub/Assets/"), null);
                    foreach (var asset in assets)
                    {
                        if (asset.AbsolutePath.Contains(Path.GetFileName(path)))
                            return true;
                    }
                    return false;
                }
                
                return File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        private static string GetDefaultPhotoPath()
        {
            // Создаем простой вариант - используем иконку вместо фото
            return "avares://FitClub/Assets/default_trainer.png";
        }

        // Метод для создания placeholder если фото совсем нет
        public static string CreatePlaceholderPath()
        {
            return "avares://FitClub/Assets/default_trainer.png";
        }
    }
}