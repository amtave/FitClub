using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Platform.Storage;

namespace FitClub.Trainer.Views
{
    public class ClientBookingItem
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public bool IsSubscription { get; set; }
        public Bitmap AvatarBitmap { get; set; }
    }

    public partial class GroupTrainingDetailsWindow : Window
    {
        private readonly TrainingSchedule _schedule;
        private readonly AppDbContext _db;
        private List<ClientBookingItem> _clients;

        public GroupTrainingDetailsWindow(TrainingSchedule schedule)
        {
            InitializeComponent();
            _schedule = schedule;
            _db = new AppDbContext();
            
            LoadData();
        }

        private void LoadData()
        {
            var scheduleWithDetails = _db.TrainingSchedules.AsNoTracking()
                .Include(s => s.GroupTraining)
                .Include(s => s.TrainingBookings)
                .ThenInclude(b => b.Client)
                .FirstOrDefault(s => s.ScheduleId == _schedule.ScheduleId);

            if (scheduleWithDetails == null) return;

            TrainingTimeText.Text = scheduleWithDetails.TimeRangeFormatted;
            TrainingDateText.Text = scheduleWithDetails.TrainingDate.ToString("dd MMMM yyyy");
            TrainingNameText.Text = scheduleWithDetails.GroupTraining?.Name ?? "Тренировка";
            TrainingStatsText.Text = $"Записано: {scheduleWithDetails.CurrentParticipants} из {scheduleWithDetails.MaxParticipants}";

            _clients = new List<ClientBookingItem>();

            foreach (var booking in scheduleWithDetails.TrainingBookings)
            {
                bool isSub = _db.ClientSubscriptions.Any(cs => 
                    cs.ClientId == booking.ClientId && 
                    cs.IsActive && 
                    cs.SelectedTrainingTypeId == scheduleWithDetails.TrainingId && 
                    cs.SelectedTrainerId == scheduleWithDetails.TrainerId);

                Bitmap avatar = null;
                if (!string.IsNullOrEmpty(booking.Client.AvatarPath))
                {
                    try
                    {
                        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Avatars", booking.Client.AvatarPath);
                        if (File.Exists(fullPath))
                        {
                            avatar = new Bitmap(fullPath);
                        }
                    }
                    catch { }
                }

                _clients.Add(new ClientBookingItem
                {
                    FullName = booking.Client.FullName,
                    Phone = booking.Client.Phone,
                    Email = booking.Client.Email,
                    IsSubscription = isSub,
                    AvatarBitmap = avatar
                });
            }

            _clients = _clients.OrderBy(c => c.FullName).ToList();
            ClientsItemsControl.ItemsSource = _clients;
            NoClientsPanel.IsVisible = !_clients.Any();
        }

        private async void DownloadList_Click(object sender, RoutedEventArgs e)
        {
            if (_clients == null || !_clients.Any())
            {
                var noDataBox = MessageBoxManager.GetMessageBoxStandard("Пусто", "Нет записей для скачивания.", ButtonEnum.Ok);
                await noDataBox.ShowWindowDialogAsync(this);
                return;
            }

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить список участников",
                SuggestedFileName = $"Список_{_schedule.GroupTraining?.Name}_{_schedule.TrainingDate:yyyyMMdd}.txt",
                DefaultExtension = "txt",
                FileTypeChoices = new[] { new FilePickerFileType("Текстовый файл") { Patterns = new[] { "*.txt" } } }
            });

            if (file != null)
            {
                var sb = new StringBuilder();
                sb.AppendLine("================================================");
                sb.AppendLine($" ТРЕНИРОВКА: {_schedule.GroupTraining?.Name}");
                sb.AppendLine($" ДАТА: {_schedule.TrainingDate:dd.MM.yyyy}");
                sb.AppendLine($" ВРЕМЯ: {_schedule.TimeRangeFormatted}");
                sb.AppendLine($" УЧАСТНИКОВ: {_schedule.CurrentParticipants}/{_schedule.MaxParticipants}");
                sb.AppendLine("================================================");
                sb.AppendLine();

                int counter = 1;
                foreach (var c in _clients)
                {
                    sb.AppendLine($"{counter}. {c.FullName}");
                    sb.AppendLine($"   Телефон: {c.Phone}");
                    sb.AppendLine($"   Email: {c.Email}");
                    sb.AppendLine($"   Тип оплаты: {(c.IsSubscription ? "По абонементу" : "Разовая оплата")}");
                    sb.AppendLine("------------------------------------------------");
                    counter++;
                }

                await using var stream = await file.OpenWriteAsync();
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteAsync(sb.ToString());

                var box = MessageBoxManager.GetMessageBoxStandard("Успех", "Список успешно сохранен!", ButtonEnum.Ok);
                await box.ShowWindowDialogAsync(this);
            }
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}