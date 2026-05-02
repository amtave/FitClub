using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using FitClub.Models;
using FitClub.Trainer.Services;
using FitClub.Services;
using Microsoft.EntityFrameworkCore;
using Avalonia.Media.Imaging;
using System.IO;

namespace FitClub.Trainer.Views
{
    public partial class IndividualTrainingsView : UserControl
    {
        private readonly IndividualTrainingService _trainingService;
        private readonly AppDbContext _context;
        private Models.Trainer _currentTrainer;
        private DateTime _currentMonth;
        private DateTime? _selectedDate;

        public IndividualTrainingsView()
        {
            InitializeComponent();
            _trainingService = new IndividualTrainingService(new AppDbContext());
            _context = new AppDbContext();
            
            // Устанавливаем текущий месяц
            _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            
            LoadCurrentTrainer();
            UpdateTrainerInfo(); 
            LoadCalendar();
            UpdateUI();
        }

        private void LoadCurrentTrainer()
        {
            if (UserSession.IsLoggedIn && UserSession.CurrentUserRole == "Trainer")
            {
                _currentTrainer = _context.Trainers
                    .FirstOrDefault(t => t.Email == UserSession.CurrentUserEmail && t.IsActive);
            }
            
            if (_currentTrainer == null)
            {
                _currentTrainer = _context.Trainers.FirstOrDefault(t => t.IsActive);
            }
        }

        private void UpdateTrainerInfo()
        {
            if (_currentTrainer != null)
            {
                var trainerInfoText = this.FindControl<TextBlock>("TrainerInfoText");
                if (trainerInfoText != null)
                {
                    trainerInfoText.Text = $"Тренер: {_currentTrainer.LastName} {_currentTrainer.FirstName} {_currentTrainer.MiddleName} | Текущий месяц: {_currentMonth:MMMM yyyy}";
                }
            }
        }

        private void LoadCalendar()
        {
            if (_currentTrainer == null) return;
            CreateCalendarGrid();
        }

        private void CreateCalendarGrid()
        {
            var calendarGrid = this.FindControl<Grid>("CalendarGrid");
            if (calendarGrid == null) return;

            calendarGrid.Children.Clear();

            // ПЕРВЫЙ ДЕНЬ ТЕКУЩЕГО ВЫБРАННОГО МЕСЯЦА
            var firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            
            // КОРРЕКТИРОВКА: Понедельник = 0, Воскресенье = 6
            int emptyDays = firstDayOfWeek == 0 ? 6 : firstDayOfWeek - 1;

            int row = 0;
            int col = 0;
            var currentDate = firstDayOfMonth.AddDays(-emptyDays);

            // Заполняем всю сетку 6x7 = 42 дня (6 недель)
            for (int i = 0; i < 42; i++)
            {
                bool isCurrentMonth = currentDate.Month == firstDayOfMonth.Month;
                bool hasIndividualTrainings = false;
                
                if (isCurrentMonth && _currentTrainer != null)
                {
                    hasIndividualTrainings = _context.IndividualTrainings
                        .Any(it => it.TrainerId == _currentTrainer.TrainerId && 
                                  it.TrainingDate.Date == currentDate.Date &&
                                  it.IsActive);
                }

                bool isPast = currentDate < DateTime.Today;
                bool isWorkingDay = _currentTrainer != null && IsTrainerWorkingDay(_currentTrainer, currentDate);
                bool isEnabled = isCurrentMonth && isWorkingDay && !isPast;

                // Используем упрощенный подход для создания дня календаря
                var dayControl = CreateCalendarDay(currentDate, isCurrentMonth, isWorkingDay, hasIndividualTrainings, isPast, isEnabled, col, row);
                calendarGrid.Children.Add(dayControl);

                col++;
                if (col >= 7)
                {
                    col = 0;
                    row++;
                }

                currentDate = currentDate.AddDays(1);
            }
        }

        private bool IsTrainerWorkingDay(Models.Trainer trainer, DateTime date)
{
    if (trainer?.WorkSchedule == null) return false;

    var dayOfWeek = date.DayOfWeek;
    
    switch (trainer.WorkSchedule.ToLower())
    {
        case "mon-fri":
            return dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Friday;
        case "wed-sun":
            // Среда-воскресенье: среда, четверг, пятница, суббота, воскресенье
            return dayOfWeek >= DayOfWeek.Wednesday || dayOfWeek == DayOfWeek.Sunday;
        default:
            return dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Friday;
    }
}

        private void SelectDate(DateTime date)
        {
            _selectedDate = date;
            LoadTrainingsForDate(date);
            UpdateUI();
        }

        private void LoadTrainingsForDate(DateTime date)
{
    if (_currentTrainer == null) return;

    try
    {
        var trainings = _context.IndividualTrainings
            .Include(it => it.Client) // ВАЖНО: включаем клиента
            .Where(it => it.TrainerId == _currentTrainer.TrainerId &&
                       it.TrainingDate.Date == date.Date &&
                       it.IsActive)
            .OrderBy(it => it.StartTime)
            .ToList();

        // ДОБАВЬТЕ ОТЛАДОЧНУЮ ИНФОРМАЦИЮ
        foreach (var training in trainings)
        {
            System.Diagnostics.Debug.WriteLine($"Тренировка: {training.TrainingDate:dd.MM.yyyy} {training.StartTime:hh\\:mm}");
            System.Diagnostics.Debug.WriteLine($"Клиент: {training.Client?.FullName}");
            System.Diagnostics.Debug.WriteLine($"AvatarPath: {training.Client?.AvatarPath}");
            System.Diagnostics.Debug.WriteLine($"ClientId: {training.Client?.ClientId}");
        }

        var trainingViewModels = trainings.Select(t => new TrainingViewModel
        {
            Training = t
        }).ToList();

        var timeSlotsControl = this.FindControl<ItemsControl>("TimeSlotsItemsControl");
        if (timeSlotsControl != null)
        {
            timeSlotsControl.ItemsSource = trainingViewModels;
        }
        
        var selectedDateText = this.FindControl<TextBlock>("SelectedDateText");
        if (selectedDateText != null)
        {
            selectedDateText.Text = $"Индивидуальные занятия на {date:dd.MM.yyyy}";
        }
        
        var noSlotsMessage = this.FindControl<TextBlock>("NoSlotsMessage");
        if (noSlotsMessage != null)
        {
            noSlotsMessage.IsVisible = !trainingViewModels.Any();
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Ошибка загрузки тренировок: {ex.Message}");
    }
}

        private void UpdateUI()
        {
            var selectedDateBorder = this.FindControl<Border>("SelectedDateBorder");
            var noDateSelectedBorder = this.FindControl<Border>("NoDateSelectedBorder");
            
            if (selectedDateBorder != null && noDateSelectedBorder != null)
            {
                bool hasSelectedDate = _selectedDate != null;
                selectedDateBorder.IsVisible = hasSelectedDate;
                noDateSelectedBorder.IsVisible = !hasSelectedDate;
            }
            
            CreateCalendarGrid();
        }

        // МЕТОДЫ ДЛЯ НАВИГАЦИИ ПО МЕСЯЦАМ
        public void NavigateToPreviousMonth()
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            UpdateTrainerInfo();
            LoadCalendar();
            UpdateUI();
        }

        public void NavigateToNextMonth()
        {
            _currentMonth = _currentMonth.AddMonths(1);
            UpdateTrainerInfo();
            LoadCalendar();
            UpdateUI();
        }

        public void NavigateToCurrentMonth()
        {
            _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            UpdateTrainerInfo();
            LoadCalendar();
            UpdateUI();
        }

        // ОБРАБОТЧИКИ СОБЫТИЙ ДЛЯ КНОПОК НАВИГАЦИИ
        private void PreviousMonth_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPreviousMonth();
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            NavigateToNextMonth();
        }

        private void CurrentMonth_Click(object sender, RoutedEventArgs e)
        {
            NavigateToCurrentMonth();
        }

        private Border CreateCalendarDay(DateTime date, bool isCurrentMonth, bool isWorkingDay, bool hasTrainings, bool isPast, bool isEnabled, int column, int row)
        {
            var isSelected = date == _selectedDate;
            
            // Определяем цвета в зависимости от состояния
            var backgroundColor = GetBackgroundColor(isWorkingDay, isSelected, isPast);
            var dayForeground = GetDayForeground(isWorkingDay, isPast, isSelected);
            var monthForeground = GetMonthForeground(isWorkingDay, isPast, isSelected);
            var indicatorColor = GetIndicatorColor(isWorkingDay, hasTrainings);

            var border = new Border
            {
                Width = 70,
                Height = 70,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Background = backgroundColor,
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0)
            };

            var button = new Button
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                IsEnabled = isEnabled
            };

            button.Click += (s, e) =>
            {
                if (isEnabled)
                {
                    SelectDate(date);
                }
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Spacing = 2
            };

            var dayText = new TextBlock
            {
                Text = date.Day.ToString(),
                FontSize = 14,
                FontWeight = isSelected ? FontWeight.Bold : FontWeight.Normal,
                Foreground = dayForeground,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var monthText = new TextBlock
            {
                Text = date.ToString("MMM").ToUpper(),
                FontSize = 10,
                Foreground = monthForeground,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            var indicator = new Border
            {
                Height = 4,
                Width = 20,
                Background = indicatorColor,
                CornerRadius = new CornerRadius(2),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };

            stackPanel.Children.Add(dayText);
            stackPanel.Children.Add(monthText);
            stackPanel.Children.Add(indicator);

            button.Content = stackPanel;
            border.Child = button;

            Grid.SetColumn(border, column);
            Grid.SetRow(border, row);

            return border;
        }

        private IBrush GetBackgroundColor(bool isWorkingDay, bool isSelected, bool isPast)
        {
            if (!isWorkingDay) return new SolidColorBrush(Color.FromRgb(245, 245, 245));
            if (isSelected) return new SolidColorBrush(Color.FromRgb(243, 156, 18));
            if (isPast) return new SolidColorBrush(Color.FromRgb(220, 220, 220));
            return new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }

        private IBrush GetDayForeground(bool isWorkingDay, bool isPast, bool isSelected)
        {
            if (!isWorkingDay) return Brushes.Gray;
            if (isPast) return Brushes.LightGray;
            if (isSelected) return Brushes.White;
            return Brushes.Black;
        }

        private IBrush GetMonthForeground(bool isWorkingDay, bool isPast, bool isSelected)
        {
            if (!isWorkingDay) return Brushes.LightGray;
            if (isPast) return Brushes.LightGray;
            if (isSelected) return Brushes.White;
            return Brushes.Gray;
        }

        private IBrush GetIndicatorColor(bool isWorkingDay, bool hasTrainings)
        {
            if (!isWorkingDay) return new SolidColorBrush(Color.FromRgb(149, 165, 166));
            if (hasTrainings) return new SolidColorBrush(Color.FromRgb(52, 152, 219));
            return new SolidColorBrush(Color.FromRgb(46, 204, 113));
        }

        public void RefreshView()
        {
            LoadCurrentTrainer();
            UpdateTrainerInfo();
            LoadCalendar();
            UpdateUI();
        }
    }

    // ViewModel для отображения тренировки
public class TrainingViewModel
{
    public IndividualTraining Training { get; set; }
    
    // Время тренировки "08:00 - 09:00"
    public string TimeRangeFormatted => $"{Training.StartTime:hh\\:mm} - {Training.EndTime:hh\\:mm}";
    
    // Продолжительность "60 мин"
    public string DurationFormatted 
    {
        get
        {
            var duration = Training.EndTime - Training.StartTime;
            return $"{(int)duration.TotalMinutes} мин";
        }
    }
    
    public string ClientFullName => Training.ClientFullName;
    public string ClientPhone => Training.ClientPhone;
    
    // Загрузка аватарки
    public Bitmap ClientAvatar 
    {
        get
        {
            try
            {
                if (Training.Client != null && !string.IsNullOrEmpty(Training.Client.AvatarPath))
                {
                    string avatarsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Avatars");
                    string avatarPath = Path.Combine(avatarsFolder, Training.Client.AvatarPath);
                    
                    if (File.Exists(avatarPath))
                    {
                        return new Bitmap(avatarPath);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки аватарки: {ex.Message}");
            }

            // Загружаем аватарку по умолчанию
            return LoadDefaultAvatar();
        }
    }
    
    private Bitmap LoadDefaultAvatar()
    {
        try
        {
            string defaultAvatarPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "default_avatar.png");
            if (File.Exists(defaultAvatarPath))
            {
                return new Bitmap(defaultAvatarPath);
            }
            
            var resourceUri = new Uri("avares://FitClub/Assets/default_avatar.png");
            using var stream = Avalonia.Platform.AssetLoader.Open(resourceUri);
            return new Bitmap(stream);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка загрузки дефолтной аватарки: {ex.Message}");
            return null;
        }
    }
}
}