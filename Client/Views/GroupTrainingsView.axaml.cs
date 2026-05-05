using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Linq;
using FitClub.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using FitClub.Services;
using FitClub.Client;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FitClub.Views
{
    public partial class GroupTrainingsView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly TrainingService _trainingService;
        private Models.Client _currentClient;
        private string _currentFilter = "Все";
        private string _searchText = "";
        private List<GroupTraining> _allTrainings;

        public GroupTrainingsView()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _trainingService = new TrainingService(_context);
            IntensityComboBox.SelectedIndex = 0;
            LoadCurrentClient();
            LoadClientSubscription();
            LoadGroupTrainings();
            UpdatePassportWarning();
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
                        .Include(cs => cs.SelectedTrainer)
                        .Where(cs => cs.ClientId == _currentClient.ClientId && cs.IsActive && cs.EndDate >= DateTime.Today && cs.GroupRemainingVisits > 0)
                        .OrderByDescending(cs => cs.EndDate)
                        .FirstOrDefault();

                    if (activeSubscription != null && activeSubscription.SelectedTrainingTypeId.HasValue && activeSubscription.SelectedTrainer != null)
                    {
                        var groupTraining = _context.GroupTrainings.Find(activeSubscription.SelectedTrainingTypeId.Value);
                        
                        SubscriptionNameText.Text = activeSubscription.Tariff?.Name ?? "Абонемент на групповые";
                        SubscriptionEndDateText.Text = activeSubscription.EndDate.ToString("dd.MM.yyyy");
                        SubscriptionRemainingText.Text = $"{activeSubscription.GroupRemainingVisits} из {activeSubscription.GroupTotalVisits}";
                        SubscriptionDirectionText.Text = groupTraining?.Name ?? "Неизвестно";
                        SubscriptionTrainerText.Text = activeSubscription.SelectedTrainer.FullName;
                        
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

        private void LoadGroupTrainings()
        {
            _allTrainings = _trainingService.GetActiveGroupTrainings();

            foreach (var training in _allTrainings)
            {
                training.AvailableTrainers = _trainingService.GetTrainersForTraining(training.TrainingId);
            }

            ApplyFilters();
        }

        private void UpdatePassportWarning()
        {
            if (_currentClient != null)
            {
                PassportWarningBorder.IsVisible = !_currentClient.HasPassportData;
            }
            else
            {
                PassportWarningBorder.IsVisible = false;
            }
        }

        private void TrainerName_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is TextBlock textBlock)
            {
                if (textBlock.Tag == null)
                {
                    return;
                }

                if (textBlock.Tag is Models.Trainer trainer)
                {
                    if (this.VisualRoot is Menu_client menuClient)
                    {
                        menuClient.NavigateToTrainersView(trainer.FullName);
                    }
                }
            }
        }

        private void IntensityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IntensityComboBox?.SelectedItem is ComboBoxItem item)
            {
                _currentFilter = item.Content.ToString();
                ApplyFilters();
            }
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            _searchText = SearchTextBox.Text ?? "";
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_allTrainings == null) return;

            var filtered = _allTrainings.AsEnumerable();

            if (!string.IsNullOrEmpty(_currentFilter) && _currentFilter != "Все")
            {
                filtered = filtered.Where(t => t.IntensityLevel?.Name == _currentFilter);
            }

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filtered = filtered.Where(t => 
                    (t.Name != null && t.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (t.AvailableTrainers != null && t.AvailableTrainers.Any(tr => tr.FullName != null && tr.FullName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)))
                );
            }

            TrainingsItemsControl.ItemsSource = filtered.ToList();
        }

        private async void BookTrainingButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentClient == null)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Для записи на тренировку необходимо авторизоваться в системе!",
                    ButtonEnum.Ok);
                await box.ShowWindowDialogAsync((Window)this.VisualRoot);
                return;
            }

            if (!_currentClient.HasPassportData)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Паспортные данные не заполнены",
                    "Для записи на групповые тренировки необходимо заполнить паспортные данные.\n\nПерейдите в раздел 'Мой аккаунт' для их добавления.",
                    ButtonEnum.Ok);
                await box.ShowWindowDialogAsync((Window)this.VisualRoot);
                return;
            }

            if (sender is Button button && button.Tag is GroupTraining training)
            {
                var bookingWindow = new TrainingBookingWindow(training, _currentClient, _trainingService);
                await bookingWindow.ShowDialog((Window)this.VisualRoot);
                LoadClientSubscription();
            }
        }

        public void RefreshView()
        {
            LoadCurrentClient();
            LoadClientSubscription();
            LoadGroupTrainings();
            UpdatePassportWarning();
        }
    }
}