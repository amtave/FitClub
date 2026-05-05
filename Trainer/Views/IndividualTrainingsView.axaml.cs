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
using System.Threading.Tasks;

namespace FitClub.Trainer.Views
{
    public class IndCalendarDay
    {
        public int Day { get; set; }
        public DateTime Date { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSelected { get; set; }
        public IBrush BorderColor { get; set; } = Brushes.Transparent;
        public Avalonia.Thickness BorderThickness { get; set; } = new Avalonia.Thickness(0);
    }

    public class IndScheduleItemViewModel
    {
        public TimeSpan TimeStart { get; set; }
        public string TimeRangeFormatted { get; set; }
        public bool IsFree { get; set; }
        public string ClientFullName { get; set; }
        public string ClientPhone { get; set; }
        public string ClientEmail { get; set; }
        public Bitmap ClientAvatar { get; set; }
        public bool IsSubscription { get; set; }
        public IBrush BackgroundBrush { get; set; }
        public IBrush BorderBrush { get; set; }
        public IndividualTraining OriginalItem { get; set; }
    }

    public partial class IndividualTrainingsView : UserControl
    {
        private readonly AppDbContext _db;
        private Models.Trainer _currentTrainer;
        private DateTime _currentDisplayMonth;
        private DateTime? _selectedDate;

        public IndividualTrainingsView()
        {
            InitializeComponent();
            _db = new AppDbContext();
            _currentDisplayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _selectedDate = DateTime.Today;
            LoadCurrentTrainer();
        }

        public void RefreshView()
        {
            LoadCurrentTrainer();
        }

        private void LoadCurrentTrainer()
        {
            if (UserSession.IsLoggedIn)
            {
                var user = _db.Users.FirstOrDefault(u => u.Email == UserSession.CurrentUserEmail);
                if (user != null)
                {
                    _currentTrainer = _db.Trainers.FirstOrDefault(t => t.Email == user.Email);
                }
            }

            UpdateCalendar();
            LoadScheduleForSelectedDate();
        }

        private bool IsDayOffForTrainer(Models.Trainer t, DateTime d)
        {
            if (t == null) return false;
            int dw = (int)d.DayOfWeek;
            
            if (t.WorkSchedule == "mon-fri")
            {
                return dw == 6 || dw == 0;
            }
            if (t.WorkSchedule == "wed-sun")
            {
                return dw == 1 || dw == 2;
            }
            return false;
        }

        private void UpdateCalendar()
        {
            MonthYearText.Text = _currentDisplayMonth.ToString("MMMM yyyy");
            var days = new List<IndCalendarDay>();
            
            int daysInMonth = DateTime.DaysInMonth(_currentDisplayMonth.Year, _currentDisplayMonth.Month);
            var firstDayOfMonth = new DateTime(_currentDisplayMonth.Year, _currentDisplayMonth.Month, 1);
            
            int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            if (firstDayOfWeek == 0) firstDayOfWeek = 7;
            
            for (int i = 1; i < firstDayOfWeek; i++)
            {
                days.Add(new IndCalendarDay { IsEnabled = false });
            }

            if (_currentTrainer != null)
            {
                var indDays = _db.IndividualTrainings.AsNoTracking()
                    .Where(it => it.TrainerId == _currentTrainer.TrainerId && it.TrainingDate.Year == _currentDisplayMonth.Year && it.TrainingDate.Month == _currentDisplayMonth.Month)
                    .Select(it => it.TrainingDate.Day)
                    .Distinct()
                    .ToList();
                
                var absences = _db.TrainerAbsences.AsNoTracking()
                    .Where(a => a.TrainerId == _currentTrainer.TrainerId && a.AbsenceDate.Year == _currentDisplayMonth.Year && a.AbsenceDate.Month == _currentDisplayMonth.Month)
                    .ToList();

                for (int i = 1; i <= daysInMonth; i++)
                {
                    var date = new DateTime(_currentDisplayMonth.Year, _currentDisplayMonth.Month, i);
                    bool isSelected = _selectedDate.HasValue && _selectedDate.Value.Date == date.Date;
                    
                    IBrush borderColor = Brushes.Transparent;
                    Avalonia.Thickness thick = new Avalonia.Thickness(0);

                    var absence = absences.FirstOrDefault(a => a.AbsenceDate.Day == i);
                    
                    if (absence != null)
                    {
                        thick = new Avalonia.Thickness(3);
                        borderColor = absence.ReasonType == "truancy" ? Brush.Parse("#E74C3C") : Brush.Parse("#F39C12");
                    }
                    else if (indDays.Contains(i))
                    {
                        thick = new Avalonia.Thickness(3);
                        borderColor = IsDayOffForTrainer(_currentTrainer, date) ? Brush.Parse("#8E44AD") : Brush.Parse("#2ECC71");
                    }

                    days.Add(new IndCalendarDay 
                    { 
                        Day = i, 
                        Date = date, 
                        IsEnabled = true, 
                        IsSelected = isSelected, 
                        BorderColor = borderColor, 
                        BorderThickness = thick 
                    });
                }
            }
            
            CalendarItemsControl.ItemsSource = days;
        }

        private void CalendarDay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is IndCalendarDay day && day.IsEnabled)
            {
                _selectedDate = day.Date;
                UpdateCalendar(); 
                LoadScheduleForSelectedDate();
            }
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e) 
        { 
            _currentDisplayMonth = _currentDisplayMonth.AddMonths(-1); 
            UpdateCalendar(); 
        }
        
        private void NextMonth_Click(object sender, RoutedEventArgs e) 
        { 
            _currentDisplayMonth = _currentDisplayMonth.AddMonths(1); 
            UpdateCalendar(); 
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

        private void LoadScheduleForSelectedDate()
        {
            if (_selectedDate == null || _currentTrainer == null) return;

            SelectedDateText.Text = _selectedDate.Value.ToString("dd MMMM yyyy");
            var targetDate = _selectedDate.Value.Date;

            AbsenceInfoBorder.IsVisible = false;
            DownloadScheduleBtn.IsVisible = false;
            
            var absence = _db.TrainerAbsences.FirstOrDefault(a => a.TrainerId == _currentTrainer.TrainerId && a.AbsenceDate == targetDate);
            if (absence != null)
            {
                AbsenceInfoBorder.IsVisible = true;
                AbsenceDateRangeText.IsVisible = false;

                if (absence.ReasonType == "truancy") 
                { 
                    AbsenceInfoBorder.BorderBrush = Brush.Parse("#E74C3C"); 
                    AbsenceInfoBorder.Background = Brush.Parse("#FDEDEC"); 
                    AbsenceTitleText.Text = "🚫 ПРОГУЛ / НЕУВАЖИТЕЛЬНАЯ ПРИЧИНА"; 
                    AbsenceTitleText.Foreground = Brush.Parse("#E74C3C"); 
                    AbsencePhotoBorder.BorderBrush = Brush.Parse("#E74C3C");
                }
                else if (absence.ReasonType == "sick") 
                { 
                    AbsenceTitleText.Text = "💊 БОЛЬНИЧНЫЙ ЛИСТ"; 
                    AbsenceTitleText.Foreground = Brush.Parse("#F39C12"); 
                    AbsenceInfoBorder.BorderBrush = Brush.Parse("#F39C12"); 
                    AbsenceInfoBorder.Background = Brush.Parse("#FFF3E0"); 
                    AbsencePhotoBorder.BorderBrush = Brush.Parse("#F39C12");

                    var sickDays = _db.TrainerAbsences
                        .Where(a => a.TrainerId == _currentTrainer.TrainerId && a.ReasonType == "sick")
                        .Select(a => a.AbsenceDate)
                        .ToList();
                        
                    DateTime sDate = targetDate;
                    while (sickDays.Contains(sDate.AddDays(-1)))
                    {
                        sDate = sDate.AddDays(-1);
                    }
                    
                    DateTime eDate = targetDate;
                    while (sickDays.Contains(eDate.AddDays(1)))
                    {
                        eDate = eDate.AddDays(1);
                    }

                    if (sDate != eDate)
                    {
                        AbsenceDateRangeText.Text = $"Период больничного: с {sDate:dd.MM.yyyy} по {eDate:dd.MM.yyyy}";
                        AbsenceDateRangeText.IsVisible = true;
                    }
                }
                else 
                { 
                    AbsenceTitleText.Text = "📝 ЗАЯВЛЕНИЕ / УВАЖИТЕЛЬНАЯ ПРИЧИНА"; 
                    AbsenceTitleText.Foreground = Brush.Parse("#F39C12"); 
                    AbsenceInfoBorder.BorderBrush = Brush.Parse("#F39C12"); 
                    AbsenceInfoBorder.Background = Brush.Parse("#FFF3E0"); 
                    AbsencePhotoBorder.BorderBrush = Brush.Parse("#F39C12");
                }
                
                AbsenceCommentText.Text = string.IsNullOrEmpty(absence.Description) ? "Без комментария" : absence.Description;

                if (!string.IsNullOrEmpty(absence.DocumentPhotoPath))
                {
                    try
                    {
                        string path = Path.Combine(Directory.GetCurrentDirectory(), "AbsenceDocs", absence.DocumentPhotoPath);
                        if (File.Exists(path))
                        {
                            AbsencePhotoPreview.Source = new Bitmap(path);
                            AbsencePhotoBorder.IsVisible = true;
                        }
                        else
                        {
                            AbsencePhotoBorder.IsVisible = false;
                        }
                    }
                    catch
                    {
                        AbsencePhotoBorder.IsVisible = false;
                    }
                }
                else
                {
                    AbsencePhotoBorder.IsVisible = false;
                }
            }

            var indSessions = _db.IndividualTrainings.AsNoTracking()
                .Include(i => i.Client)
                .Where(i => i.TrainerId == _currentTrainer.TrainerId && i.TrainingDate.Date == targetDate)
                .OrderBy(i => i.StartTime)
                .ToList();

            var unifiedList = new List<IndScheduleItemViewModel>();

            foreach (var i in indSessions)
            {
                bool isFree = i.ClientId == null;
                bool isSub = false;

                if (!isFree)
                {
                    isSub = i.Price == 0 || _db.ClientSubscriptions.Any(cs => 
                        cs.ClientId == i.ClientId && 
                        cs.IsActive && 
                        cs.IndividualTrainerId == _currentTrainer.TrainerId);
                }
                
                unifiedList.Add(new IndScheduleItemViewModel 
                { 
                    TimeStart = i.StartTime, 
                    TimeRangeFormatted = $"{i.StartTime:hh\\:mm} - {i.EndTime:hh\\:mm}", 
                    IsFree = isFree,
                    ClientFullName = i.Client?.FullName ?? "",
                    ClientPhone = i.Client?.Phone ?? "",
                    ClientEmail = i.Client?.Email ?? "",
                    ClientAvatar = isFree ? null : GetClientAvatar(i.Client?.AvatarPath),
                    IsSubscription = isSub,
                    BackgroundBrush = Brush.Parse(isFree ? "#FAFAFA" : "#FDFEFE"), 
                    BorderBrush = Brush.Parse(isFree ? "#2ECC71" : "#9B59B6"), 
                    OriginalItem = i 
                });
            }

            ScheduleItemsControl.ItemsSource = unifiedList;
            
            bool hasItems = unifiedList.Any();
            bool hasBookings = unifiedList.Any(x => !x.IsFree);
            
            NoSchedulesPanel.IsVisible = !hasItems && absence == null;
            DownloadScheduleBtn.IsVisible = hasBookings && absence == null;
        }

        private async void OpenPlanWindow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is IndScheduleItemViewModel item && item.OriginalItem?.Client != null)
            {
                var client = item.OriginalItem.Client;
                var plan = _db.TrainingPlans.FirstOrDefault(tp => tp.ClientId == client.ClientId && tp.IsActive);

                if (plan != null)
                {
                    var viewWindow = new ViewTrainingPlanWindow(plan);
                    await viewWindow.ShowDialog((Window)this.VisualRoot);
                }
                else
                {
                    var createWindow = new CreateTrainingPlanWindow(_currentTrainer, client);
                    await createWindow.ShowDialog((Window)this.VisualRoot);
                }
            }
        }

        private async void DownloadSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDate == null || _currentTrainer == null) return;

            var items = ScheduleItemsControl.ItemsSource as List<IndScheduleItemViewModel>;
            if (items == null) return;

            var bookedItems = items.Where(i => !i.IsFree).ToList();
            if (!bookedItems.Any())
            {
                var noDataBox = MessageBoxManager.GetMessageBoxStandard("Пусто", "Нет записей для скачивания.", ButtonEnum.Ok);
                await noDataBox.ShowWindowDialogAsync((Window)this.VisualRoot);
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить расписание индивидуальных",
                SuggestedFileName = $"Расписание_{_currentTrainer.LastName}_{_selectedDate.Value:yyyyMMdd}.txt",
                DefaultExtension = "txt",
                FileTypeChoices = new[] { new FilePickerFileType("Текстовый файл") { Patterns = new[] { "*.txt" } } }
            });

            if (file != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("================================================");
                sb.AppendLine($" ИНДИВИДУАЛЬНЫЕ ТРЕНИРОВКИ");
                sb.AppendLine($" ТРЕНЕР: {_currentTrainer.FullName}");
                sb.AppendLine($" ДАТА: {_selectedDate.Value:dd.MM.yyyy}");
                sb.AppendLine($" ЗАПИСЕЙ: {bookedItems.Count}");
                sb.AppendLine("================================================");
                sb.AppendLine();

                foreach (var item in bookedItems)
                {
                    sb.AppendLine($"ВРЕМЯ: {item.TimeRangeFormatted}");
                    sb.AppendLine($"КЛИЕНТ: {item.ClientFullName}");
                    sb.AppendLine($"   Телефон: {item.ClientPhone}");
                    sb.AppendLine($"   Email: {item.ClientEmail}");
                    sb.AppendLine($"   Тип оплаты: {(item.IsSubscription ? "По абонементу" : "Разовая оплата")}");
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