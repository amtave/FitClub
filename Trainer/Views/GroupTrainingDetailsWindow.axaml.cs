using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Avalonia.Media.Imaging;
using System.IO;
using System;
using Avalonia.Media;

namespace FitClub.Trainer.Views
{
    public partial class GroupTrainingDetailsWindow : Window
    {
        private readonly TrainingSchedule _schedule;
        private readonly Models.Trainer _trainer;
        private readonly AppDbContext _context;

        public GroupTrainingDetailsWindow(TrainingSchedule schedule, Models.Trainer trainer)
        {
            InitializeComponent();
            
            _schedule = schedule;
            _trainer = trainer;
            _context = new AppDbContext();

            // Устанавливаем позиционирование по центру экрана
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            try
            {
                LoadTrainingDetails();
                LoadParticipants();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка инициализации окна: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadTrainingDetails()
        {
            try
            {
                // Загружаем детали тренировки с включением всех связанных данных
                var training = _context.TrainingSchedules
                    .Include(ts => ts.GroupTraining)
                        .ThenInclude(gt => gt.TrainingType)
                    .Include(ts => ts.GroupTraining)
                        .ThenInclude(gt => gt.IntensityLevel)
                    .Include(ts => ts.Trainer)
                    .FirstOrDefault(ts => ts.ScheduleId == _schedule.ScheduleId);

                if (training?.GroupTraining != null)
                {
                    // Устанавливаем значения
                    var trainingName = this.FindControl<TextBlock>("TrainingName");
                    var dateAndTime = this.FindControl<TextBlock>("DateAndTime");
                    var duration = this.FindControl<TextBlock>("Duration");
                    var participantsInfo = this.FindControl<TextBlock>("ParticipantsInfo");
                    var intensityLevel = this.FindControl<TextBlock>("IntensityLevel");

                    if (trainingName != null) 
                        trainingName.Text = training.GroupTraining.Name;
                    
                    if (dateAndTime != null) 
                        dateAndTime.Text = $"{training.TrainingDate:dd.MM.yyyy} {training.TrainingTime:hh\\:mm}";
                    
                    if (duration != null) 
                        duration.Text = $"{training.GroupTraining.DurationMinutes} мин";
                    
                    if (participantsInfo != null) 
                        participantsInfo.Text = $"{training.CurrentParticipants}/{training.MaxParticipants}";
                    
                    if (intensityLevel != null) 
                        intensityLevel.Text = training.GroupTraining.IntensityLevel?.Name ?? "Не указана";
                }
                else
                {
                    ShowErrorMessage("Тренировка не найдена в базе данных");
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка загрузки деталей тренировки: {ex.Message}");
            }
        }

        private void LoadParticipants()
{
    try
    {
        var bookings = _context.TrainingBookings
            .Include(tb => tb.Client)
            .Where(tb => tb.ScheduleId == _schedule.ScheduleId && tb.Status == "confirmed")
            .ToList();

        // ДОБАВЬТЕ ОТЛАДОЧНУЮ ИНФОРМАЦИЮ
        foreach (var booking in bookings)
        {
            System.Diagnostics.Debug.WriteLine($"Клиент: {booking.Client?.LastName} {booking.Client?.FirstName} {booking.Client?.MiddleName}");
            System.Diagnostics.Debug.WriteLine($"FullName: {booking.Client?.FullName}");
        }

        // Добавляем аватарки клиентам
        foreach (var booking in bookings)
        {
            booking.Client.AvatarBitmap = LoadClientAvatar(booking.Client);
        }

        var participantsControl = this.FindControl<ItemsControl>("ParticipantsItemsControl");
        var noParticipantsMessage = this.FindControl<TextBlock>("NoParticipantsMessage");

        if (participantsControl != null && noParticipantsMessage != null)
        {
            participantsControl.ItemsSource = bookings;
            noParticipantsMessage.IsVisible = !bookings.Any();
        }
    }
    catch (Exception ex)
    {
        ShowErrorMessage($"Ошибка загрузки участников: {ex.Message}");
    }
}

        private Bitmap LoadClientAvatar(FitClub.Models.Client client)
        {
            try
            {
                if (!string.IsNullOrEmpty(client.AvatarPath))
                {
                    string avatarsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Avatars");
                    string avatarPath = Path.Combine(avatarsFolder, client.AvatarPath);
                    
                    if (File.Exists(avatarPath))
                    {
                        return new Bitmap(avatarPath);
                    }
                }
            }
            catch (Exception ex)
            {
                // Не показываем ошибку для каждой аватарки, чтобы не заспамить
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки аватарки клиента: {ex.Message}");
            }

            // Загружаем аватарку по умолчанию
            return LoadDefaultAvatar();
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

        private void ShowErrorMessage(string message)
        {
            try
            {
                var messageBox = new Window
                {
                    Title = "Ошибка",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    CanResize = false
                };

                var stackPanel = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 10
                };

                var textBlock = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                var button = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Width = 80
                };

                button.Click += (s, e) => messageBox.Close();

                stackPanel.Children.Add(textBlock);
                stackPanel.Children.Add(button);

                messageBox.Content = stackPanel;

                messageBox.Show();
            }
            catch
            {
                // Если даже MessageBox не работает, просто игнорируем
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) 
        {
            this.Close();
        }
    }
}