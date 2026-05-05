using Avalonia.Controls;
using Avalonia.Interactivity;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitClub.Admin.Views
{
    public partial class TrainerDetailsWindow : Window
    {
        private Models.Trainer _currentTrainer;

        public TrainerDetailsWindow(Models.Trainer trainer)
        {
            InitializeComponent();
            _currentTrainer = trainer;
            
            MonthComboBox.SelectedIndex = DateTime.Today.Month - 1;
            MonthComboBox.SelectionChanged += (s, e) => { if (_currentTrainer != null) LoadStats(); };
            
            LoadStats();
        }

        public TrainerDetailsWindow() 
        { 
            InitializeComponent(); 
        }

        private async void LoadStats()
        {
            if (MonthComboBox.SelectedIndex < 0) return;

            int month = MonthComboBox.SelectedIndex + 1;
            int year = DateTime.Today.Year;
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            using var db = new AppDbContext();
            
            var vm = new TrainerStatsViewModel
            {
                Trainer = _currentTrainer,
                DailyActivity = new List<ActivityBar>()
            };

            var groupSessions = await db.TrainingSchedules
                .Where(s => s.TrainerId == _currentTrainer.TrainerId && s.TrainingDate >= startDate && s.TrainingDate <= endDate && s.CurrentParticipants > 0)
                .Include(s => s.TrainingBookings)
                .ToListAsync();

            var indSessions = await db.IndividualTrainings
                .Where(it => it.TrainerId == _currentTrainer.TrainerId && it.TrainingDate >= startDate && it.TrainingDate <= endDate && it.ClientId != null)
                .Include(it => it.Client)
                .ToListAsync();

            vm.TotalSessions = groupSessions.Count + indSessions.Count;

            var clientIds = groupSessions.SelectMany(s => s.TrainingBookings.Select(b => b.ClientId))
                            .Union(indSessions.Select(i => i.ClientId.Value))
                            .Distinct();
            vm.TotalClients = clientIds.Count();

            vm.MonthlyRevenue = indSessions.Sum(it => 
            {
                if (it.Price > 0) return it.Price;
                if (_currentTrainer.IndividualTrainingPrice > 0) return _currentTrainer.IndividualTrainingPrice;
                return 2000m; 
            });

            if (groupSessions.Any())
            {
                vm.AvgLoad = groupSessions.Average(s => (double)s.CurrentParticipants / s.MaxParticipants);
            }

            var allBookings = groupSessions.SelectMany(s => s.TrainingBookings.Select(b => b.ClientId))
                                .Concat(indSessions.Select(i => i.ClientId.Value))
                                .GroupBy(id => id)
                                .OrderByDescending(g => g.Count())
                                .FirstOrDefault();

            if (allBookings != null)
            {
                var topClient = await db.Clients.FindAsync(allBookings.Key);
                if (topClient != null)
                {
                    vm.TopClientName = $"{topClient.LastName} {topClient.FirstName}";
                    vm.TopClientVisits = allBookings.Count();
                }
            }

            var allIndSessions = await db.IndividualTrainings
                .Where(it => it.TrainingDate >= startDate && it.TrainingDate <= endDate && it.ClientId != null)
                .ToListAsync();

            var topTrainers = allIndSessions
                .GroupBy(it => it.TrainerId)
                .Select(g => new 
                { 
                    TrainerId = g.Key, 
                    Count = g.Count(),
                    Revenue = g.Sum(x => x.Price > 0 ? x.Price : 2000m)
                })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.Revenue)
                .ToList();

            int rank = topTrainers.FindIndex(x => x.TrainerId == _currentTrainer.TrainerId) + 1;
            
            if (rank > 0)
            {
                vm.RankText = $"{rank}-е место";
            }
            else
            {
                vm.RankText = "Вне рейтинга";
            }

            int daysInMonth = DateTime.DaysInMonth(year, month);
            for (int d = 1; d <= daysInMonth; d++)
            {
                var date = new DateTime(year, month, d);
                int groupClients = groupSessions.Where(s => s.TrainingDate == date).Sum(s => s.CurrentParticipants);
                int indClients = indSessions.Count(i => i.TrainingDate == date);
                int count = groupClients + indClients;
                
                vm.DailyActivity.Add(new ActivityBar 
                { 
                    Day = d.ToString(), 
                    CountText = count > 0 ? count.ToString() : "",
                    BarHeight = Math.Max(15, Math.Min(120, count * 15))
                });
            }

            DataContext = vm;
        }

        private async void ExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrainerStatsViewModel vm)
            {
                int month = MonthComboBox.SelectedIndex + 1;
                int year = DateTime.Today.Year;
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                using var db = new AppDbContext();
                
                var groups = await db.TrainingSchedules
                    .Where(s => s.TrainerId == _currentTrainer.TrainerId && s.TrainingDate >= startDate && s.TrainingDate <= endDate && s.CurrentParticipants > 0)
                    .Include(s => s.GroupTraining)
                    .ToListAsync();

                var individuals = await db.IndividualTrainings
                    .Where(it => it.TrainerId == _currentTrainer.TrainerId && it.TrainingDate >= startDate && it.TrainingDate <= endDate && it.ClientId != null)
                    .Include(it => it.Client)
                    .ToListAsync();

                var allEvents = new List<(DateTime Date, string Type, string Client, string Sum)>();

                foreach(var g in groups)
                {
                    allEvents.Add((g.TrainingDate, $"Групповая: {g.GroupTraining?.Name}", $"{g.CurrentParticipants} чел.", "—"));
                }

                foreach(var ind in individuals)
                {
                    decimal price = ind.Price > 0 ? ind.Price : (_currentTrainer.IndividualTrainingPrice > 0 ? _currentTrainer.IndividualTrainingPrice : 2000m);
                    allEvents.Add((ind.TrainingDate, "Индивидуальная", ind.Client?.FullName ?? "Неизвестно", $"{price:N0} ₽"));
                }

                allEvents = allEvents.OrderBy(x => x.Date).ToList();

                var sb = new StringBuilder();
                sb.Append("<html><head><meta charset='UTF-8'><style>");
                sb.Append("body{font-family:Arial,sans-serif;padding:40px;color:#2C3E50;line-height:1.6;}");
                sb.Append(".header{border-bottom:3px solid #FF8955;padding-bottom:20px;margin-bottom:30px;}");
                sb.Append("h1{color:#2C3E50;margin:0;font-size:28px;}");
                sb.Append("h2{color:#7F8C8D;margin:5px 0 15px 0;font-size:18px;font-weight:normal;}");
                sb.Append("table{width:100%;border-collapse:collapse;margin-top:20px;margin-bottom:40px;box-shadow:0 1px 3px rgba(0,0,0,0.1);}");
                sb.Append("th,td{border:1px solid #E0E6ED;padding:12px 15px;text-align:left;}");
                sb.Append("th{background:#F8F9FA;color:#2C3E50;font-weight:bold;text-transform:uppercase;font-size:13px;}");
                sb.Append("tr:nth-child(even){background-color:#FAFBFC;}");
                sb.Append(".summary{display:grid;grid-template-columns:repeat(3,1fr);gap:20px;margin-bottom:40px;}");
                sb.Append(".stat-card{background:#F8F9FA;padding:20px;border-radius:8px;border-left:4px solid #3498DB;}");
                sb.Append(".stat-title{font-size:12px;color:#7F8C8D;text-transform:uppercase;font-weight:bold;margin-bottom:5px;}");
                sb.Append(".stat-value{font-size:24px;color:#2C3E50;font-weight:bold;}");
                sb.Append(".footer{margin-top:50px;font-size:12px;color:#BDC3C7;text-align:right;border-top:1px solid #E0E6ED;padding-top:20px;}");
                sb.Append("</style></head><body>");

                sb.Append($"<div class='header'><h1>ОТЧЕТ О РАБОТЕ ТРЕНЕРА</h1><h2>{vm.Trainer.FullName} | {vm.Trainer.Specialization}</h2>");
                sb.Append($"<p><strong>Отчетный месяц:</strong> {MonthComboBox.SelectionBoxItem}</p></div>");

                sb.Append("<div class='summary'>");
                sb.Append($"<div class='stat-card'><div class='stat-title'>Проведено занятий</div><div class='stat-value'>{vm.TotalSessions}</div></div>");
                sb.Append($"<div class='stat-card' style='border-left-color:#27AE60;'><div class='stat-title'>Уникальных клиентов</div><div class='stat-value'>{vm.TotalClients}</div></div>");
                sb.Append($"<div class='stat-card' style='border-left-color:#E67E22;'><div class='stat-title'>Сумма выручки</div><div class='stat-value'>{vm.MonthlyRevenue:N0} ₽</div></div>");
                sb.Append("</div>");

                sb.Append("<h3>Детализация проведенных занятий</h3>");
                sb.Append("<table><tr><th>Дата</th><th>Тип / Название</th><th>Клиент(ы)</th><th>Сумма</th></tr>");

                if (allEvents.Any())
                {
                    foreach(var ev in allEvents)
                    {
                        sb.Append($"<tr><td>{ev.Date:dd.MM.yyyy}</td><td>{ev.Type}</td><td>{ev.Client}</td><td>{ev.Sum}</td></tr>");
                    }
                }
                else
                {
                    sb.Append("<tr><td colspan='4' style='text-align:center;padding:30px;color:#7F8C8D;'>Нет проведенных занятий за выбранный месяц</td></tr>");
                }
                
                sb.Append("</table>");
                sb.Append($"<div class='footer'>Сформировано учетной системой FitClub &bull; {DateTime.Now:dd.MM.yyyy HH:mm}</div>");
                sb.Append("</body></html>");

                var dialog = new Avalonia.Platform.Storage.FilePickerSaveOptions 
                { 
                    Title = "Сохранить отчет", 
                    SuggestedFileName = $"Отчет_{vm.Trainer.LastName}_{month}_{year}.html",
                    DefaultExtension = "html" 
                };
                
                var file = await this.StorageProvider.SaveFilePickerAsync(dialog);
                
                if (file != null) 
                {
                    await using var stream = await file.OpenWriteAsync();
                    using var writer = new System.IO.StreamWriter(stream, Encoding.UTF8);
                    await writer.WriteAsync(sb.ToString());
                }
            }
        }
    }

    public class TrainerStatsViewModel
    {
        public Models.Trainer Trainer { get; set; }
        public int TotalSessions { get; set; }
        public int TotalClients { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public double AvgLoad { get; set; }
        public string TopClientName { get; set; } = "Нет данных";
        public int TopClientVisits { get; set; }
        public string RankText { get; set; } = "Без рейтинга";
        public List<ActivityBar> DailyActivity { get; set; }
    }

    public class ActivityBar
    {
        public string Day { get; set; }
        public string CountText { get; set; }
        public int BarHeight { get; set; }
    }
}