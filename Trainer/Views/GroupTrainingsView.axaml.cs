using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using FitClub.Models;
using FitClub.Services;
using Microsoft.EntityFrameworkCore;
using System.IO;
using Avalonia.Media.Imaging;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;

namespace FitClub.Trainer.Views
{
    public partial class GroupTrainingsView : UserControl
    {
        private readonly TrainingService _trainingService;
        private readonly AppDbContext _context;
        private Models.Trainer _currentTrainer;
        private DateTime _currentMonth;
        private DateTime? _selectedDate;

        public GroupTrainingsView()
        {
            try
            {
                InitializeComponent();
                _trainingService = new TrainingService(new AppDbContext());
                _context = new AppDbContext();
                
                // Устанавливаем текущий месяц
                _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                
                LoadCurrentTrainer();
                UpdateTrainerInfo(); 
                LoadCalendar();
                UpdateUI();
            }
            catch (Exception ex)
            {
                ShowSimpleError($"Ошибка инициализации: {ex.Message}");
            }
        }

        private void LoadCurrentTrainer()
        {
            try
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
            catch (Exception ex)
            {
                ShowSimpleError($"Ошибка загрузки тренера: {ex.Message}");
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
            try
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
                    bool hasGroupTrainings = false;
                    
                    if (isCurrentMonth && _currentTrainer != null)
                    {
                        hasGroupTrainings = _context.TrainingSchedules
                            .Any(ts => ts.TrainerId == _currentTrainer.TrainerId && 
                                      ts.TrainingDate.Date == currentDate.Date &&
                                      ts.IsActive);
                    }

                    bool isPast = currentDate < DateTime.Today;
                    bool isWorkingDay = _currentTrainer != null && IsTrainerWorkingDay(_currentTrainer, currentDate);
                    bool isEnabled = isCurrentMonth && isWorkingDay && !isPast;

                    // Используем упрощенный подход для создания дня календаря
                    var dayControl = CreateCalendarDay(currentDate, isCurrentMonth, isWorkingDay, hasGroupTrainings, isPast, isEnabled, col, row);
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
            catch (Exception ex)
            {
                ShowSimpleError($"Ошибка создания календаря: {ex.Message}");
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
            LoadGroupTrainingsForDate(date);
            UpdateUI();
        }

        private void LoadGroupTrainingsForDate(DateTime date)
        {
            if (_currentTrainer == null) return;

            try
            {
                var schedules = _context.TrainingSchedules
                    .Include(ts => ts.GroupTraining)
                        .ThenInclude(gt => gt.TrainingType)
                    .Include(ts => ts.GroupTraining)
                        .ThenInclude(gt => gt.IntensityLevel)
                    .Where(ts => ts.TrainerId == _currentTrainer.TrainerId &&
                               ts.TrainingDate.Date == date.Date &&
                               ts.IsActive)
                    .OrderBy(ts => ts.TrainingTime)
                    .ToList();

                var trainingViewModels = schedules.Select(ts => new GroupTrainingViewModel(ts, OpenTrainingDetails)).ToList();

                var trainingsControl = this.FindControl<ItemsControl>("GroupTrainingsItemsControl");
                if (trainingsControl != null)
                {
                    trainingsControl.ItemsSource = trainingViewModels;
                }
                
                var selectedDateText = this.FindControl<TextBlock>("SelectedDateText");
                if (selectedDateText != null)
                {
                    selectedDateText.Text = $"Групповые занятия на {date:dd.MM.yyyy}";
                }
                
                var noTrainingsMessage = this.FindControl<TextBlock>("NoTrainingsMessage");
                if (noTrainingsMessage != null)
                {
                    noTrainingsMessage.IsVisible = !trainingViewModels.Any();
                }
            }
            catch (Exception ex)
            {
                ShowSimpleError($"Ошибка загрузки групповых тренировок: {ex.Message}");
            }
        }

        private void OpenTrainingDetails(TrainingSchedule schedule)
        {
            try
            {
                if (schedule == null)
                {
                    ShowSimpleError("Ошибка: расписание не найдено");
                    return;
                }

                // ВОЗВРАЩАЕМ ОРИГИНАЛЬНОЕ ОКНО
                var detailsWindow = new GroupTrainingDetailsWindow(schedule, _currentTrainer);
                detailsWindow.Show();
            }
            catch (Exception ex)
            {
                ShowSimpleError($"Ошибка открытия деталей тренировки: {ex.Message}");
            }
        }

        private void ShowSimpleError(string message)
        {
            try
            {
                var errorWindow = new Window
                {
                    Title = "Ошибка",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };

                var panel = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 10,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var textBlock = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };

                var button = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Width = 80
                };

                button.Click += (s, e) => errorWindow.Close();

                panel.Children.Add(textBlock);
                panel.Children.Add(button);

                errorWindow.Content = panel;
                errorWindow.Show();
            }
            catch
            {
                // Если даже простой MessageBox не работает, игнорируем
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

    // ViewModel для отображения групповой тренировки
    public class GroupTrainingViewModel
    {
        private readonly TrainingSchedule _schedule;
        private readonly Action<TrainingSchedule> _openTrainingAction;

        public GroupTrainingViewModel(TrainingSchedule schedule, Action<TrainingSchedule> openTrainingAction)
        {
            _schedule = schedule;
            _openTrainingAction = openTrainingAction;
            OpenTrainingCommand = new RelayCommand(OpenTraining);
        }

        // Время тренировки "08:00"
        public string TimeFormatted => _schedule.TrainingTime.ToString(@"hh\:mm");
        
        // Продолжительность "60 мин"
        public string DurationFormatted => $"{_schedule.GroupTraining?.DurationMinutes ?? 0} мин";
        
        public string TrainingName => _schedule.GroupTraining?.Name ?? "Неизвестная тренировка";
        
        public string TrainingDescription => _schedule.GroupTraining?.Description ?? "Описание отсутствует";
        
        // Участники "5/20"
        public string ParticipantsInfo => $"{_schedule.CurrentParticipants}/{_schedule.MaxParticipants}";
        
        public string IntensityLevel => _schedule.GroupTraining?.IntensityLevel?.Name ?? "Не указана";

        // Команда для открытия деталей тренировки
        public ICommand OpenTrainingCommand { get; }

        // Метод для получения ID расписания
        public int GetScheduleId() => _schedule.ScheduleId;

        // Метод для открытия деталей тренировки
        public void OpenTraining()
        {
            _openTrainingAction?.Invoke(_schedule);
        }
    }

    // Простая реализация RelayCommand
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}