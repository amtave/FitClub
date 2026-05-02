using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.IO;
using System.Threading.Tasks;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using Avalonia.Platform;

namespace FitClub.Trainer.Views
{
    public partial class AccountView : UserControl
    {
        private User _currentUser;
        private Models.Trainer _currentTrainer;
        private AppDbContext _context;

        public AccountView(User user, Models.Trainer trainer)
        {
            InitializeComponent();
            _currentUser = user;
            _currentTrainer = trainer;
            _context = new AppDbContext();

            LoadTrainerData();
        }

        private void LoadTrainerData()
        {
            if (_currentTrainer != null)
            {
                // Основная информация
                FullNameText.Text = _currentTrainer.FullName;
                EmailText.Text = _currentTrainer.Email;
                PhoneText.Text = _currentTrainer.Phone ?? "Не указан";
                SpecializationText.Text = _currentTrainer.Specialization ?? "Не указана";
                ExperienceText.Text = _currentTrainer.ExperienceInfo;

                // Дополнительная информация
                BioText.Text = _currentTrainer.Bio ?? "Биография не заполнена";
                AchievementsText.Text = _currentTrainer.Achievements ?? "Достижения не указаны";

                // Загружаем фото
                LoadPhoto();
            }
            else
            {
                FullNameText.Text = "Информация не загружена";
                EmailText.Text = "Информация не загружена";
            }
        }

        private void LoadPhoto()
        {
            try
            {
                if (_currentTrainer != null && !string.IsNullOrEmpty(_currentTrainer.PhotoPath))
                {
                    // Используем PhotoBitmap из модели Trainer
                    PhotoImage.Source = _currentTrainer.PhotoBitmap;
                }
                else
                {
                    LoadDefaultPhoto();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки фото: {ex.Message}");
                LoadDefaultPhoto();
            }
        }

        private void LoadDefaultPhoto()
        {
            try
            {
                // Пробуем загрузить стандартное фото тренера
                var resourceUri = new Uri("avares://FitClub/Assets/default_trainer.png");
                using var stream = AssetLoader.Open(resourceUri);
                PhotoImage.Source = new Bitmap(stream);
            }
            catch (Exception)
            {
                // Если стандартное фото не найдено, оставляем пустым
                PhotoImage.Source = null;
            }
        }

        private async void ChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog();
                dialog.Title = "Выберите фото для профиля";
                dialog.Filters.Add(new FileDialogFilter
                {
                    Name = "Изображения",
                    Extensions = { "jpg", "jpeg", "png", "bmp" }
                });
                dialog.AllowMultiple = false;

                var window = (Window)this.VisualRoot;
                var result = await dialog.ShowAsync(window);

                if (result != null && result.Length > 0)
                {
                    string selectedFile = result[0];

                    // Копируем файл в папку Assets
                    string fileName = $"trainer_{_currentTrainer.TrainerId}{Path.GetExtension(selectedFile)}";
                    string assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", fileName);

                    File.Copy(selectedFile, assetsPath, true);

                    // Обновляем путь в базе данных
                    _currentTrainer.PhotoPath = fileName;
                    _context.Trainers.Update(_currentTrainer);
                    _context.SaveChanges();

                    // Обновляем отображение
                    LoadPhoto();

                    await ShowMessage("Успех", "Фото профиля успешно обновлено!");
                }
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка при изменении фото: {ex.Message}");
            }
        }

        private async void RemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Подтверждение",
                "Вы уверены, что хотите удалить фото профиля?",
                ButtonEnum.YesNo);

            var result = await box.ShowWindowDialogAsync((Window)this.VisualRoot);

            if (result == ButtonResult.Yes)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_currentTrainer.PhotoPath))
                    {
                        string photoPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", _currentTrainer.PhotoPath);
                        if (File.Exists(photoPath))
                        {
                            File.Delete(photoPath);
                        }
                    }

                    _currentTrainer.PhotoPath = null;
                    _context.Trainers.Update(_currentTrainer);
                    _context.SaveChanges();

                    LoadPhoto();

                    await ShowMessage("Успех", "Фото профиля успешно удалено!");
                }
                catch (Exception ex)
                {
                    await ShowError($"Ошибка при удалении фото: {ex.Message}");
                }
            }
        }

        private async void EditData_Click(object sender, RoutedEventArgs e)
        {
            await ShowMessage("Информация", "Функция редактирования данных будет реализована позже");
        }

        private async void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            await ShowMessage("Информация", "Функция смены пароля будет реализована позже");
        }

        private async Task ShowMessage(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok);
            await box.ShowWindowDialogAsync((Window)this.VisualRoot);
        }

        private async Task ShowError(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", message, ButtonEnum.Ok);
            await box.ShowWindowDialogAsync((Window)this.VisualRoot);
        }

        public void RefreshView()
        {
            _context = new AppDbContext();
            if (_currentTrainer != null)
            {
                _currentTrainer = _context.Trainers.Find(_currentTrainer.TrainerId);
            }

            LoadTrainerData();
        }
    }
}