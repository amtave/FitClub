using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FitClub.Trainer.Views
{
    public class TrainerClientViewModel
    {
        public Models.Client Client { get; set; }
        public string ClientFullName { get; set; }
        public string ClientPhone { get; set; }
        public string ClientEmail { get; set; }
        public Bitmap AvatarBitmap { get; set; }
        public string TariffName { get; set; }
        public string ExpirationText { get; set; }
        public IBrush ExpirationColor { get; set; }
        public bool ShowGroup { get; set; }
        public string GroupVisitsText { get; set; }
        public bool ShowInd { get; set; }
        public string IndVisitsText { get; set; }
        public bool NeedsPlan { get; set; }
        public string PlanButtonText => NeedsPlan ? "Создать план" : "План тренировок";
    }

    public partial class TrainerClientsView : UserControl
    {
        private readonly AppDbContext _db;
        private Models.Trainer _currentTrainer;
        private List<TrainerClientViewModel> _allClients;

        public TrainerClientsView()
        {
            InitializeComponent();
            _db = new AppDbContext();
            LoadCurrentTrainer();
        }

        public void RefreshView()
        {
            LoadCurrentTrainer();
        }

        private void LoadCurrentTrainer()
        {
            if (UserSession.IsLoggedIn)
            {
                var user = _db.Users.FirstOrDefault(u => u.Email == UserSession.CurrentUserEmail);
                if (user != null)
                {
                    _currentTrainer = _db.Trainers.FirstOrDefault(t => t.Email == user.Email);
                }
            }

            LoadClientsData();
        }

        private void LoadClientsData()
        {
            if (_currentTrainer == null) return;

            var activeSubs = _db.ClientSubscriptions.AsNoTracking()
                .Include(cs => cs.Client)
                .Include(cs => cs.Tariff)
                .Where(cs => cs.IsActive && cs.EndDate >= DateTime.Today &&
                            (cs.SelectedTrainerId == _currentTrainer.TrainerId || 
                             cs.IndividualTrainerId == _currentTrainer.TrainerId))
                .ToList();

            var clientPlans = _db.TrainingPlans.AsNoTracking()
                .Where(tp => tp.TrainerId == _currentTrainer.TrainerId && tp.IsActive)
                .Select(tp => tp.ClientId)
                .ToList();

            _allClients = new List<TrainerClientViewModel>();

            foreach (var sub in activeSubs)
            {
                bool isGroupLinked = sub.SelectedTrainerId == _currentTrainer.TrainerId && sub.GroupRemainingVisits > 0;
                bool isIndLinked = sub.IndividualTrainerId == _currentTrainer.TrainerId && sub.IndividualRemainingVisits > 0;

                if (!isGroupLinked && !isIndLinked) continue;

                var client = sub.Client;
                if (client == null) continue;

                int daysLeft = (sub.EndDate - DateTime.Today).Days;
                IBrush expColor = daysLeft <= 3 ? Brush.Parse("#E74C3C") : Brush.Parse("#7F8C8D");
                string expText = daysLeft <= 3 ? $"Истекает через {daysLeft} дн. ({sub.EndDate:dd.MM.yyyy})" : $"Действует до: {sub.EndDate:dd.MM.yyyy}";

                bool hasPlan = clientPlans.Contains(client.ClientId);

                _allClients.Add(new TrainerClientViewModel
                {
                    Client = client,
                    ClientFullName = $"{client.LastName} {client.FirstName} {client.MiddleName}".Trim(),
                    ClientPhone = client.Phone,
                    ClientEmail = client.Email,
                    AvatarBitmap = GetClientAvatar(client.AvatarPath),
                    TariffName = sub.Tariff?.Name ?? "Абонемент",
                    ExpirationText = expText,
                    ExpirationColor = expColor,
                    ShowGroup = isGroupLinked,
                    GroupVisitsText = $"Групповые: {sub.GroupRemainingVisits} шт",
                    ShowInd = isIndLinked,
                    IndVisitsText = $"Индив.: {sub.IndividualRemainingVisits} шт",
                    NeedsPlan = !hasPlan
                });
            }

            _allClients = _allClients.OrderBy(c => c.ClientFullName).ToList();
            ApplyFilter();
        }

        private Bitmap GetClientAvatar(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Avatars", path);
                    if (File.Exists(fullPath))
                    {
                        return new Bitmap(fullPath);
                    }
                }
                catch { }
            }

            try
            {
                var resourceUri = new Uri("avares://FitClub/Assets/default_avatar.png");
                using var stream = Avalonia.Platform.AssetLoader.Open(resourceUri);
                return new Bitmap(stream);
            }
            catch 
            { 
                return null; 
            }
        }

        private void ApplyFilter()
        {
            if (_allClients == null) return;

            var filtered = _allClients.AsEnumerable();

            string searchText = SearchTextBox.Text?.ToLower() ?? "";
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = filtered.Where(c => c.ClientFullName.ToLower().Contains(searchText));
            }

            if (PlanFilterComboBox?.SelectedItem is ComboBoxItem planItem)
            {
                string plan = planItem.Content.ToString();
                if (plan == "Есть")
                {
                    filtered = filtered.Where(c => !c.NeedsPlan);
                }
                else if (plan == "Нет")
                {
                    filtered = filtered.Where(c => c.NeedsPlan);
                }
            }

            if (SubFilterComboBox?.SelectedItem is ComboBoxItem subItem)
            {
                string sub = subItem.Content.ToString();
                if (sub == "Индивидуальные")
                {
                    filtered = filtered.Where(c => c.ShowInd);
                }
                else if (sub == "Групповые")
                {
                    filtered = filtered.Where(c => c.ShowGroup);
                }
            }

            var resultList = filtered.ToList();
            ClientsItemsControl.ItemsSource = resultList;
            
            if (NoClientsPanel != null)
            {
                NoClientsPanel.IsVisible = !resultList.Any();
            }
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            ApplyFilter();
        }

        private async void OpenPlanWindow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TrainerClientViewModel item)
            {
                if (item.NeedsPlan)
                {
                    var createWindow = new CreateTrainingPlanWindow(_currentTrainer, item.Client);
                    await createWindow.ShowDialog((Window)this.VisualRoot);
                }
                else
                {
                    var plan = _db.TrainingPlans.FirstOrDefault(tp => tp.ClientId == item.Client.ClientId && tp.IsActive);
                    if (plan != null)
                    {
                        var viewWindow = new ViewTrainingPlanWindow(plan);
                        await viewWindow.ShowDialog((Window)this.VisualRoot);
                    }
                }
                
                LoadClientsData(); 
            }
        }
    }
}