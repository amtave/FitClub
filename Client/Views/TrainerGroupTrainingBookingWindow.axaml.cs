using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FitClub.Models;
using FitClub.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using FitClub.Client.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace FitClub.Client.Views
{
    public partial class TrainerGroupTrainingBookingWindow : Window
    {
        private readonly AppDbContext _db;
        private readonly GroupTraining _groupTraining; 
        private readonly Models.Trainer _trainer;
        private readonly Models.Client _client;
        private readonly TrainingService _trainingService;
        private DateTime _currentMonth;
        private DateTime? _selectedDate;

        public TrainerGroupTrainingBookingWindow(GroupTraining groupTraining, Models.Trainer trainer, Models.Client client, TrainingService service)
        {
            InitializeComponent();
            _db = new AppDbContext();
            _groupTraining = groupTraining;
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
            var availableDays = _db.TrainingSchedules.AsNoTracking()
                .Where(ts => ts.TrainerId == _trainer.TrainerId && 
                             ts.TrainingDate.Year == _currentMonth.Year && 
                             ts.TrainingDate.Month == _currentMonth.Month &&
                             ts.IsActive && ts.CurrentParticipants < ts.MaxParticipants)
                .Where(ts => ts.TrainingDate > now.Date || (ts.TrainingDate == now.Date && ts.TrainingTime > now.TimeOfDay))
                .Select(ts => ts.TrainingDate.Day)
                .Distinct().ToList();

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
                    Day = i, Date = date,
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

            var query = _db.TrainingSchedules.AsNoTracking()
                .Include(ts => ts.GroupTraining)
                .Include(ts => ts.Trainer)
                .Where(ts => ts.TrainerId == _trainer.TrainerId && ts.TrainingDate.Date == targetDate && ts.IsActive && ts.CurrentParticipants < ts.MaxParticipants)
                .Where(ts => ts.TrainingDate > now.Date || (ts.TrainingDate == now.Date && ts.TrainingTime > now.TimeOfDay));

            if (_groupTraining != null)
            {
                query = query.Where(ts => ts.TrainingId == _groupTraining.TrainingId);
            }

            var slots = query.OrderBy(ts => ts.TrainingTime).ToList();

            SlotsControl.ItemsSource = slots;
            NoSlotsPanel.IsVisible = !slots.Any();

            if (slots.Any())
            {
                var first = slots.First().GroupTraining;
                TrainingTitleText.Text = first?.Name ?? "Тренировка";
                TrainingDescText.Text = string.IsNullOrEmpty(first?.Description) ? "" : first.Description;
                TrainingImage.Source = first?.TrainingImage;
            }
            else
            {
                TrainingTitleText.Text = "Выберите направление";
                TrainingDescText.Text = "Выберите подходящую дату в календаре, чтобы увидеть доступные тренировки тренера.";
                TrainingImage.Source = null;
            }
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

        private void PrevMonthClick(object sender, RoutedEventArgs e) { _currentMonth = _currentMonth.AddMonths(-1); UpdateCalendar(); }
        private void NextMonthClick(object sender, RoutedEventArgs e) { _currentMonth = _currentMonth.AddMonths(1); UpdateCalendar(); }

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
                    var confirm = await MessageBoxManager.GetMessageBoxStandard("Подтверждение записи", "Подтверждаете ли вы запись на групповую тренировку по абонементу?", ButtonEnum.YesNo).ShowWindowDialogAsync(this);
                    
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

                var paymentWindow = new PaymentTrainingWindow(slot.GroupTraining, slot, _client, _trainingService);
                var result = await paymentWindow.ShowDialog<bool>(this);
                if (result) Close();
                else LoadSlotsForSelectedDate();
            }
        }

        private void CloseClick(object sender, RoutedEventArgs e) => Close();
    }
}