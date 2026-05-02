using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FitClub.Models;
using FitClub.Services;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform.Storage;
using System.IO;
using Avalonia.Controls.Primitives;
using Avalonia;

namespace FitClub.Views
{
    public class WorkoutDisplayItem
    {
        public int? BookingId { get; set; }
        public int? IndividualTrainingId { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public DateTime FullDateTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string TypeName { get; set; }
        public string Icon { get; set; }
        public IBrush TypeColor { get; set; }
        public object OriginalObject { get; set; }
        public string WorkoutType { get; set; }
        public int? Rating { get; set; }
        public string Review { get; set; }
        public decimal Price { get; set; }
        public bool IsSubscriptionBooking => Price == 0;

        public string DateTimeRangeString => WorkoutType == "Visit" 
            ? FullDateTime.ToString("dd MMMM") 
            : $"{FullDateTime:dd MMMM, HH:mm} - {FullDateTime.Add(Duration):HH:mm}";

        public bool IsRated => Rating.HasValue && Rating.Value > 0;
        public string RatingText => IsRated ? $"Ваша оценка: {Rating} ★" : "";
        public string ReviewText => IsRated && !string.IsNullOrWhiteSpace(Review) ? $"«{Review}»" : "";
    }

    public partial class MyWorkoutsView : UserControl
    {
        private readonly AppDbContext _context;
        private Models.Client _currentClient;

        public MyWorkoutsView()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadCurrentClient();
            LoadAllWorkouts();
        }

        public void RefreshView()
        {
            LoadCurrentClient();
            LoadAllWorkouts();
        }

        private void LoadCurrentClient()
        {
            if (UserSession.IsLoggedIn)
            {
                _currentClient = _context.GetClientByEmail(UserSession.CurrentUserEmail);
            }
        }

        private void LoadAllWorkouts()
        {
            if (_currentClient == null) return;

            var allItems = new List<WorkoutDisplayItem>();
            var now = DateTime.Now;

            var groupBookings = _context.TrainingBookings
                .AsNoTracking()
                .Include(b => b.TrainingSchedule).ThenInclude(s => s.GroupTraining)
                .Include(b => b.TrainingSchedule).ThenInclude(s => s.Trainer)
                .Where(b => b.ClientId == _currentClient.ClientId).ToList();

            foreach (var b in groupBookings)
            {
                var dt = b.TrainingSchedule.TrainingDate.Date.Add(b.TrainingSchedule.TrainingTime);
                
                bool isSubscription = _context.ClientSubscriptions.Any(cs => 
                    cs.ClientId == _currentClient.ClientId && 
                    cs.IsActive && 
                    cs.SelectedTrainingTypeId == b.TrainingSchedule.TrainingId && 
                    cs.SelectedTrainerId == b.TrainingSchedule.TrainerId);

                allItems.Add(new WorkoutDisplayItem
                {
                    BookingId = b.BookingId,
                    Title = b.TrainingSchedule.GroupTraining.Name,
                    Subtitle = b.TrainingSchedule.Trainer.FullName,
                    FullDateTime = dt,
                    Duration = TimeSpan.FromMinutes(b.TrainingSchedule.GroupTraining.DurationMinutes),
                    TypeName = "ГРУППОВАЯ",
                    Icon = "👥",
                    TypeColor = Brush.Parse("#FEF5EE"),
                    OriginalObject = b,
                    WorkoutType = "Group",
                    Rating = b.Rating,
                    Review = b.Review,
                    Price = isSubscription ? 0 : b.TrainingSchedule.GroupTraining.Price 
                });
            }

            var individualTrainings = _context.IndividualTrainings
                .AsNoTracking()
                .Include(it => it.Trainer)
                .Where(it => it.ClientId == _currentClient.ClientId && it.IsActive).ToList();

            foreach (var it in individualTrainings)
            {
                var dt = it.TrainingDate.Date.Add(it.StartTime);
                allItems.Add(new WorkoutDisplayItem
                {
                    IndividualTrainingId = it.IndividualTrainingId,
                    Title = "Индивидуальное занятие",
                    Subtitle = it.Trainer.FullName,
                    FullDateTime = dt,
                    Duration = it.EndTime - it.StartTime,
                    TypeName = "ЛИЧНАЯ",
                    Icon = "⚡",
                    TypeColor = Brush.Parse("#EBF5FB"),
                    OriginalObject = it,
                    WorkoutType = "Individual",
                    Rating = it.Rating,
                    Review = it.Review,
                    Price = it.Price 
                });
            }

            var singleVisits = _context.SingleGymVisits
                .AsNoTracking()
                .Where(sv => sv.ClientId == _currentClient.ClientId).ToList();
                
            foreach (var sv in singleVisits)
            {
                allItems.Add(new WorkoutDisplayItem
                {
                    Title = "Разовое посещение зала",
                    Subtitle = "Тренажерный зал",
                    FullDateTime = sv.VisitDate,
                    Duration = TimeSpan.Zero,
                    TypeName = "ВИЗИТ",
                    Icon = "🏋️",
                    TypeColor = Brush.Parse("#EAFaf1"),
                    OriginalObject = sv,
                    WorkoutType = "Visit",
                    Price = sv.Price 
                });
            }

            UpcomingWorkoutsControl.ItemsSource = allItems.Where(i => i.FullDateTime >= now).OrderBy(i => i.FullDateTime).ToList();
            PastWorkoutsControl.ItemsSource = allItems.Where(i => i.FullDateTime < now).OrderByDescending(i => i.FullDateTime).ToList();
            NoUpcomingText.IsVisible = !allItems.Any(i => i.FullDateTime >= now);
        }

        private async void ShowQRCode_Click(object sender, RoutedEventArgs e)
        {
            var qrWindow = new Window
            {
                Title = "Ваш пропуск",
                Width = 350,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Background = Brush.Parse("#F8F9FA")
            };

            var mainStack = new StackPanel { Spacing = 20, Margin = new Thickness(30), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            
            var title = new TextBlock 
            { 
                Text = "QR-КОД ДЛЯ ВХОДА", 
                FontSize = 18, 
                FontWeight = FontWeight.ExtraBold, 
                Foreground = Brush.Parse("#2C3E50"), 
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center 
            };
            
            var qrBorder = new Border 
            { 
                Background = Brushes.White, 
                CornerRadius = new CornerRadius(16), 
                Padding = new Thickness(20), 
                BoxShadow = new BoxShadows(new BoxShadow { Blur = 15, Color = Color.Parse("#15000000"), OffsetY = 5 }) 
            };

            var qrGrid = new UniformGrid { Columns = 15, Rows = 15, Width = 200, Height = 200, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            var rand = new Random();

            for (int r = 0; r < 15; r++)
            {
                for (int c = 0; c < 15; c++)
                {
                    bool isFinderPattern = (r < 4 && c < 4) || (r < 4 && c > 10) || (r > 10 && c < 4);
                    bool isBlack = isFinderPattern ? (r == 0 || r == 3 || c == 0 || c == 3 || (r > 0 && r < 3 && c > 0 && c < 3) || (r == 1 && c == 1) || (r == 2 && c == 2) || (r == 1 && c == 2) || (r == 2 && c == 1)) : rand.NextDouble() > 0.5;
                    qrGrid.Children.Add(new Border { Background = isBlack ? Brushes.Black : Brushes.White });
                }
            }

            qrBorder.Child = qrGrid;

            var subtitle = new TextBlock 
            { 
                Text = "Приложите экран к сканеру на турникете", 
                FontSize = 13, 
                Foreground = Brush.Parse("#7F8C8D"), 
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center 
            };

            var closeBtn = new Button 
            { 
                Content = "Закрыть", 
                Background = Brush.Parse("#2C3E50"), 
                Foreground = Brushes.White, 
                FontWeight = FontWeight.Bold, 
                CornerRadius = new CornerRadius(8), 
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            closeBtn.Click += (s, ev) => qrWindow.Close();

            mainStack.Children.Add(title);
            mainStack.Children.Add(qrBorder);
            mainStack.Children.Add(subtitle);
            mainStack.Children.Add(closeBtn);
            
            qrWindow.Content = mainStack;
            
            await qrWindow.ShowDialog((Window)this.VisualRoot);
        }

        private async void DownloadReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is WorkoutDisplayItem item)
            {
                if (item.Price == 0)
                {
                    var noReceiptBox = MessageBoxManager.GetMessageBoxStandard("Чек не требуется", "Эта тренировка была забронирована по абонементу (бесплатно), чек для нее не формируется.", ButtonEnum.Ok);
                    await noReceiptBox.ShowWindowDialogAsync((Window)this.VisualRoot);
                    return;
                }

                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Сохранить чек об оплате",
                    DefaultExtension = "txt",
                    SuggestedFileName = $"Чек_{item.TypeName}_{item.FullDateTime:yyyyMMdd}.txt",
                    FileTypeChoices = new[] { new FilePickerFileType("Текстовый файл") { Patterns = new[] { "*.txt" } } }
                });

                if (file != null)
                {
                    decimal tax = item.Price * 0.2m;
                    string receiptText = 
                        "------------------------------------------------\n" +
                        "                  КАССОВЫЙ ЧЕК                  \n" +
                        "                    ПРИХОД                      \n" +
                        "------------------------------------------------\n" +
                        "ООО \"FITCLUB\"\n" +
                        "ИНН: 7712345678\n" +
                        "СНО: ОСН\n" +
                        "Адрес: г. Москва, ул. Фитнесная, д. 15\n" +
                        "Место расчетов: Фитнес-клуб \"FitClub\"\n\n" +
                        $"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}\n" +
                        "Кассир: Администратор\n" +
                        "Смена: 142\n" +
                        $"Чек №: {new Random().Next(1000, 9999)}\n" +
                        "------------------------------------------------\n" +
                        "НАИМЕНОВАНИЕ ТОВАРА / УСЛУГИ\n" +
                        $"{item.Title} ({item.TypeName})\n" +
                        $"Исполнитель: {item.Subtitle}\n" +
                        $"Дата занятия: {item.DateTimeRangeString}\n" +
                        "------------------------------------------------\n" +
                        $"ИТОГО:                               {item.Price:N2} ₽\n" +
                        $"В Т.Ч. НДС 20%:                      {tax:N2} ₽\n" +
                        "ПОЛУЧЕНО:\n" +
                        $"БЕЗНАЛИЧНЫМИ:                        {item.Price:N2} ₽\n" +
                        "------------------------------------------------\n" +
                        "ФД: 1234567890\n" +
                        "ФПД: 0987654321\n" +
                        "РН ККТ: 1234567890123456\n" +
                        "ЗН ККТ: 01234567890123\n" +
                        "Сайт ФНС: www.nalog.gov.ru\n" +
                        "------------------------------------------------\n" +
                        "                                                \n" +
                        "                █▀▀▀▀▀█ ▀█ ▄ █▀▀▀▀▀█            \n" +
                        "                █ ███ █ ▄▀█▄ █ ███ █            \n" +
                        "                █ ▀▀▀ █ █ ▀▄ █ ▀▀▀ █            \n" +
                        "                ▀▀▀▀▀▀▀ ▀ █ ▀ ▀▀▀▀▀▀            \n" +
                        "                █▄ ▀█▄▀▄▀▄▀▀▄▄█▄▀█▀▀            \n" +
                        "                █▀ ▄▄ ▀▀▀▄ ▄▀▄ ▄▄ █▄            \n" +
                        "                ▀ ▀▀▀ ▀▀▀▀▀▀  ▀  ▀▀▀            \n" +
                        "                █▀▀▀▀▀█ ▄▀▀▀▄█▄█ █▀█            \n" +
                        "                █ ███ █ █ ▄▀ ▄ █▄ █▄            \n" +
                        "                █ ▀▀▀ █ ▄▀ █▀▀▄▀▀█ ▄            \n" +
                        "                ▀▀▀▀▀▀▀ ▀▀▀▀ ▀▀ ▀▀▀▀            \n" +
                        "                                                \n" +
                        "        СПАСИБО, ЧТО ВЫБИРАЕТЕ FITCLUB!         \n";

                    await using var stream = await file.OpenWriteAsync();
                    using var writer = new StreamWriter(stream);
                    await writer.WriteAsync(receiptText);

                    var box = MessageBoxManager.GetMessageBoxStandard("Успех", "Электронный чек успешно сохранен!", ButtonEnum.Ok);
                    await box.ShowWindowDialogAsync((Window)this.VisualRoot);
                }
            }
        }

        private async void RateWorkout_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is WorkoutDisplayItem item)
            {
                var ratingWindow = new WorkoutRatingWindow(item);
                var result = await ratingWindow.ShowDialog<bool>((Window)this.VisualRoot);
                if (result) LoadAllWorkouts();
            }
        }

        private async void CancelWorkout_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is WorkoutDisplayItem item)
            {
                var confirm = await MessageBoxManager.GetMessageBoxStandard("Подтверждение", $"Вы действительно хотите отменить запись на {item.Title}?", ButtonEnum.YesNo).ShowWindowDialogAsync((Window)this.VisualRoot);
                if (confirm == ButtonResult.Yes)
                {
                    try
                    {
                        using var db = new AppDbContext();
                        
                        if (item.WorkoutType == "Group" && item.OriginalObject is TrainingBooking gb)
                        {
                            var b = db.TrainingBookings.Find(gb.BookingId);
                            var s = db.TrainingSchedules.Find(gb.ScheduleId);
                            
                            if (s != null && s.CurrentParticipants > 0) s.CurrentParticipants--;
                            if (b != null)
                            {
                                if (item.Price == 0) 
                                {
                                    var activeSub = db.ClientSubscriptions.FirstOrDefault(cs => cs.ClientId == _currentClient.ClientId && cs.IsActive && cs.EndDate >= DateTime.Today);
                                    if (activeSub != null && activeSub.GroupRemainingVisits.HasValue)
                                    {
                                        activeSub.GroupRemainingVisits++;
                                        db.ClientSubscriptions.Update(activeSub);
                                    }
                                }
                                db.TrainingBookings.Remove(b);
                            }
                        }
                        else if (item.WorkoutType == "Individual" && item.OriginalObject is IndividualTraining it)
                        {
                            var t = db.IndividualTrainings.Include(x => x.Trainer).FirstOrDefault(x => x.IndividualTrainingId == it.IndividualTrainingId);
                            if (t != null) 
                            { 
                                if (item.Price == 0)
                                {
                                    var activeSub = db.ClientSubscriptions.FirstOrDefault(cs => cs.ClientId == _currentClient.ClientId && cs.IsActive && cs.EndDate >= DateTime.Today);
                                    if (activeSub != null && activeSub.IndividualRemainingVisits.HasValue)
                                    {
                                        activeSub.IndividualRemainingVisits++;
                                        db.ClientSubscriptions.Update(activeSub);
                                    }
                                }
                                
                                t.ClientId = null; 
                                t.Price = t.Trainer?.IndividualTrainingPrice ?? 2000m; 
                            }
                        }
                        else if (item.WorkoutType == "Visit" && item.OriginalObject is SingleGymVisit sv)
                        {
                            var v = db.SingleGymVisits.Find(sv.VisitId);
                            if (v != null) db.SingleGymVisits.Remove(v);
                        }
                        
                        await db.SaveChangesAsync();
                        LoadAllWorkouts();
                    }
                    catch { }
                }
            }
        }
    }
}