using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FitClub.Trainer.Views
{
    public class TodayWorkoutItem
    {
        public TimeSpan StartTime { get; set; }
        public string TimeFormatted { get; set; }
        public IBrush BorderBrush { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public bool IsGroup { get; set; }
        public bool IsIndividual { get; set; }
        public bool IsSubscription { get; set; }
        public Bitmap ClientAvatar { get; set; }
        public object OriginalItem { get; set; }
    }

    public class ClubNewsItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string DateFormatted { get; set; }
    }

    public partial class HomeView : UserControl
    {
        private Models.Trainer _currentTrainer;

        public HomeView()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using var db = new AppDbContext();
                
                if (UserSession.IsLoggedIn)
                {
                    var user = db.Users.FirstOrDefault(u => u.Email == UserSession.CurrentUserEmail);
                    if (user != null)
                    {
                        _currentTrainer = db.Trainers.FirstOrDefault(t => t.Email == user.Email);
                    }
                }

                if (_currentTrainer == null) return;

                GreetingText.Text = $"Отличный день для новых рекордов, {_currentTrainer.FirstName}! 🏆";
                DateText.Text = $"Сегодня: {DateTime.Today:dd MMMM yyyy, dddd}";

                LoadSchedule(db);
                LoadStatistics(db);
                LoadNews(db);
            }
            catch { }
        }

        private void LoadSchedule(AppDbContext db)
        {
            var today = DateTime.Today;
            var schedule = new List<TodayWorkoutItem>();

            var indTrainings = db.IndividualTrainings
                .AsNoTracking()
                .Include(t => t.Client)
                .Where(t => t.TrainerId == _currentTrainer.TrainerId && t.TrainingDate.Date == today && t.ClientId != null)
                .ToList();

            foreach (var it in indTrainings)
            {
                bool isSub = it.Price == 0 || db.ClientSubscriptions.Any(cs => 
                    cs.ClientId == it.ClientId && 
                    cs.IsActive && 
                    cs.IndividualTrainerId == _currentTrainer.TrainerId);

                schedule.Add(new TodayWorkoutItem
                {
                    StartTime = it.StartTime,
                    TimeFormatted = $"{it.StartTime:hh\\:mm} - {it.EndTime:hh\\:mm}",
                    BorderBrush = Brush.Parse("#9B59B6"),
                    Title = it.Client?.FullName ?? "Неизвестный клиент",
                    Subtitle = $"{it.Client?.Phone ?? "-"}   {it.Client?.Email ?? "-"}",
                    IsGroup = false,
                    IsIndividual = true,
                    IsSubscription = isSub,
                    ClientAvatar = GetClientAvatar(it.Client?.AvatarPath),
                    OriginalItem = it
                });
            }

            var groupTrainings = db.TrainingSchedules
                .AsNoTracking()
                .Include(s => s.GroupTraining)
                .Where(s => s.TrainerId == _currentTrainer.TrainerId && s.TrainingDate.Date == today && s.CurrentParticipants > 0)
                .ToList();

            foreach (var gt in groupTrainings)
            {
                var endTime = gt.TrainingTime.Add(TimeSpan.FromMinutes(gt.GroupTraining?.DurationMinutes ?? 60));
                schedule.Add(new TodayWorkoutItem
                {
                    StartTime = gt.TrainingTime,
                    TimeFormatted = $"{gt.TrainingTime:hh\\:mm} - {endTime:hh\\:mm}",
                    BorderBrush = Brush.Parse("#3498DB"),
                    Title = gt.GroupTraining?.Name ?? "Групповая тренировка",
                    Subtitle = $"{gt.CurrentParticipants}/{gt.MaxParticipants} записано",
                    IsGroup = true,
                    IsIndividual = false,
                    IsSubscription = false,
                    OriginalItem = gt
                });
            }

            var sortedSchedule = schedule.OrderBy(s => s.StartTime).ToList();

            if (sortedSchedule.Any())
            {
                TodayScheduleList.ItemsSource = sortedSchedule;
                TodayScheduleList.IsVisible = true;
                DownloadScheduleBtn.IsVisible = true;
                NoWorkoutsPanel.IsVisible = false;
            }
            else
            {
                TodayScheduleList.IsVisible = false;
                DownloadScheduleBtn.IsVisible = false;
                NoWorkoutsPanel.IsVisible = true;
            }
        }

        private void LoadStatistics(AppDbContext db)
        {
            var now = DateTime.Now;
            var todayDate = now.Date;
            var currentTime = now.TimeOfDay;

            int diff = (7 + (todayDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = todayDate.AddDays(-1 * diff);

            var indThisWeekDb = db.IndividualTrainings
                .AsNoTracking()
                .Where(t => t.TrainerId == _currentTrainer.TrainerId && t.TrainingDate >= startOfWeek && t.TrainingDate <= todayDate && t.ClientId != null)
                .ToList();

            var indThisWeek = indThisWeekDb
                .Where(t => t.TrainingDate < todayDate || (t.TrainingDate == todayDate && t.StartTime <= currentTime))
                .ToList();

            var groupThisWeekDb = db.TrainingSchedules
                .AsNoTracking()
                .Where(s => s.TrainerId == _currentTrainer.TrainerId && s.TrainingDate >= startOfWeek && s.TrainingDate <= todayDate && s.CurrentParticipants > 0)
                .ToList();

            var groupThisWeek = groupThisWeekDb
                .Where(s => s.TrainingDate < todayDate || (s.TrainingDate == todayDate && s.TrainingTime <= currentTime))
                .ToList();

            int totalTrainings = indThisWeek.Count + groupThisWeek.Count;
            decimal totalEarned = 0;

            totalEarned += indThisWeek.Count * 1000m; 

            foreach(var gt in groupThisWeek)
            {
                totalEarned += 800m + (gt.CurrentParticipants * 150m);
            }

            TrainingsCountText.Text = totalTrainings.ToString();
            EarnedText.Text = $"{totalEarned:N0} ₽";
        }

        private void LoadNews(AppDbContext db)
        {
            var newsQuery = db.News
                .Where(n => n.IsActive == true)
                .OrderByDescending(n => n.CreatedAt)
                .Take(3)
                .ToList();

            var newsList = newsQuery.Select(n => new ClubNewsItem {
                Title = n.Title,
                Description = n.Description,
                DateFormatted = n.CreatedAt.ToString("dd.MM.yyyy")
            }).ToList();

            if (newsList.Any())
            {
                NewsItemsControl.ItemsSource = newsList;
                NewsItemsControl.IsVisible = true;
                NoNewsText.IsVisible = false;
            }
            else
            {
                NewsItemsControl.IsVisible = false;
                NoNewsText.IsVisible = true;
            }
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
            return null;
        }

        private async void OpenGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TodayWorkoutItem item && item.OriginalItem is TrainingSchedule schedule)
            {
                var detailsWindow = new GroupTrainingDetailsWindow(schedule);
                await detailsWindow.ShowDialog((Window)this.VisualRoot);
            }
        }

        private async void OpenPlan_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TodayWorkoutItem item && item.OriginalItem is IndividualTraining it && it.Client != null)
            {
                using var db = new AppDbContext();
                var plan = db.TrainingPlans.FirstOrDefault(tp => tp.ClientId == it.Client.ClientId && tp.IsActive);

                if (plan != null)
                {
                    var viewWindow = new ViewTrainingPlanWindow(plan);
                    await viewWindow.ShowDialog((Window)this.VisualRoot);
                }
                else
                {
                    var createWindow = new CreateTrainingPlanWindow(_currentTrainer, it.Client);
                    await createWindow.ShowDialog((Window)this.VisualRoot);
                }
            }
        }

        private async void DownloadSchedule_Click(object sender, RoutedEventArgs e)
        {
            var items = TodayScheduleList.ItemsSource as List<TodayWorkoutItem>;
            if (items == null || !items.Any()) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить расписание",
                SuggestedFileName = $"Расписание_на_сегодня_{DateTime.Today:yyyyMMdd}.txt",
                DefaultExtension = "txt",
                FileTypeChoices = new[] { new FilePickerFileType("Текстовый файл") { Patterns = new[] { "*.txt" } } }
            });

            if (file != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("================================================");
                sb.AppendLine($" РАСПИСАНИЕ ТРЕНИРОВОК");
                sb.AppendLine($" ТРЕНЕР: {_currentTrainer?.FullName}");
                sb.AppendLine($" ДАТА: {DateTime.Today:dd.MM.yyyy}");
                sb.AppendLine($" ВСЕГО ЗАНЯТИЙ: {items.Count}");
                sb.AppendLine("================================================");
                sb.AppendLine();

                foreach (var item in items)
                {
                    sb.AppendLine($"ВРЕМЯ: {item.TimeFormatted}");
                    sb.AppendLine($"ТИП: {(item.IsGroup ? "Групповая" : "Индивидуальная")}");
                    sb.AppendLine($"ОПИСАНИЕ: {item.Title}");
                    sb.AppendLine($"ИНФО: {item.Subtitle}");
                    sb.AppendLine("------------------------------------------------");
                }

                await using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteAsync(sb.ToString());

                var box = MessageBoxManager.GetMessageBoxStandard("Успех", "Расписание успешно сохранено!", ButtonEnum.Ok);
                await box.ShowWindowDialogAsync((Window)this.VisualRoot);
            }
        }
    }
}