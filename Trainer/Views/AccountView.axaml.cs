using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using Avalonia.Platform;
using Avalonia.Media;

namespace FitClub.Trainer.Views
{
    public class TrainerReviewItem
    {
        public string ClientFullName { get; set; }
        public string DateString { get; set; }
        public string Stars { get; set; }
        public string ReviewText { get; set; }
        public Bitmap ClientAvatar { get; set; }
        public string TrainingTypeBadge { get; set; }
    }

    public partial class AccountView : UserControl
    {
        private User _currentUser;
        private Models.Trainer _currentTrainer;
        private AppDbContext _context;
        private List<TrainerReviewItem> _allReviews;
        private bool _isAllReviewsShown = false;
        private bool _isEditMode = false;

        public AccountView(User user, Models.Trainer trainer)
        {
            InitializeComponent();
            _currentUser = user;
            _currentTrainer = trainer;
            _context = new AppDbContext();

            LoadTrainerData();
            LoadReviews();
        }

        private void LoadTrainerData()
        {
            if (_currentTrainer != null)
            {
                FullNameText.Text = _currentTrainer.FullName;
                EmailText.Text = _currentTrainer.Email;
                PhoneText.Text = _currentTrainer.Phone ?? "Не указан";
                SpecializationText.Text = _currentTrainer.Specialization ?? "Не указана";
                ExperienceText.Text = _currentTrainer.ExperienceInfo;

                BioText.Text = _currentTrainer.Bio ?? "Биография не заполнена";
                AchievementsText.Text = _currentTrainer.Achievements ?? "Достижения не указаны";

                BioEditBox.Text = _currentTrainer.Bio;
                AchievementsEditBox.Text = _currentTrainer.Achievements;

                LoadPhoto();
            }
        }

        private void LoadReviews()
        {
            if (_currentTrainer == null) return;

            var groupReviews = _context.TrainingBookings
                .AsNoTracking()
                .Include(b => b.Client)
                .Include(b => b.TrainingSchedule)
                .Where(b => b.TrainingSchedule.TrainerId == _currentTrainer.TrainerId && b.Rating != null)
                .Select(b => new { 
                    Client = b.Client, 
                    Date = b.TrainingSchedule.TrainingDate, 
                    Rating = b.Rating.Value, 
                    Review = b.Review,
                    Type = "ГРУППОВАЯ"
                })
                .ToList();

            var indReviews = _context.IndividualTrainings
                .AsNoTracking()
                .Include(i => i.Client)
                .Where(i => i.TrainerId == _currentTrainer.TrainerId && i.Rating != null && i.ClientId != null)
                .Select(i => new { 
                    Client = i.Client, 
                    Date = i.TrainingDate, 
                    Rating = i.Rating.Value, 
                    Review = i.Review,
                    Type = "ИНДИВИДУАЛЬНАЯ"
                })
                .ToList();

            var combinedReviews = groupReviews.Concat(indReviews)
                .OrderByDescending(r => r.Date)
                .ToList();

            if (combinedReviews.Any())
            {
                double avg = combinedReviews.Average(r => r.Rating);
                AverageRatingText.Text = $"Средняя оценка: {avg:F1} ★";
                
                _allReviews = combinedReviews.Select(r => new TrainerReviewItem {
                    ClientFullName = r.Client?.FullName ?? "Аноним",
                    DateString = r.Date.ToString("dd MMMM yyyy"),
                    Stars = new string('★', r.Rating) + new string('☆', 5 - r.Rating),
                    ReviewText = r.Review,
                    ClientAvatar = GetClientAvatar(r.Client?.AvatarPath),
                    TrainingTypeBadge = r.Type
                }).ToList();

                UpdateReviewsDisplay();
                NoReviewsText.IsVisible = false;
            }
            else
            {
                AverageRatingText.Text = "Рейтинг пока не сформирован";
                ReviewsItemsControl.ItemsSource = null;
                ShowMoreBtn.IsVisible = false;
                NoReviewsText.IsVisible = true;
            }
        }

        private void UpdateReviewsDisplay()
        {
            if (_allReviews == null) return;

            if (_isAllReviewsShown || _allReviews.Count <= 3)
            {
                ReviewsItemsControl.ItemsSource = _allReviews;
                ShowMoreBtn.IsVisible = _allReviews.Count > 3;
                ShowMoreBtn.Content = "Скрыть отзывы";
            }
            else
            {
                ReviewsItemsControl.ItemsSource = _allReviews.Take(3).ToList();
                ShowMoreBtn.IsVisible = true;
                ShowMoreBtn.Content = "Показать больше";
            }
        }

        private void ShowMoreReviews_Click(object sender, RoutedEventArgs e)
        {
            _isAllReviewsShown = !_isAllReviewsShown;
            UpdateReviewsDisplay();
        }

        private Bitmap GetClientAvatar(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Avatars", path);
                    if (File.Exists(fullPath))
                    {
                        return new Bitmap(fullPath);
                    }
                }
                catch { }
            }

            try
            {
                var resourceUri = new Uri("avares://FitClub/Assets/default_avatar.png");
                using var stream = AssetLoader.Open(resourceUri);
                return new Bitmap(stream);
            }
            catch 
            { 
                return null; 
            }
        }

        private void LoadPhoto()
        {
            try
            {
                if (_currentTrainer != null && !string.IsNullOrEmpty(_currentTrainer.PhotoPath))
                {
                    PhotoImage.Source = _currentTrainer.PhotoBitmap;
                }
                else
                {
                    LoadDefaultPhoto();
                }
            }
            catch
            {
                LoadDefaultPhoto();
            }
        }

        private void LoadDefaultPhoto()
        {
            try
            {
                var resourceUri = new Uri("avares://FitClub/Assets/default_trainer.png");
                using var stream = AssetLoader.Open(resourceUri);
                PhotoImage.Source = new Bitmap(stream);
            }
            catch
            {
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
                    string fileName = $"trainer_{_currentTrainer.TrainerId}{Path.GetExtension(selectedFile)}";
                    string assetsPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", fileName);

                    File.Copy(selectedFile, assetsPath, true);

                    _currentTrainer.PhotoPath = fileName;
                    _context.Trainers.Update(_currentTrainer);
                    _context.SaveChanges();

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
            var box = MessageBoxManager.GetMessageBoxStandard("Подтверждение", "Вы уверены, что хотите удалить фото профиля?", ButtonEnum.YesNo);
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

        private void EditData_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditMode)
            {
                _isEditMode = true;
                
                BioText.IsVisible = false;
                AchievementsText.IsVisible = false;
                BioEditBox.IsVisible = true;
                AchievementsEditBox.IsVisible = true;

                EditDataBtn.Content = "Сохранить данные";
                EditDataBtn.Background = Brush.Parse("#27AE60");
                EditDataBtn.Foreground = Brushes.White;
                CancelEditBtn.IsVisible = true;
                ChangePasswordBtn.IsVisible = false;
            }
            else
            {
                try
                {
                    _currentTrainer.Bio = BioEditBox.Text;
                    _currentTrainer.Achievements = AchievementsEditBox.Text;
                    
                    _context.Trainers.Update(_currentTrainer);
                    _context.SaveChanges();

                    ResetEditMode();
                    LoadTrainerData();
                }
                catch (Exception ex)
                {
                    ShowError($"Ошибка при сохранении данных: {ex.Message}");
                }
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ResetEditMode();
            BioEditBox.Text = _currentTrainer.Bio;
            AchievementsEditBox.Text = _currentTrainer.Achievements;
        }

        private void ResetEditMode()
        {
            _isEditMode = false;
            
            BioText.IsVisible = true;
            AchievementsText.IsVisible = true;
            BioEditBox.IsVisible = false;
            AchievementsEditBox.IsVisible = false;

            EditDataBtn.Content = "Изменить данные";
            EditDataBtn.Background = Brush.Parse("#EBF5FB");
            EditDataBtn.Foreground = Brush.Parse("#3498DB");
            CancelEditBtn.IsVisible = false;
            ChangePasswordBtn.IsVisible = true;
        }

        private async void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var changePasswordWindow = new TrainerChangePasswordWindow(_currentTrainer);
            await changePasswordWindow.ShowDialog((Window)this.VisualRoot);
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
            LoadReviews();
        }
    }
}