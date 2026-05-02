using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitClub.Admin.Views
{
    public partial class StatisticsView : UserControl
    {
        public int TotalClients { get; set; }
        public int ActiveSubscriptionsCount { get; set; }
        public int StaffTrainersCount { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public string TodayRevenueText { get; set; }
        public string MonthRevenueText { get; set; }
        public string CurrentMonthText { get; set; }
        
        public List<SubscriptionBarItem> SubscriptionItems { get; set; } = new List<SubscriptionBarItem>();
        public List<TrainingBarItem> TrainingItems { get; set; } = new List<TrainingBarItem>();

        public StatisticsView()
        {
            InitializeComponent();
            LoadStatistics();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private async void LoadStatistics()
        {
            try
            {
                using var context = new AppDbContext();
                var today = DateTime.Today;
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                
                CurrentMonthText = $"ГРАФИКИ ЗА {firstDayOfMonth:MMMM yyyy}".ToUpper();
                
                TotalClients = await context.Clients.CountAsync();
                ActiveSubscriptionsCount = await context.ClientSubscriptions.CountAsync(s => s.IsActive && s.EndDate >= today);
                StaffTrainersCount = await context.Trainers.CountAsync(t => t.IsActive);

                var todaySubsRevenue = await context.ClientSubscriptions.Where(s => s.PurchaseDate == today).Include(s => s.Tariff).SumAsync(s => s.Tariff.Price);
                var monthSubsRevenue = await context.ClientSubscriptions.Where(s => s.PurchaseDate >= firstDayOfMonth && s.PurchaseDate <= lastDayOfMonth).Include(s => s.Tariff).SumAsync(s => s.Tariff.Price);

                var todayGroupsRevenue = await context.TrainingBookings.Where(b => b.BookingDate.Date == today).Include(b => b.GroupTraining).SumAsync(b => b.GroupTraining.Price);
                var monthGroupsRevenue = await context.TrainingBookings.Where(b => b.BookingDate.Date >= firstDayOfMonth && b.BookingDate.Date <= lastDayOfMonth).Include(b => b.GroupTraining).SumAsync(b => b.GroupTraining.Price);

                var todayIndTrainings = await context.IndividualTrainings.Include(it => it.Trainer).Where(it => it.TrainingDate.Date == today && it.ClientId != null).ToListAsync();
                var monthIndTrainings = await context.IndividualTrainings.Include(it => it.Trainer).Where(it => it.TrainingDate.Date >= firstDayOfMonth && it.TrainingDate.Date <= lastDayOfMonth && it.ClientId != null).ToListAsync();

                var todayIndsRevenue = todayIndTrainings.Sum(it => it.Price > 0 ? it.Price : (it.Trainer != null ? it.Trainer.IndividualTrainingPrice : 2000m));
                var monthIndsRevenue = monthIndTrainings.Sum(it => it.Price > 0 ? it.Price : (it.Trainer != null ? it.Trainer.IndividualTrainingPrice : 2000m));

                TodayRevenue = todaySubsRevenue + todayGroupsRevenue + todayIndsRevenue;
                MonthRevenue = monthSubsRevenue + monthGroupsRevenue + monthIndsRevenue;

                TodayRevenueText = $"{TodayRevenue:N0} ₽";
                MonthRevenueText = $"{MonthRevenue:N0} ₽";

                var subsData = await context.ClientSubscriptions
                    .Where(s => s.PurchaseDate.Date >= firstDayOfMonth && s.PurchaseDate.Date <= today)
                    .Select(s => new { s.PurchaseDate.Date })
                    .GroupBy(s => s.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync();

                var groupData = await context.TrainingBookings
                    .Where(b => b.BookingDate.Date >= firstDayOfMonth && b.BookingDate.Date <= today)
                    .GroupBy(b => b.BookingDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToListAsync();

                var indData = monthIndTrainings
                    .Where(it => it.TrainingDate.Date <= today)
                    .GroupBy(it => it.TrainingDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .ToList();

                int maxSub = 1, maxTrain = 1;
                var tempSubs = new List<SubscriptionBarItem>();
                var tempTrains = new List<TrainingBarItem>();

                for (int day = 1; day <= today.Day; day++)
                {
                    var date = new DateTime(today.Year, today.Month, day);
                    int sCount = subsData.FirstOrDefault(d => d.Date == date)?.Count ?? 0;
                    int gCount = groupData.FirstOrDefault(d => d.Date == date)?.Count ?? 0;
                    int iCount = indData.FirstOrDefault(d => d.Date == date)?.Count ?? 0;

                    tempSubs.Add(new SubscriptionBarItem { DateFormatted = day.ToString(), Count = sCount });
                    tempTrains.Add(new TrainingBarItem { DateFormatted = day.ToString(), GroupCount = gCount, IndividualCount = iCount });

                    if (sCount > maxSub) maxSub = sCount;
                    if ((gCount + iCount) > maxTrain) maxTrain = (gCount + iCount);
                }

                tempSubs.ForEach(i => i.MaxCount = maxSub);
                tempTrains.ForEach(i => i.MaxCount = maxTrain);
                
                SubscriptionItems = tempSubs;
                TrainingItems = tempTrains;
                DataContext = null; DataContext = this;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        public async void ExportMonthlyReport(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var picker = this.FindControl<DatePicker>("MonthPicker");
            if (picker?.SelectedDate == null) return;
            var date = picker.SelectedDate.Value.DateTime;
            await GenerateHtmlReport(new DateTime(date.Year, date.Month, 1), new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)), $"Отчет за {date:MMMM yyyy}");
        }

        public async void ExportDailyReport(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var picker = this.FindControl<CalendarDatePicker>("DayPicker");
            if (picker?.SelectedDate == null) return;
            var date = picker.SelectedDate.Value;
            await GenerateHtmlReport(date, date, $"Отчет за {date:dd.MM.yyyy}");
        }

        private async Task GenerateHtmlReport(DateTime start, DateTime end, string title)
        {
            using var context = new AppDbContext();
            
            var subs = await context.ClientSubscriptions.Where(s => s.PurchaseDate >= start.Date && s.PurchaseDate <= end.Date).Include(s => s.Tariff).Include(s => s.Client).ToListAsync();
            var groups = await context.TrainingBookings.Where(b => b.BookingDate.Date >= start.Date && b.BookingDate.Date <= end.Date).Include(b => b.Client).Include(b => b.GroupTraining).Include(b => b.TrainingSchedule).ThenInclude(ts => ts.Trainer).ToListAsync();
            var inds = await context.IndividualTrainings.Where(it => it.TrainingDate.Date >= start.Date && it.TrainingDate.Date <= end.Date && it.ClientId != null).Include(it => it.Client).Include(it => it.Trainer).ToListAsync();

            decimal totalSubs = subs.Sum(s => s.Tariff.Price);
            decimal totalGroups = groups.Sum(g => g.GroupTraining.Price);
            decimal totalInds = inds.Sum(i => i.Price > 0 ? i.Price : (i.Trainer != null ? i.Trainer.IndividualTrainingPrice : 2000m));
            decimal grandTotal = totalSubs + totalGroups + totalInds;

            var sb = new StringBuilder();
            sb.Append("<html><head><meta charset='UTF-8'><style>body{font-family:sans-serif;padding:30px;color:#333;} h2,h3{color:#2C3E50;border-bottom:2px solid #3498DB;padding-bottom:5px;} .total-box{background:#f8f9fa;padding:15px;border-left:5px solid #3498DB;margin-bottom:25px;} table{width:100%;border-collapse:collapse;margin-bottom:15px;} th,td{border:1px solid #ddd;padding:12px;text-align:left;} th{background:#f8f9fa;font-weight:bold;} .subtotal{text-align:right;font-weight:bold;padding:10px;background:#fcfcfc;border-top:2px solid #3498DB;}</style></head><body>");
            
            sb.Append($"<h2>{title}</h2>");
            string revenueLabel = start.Date == end.Date ? "день" : "выбранный период";
            sb.Append($"<div class='total-box'><h3>Итого выручка за {revenueLabel}: {grandTotal:N0} ₽</h3></div>");
            
            sb.Append("<h3>1. Продажи абонементов</h3>");
            if (subs.Any()) {
                sb.Append("<table><tr><th>Дата</th><th>Клиент (ФИО)</th><th>Тариф</th><th>Сумма</th></tr>");
                foreach(var s in subs) sb.Append($"<tr><td>{s.PurchaseDate:dd.MM.yyyy}</td><td>{s.Client.LastName} {s.Client.FirstName} {s.Client.MiddleName}</td><td>{s.Tariff.Name}</td><td>{s.Tariff.Price:N0} ₽</td></tr>");
                sb.Append("</table><div class='subtotal'>Итого выручка по абонементам: " + totalSubs.ToString("N0") + " ₽</div><br/>");
            } else sb.Append("<p>Данные отсутствуют.</p>");

            sb.Append("<h3>2. Групповые тренировки</h3>");
            if (groups.Any()) {
                sb.Append("<table><tr><th>Дата записи</th><th>Клиент (ФИО)</th><th>Тренировка</th><th>Тренер (ФИО)</th><th>Сумма</th></tr>");
                foreach(var g in groups) sb.Append($"<tr><td>{g.BookingDate:dd.MM.yyyy HH:mm}</td><td>{g.Client.LastName} {g.Client.FirstName} {g.Client.MiddleName}</td><td>{g.GroupTraining.Name}</td><td>{g.TrainingSchedule?.Trainer?.LastName} {g.TrainingSchedule?.Trainer?.FirstName} {g.TrainingSchedule?.Trainer?.MiddleName}</td><td>{g.GroupTraining.Price:N0} ₽</td></tr>");
                sb.Append("</table><div class='subtotal'>Итого выручка по групповым: " + totalGroups.ToString("N0") + " ₽</div><br/>");
            } else sb.Append("<p>Данные отсутствуют.</p>");

            sb.Append("<h3>3. Индивидуальные тренировки</h3>");
            if (inds.Any()) {
                sb.Append("<table><tr><th>Дата записи</th><th>Клиент (ФИО)</th><th>Тренер (ФИО)</th><th>Дата занятия</th><th>Сумма</th></tr>");
                foreach(var i in inds) {
                    decimal actualPrice = i.Price > 0 ? i.Price : (i.Trainer != null ? i.Trainer.IndividualTrainingPrice : 2000m);
                    sb.Append($"<tr><td>{i.CreatedAt:dd.MM.yyyy HH:mm}</td><td>{i.Client.LastName} {i.Client.FirstName} {i.Client.MiddleName}</td><td>{i.Trainer.LastName} {i.Trainer.FirstName} {i.Trainer.MiddleName}</td><td>{i.TrainingDate:dd.MM.yyyy}</td><td>{actualPrice:N0} ₽</td></tr>");
                }
                sb.Append("</table><div class='subtotal'>Итого выручка по индивидуальным: " + totalInds.ToString("N0") + " ₽</div><br/>");
            } else sb.Append("<p>Данные отсутствуют.</p>");

            sb.Append("</body></html>");

            var dialog = new SaveFileDialog { Title = "Сохранить отчет", InitialFileName = $"{title}.html", DefaultExtension = "html" };
            var result = await dialog.ShowAsync((Window)this.VisualRoot!);
            if (result != null) await File.WriteAllTextAsync(result, sb.ToString(), Encoding.UTF8);
        }
    }

    public class SubscriptionBarItem
    {
        public string DateFormatted { get; set; } = "";
        public int Count { get; set; }
        public int MaxCount { get; set; }
        public string CountText => Count > 0 ? Count.ToString() : "";
        public string BarColor => Count > 0 ? "#FF8955" : "#E0E0E0";
        public int BarHeight => MaxCount > 0 ? Math.Max(2, (Count * 130 / MaxCount)) : 2;
    }

    public class TrainingBarItem
    {
        public string DateFormatted { get; set; } = "";
        public int GroupCount { get; set; }
        public int IndividualCount { get; set; }
        public int MaxCount { get; set; }
        public string GroupCountText => GroupCount > 0 ? GroupCount.ToString() : "";
        public string IndividualCountText => IndividualCount > 0 ? IndividualCount.ToString() : "";
        public int GroupBarHeight => MaxCount > 0 ? Math.Max(2, (GroupCount * 130 / MaxCount)) : 2;
        public int IndividualBarHeight => MaxCount > 0 ? Math.Max(2, (IndividualCount * 130 / MaxCount)) : 2;
    }
}