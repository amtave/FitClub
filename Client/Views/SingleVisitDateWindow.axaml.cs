using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using FitClub.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace FitClub.Views
{
    public class SingleVisitCalendarDay
    {
        public int Day { get; set; }
        public DateTime Date { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsSelected { get; set; }
        public string DayText => Day > 0 ? Day.ToString() : "";
        public IBrush BackgroundBrush => IsSelected ? Brush.Parse("#FF8955") : (IsEnabled ? Brushes.White : Brushes.Transparent);
        public IBrush ForegroundBrush => IsSelected ? Brushes.White : (IsEnabled ? Brush.Parse("#2C3E50") : Brush.Parse("#BDC3C7"));
    }

    public partial class SingleVisitDateWindow : Window
    {
        private readonly Models.Tariff _tariff;
        private readonly Models.Client _client;
        private readonly AppDbContext _context;
        private DateTime _currentMonth;

        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public bool PaymentConfirmed { get; set; } = false;

        public SingleVisitDateWindow(Models.Tariff tariff, Models.Client client, AppDbContext context)
        {
            InitializeComponent();
            _tariff = tariff;
            _client = client;
            _context = context;

            _currentMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            
            UpdateCalendar();
            UpdateSelectedDateText();
        }

        private void UpdateCalendar()
        {
            MonthText.Text = _currentMonth.ToString("MMMM yyyy").ToUpper();
            var days = new List<SingleVisitCalendarDay>();
            int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            var firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int firstDayOfWeek = (int)firstDay.DayOfWeek;
            if (firstDayOfWeek == 0) firstDayOfWeek = 7;

            for (int i = 1; i < firstDayOfWeek; i++)
            {
                days.Add(new SingleVisitCalendarDay { Day = 0, IsEnabled = false });
            }

            for (int i = 1; i <= daysInMonth; i++)
            {
                var date = new DateTime(_currentMonth.Year, _currentMonth.Month, i);
                bool isSelected = SelectedDate.Date == date.Date;
                bool isEnabled = date.Date >= DateTime.Today;

                days.Add(new SingleVisitCalendarDay
                {
                    Day = i,
                    Date = date,
                    IsEnabled = isEnabled,
                    IsSelected = isSelected
                });
            }

            CalendarControl.ItemsSource = days;
        }

        private void DayClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is SingleVisitCalendarDay day && day.IsEnabled)
            {
                SelectedDate = day.Date;
                UpdateCalendar();
                UpdateSelectedDateText();
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

        private void UpdateSelectedDateText()
        {
            SelectedDateText.Text = SelectedDate.ToString("dd MMMM yyyy");
        }

        private async void PayButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedDate < DateTime.Today)
            {
                SelectedDate = DateTime.Today;
            }
            
            var paymentWindow = new SingleVisitPaymentWindow(_tariff, _client, SelectedDate, _context);
            await paymentWindow.ShowDialog(this);

            if (paymentWindow.PaymentSuccess)
            {
                PaymentConfirmed = true;
                var successBox = MessageBoxManager.GetMessageBoxStandard(
                    "Успех",
                    $"Вы успешно оплатили разовое посещение!\n" +
                    $"Дата посещения: {SelectedDate:dd.MM.yyyy}\n" +
                    $"Стоимость: {_tariff.Price:0} ₽",
                    ButtonEnum.Ok);
                await successBox.ShowWindowDialogAsync(this);
                Close();
            }
            else
            {
                PaymentConfirmed = false;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentConfirmed = false;
            Close();
        }
    }
}