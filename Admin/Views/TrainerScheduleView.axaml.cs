using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitClub.Admin.Views
{
    public class ExtendedCalendarDay
    {
        public int Day { get; set; }
        public DateTime Date { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSelected { get; set; }
        public IBrush BorderColor { get; set; } = Brushes.Transparent;
        public Avalonia.Thickness BorderThickness { get; set; } = new Avalonia.Thickness(0);
    }

    public class ScheduleItemViewModel
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

    public partial class TrainerScheduleView : UserControl
    {
        private readonly AppDbContext _db;
        private List<Models.Trainer> _trainers;
        private DateTime _currentDisplayMonth;
        private DateTime? _selectedDate;

        public TrainerScheduleView()
        {
            InitializeComponent();
            _db = new AppDbContext();
            _currentDisplayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _selectedDate = DateTime.Today;
            ReportMonthPicker.SelectedIndex = DateTime.Today.Month - 1;
            LoadTrainers();
        }

        private void LoadTrainers()
        {
            _trainers = _db.Trainers.Where(t => t.IsActive).OrderBy(t => t.LastName).ToList();
            TrainerComboBox.ItemsSource = _trainers;
            
            if (_trainers.Any())
            {
                TrainerComboBox.SelectedIndex = 0;
            }
        }

        private void TrainerChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCalendar();
            LoadScheduleForSelectedDate();
        }

        private bool IsDayOffForTrainer(Models.Trainer t, DateTime d)
        {
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
            var days = new List<ExtendedCalendarDay>();
            
            int daysInMonth = DateTime.DaysInMonth(_currentDisplayMonth.Year, _currentDisplayMonth.Month);
            var firstDayOfMonth = new DateTime(_currentDisplayMonth.Year, _currentDisplayMonth.Month, 1);
            
            int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            if (firstDayOfWeek == 0)
            {
                firstDayOfWeek = 7; 
            }
            
            for (int i = 1; i < firstDayOfWeek; i++)
            {
                days.Add(new ExtendedCalendarDay { IsEnabled = false });
            }

            if (TrainerComboBox.SelectedItem is Models.Trainer trainer)
            {
                var groupDays = _db.TrainingSchedules.AsNoTracking()
                    .Where(ts => ts.TrainerId == trainer.TrainerId && ts.TrainingDate.Year == _currentDisplayMonth.Year && ts.TrainingDate.Month == _currentDisplayMonth.Month)
                    .Select(ts => ts.TrainingDate.Day)
                    .ToList();
                    
                var indDays = _db.IndividualTrainings.AsNoTracking()
                    .Where(it => it.TrainerId == trainer.TrainerId && it.TrainingDate.Year == _currentDisplayMonth.Year && it.TrainingDate.Month == _currentDisplayMonth.Month)
                    .Select(it => it.TrainingDate.Day)
                    .ToList();
                    
                var daysWithSchedules = groupDays.Union(indDays).Distinct().ToList();
                
                var absences = _db.TrainerAbsences.AsNoTracking()
                    .Where(a => a.TrainerId == trainer.TrainerId && a.AbsenceDate.Year == _currentDisplayMonth.Year && a.AbsenceDate.Month == _currentDisplayMonth.Month)
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
                        
                        if (absence.ReasonType == "truancy")
                        {
                            borderColor = Brush.Parse("#E74C3C");
                        }
                        else
                        {
                            borderColor = Brush.Parse("#F39C12");
                        }
                    }
                    else if (daysWithSchedules.Contains(i))
                    {
                        thick = new Avalonia.Thickness(3);
                        
                        if (IsDayOffForTrainer(trainer, date))
                        {
                            borderColor = Brush.Parse("#8E44AD");
                        }
                        else
                        {
                            borderColor = Brush.Parse("#2ECC71");
                        }
                    }

                    days.Add(new ExtendedCalendarDay 
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
            if (sender is Button btn && btn.DataContext is ExtendedCalendarDay day && day.IsEnabled)
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
            if (_selectedDate == null || !(TrainerComboBox.SelectedItem is Models.Trainer trainer))
            {
                return;
            }

            SelectedDateText.Text = _selectedDate.Value.ToString("dd MMMM yyyy");
            ActionButtonsPanel.IsVisible = true;
            AbsenceInfoBorder.IsVisible = false;

            var targetDate = _selectedDate.Value.Date;

            var absence = _db.TrainerAbsences.FirstOrDefault(a => a.TrainerId == trainer.TrainerId && a.AbsenceDate == targetDate);
            
            if (absence != null)
            {
                AbsenceInfoBorder.IsVisible = true;
                AddGroupBtn.IsVisible = false;
                AddIndBtn.IsVisible = false;
                DuplicateDayButton.IsVisible = false;
                ClearDayButton.IsVisible = true;
                ClearDayButton.Content = "🗑 Удалить пропуск";
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
                        .Where(a => a.TrainerId == trainer.TrainerId && a.ReasonType == "sick")
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
            else
            {
                AddGroupBtn.IsVisible = true;
                AddIndBtn.IsVisible = true;
                DuplicateDayButton.IsVisible = true;
                ClearDayButton.Content = "🗑 Очистить день (Пропуск)";
            }

            var groupSessions = _db.TrainingSchedules.AsNoTracking()
                .Include(s => s.GroupTraining)
                .Where(s => s.TrainerId == trainer.TrainerId && s.TrainingDate.Date == targetDate)
                .ToList();
                
            var indSessions = _db.IndividualTrainings.AsNoTracking()
                .Include(i => i.Client)
                .Where(i => i.TrainerId == trainer.TrainerId && i.TrainingDate.Date == targetDate)
                .ToList();

            var unifiedList = new List<ScheduleItemViewModel>();

            foreach (var g in groupSessions)
            {
                unifiedList.Add(new ScheduleItemViewModel 
                { 
                    TimeStart = g.TrainingTime, 
                    TimeRangeFormatted = g.TimeRangeFormatted, 
                    Title = $"👥 {g.GroupTraining?.Name}", 
                    Subtitle = g.OccupiedSlotsFormatted, 
                    BackgroundBrush = Brush.Parse("#F8F9FA"), 
                    BorderBrush = Brush.Parse("#3498DB"), 
                    IsGroup = true, 
                    OriginalItem = g 
                });
            }

            foreach (var i in indSessions)
            {
                bool isFree = i.ClientId == null;
                
                unifiedList.Add(new ScheduleItemViewModel 
                { 
                    TimeStart = i.StartTime, 
                    TimeRangeFormatted = $"{i.StartTime:hh\\:mm} - {i.EndTime:hh\\:mm}", 
                    Title = isFree ? "⚪ Свободное окно" : "👤 Индивидуальное занятие", 
                    Subtitle = isFree ? "Доступно для записи" : $"Клиент: {i.Client?.LastName} {i.Client?.FirstName}", 
                    BackgroundBrush = Brush.Parse(isFree ? "#FAFAFA" : "#FDFEFE"), 
                    BorderBrush = Brush.Parse(isFree ? "#2ECC71" : "#9B59B6"), 
                    IsGroup = false, 
                    OriginalItem = i 
                });
            }

            var sortedList = unifiedList.OrderBy(x => x.TimeStart).ToList();
            ScheduleItemsControl.ItemsSource = sortedList;
            
            bool hasItems = sortedList.Any();
            NoSchedulesText.IsVisible = !hasItems && absence == null;
            
            if (absence == null)
            {
                ClearDayButton.IsVisible = hasItems;
            }
        }

        private async void ClearDayClick(object sender, RoutedEventArgs e)
        {
            if (TrainerComboBox.SelectedItem is Models.Trainer trainer && _selectedDate != null)
            {
                var targetDate = _selectedDate.Value.Date;
                var existingAbs = _db.TrainerAbsences.FirstOrDefault(a => a.TrainerId == trainer.TrainerId && a.AbsenceDate == targetDate);

                if (existingAbs != null)
                {
                    var dialog = new Window
                    {
                        Title = "Удаление пропуска", 
                        Width = 300, 
                        Height = 150, 
                        WindowStartupLocation = WindowStartupLocation.CenterOwner, 
                        CanResize = false,
                        Content = new StackPanel 
                        { 
                            Spacing = 20, 
                            Margin = new Avalonia.Thickness(20), 
                            Children = 
                            { 
                                new TextBlock { Text = "Вы уверены, что хотите удалить запись о пропуске?", TextWrapping = TextWrapping.Wrap }, 
                                new StackPanel 
                                { 
                                    Orientation = Avalonia.Layout.Orientation.Horizontal, 
                                    Spacing = 10, 
                                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, 
                                    Children = 
                                    { 
                                        new Button { Content = "Нет", Width = 60, Name = "No" }, 
                                        new Button { Content = "Да", Width = 60, Name = "Yes", Background = Brush.Parse("#E74C3C"), Foreground = Brushes.White } 
                                    } 
                                } 
                            } 
                        }
                    };

                    bool confirm = false;
                    var panel = dialog.Content as StackPanel;
                    var buttons = panel.Children.OfType<StackPanel>().First().Children.OfType<Button>();
                    
                    buttons.First(b => b.Name == "Yes").Click += (s, ev) => { confirm = true; dialog.Close(); };
                    buttons.First(b => b.Name == "No").Click += (s, ev) => { confirm = false; dialog.Close(); };

                    await dialog.ShowDialog(VisualRoot as Window);
                    
                    if (confirm)
                    {
                        _db.TrainerAbsences.Remove(existingAbs);
                        await _db.SaveChangesAsync();
                        LoadScheduleForSelectedDate();
                        UpdateCalendar();
                    }
                }
                else
                {
                    var win = new TrainerAbsenceWindow(_selectedDate.Value);
                    await win.ShowDialog(VisualRoot as Window);

                    if (win.Confirmed)
                    {
                        for (var d = _selectedDate.Value.Date; d <= win.EndDate; d = d.AddDays(1))
                        {
                            var schedulesToDelete = _db.TrainingSchedules
                                .Include(s => s.TrainingBookings)
                                .Include(s => s.GroupTraining)
                                .Where(s => s.TrainerId == trainer.TrainerId && s.TrainingDate.Date == d)
                                .ToList();

                            foreach (var s in schedulesToDelete)
                            {
                                foreach (var b in s.TrainingBookings.ToList())
                                {
                                    var activeSub = _db.ClientSubscriptions.FirstOrDefault(cs => cs.ClientId == b.ClientId && cs.IsActive && cs.EndDate >= DateTime.Today);
                                    if (activeSub != null && activeSub.GroupRemainingVisits.HasValue)
                                    {
                                        activeSub.GroupRemainingVisits++;
                                        _db.ClientSubscriptions.Update(activeSub);
                                    }

                                    _db.ClientNotifications.Add(new ClientNotification
                                    {
                                        ClientId = b.ClientId,
                                        Message = $"Ваша групповая тренировка «{s.GroupTraining?.Name}» ({s.TrainingDate:dd.MM.yyyy} в {s.TrainingTime:hh\\:mm}) была отменена по причине изменения графика тренера. Занятие/средства возвращены.",
                                        CreatedAt = DateTime.UtcNow,
                                        IsRead = false
                                    });
                                    _db.TrainingBookings.Remove(b);
                                }
                                _db.TrainingSchedules.Remove(s);
                            }

                            var indsToDelete = _db.IndividualTrainings
                                .Where(s => s.TrainerId == trainer.TrainerId && s.TrainingDate.Date == d)
                                .ToList();

                            foreach (var i in indsToDelete)
                            {
                                if (i.ClientId != null)
                                {
                                    var activeSub = _db.ClientSubscriptions.FirstOrDefault(cs => cs.ClientId == i.ClientId && cs.IsActive && cs.EndDate >= DateTime.Today);
                                    if (activeSub != null && activeSub.IndividualRemainingVisits.HasValue && i.Price == 0)
                                    {
                                        activeSub.IndividualRemainingVisits++;
                                        _db.ClientSubscriptions.Update(activeSub);
                                    }

                                    _db.ClientNotifications.Add(new ClientNotification
                                    {
                                        ClientId = i.ClientId.Value,
                                        Message = $"Ваша индивидуальная тренировка ({i.TrainingDate:dd.MM.yyyy} в {i.StartTime:hh\\:mm}) была отменена по причине изменения графика тренера. Занятие/средства возвращены.",
                                        CreatedAt = DateTime.UtcNow,
                                        IsRead = false
                                    });
                                }
                                _db.IndividualTrainings.Remove(i);
                            }

                            var abs = _db.TrainerAbsences.FirstOrDefault(a => a.TrainerId == trainer.TrainerId && a.AbsenceDate == d);
                            
                            if (abs == null)
                            {
                                _db.TrainerAbsences.Add(new TrainerAbsence 
                                { 
                                    TrainerId = trainer.TrainerId, 
                                    AbsenceDate = d, 
                                    ReasonType = win.ReasonType, 
                                    Description = win.Comment, 
                                    DocumentPhotoPath = win.FinalPhotoPath 
                                });
                            }
                        }

                        await _db.SaveChangesAsync();
                        LoadScheduleForSelectedDate();
                        UpdateCalendar();
                    }
                }
            }
        }

        private async void OpenAddSlots_Click(object sender, RoutedEventArgs e)
        {
            if (TrainerComboBox.SelectedItem is Models.Trainer trainer && _selectedDate != null)
            {
                var targetDate = _selectedDate.Value.Date;
                var groupSessions = _db.TrainingSchedules.Include(ts => ts.GroupTraining).Where(s => s.TrainerId == trainer.TrainerId && s.TrainingDate.Date == targetDate).ToList();
                var indSessions = _db.IndividualTrainings.Where(i => i.TrainerId == trainer.TrainerId && i.TrainingDate.Date == targetDate).ToList();

                var occupiedRanges = new List<(TimeSpan Start, TimeSpan End)>();
                
                foreach (var g in groupSessions)
                {
                    if (g.GroupTraining != null)
                    {
                        occupiedRanges.Add((g.TrainingTime, g.TrainingTime.Add(TimeSpan.FromMinutes(g.GroupTraining.DurationMinutes))));
                    }
                }
                
                foreach (var i in indSessions)
                {
                    occupiedRanges.Add((i.StartTime, i.EndTime));
                }

                var dialog = new AddIndividualSlotsWindow(occupiedRanges);
                var result = await dialog.ShowDialog<bool>(VisualRoot as Window);
                
                if (result)
                {
                    foreach (var time in dialog.SelectedTimes)
                    {
                        bool exists = _db.IndividualTrainings.Any(i => i.TrainerId == trainer.TrainerId && i.TrainingDate.Date == targetDate && i.StartTime == time);
                        
                        if (!exists)
                        {
                            _db.IndividualTrainings.Add(new IndividualTraining
                            {
                                TrainerId = trainer.TrainerId, 
                                TrainingDate = targetDate, 
                                StartTime = time, 
                                EndTime = time.Add(TimeSpan.FromHours(1)),
                                ClientId = null, 
                                IsActive = true, 
                                CreatedAt = DateTime.UtcNow, 
                                Price = trainer.IndividualTrainingPrice
                            });
                        }
                    }
                    
                    await _db.SaveChangesAsync();
                    LoadScheduleForSelectedDate();
                    UpdateCalendar();
                }
            }
        }

        private async void DeleteSessionClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ScheduleItemViewModel item)
            {
                var dialog = new Window
                {
                    Title = "Удаление", 
                    Width = 300, 
                    Height = 150, 
                    WindowStartupLocation = WindowStartupLocation.CenterOwner, 
                    CanResize = false,
                    Content = new StackPanel 
                    { 
                        Spacing = 20, 
                        Margin = new Avalonia.Thickness(20), 
                        Children = 
                        { 
                            new TextBlock { Text = "Вы уверены, что хотите удалить эту запись?", TextWrapping = TextWrapping.Wrap }, 
                            new StackPanel 
                            { 
                                Orientation = Avalonia.Layout.Orientation.Horizontal, 
                                Spacing = 10, 
                                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, 
                                Children = 
                                { 
                                    new Button { Content = "Нет", Width = 60, Name = "No" }, 
                                    new Button { Content = "Да", Width = 60, Name = "Yes", Background = Brush.Parse("#E74C3C"), Foreground = Brushes.White } 
                                } 
                            } 
                        } 
                    } 
                };

                bool confirm = false;
                var panel = dialog.Content as StackPanel;
                var buttons = panel.Children.OfType<StackPanel>().First().Children.OfType<Button>();
                
                buttons.First(b => b.Name == "Yes").Click += (s, ev) => { confirm = true; dialog.Close(); };
                buttons.First(b => b.Name == "No").Click += (s, ev) => { confirm = false; dialog.Close(); };

                await dialog.ShowDialog(VisualRoot as Window);
                
                if (!confirm)
                {
                    return;
                }

                if (item.IsGroup)
                {
                    var original = (TrainingSchedule)item.OriginalItem;
                    var trackedEntity = await _db.TrainingSchedules
                        .Include(ts => ts.TrainingBookings)
                        .Include(ts => ts.GroupTraining)
                        .FirstOrDefaultAsync(ts => ts.ScheduleId == original.ScheduleId);
                    
                    if (trackedEntity != null)
                    {
                        foreach (var b in trackedEntity.TrainingBookings.ToList())
                        {
                            var activeSub = _db.ClientSubscriptions.FirstOrDefault(cs => cs.ClientId == b.ClientId && cs.IsActive && cs.EndDate >= DateTime.Today);
                            if (activeSub != null && activeSub.GroupRemainingVisits.HasValue)
                            {
                                activeSub.GroupRemainingVisits++;
                                _db.ClientSubscriptions.Update(activeSub);
                            }

                            _db.ClientNotifications.Add(new ClientNotification
                            {
                                ClientId = b.ClientId,
                                Message = $"Ваша групповая тренировка «{trackedEntity.GroupTraining?.Name}» ({trackedEntity.TrainingDate:dd.MM.yyyy} в {trackedEntity.TrainingTime:hh\\:mm}) была отменена администратором. Занятие/средства возвращены.",
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false
                            });
                            _db.TrainingBookings.Remove(b);
                        }
                        _db.TrainingSchedules.Remove(trackedEntity);
                    }
                }
                else
                {
                    var original = (IndividualTraining)item.OriginalItem;
                    var trackedEntity = await _db.IndividualTrainings.FindAsync(original.IndividualTrainingId);
                    
                    if (trackedEntity != null)
                    {
                        if (trackedEntity.ClientId != null)
                        {
                            var activeSub = _db.ClientSubscriptions.FirstOrDefault(cs => cs.ClientId == trackedEntity.ClientId && cs.IsActive && cs.EndDate >= DateTime.Today);
                            if (activeSub != null && activeSub.IndividualRemainingVisits.HasValue && trackedEntity.Price == 0)
                            {
                                activeSub.IndividualRemainingVisits++;
                                _db.ClientSubscriptions.Update(activeSub);
                            }

                            _db.ClientNotifications.Add(new ClientNotification
                            {
                                ClientId = trackedEntity.ClientId.Value,
                                Message = $"Ваша индивидуальная тренировка ({trackedEntity.TrainingDate:dd.MM.yyyy} в {trackedEntity.StartTime:hh\\:mm}) была отменена администратором. Занятие/средства возвращены.",
                                CreatedAt = DateTime.UtcNow,
                                IsRead = false
                            });
                        }
                        _db.IndividualTrainings.Remove(trackedEntity);
                    }
                }
                
                await _db.SaveChangesAsync();
                LoadScheduleForSelectedDate();
                UpdateCalendar(); 
            }
        }

        private async void DuplicateDay_Click(object sender, RoutedEventArgs e)
        {
            if (TrainerComboBox.SelectedItem is Models.Trainer trainer && _selectedDate != null)
            {
                var sourceDate = _selectedDate.Value.Date;
                var dialog = new DuplicateScheduleWindow(sourceDate);
                var result = await dialog.ShowDialog<bool>(VisualRoot as Window);

                if (result && dialog.SelectedStartDate != null && dialog.SelectedEndDate != null)
                {
                    var start = dialog.SelectedStartDate.Value.Date;
                    var end = dialog.SelectedEndDate.Value.Date;
                    
                    if (end < start)
                    {
                        return;
                    }

                    var sourceGroups = _db.TrainingSchedules.AsNoTracking()
                        .Where(s => s.TrainerId == trainer.TrainerId && s.TrainingDate.Date == sourceDate)
                        .ToList();
                        
                    var sourceInds = _db.IndividualTrainings.AsNoTracking()
                        .Where(i => i.TrainerId == trainer.TrainerId && i.TrainingDate.Date == sourceDate)
                        .ToList();

                    for (var currentDate = start; currentDate <= end; currentDate = currentDate.AddDays(1))
                    {
                        if (currentDate == sourceDate)
                        {
                            continue;
                        }

                        foreach (var g in sourceGroups)
                        {
                            bool exists = _db.TrainingSchedules.Any(ts => ts.TrainerId == trainer.TrainerId && ts.TrainingDate.Date == currentDate && ts.TrainingTime == g.TrainingTime);
                            
                            if (!exists)
                            {
                                _db.TrainingSchedules.Add(new TrainingSchedule 
                                { 
                                    TrainingId = g.TrainingId, 
                                    TrainerId = g.TrainerId, 
                                    TrainingDate = currentDate, 
                                    TrainingTime = g.TrainingTime, 
                                    MaxParticipants = g.MaxParticipants, 
                                    CurrentParticipants = 0, 
                                    IsActive = true 
                                });
                            }
                        }

                        foreach (var i in sourceInds)
                        {
                            bool exists = _db.IndividualTrainings.Any(it => it.TrainerId == trainer.TrainerId && it.TrainingDate.Date == currentDate && it.StartTime == i.StartTime);
                            
                            if (!exists)
                            {
                                _db.IndividualTrainings.Add(new IndividualTraining 
                                { 
                                    TrainerId = trainer.TrainerId, 
                                    TrainingDate = currentDate, 
                                    StartTime = i.StartTime, 
                                    EndTime = i.EndTime, 
                                    ClientId = null, 
                                    IsActive = true, 
                                    CreatedAt = DateTime.UtcNow, 
                                    Price = trainer.IndividualTrainingPrice 
                                });
                            }
                        }
                    }
                    
                    await _db.SaveChangesAsync();
                    LoadScheduleForSelectedDate();
                    UpdateCalendar();
                }
            }
        }

        private async void ExportTrainerReport_Click(object sender, RoutedEventArgs e)
        {
            if (!(TrainerComboBox.SelectedItem is Models.Trainer trainer))
            {
                return;
            }
            
            int month = ReportMonthPicker.SelectedIndex + 1;
            int year = DateTime.Today.Year;
            DateTime start = new DateTime(year, month, 1);
            DateTime end = start.AddMonths(1).AddDays(-1);

            var schedules = _db.TrainingSchedules
                .Include(s => s.GroupTraining)
                .Include(s => s.TrainingBookings)
                .ThenInclude(b => b.Client)
                .Where(s => s.TrainerId == trainer.TrainerId && s.TrainingDate >= start && s.TrainingDate <= end)
                .ToList();
                
            var individuals = _db.IndividualTrainings
                .Include(i => i.Client)
                .Where(i => i.TrainerId == trainer.TrainerId && i.TrainingDate >= start && i.TrainingDate <= end)
                .ToList();
                
            var absences = _db.TrainerAbsences
                .Where(a => a.TrainerId == trainer.TrainerId && a.AbsenceDate >= start && a.AbsenceDate <= end)
                .ToList();

            int scheduledWorkDays = 0;
            int actualWorkedDays = 0;
            int workedOnDaysOff = 0;
            int sickDays = 0;
            int leaveDays = 0;
            int truancyDays = 0;
            decimal totalRevenue = 0;

            var sb = new StringBuilder();
            sb.Append("<html><head><meta charset='UTF-8'><style>");
            sb.Append("body{font-family:sans-serif;padding:30px;color:#2C3E50;} h1,h2{color:#2C3E50;} ");
            sb.Append("table{width:100%;border-collapse:collapse;margin-top:20px;} th,td{border:1px solid #ddd;padding:12px;text-align:left;} th{background:#f8f9fa;} ");
            sb.Append(".row-weekend{background-color:#F5EEF8 !important;} .row-sick{background-color:#FEF9E7 !important;} .row-truancy{background-color:#FDEDEC !important;} .row-normal{background-color:#FFFFFF;} ");
            sb.Append(".summary-table{width:100%; margin-bottom: 30px; border: 2px solid #3498DB;} .summary-table th{background:#3498DB; color:white; font-size:14px;} .summary-table td{font-size:16px; font-weight:bold; text-align:center;}");
            sb.Append("</style></head><body>");

            sb.Append($"<h1>Выписка по графику: {trainer.FullName}</h1><h2>Период: {ReportMonthPicker.SelectionBoxItem} {year}</h2>");
            
            var rowsHtml = new StringBuilder();
            rowsHtml.Append("<table><tr><th>Дата</th><th>Статус дня</th><th>Тренировок (с людьми)</th><th>Детализация (Клиенты)</th><th>Сумма</th><th>Причина пропуска</th></tr>");

            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                bool isDayOff = IsDayOffForTrainer(trainer, d);
                if (!isDayOff)
                {
                    scheduledWorkDays++;
                }

                var dayAbsence = absences.FirstOrDefault(a => a.AbsenceDate.Date == d.Date);
                var dayGroups = schedules.Where(s => s.TrainingDate.Date == d.Date).ToList();
                var dayInds = individuals.Where(i => i.TrainingDate.Date == d.Date).ToList();
                
                var conductedGroups = dayGroups.Where(g => g.CurrentParticipants > 0).ToList();
                var conductedInds = dayInds.Where(i => i.ClientId != null).ToList();
                
                int totalConducted = conductedGroups.Count + conductedInds.Count;
                
                decimal dayRevenue = 0;
                foreach(var ind in conductedInds)
                {
                    dayRevenue += ind.Price > 0 ? ind.Price : (trainer.IndividualTrainingPrice > 0 ? trainer.IndividualTrainingPrice : 2000m);
                }
                totalRevenue += dayRevenue;

                if (dayAbsence != null)
                {
                    if (dayAbsence.ReasonType == "sick") sickDays++;
                    else if (dayAbsence.ReasonType == "truancy") truancyDays++;
                    else leaveDays++;
                }
                else if (totalConducted > 0)
                {
                    if (isDayOff) workedOnDaysOff++;
                    else actualWorkedDays++;
                }

                string rowClass = "row-normal";
                string status = isDayOff ? "Выходной" : "Рабочий";
                string reason = "";
                
                if (dayAbsence != null)
                {
                    if (dayAbsence.ReasonType == "sick") { reason = "Больничный: " + dayAbsence.Description; rowClass = "row-sick"; }
                    else if (dayAbsence.ReasonType == "truancy") { reason = "Прогул: " + dayAbsence.Description; rowClass = "row-truancy"; status = "Прогул"; }
                    else { reason = "Заявление: " + dayAbsence.Description; rowClass = "row-sick"; }
                }
                else if (totalConducted > 0 && isDayOff)
                {
                    rowClass = "row-weekend";
                    status = "Работа в выходной";
                }

                var clientNames = conductedGroups
                    .SelectMany(g => g.TrainingBookings.Select(b => b.Client.FullName))
                    .Concat(conductedInds.Select(i => i.Client.FullName));
                    
                string details = string.Join(", ", clientNames);
                
                if (string.IsNullOrEmpty(details))
                {
                    if (!dayGroups.Any() && !dayInds.Any())
                    {
                        details = "Нет расписания";
                    }
                    else
                    {
                        details = "Нет записей";
                    }
                }

                rowsHtml.Append($"<tr class='{rowClass}'><td>{d:dd.MM (ddd)}</td><td>{status}</td><td>{totalConducted}</td><td>{details}</td><td>{(dayRevenue > 0 ? dayRevenue.ToString("N0") + " ₽" : "—")}</td><td>{reason}</td></tr>");
            }
            
            rowsHtml.Append("</table>");

            sb.Append("<table class='summary-table'>");
            sb.Append("<tr><th>Рабочих дней по графику</th><th>Фактически отработано</th><th>Выходов в выходные</th><th>Больничных / Отгулов</th><th>Прогулов</th><th>Итого выручка</th></tr>");
            sb.Append($"<tr><td>{scheduledWorkDays}</td><td>{actualWorkedDays}</td><td style='color:#8E44AD;'>{workedOnDaysOff}</td><td style='color:#F39C12;'>{sickDays + leaveDays}</td><td style='color:#E74C3C;'>{truancyDays}</td><td style='color:#27AE60;'>{totalRevenue:N0} ₽</td></tr>");
            sb.Append("</table>");

            sb.Append("<h3>Детализация по дням:</h3>");
            sb.Append(rowsHtml.ToString());

            sb.Append($"<div style='margin-top:40px; font-size:12px; color:#BDC3C7; text-align:right;'>Сформировано системой FitClub: {DateTime.Now:dd.MM.yyyy HH:mm}</div>");
            sb.Append("</body></html>");

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null)
            {
                return;
            }

            var dialog = new Avalonia.Platform.Storage.FilePickerSaveOptions 
            { 
                Title = "Сохранить выписку", 
                SuggestedFileName = $"Выписка_{trainer.LastName}_{month}_{year}.html", 
                DefaultExtension = "html" 
            };
            
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(dialog);
            
            if (file != null) 
            { 
                await using var stream = await file.OpenWriteAsync(); 
                using var writer = new StreamWriter(stream, Encoding.UTF8); 
                await writer.WriteAsync(sb.ToString()); 
            }
        }

        private async void AddGroupTraining_Click(object sender, RoutedEventArgs e)
        {
            if (TrainerComboBox.SelectedItem is Models.Trainer trainer && _selectedDate != null)
            {
                var targetDate = _selectedDate.Value.Date;
                var trainingIds = _db.TrainingTrainers.Where(tt => tt.TrainerId == trainer.TrainerId).Select(tt => tt.TrainingId).ToList();
                var availableTrainings = _db.GroupTrainings.Where(gt => trainingIds.Contains(gt.TrainingId) && gt.IsActive).ToList();

                if (!availableTrainings.Any())
                {
                    return;
                }

                var groupSessions = _db.TrainingSchedules.Include(ts => ts.GroupTraining).Where(s => s.TrainerId == trainer.TrainerId && s.TrainingDate.Date == targetDate).ToList();
                var indSessions = _db.IndividualTrainings.Where(i => i.TrainerId == trainer.TrainerId && i.TrainingDate.Date == targetDate).ToList();

                var occupiedRanges = new List<(TimeSpan Start, TimeSpan End)>();
                
                foreach (var g in groupSessions)
                {
                    if (g.GroupTraining != null)
                    {
                        occupiedRanges.Add((g.TrainingTime, g.TrainingTime.Add(TimeSpan.FromMinutes(g.GroupTraining.DurationMinutes))));
                    }
                }
                
                foreach (var i in indSessions)
                {
                    occupiedRanges.Add((i.StartTime, i.EndTime));
                }

                var dialog = new AddGroupTrainingSlotWindow(availableTrainings, occupiedRanges);
                var result = await dialog.ShowDialog<bool>(VisualRoot as Window);

                if (result && dialog.SelectedTraining != null)
                {
                    _db.TrainingSchedules.Add(new TrainingSchedule 
                    { 
                        TrainingId = dialog.SelectedTraining.TrainingId, 
                        TrainerId = trainer.TrainerId, 
                        TrainingDate = targetDate, 
                        TrainingTime = dialog.SelectedTime, 
                        MaxParticipants = dialog.SelectedTraining.MaxParticipants, 
                        CurrentParticipants = 0, 
                        IsActive = true 
                    });
                    
                    await _db.SaveChangesAsync();
                    LoadScheduleForSelectedDate();
                    UpdateCalendar();
                }
            }
        }

        private async void GenerateMonth_Click(object sender, RoutedEventArgs e)
        {
            var service = new FitClub.Services.TrainingService(_db);
            
            await Task.Run(() => 
            {
                service.CreateMonthlySchedules();
                service.CreateMonthlyIndividualSchedules();
                service.MarkPastTrainingsAsInactive();
            });
            
            LoadScheduleForSelectedDate();
            UpdateCalendar();
        }
    }
}