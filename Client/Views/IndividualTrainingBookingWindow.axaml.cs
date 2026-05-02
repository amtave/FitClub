using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FitClub.Models;
using FitClub.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using FitClub.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace FitClub.Client.Views
{
    public class BookingCalendarDay
    {
        public int Day { get; set; }
        public DateTime Date { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSelected { get; set; }
        public IBrush BorderColor { get; set; } = Brushes.Transparent;
        public Avalonia.Thickness BorderThickness { get; set; } = new Avalonia.Thickness(0);
    }

    public partial class IndividualTrainingBookingWindow : Window
    {
        private readonly AppDbContext _db;
        private readonly Models.Trainer _trainer;
        private readonly Models.Client _client;
        private readonly TrainingService _trainingService;
        private DateTime _currentMonth;
        private DateTime? _selectedDate;

        public IndividualTrainingBookingWindow(Models.Trainer trainer, Models.Client client, TrainingService service)
        {
            InitializeComponent();
            _db = new AppDbContext();
            _trainer = trainer;
            _client = client;
            _trainingService = service;
            _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            _selectedDate = DateTime.Today;

            TrainerNameText.Text = _trainer.FullName;
            TrainerSpecText.Text = _trainer.Specialization;
            TrainerPhoto.Source = _trainer.PhotoBitmap;

            UpdateCalendar();
            LoadSlotsForSelectedDate();
        }

        private void UpdateCalendar()
        {
            MonthText.Text = _currentMonth.ToString("MMMM yyyy").ToUpper();
            var days = new List<BookingCalendarDay>();
            int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            var firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int firstDayOfWeek = (int)firstDay.DayOfWeek;
            if (firstDayOfWeek == 0) firstDayOfWeek = 7;

            for (int i = 1; i < firstDayOfWeek; i++)
                days.Add(new BookingCalendarDay { IsEnabled = false });

            var now = DateTime.Now;
            var availableDays = _db.IndividualTrainings.AsNoTracking()
                .Where(it => it.TrainerId == _trainer.TrainerId && 
                             it.ClientId == null && 
                             it.TrainingDate.Year == _currentMonth.Year && 
                             it.TrainingDate.Month == _currentMonth.Month &&
                             it.IsActive)
                .Where(it => it.TrainingDate > now.Date || (it.TrainingDate == now.Date && it.StartTime > now.TimeOfDay))
                .Select(it => it.TrainingDate.Day)
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

                days.Add(new BookingCalendarDay
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
            var now = DateTime.Now;

            var slots = _db.IndividualTrainings.AsNoTracking()
                .Where(it => it.TrainerId == _trainer.TrainerId && 
                             it.TrainingDate.Date == targetDate && 
                             it.ClientId == null &&
                             it.IsActive)
                .Where(it => it.TrainingDate > now.Date || (it.TrainingDate == now.Date && it.StartTime > now.TimeOfDay))
                .OrderBy(it => it.StartTime)
                .ToList();

            SlotsControl.ItemsSource = slots;
            NoSlotsPanel.IsVisible = !slots.Any();
        }

        private void DayClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BookingCalendarDay day && day.IsEnabled)
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
            if (sender is Button btn && btn.Tag is IndividualTraining slot)
            {
                using var context = new AppDbContext();
                DateTime bookingDate = slot.TrainingDate.Date;
                TimeSpan bookingStart = slot.StartTime;
                TimeSpan bookingEnd = slot.EndTime;

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

                var activeSub = context.ClientSubscriptions
                    .FirstOrDefault(cs => cs.ClientId == _client.ClientId
                        && cs.IsActive
                        && cs.EndDate >= DateTime.Today
                        && cs.IndividualRemainingVisits > 0
                        && cs.IndividualTrainerId == slot.TrainerId);

                if (activeSub != null)
                {
                    var confirm = await MessageBoxManager.GetMessageBoxStandard("Подтверждение записи", "Подтверждаете ли вы запись на индивидуальную тренировку по абонементу?", ButtonEnum.YesNo).ShowWindowDialogAsync(this);
                    
                    if (confirm == ButtonResult.Yes)
                    {
                        var dbSlot = context.IndividualTrainings.Find(slot.IndividualTrainingId);
                        if (dbSlot != null)
                        {
                            dbSlot.ClientId = _client.ClientId;
                            dbSlot.Price = 0;
                            context.IndividualTrainings.Update(dbSlot);
                            
                            activeSub.IndividualRemainingVisits -= 1;
                            context.ClientSubscriptions.Update(activeSub);
                            
                            await context.SaveChangesAsync();
                            
                            var box = MessageBoxManager.GetMessageBoxStandard("Успех", "Вы успешно записались на тренировку по абонементу!", ButtonEnum.Ok);
                            await box.ShowWindowDialogAsync(this);
                            Close();
                        }
                    }
                    return;
                }

                var slotModel = new IndividualTrainingSlot
                {
                    TrainingDate = slot.TrainingDate,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    TrainerId = slot.TrainerId,
                    IsAvailable = true
                };

                var paymentWindow = new IndividualTrainingPaymentWindow(_trainer, slotModel, _client, _trainingService);
                var result = await paymentWindow.ShowDialog<bool>(this);
                
                if (result)
                {
                    Close();
                }
                else
                {
                    LoadSlotsForSelectedDate();
                    UpdateCalendar();
                }
            }
        }

        private void CloseClick(object sender, RoutedEventArgs e) => Close();
    }
}