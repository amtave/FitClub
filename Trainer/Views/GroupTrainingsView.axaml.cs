using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FitClub.Trainer.Views
{
    public class TrainerExtendedCalendarDay
    {
        public int Day { get; set; }
        public DateTime Date { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSelected { get; set; }
        public IBrush BorderColor { get; set; } = Brushes.Transparent;
        public Avalonia.Thickness BorderThickness { get; set; } = new Avalonia.Thickness(0);
    }

    public class TrainerScheduleItemViewModel
    {
        public TimeSpan TimeStart { get; set; }
        public string TimeRangeFormatted { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public IBrush BackgroundBrush { get; set; }
        public IBrush BorderBrush { get; set; }
        public object OriginalItem { get; set; }
        public bool IsGroup { get; set; }
    }

    public partial class GroupTrainingsView : UserControl
    {
        private readonly AppDbContext _db;
        private Models.Trainer _currentTrainer;
        private DateTime _currentDisplayMonth;
        private DateTime? _selectedDate;

        public GroupTrainingsView()
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

            LoadTopTrainingInfo();
            UpdateCalendar();
            LoadScheduleForSelectedDate();
        }

        private void LoadTopTrainingInfo()
        {
            if (_currentTrainer == null) return;

            var trainingLink = _db.TrainingTrainers
                .Include(tt => tt.Training)
                .FirstOrDefault(tt => tt.TrainerId == _currentTrainer.TrainerId);

            if (trainingLink != null && trainingLink.Training != null)
            {
                TopTrainingTitle.Text = trainingLink.Training.Name;
                TopTrainingDesc.Text = trainingLink.Training.Description;
                TopTrainingImage.Source = trainingLink.Training.TrainingImage;
                TopTrainingBorder.IsVisible = true;
            }
            else
            {
                TopTrainingBorder.IsVisible = false;
            }
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
            var days = new List<TrainerExtendedCalendarDay>();
            
            int daysInMonth = DateTime.DaysInMonth(_currentDisplayMonth.Year, _currentDisplayMonth.Month);
            var firstDayOfMonth = new DateTime(_currentDisplayMonth.Year, _currentDisplayMonth.Month, 1);
            
            int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            if (firstDayOfWeek == 0) firstDayOfWeek = 7;
            
            for (int i = 1; i < firstDayOfWeek; i++)
            {
                days.Add(new TrainerExtendedCalendarDay { IsEnabled = false });
            }

            if (_currentTrainer != null)
            {
                var groupDays = _db.TrainingSchedules.AsNoTracking()
                    .Where(ts => ts.TrainerId == _currentTrainer.TrainerId && ts.TrainingDate.Year == _currentDisplayMonth.Year && ts.TrainingDate.Month == _currentDisplayMonth.Month)
                    .Select(ts => ts.TrainingDate.Day)
                    .ToList();
                    
                var daysWithSchedules = groupDays.Distinct().ToList();
                
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
                    else if (daysWithSchedules.Contains(i))
                    {
                        thick = new Avalonia.Thickness(3);
                        borderColor = IsDayOffForTrainer(_currentTrainer, date) ? Brush.Parse("#8E44AD") : Brush.Parse("#2ECC71");
                    }

                    days.Add(new TrainerExtendedCalendarDay 
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
            if (sender is Button btn && btn.DataContext is TrainerExtendedCalendarDay day && day.IsEnabled)
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

        private void LoadScheduleForSelectedDate()
        {
            if (_selectedDate == null || _currentTrainer == null) return;

            SelectedDateText.Text = _selectedDate.Value.ToString("dd MMMM yyyy");
            var targetDate = _selectedDate.Value.Date;

            AbsenceInfoBorder.IsVisible = false;
            
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
                            AbsencePhotoPreview.Source = new Avalonia.Media.Imaging.Bitmap(path);
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

            var groupSessions = _db.TrainingSchedules.AsNoTracking()
                .Include(s => s.GroupTraining)
                .Where(s => s.TrainerId == _currentTrainer.TrainerId && s.TrainingDate.Date == targetDate)
                .OrderBy(s => s.TrainingTime)
                .ToList();

            var unifiedList = new List<TrainerScheduleItemViewModel>();

            foreach (var g in groupSessions)
            {
                unifiedList.Add(new TrainerScheduleItemViewModel 
                { 
                    TimeStart = g.TrainingTime, 
                    TimeRangeFormatted = g.TimeRangeFormatted, 
                    Title = $"👥 {g.GroupTraining?.Name}", 
                    Subtitle = g.OccupiedSlotsFormatted, 
                    BackgroundBrush = Brush.Parse("#FDFEFE"), 
                    BorderBrush = Brush.Parse("#3498DB"), 
                    IsGroup = true, 
                    OriginalItem = g 
                });
            }

            ScheduleItemsControl.ItemsSource = unifiedList;
            
            NoSchedulesPanel.IsVisible = !unifiedList.Any() && absence == null;
        }

        private async void OpenSlot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TrainerScheduleItemViewModel item && item.IsGroup)
            {
                var original = (TrainingSchedule)item.OriginalItem;
                var detailsWindow = new GroupTrainingDetailsWindow(original);
                await detailsWindow.ShowDialog((Window)this.VisualRoot);
            }
        }
    }
}