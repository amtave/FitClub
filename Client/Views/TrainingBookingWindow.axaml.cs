using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FitClub.Models;
using FitClub.Services;
using FitClub.Client.Views;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FitClub.Views
{
    public class ClientBookingCalendarDay
    {
        public int Day { get; set; }
        public DateTime Date { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSelected { get; set; }
        public IBrush BorderColor { get; set; } = Brushes.Transparent;
        public Avalonia.Thickness BorderThickness { get; set; } = new Avalonia.Thickness(0);
    }

    public class TrainerComboBoxItem
    {
        public int TrainerId { get; set; }
        public string FullName { get; set; }
        public Avalonia.Media.Imaging.Bitmap PhotoBitmap { get; set; }
        public bool HasPhoto => PhotoBitmap != null;
    }

    public partial class TrainingBookingWindow : Window
    {
        private readonly AppDbContext _db;
        private readonly GroupTraining _training;
        private readonly Models.Client _client;
        private readonly TrainingService _trainingService;
        private DateTime _currentMonth;
        private DateTime? _selectedDate;
        private List<TrainingSchedule> _allSchedulesCache;
        private int _selectedTrainerId = 0; 

        public TrainingBookingWindow(GroupTraining training, Models.Client client, TrainingService trainingService, Models.Trainer selectedTrainer = null)
        {
            InitializeComponent();
            _db = new AppDbContext();
            _training = training;
            _client = client;
            _trainingService = trainingService;
            _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _selectedDate = DateTime.Today;

            if (selectedTrainer != null)
            {
                _selectedTrainerId = selectedTrainer.TrainerId;
            }

            SetupHeader();
            LoadSchedulesFromDatabase();
            SetupTrainerFilter();
            UpdateCalendar();
            LoadSlotsForSelectedDate();
        }

        private void SetupHeader()
        {
            TrainingNameText.Text = _training.Name;
            DescriptionText.Text = string.IsNullOrEmpty(_training.Description) ? "Описание отсутствует." : _training.Description;
            TrainingImage.Source = _training.TrainingImage;
        }

        private void LoadSchedulesFromDatabase()
        {
            var now = DateTime.Now;
            _allSchedulesCache = _db.TrainingSchedules.AsNoTracking()
                .Include(ts => ts.Trainer)
                .Include(ts => ts.GroupTraining)
                .Where(ts => ts.TrainingId == _training.TrainingId && 
                             ts.IsActive && 
                             ts.CurrentParticipants < ts.MaxParticipants)
                .Where(ts => ts.TrainingDate > now.Date || (ts.TrainingDate == now.Date && ts.TrainingTime > now.TimeOfDay))
                .ToList();
        }

        private void SetupTrainerFilter()
        {
            var uniqueTrainers = _allSchedulesCache
                .Where(s => s.Trainer != null)
                .Select(s => s.Trainer)
                .GroupBy(t => t.TrainerId)
                .Select(g => g.First())
                .OrderBy(t => t.FullName)
                .ToList();

            var trainerOptions = new List<TrainerComboBoxItem>();
            trainerOptions.Add(new TrainerComboBoxItem { FullName = "Все тренеры", TrainerId = 0, PhotoBitmap = null });

            foreach (var t in uniqueTrainers)
            {
                trainerOptions.Add(new TrainerComboBoxItem
                {
                    TrainerId = t.TrainerId,
                    FullName = t.FullName,
                    PhotoBitmap = t.PhotoBitmap
                });
            }

            TrainerFilterComboBox.ItemsSource = trainerOptions;
            
            var selected = trainerOptions.FirstOrDefault(t => t.TrainerId == _selectedTrainerId) ?? trainerOptions[0];
            TrainerFilterComboBox.SelectedItem = selected;
        }

        private void TrainerFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrainerFilterComboBox.SelectedItem is TrainerComboBoxItem item)
            {
                _selectedTrainerId = item.TrainerId;
                UpdateCalendar();
                LoadSlotsForSelectedDate();
            }
        }

        private void UpdateCalendar()
        {
            MonthText.Text = _currentMonth.ToString("MMMM yyyy").ToUpper();
            var days = new List<ClientBookingCalendarDay>();
            int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            var firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int firstDayOfWeek = (int)firstDay.DayOfWeek;
            
            if (firstDayOfWeek == 0) firstDayOfWeek = 7;

            for (int i = 1; i < firstDayOfWeek; i++)
            {
                days.Add(new ClientBookingCalendarDay { IsEnabled = false });
            }

            var availableDays = _allSchedulesCache
                .Where(s => s.TrainingDate.Year == _currentMonth.Year && 
                            s.TrainingDate.Month == _currentMonth.Month &&
                            (_selectedTrainerId == 0 || s.TrainerId == _selectedTrainerId))
                .Select(s => s.TrainingDate.Day)
                .Distinct()
                .ToList();

            for (int i = 1; i <= daysInMonth; i++)
            {
                var date = new DateTime(_currentMonth.Year, _currentMonth.Month, i);
                bool isSelected = _selectedDate.HasValue && _selectedDate.Value.Date == date.Date;
                
                IBrush borderColor = Brushes.Transparent;
                double thick = 0;

                if (availableDays.Contains(i) && date >= DateTime.Today)
                {
                    borderColor = Brush.Parse("#2ECC71");
                    thick = 2;
                }

                days.Add(new ClientBookingCalendarDay
                {
                    Day = i,
                    Date = date,
                    IsEnabled = date >= DateTime.Today,
                    IsSelected = isSelected,
                    BorderColor = borderColor,
                    BorderThickness = new Avalonia.Thickness(thick)
                });
            }
            CalendarControl.ItemsSource = days;
        }

        private void LoadSlotsForSelectedDate()
        {
            if (!_selectedDate.HasValue) return;

            SelectedDateText.Text = _selectedDate.Value.ToString("dd MMMM yyyy");
            var targetDate = _selectedDate.Value.Date;

            var slots = _allSchedulesCache
                .Where(s => s.TrainingDate.Date == targetDate && 
                           (_selectedTrainerId == 0 || s.TrainerId == _selectedTrainerId))
                .OrderBy(s => s.TrainingTime)
                .ToList();

            SlotsControl.ItemsSource = slots;
            NoSlotsPanel.IsVisible = !slots.Any();
        }

        private void DayClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ClientBookingCalendarDay day && day.IsEnabled)
            {
                _selectedDate = day.Date;
                UpdateCalendar();
                LoadSlotsForSelectedDate();
            }
        }

        private void PrevMonthClick(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            UpdateCalendar();
        }

        private void NextMonthClick(object sender, RoutedEventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            UpdateCalendar();
        }

        private async void BookSlotClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TrainingSchedule slot)
            {
                using var context = new AppDbContext();
                DateTime bookingDate = slot.TrainingDate.Date;
                TimeSpan bookingStart = slot.TrainingTime;
                TimeSpan bookingEnd = bookingStart.Add(TimeSpan.FromMinutes(slot.GroupTraining.DurationMinutes));

                var clientGroupBookings = context.TrainingBookings
                    .Include(b => b.TrainingSchedule)
                    .ThenInclude(s => s.GroupTraining)
                    .Where(b => b.ClientId == _client.ClientId && b.TrainingSchedule.TrainingDate.Date == bookingDate)
                    .ToList();

                bool hasConflictGroup = clientGroupBookings.Any(b => 
                {
                    var existingStart = b.TrainingSchedule.TrainingTime;
                    var existingEnd = existingStart.Add(TimeSpan.FromMinutes(b.TrainingSchedule.GroupTraining.DurationMinutes));
                    return (existingStart <= bookingStart && existingEnd > bookingStart) ||
                           (existingStart < bookingEnd && existingEnd >= bookingEnd);
                });

                var clientIndividualBookings = context.IndividualTrainings
                    .Where(it => it.ClientId == _client.ClientId && it.IsActive && it.TrainingDate.Date == bookingDate)
                    .ToList();

                bool hasConflictIndividual = clientIndividualBookings.Any(it => 
                {
                    return (it.StartTime <= bookingStart && it.EndTime > bookingStart) ||
                           (it.StartTime < bookingEnd && it.EndTime >= bookingEnd);
                });

                if (hasConflictGroup || hasConflictIndividual)
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("Конфликт расписания", 
                        "В это время у вас уже запланирована другая тренировка. Пожалуйста, выберите другое время.", 
                        ButtonEnum.Ok);
                    await box.ShowWindowDialogAsync(this);
                    return;
                }

                if (slot.CurrentParticipants >= slot.MaxParticipants)
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("Внимание", "К сожалению, свободные места закончились.", ButtonEnum.Ok);
                    await box.ShowWindowDialogAsync(this);
                    return;
                }

                var activeSub = context.ClientSubscriptions
                    .FirstOrDefault(cs => cs.ClientId == _client.ClientId
                        && cs.IsActive
                        && cs.EndDate >= DateTime.Today
                        && cs.GroupRemainingVisits > 0
                        && cs.SelectedTrainingTypeId == slot.TrainingId
                        && cs.SelectedTrainerId == slot.TrainerId);

                if (activeSub != null)
                {
                    var confirm = await MessageBoxManager.GetMessageBoxStandard("Подтверждение записи", $"Подтверждаете ли вы запись на групповую тренировку по абонементу?", ButtonEnum.YesNo).ShowWindowDialogAsync(this);
                    
                    if (confirm == ButtonResult.Yes)
                    {
                        var booking = new TrainingBooking
                        {
                            ClientId = _client.ClientId,
                            ScheduleId = slot.ScheduleId,
                            TrainingId = slot.TrainingId,
                            BookingDate = DateTime.Now,
                            Status = "confirmed"
                        };
                        
                        context.TrainingBookings.Add(booking);
                        activeSub.GroupRemainingVisits -= 1;
                        
                        var dbSlot = context.TrainingSchedules.Find(slot.ScheduleId);
                        if (dbSlot != null)
                        {
                            dbSlot.CurrentParticipants += 1;
                            context.TrainingSchedules.Update(dbSlot);
                        }
                        
                        context.ClientSubscriptions.Update(activeSub);
                        context.SaveChanges();
                        
                        var box = MessageBoxManager.GetMessageBoxStandard("Успех", "Вы успешно записались на тренировку по абонементу!", ButtonEnum.Ok);
                        await box.ShowWindowDialogAsync(this);
                        Close();
                    }
                    return;
                }

                var paymentWindow = new PaymentTrainingWindow(_training, slot, _client, _trainingService);
                var result = await paymentWindow.ShowDialog<bool>(this);
                
                if (result)
                {
                    Close();
                }
                else
                {
                    LoadSchedulesFromDatabase();
                    UpdateCalendar();
                    LoadSlotsForSelectedDate();
                }
            }
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}