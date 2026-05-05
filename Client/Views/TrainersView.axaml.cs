using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using System.Linq;
using FitClub.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using FitClub.Services;
using System.Threading.Tasks;
using FitClub.Views;

namespace FitClub.Client.Views
{
    public partial class TrainersView : UserControl
    {
        private AppDbContext _context;
        private TrainingService _trainingService;
        private Models.Client _currentClient;
        private List<Models.Trainer> _allTrainers;
        private string _searchText = "";

        public TrainersView()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _trainingService = new TrainingService(_context);
            
            LoadFilters();
            SpecFilterComboBox.SelectedIndex = 0;
            GroupFilterComboBox.SelectedIndex = 0;
            ScheduleFilterComboBox.SelectedIndex = 0;
            
            LoadCurrentClient();
            LoadClientSubscription();
            LoadTrainers();
        }

        private void LoadCurrentClient()
        {
            if (UserSession.IsLoggedIn)
            {
                _currentClient = _context.GetClientByEmail(UserSession.CurrentUserEmail);
            }
        }

        private void LoadClientSubscription()
        {
            if (_currentClient != null)
            {
                try
                {
                    _context.ChangeTracker.Clear();

                    var activeSubscription = _context.ClientSubscriptions
                        .Include(cs => cs.Tariff)
                        .Include(cs => cs.IndividualTrainer)
                        .Include(cs => cs.IndividualTrainingType)
                        .Where(cs => cs.ClientId == _currentClient.ClientId && cs.IsActive && cs.EndDate >= DateTime.Today && cs.IndividualRemainingVisits > 0)
                        .OrderByDescending(cs => cs.EndDate)
                        .FirstOrDefault();

                    if (activeSubscription != null && activeSubscription.IndividualTrainingTypeId.HasValue && activeSubscription.IndividualTrainer != null)
                    {
                        SubscriptionNameText.Text = activeSubscription.Tariff?.Name ?? "Абонемент на индивидуальные";
                        SubscriptionEndDateText.Text = activeSubscription.EndDate.ToString("dd.MM.yyyy");
                        SubscriptionRemainingText.Text = $"{activeSubscription.IndividualRemainingVisits} из {activeSubscription.IndividualTotalVisits}";
                        SubscriptionDirectionText.Text = activeSubscription.IndividualTrainingType?.Name ?? "Неизвестно";
                        SubscriptionTrainerText.Text = activeSubscription.IndividualTrainer.FullName;
                        
                        ActiveSubscriptionBorder.IsVisible = true;
                    }
                    else
                    {
                        ActiveSubscriptionBorder.IsVisible = false;
                    }
                }
                catch 
                {
                    ActiveSubscriptionBorder.IsVisible = false;
                }
            }
            else
            {
                ActiveSubscriptionBorder.IsVisible = false;
            }
        }

        private void LoadFilters()
        {
            var items = new List<ComboBoxItem> { new ComboBoxItem { Content = "Все" } };
            
            var trainings = _context.GroupTrainings.Where(gt => gt.IsActive).OrderBy(gt => gt.Name).ToList();
            foreach(var t in trainings)
            {
                items.Add(new ComboBoxItem { Content = t.Name });
            }
            
            GroupFilterComboBox.ItemsSource = items;
        }

        private void LoadTrainers()
        {
            try
            {
                _allTrainers = _context.Trainers.Where(t => t.IsActive).ToList();
                
                var trainingLinks = _context.TrainingTrainers
                    .Include(tt => tt.Training)
                    .ToList();

                foreach (var trainer in _allTrainers)
                {
                    var trainerTrainings = trainingLinks
                        .Where(tt => tt.TrainerId == trainer.TrainerId && tt.Training != null)
                        .Select(tt => tt.Training.Name)
                        .ToList();
                        
                    if (trainerTrainings.Any())
                    {
                        trainer.GroupTrainingsList = string.Join(", ", trainerTrainings);
                    }
                }

                ApplyFilter();
            }
            catch (Exception) { }
        }

        private void ApplyFilter()
        {
            if (_allTrainers == null) return;

            var filtered = _allTrainers.AsEnumerable();

            if (SpecFilterComboBox?.SelectedItem is ComboBoxItem specItem)
            {
                string selectedSpec = specItem.Content.ToString();
                if (selectedSpec != "Все")
                {
                    filtered = filtered.Where(t => t.Specialization != null && t.Specialization.Contains(selectedSpec, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (GroupFilterComboBox?.SelectedItem is ComboBoxItem groupItem)
            {
                string selectedGroup = groupItem.Content.ToString();
                if (selectedGroup != "Все")
                {
                    filtered = filtered.Where(t => t.GroupTrainingsList != null && t.GroupTrainingsList.Contains(selectedGroup, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (ScheduleFilterComboBox?.SelectedItem is ComboBoxItem schedItem)
            {
                string selectedSched = schedItem.Content.ToString();
                if (selectedSched == "Пн-Пт")
                {
                    filtered = filtered.Where(t => t.WorkSchedule == "mon-fri");
                }
                else if (selectedSched == "Ср-Вс")
                {
                    filtered = filtered.Where(t => t.WorkSchedule == "wed-sun");
                }
            }

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filtered = filtered.Where(t => t.FullName.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
            }

            var resultList = filtered.ToList();
            TrainersItemsControl.ItemsSource = resultList;
            
            if (NoTrainersBorder != null)
                NoTrainersBorder.IsVisible = !resultList.Any();
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (SearchTextBox != null)
            {
                _searchText = SearchTextBox.Text ?? "";
                ApplyFilter();
            }
        }

        public void SearchAndShowTrainer(string searchText)
        {
            _searchText = searchText;
            if (SearchTextBox != null)
            {
                SearchTextBox.Text = searchText;
            }
            ApplyFilter();
        }

        private async Task<bool> ValidateClientAsync()
        {
            if (_currentClient == null)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Для записи к тренеру необходимо авторизоваться в системе!",
                    ButtonEnum.Ok);
                await box.ShowWindowDialogAsync((Window)this.VisualRoot);
                return false;
            }

            if (!_currentClient.HasPassportData)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Паспортные данные не заполнены",
                    "Для записи к тренеру необходимо заполнить паспортные данные.\n\nПерейдите в раздел 'Мой аккаунт' для их добавления.",
                    ButtonEnum.Ok);
                await box.ShowWindowDialogAsync((Window)this.VisualRoot);
                return false;
            }

            return true;
        }

        private async void BookGroupTraining_Click(object sender, RoutedEventArgs e)
        {
            if (!await ValidateClientAsync()) return;

            if (sender is Button btn && btn.Tag is Models.Trainer trainer)
            {
                var window = new TrainerGroupTrainingBookingWindow((GroupTraining)null, trainer, _currentClient, _trainingService);
                await window.ShowDialog((Window)this.VisualRoot);
                LoadClientSubscription();
            }
        }

        private async void OpenTrainerCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Models.Trainer trainer)
            {
                var cardWindow = new TrainerCardWindow(trainer);
                await cardWindow.ShowDialog((Window)this.VisualRoot);
            }
        }

        private async void BookIndividualTraining_Click(object sender, RoutedEventArgs e)
        {
            if (!await ValidateClientAsync()) return;

            if (sender is Button btn && btn.Tag is Models.Trainer trainer)
            {
                var window = new IndividualTrainingBookingWindow(trainer, _currentClient, _trainingService);
                await window.ShowDialog((Window)this.VisualRoot);
                LoadClientSubscription();
            }
        }

        public void RefreshView()
        {
            LoadCurrentClient();
            LoadClientSubscription();
            LoadTrainers();
        }
    }
}