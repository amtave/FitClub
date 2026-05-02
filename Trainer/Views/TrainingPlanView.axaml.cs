using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;

namespace FitClub.Trainer.Views
{
    public partial class TrainingPlanView : UserControl
    {
        private readonly AppDbContext _context;
        private Models.Trainer _currentTrainer;
        private List<ClientSubscription> _clientsWithoutPlans;
        private List<ClientSubscription> _filteredClientsWithoutPlans;
        private List<TrainingPlan> _existingPlans;
        private List<TrainingPlan> _filteredExistingPlans;

        public TrainingPlanView()
        {
            InitializeComponent();
            _context = new AppDbContext();

            LoadCurrentTrainer();
            LoadData();
        }

        private void LoadCurrentTrainer()
        {
            if (UserSession.IsLoggedIn && UserSession.CurrentUserRole == "Trainer")
            {
                _currentTrainer = _context.Trainers
                    .FirstOrDefault(t => t.Email == UserSession.CurrentUserEmail && t.IsActive);
            }

            if (_currentTrainer == null)
            {
                _currentTrainer = _context.Trainers.FirstOrDefault(t => t.IsActive);
            }
        }

        private void LoadData()
        {
            if (_currentTrainer == null) return;

            try
            {
                // Загружаем клиентов с индивидуальными абонементами этого тренера
                var individualSubscriptions = _context.ClientSubscriptions
                    .Include(cs => cs.Client)
                    .Include(cs => cs.Tariff)
                    .Include(cs => cs.IndividualTrainer)
                    .Include(cs => cs.SelectedTrainer)
                    .Where(cs => cs.IsActive && 
                                cs.EndDate >= DateTime.Today &&
                                (cs.VisitsType == "individual" || 
                                 cs.IndividualTotalVisits.HasValue) &&
                                (cs.IndividualTrainerId == _currentTrainer.TrainerId ||
                                 cs.SelectedTrainerId == _currentTrainer.TrainerId))
                    .ToList();

                // Загружаем существующие планы
                _existingPlans = _context.TrainingPlans
                    .Include(tp => tp.Client)
                    .Include(tp => tp.Goal)
                    .Include(tp => tp.TrainingPlanDays)
                    .Where(tp => tp.TrainerId == _currentTrainer.TrainerId && tp.IsActive)
                    .ToList();

                // Определяем клиентов без планов
                var clientsWithPlans = _existingPlans.Select(p => p.ClientId).ToHashSet();
                _clientsWithoutPlans = individualSubscriptions
                    .Where(cs => !clientsWithPlans.Contains(cs.ClientId))
                    .ToList();

                // Загружаем аватары
                LoadAvatars();

                UpdateDisplay();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadAvatars()
        {
            foreach (var subscription in _clientsWithoutPlans)
            {
                if (subscription.Client != null)
                {
                    subscription.Client.AvatarBitmap = LoadClientAvatar(subscription.Client);
                }
            }

            foreach (var plan in _existingPlans)
            {
                if (plan.Client != null)
                {
                    plan.Client.AvatarBitmap = LoadClientAvatar(plan.Client);
                }
            }
        }

        private Bitmap LoadClientAvatar(Models.Client client)
        {
            try
            {
                if (!string.IsNullOrEmpty(client.AvatarPath))
                {
                    var resourceUri = new Uri($"avares://FitClub/{client.AvatarPath}");
                    using var stream = AssetLoader.Open(resourceUri);
                    return new Bitmap(stream);
                }
            }
            catch
            {
                // Если загрузка не удалась, продолжаем
            }
            
            // Загружаем изображение по умолчанию
            try
            {
                var defaultUri = new Uri("avares://FitClub/Assets/default_avatar.png");
                using var stream = AssetLoader.Open(defaultUri);
                return new Bitmap(stream);
            }
            catch
            {
                return null;
            }
        }

        private void UpdateDisplay()
        {
            // Инициализируем отфильтрованные списки
            _filteredClientsWithoutPlans = _clientsWithoutPlans ?? new List<ClientSubscription>();
            _filteredExistingPlans = _existingPlans ?? new List<TrainingPlan>();

            var withoutPlanList = this.FindControl<ItemsControl>("ClientsWithoutPlanList");
            var existingPlansList = this.FindControl<ItemsControl>("ExistingPlansList");

            if (withoutPlanList != null)
            {
                withoutPlanList.ItemsSource = _filteredClientsWithoutPlans;
            }

            if (existingPlansList != null)
            {
                existingPlansList.ItemsSource = _filteredExistingPlans;
            }

            // Обновляем сообщения
            var noWithoutPlanMessage = this.FindControl<Border>("NoClientsWithoutPlanMessage");
            var noExistingPlansMessage = this.FindControl<Border>("NoExistingPlansMessage");

            if (noWithoutPlanMessage != null)
            {
                noWithoutPlanMessage.IsVisible = !_filteredClientsWithoutPlans.Any();
            }

            if (noExistingPlansMessage != null)
            {
                noExistingPlansMessage.IsVisible = !_filteredExistingPlans.Any();
            }
        }

        // Методы поиска
        private void OnSearchWithoutPlanTextChanged(object sender, TextChangedEventArgs e)
        {
            FilterClientsWithoutPlans();
        }

        private void OnSearchWithPlanTextChanged(object sender, TextChangedEventArgs e)
        {
            FilterExistingPlans();
        }

        private void FilterClientsWithoutPlans()
        {
            var searchBox = this.FindControl<TextBox>("SearchWithoutPlanBox");
            if (searchBox == null || _clientsWithoutPlans == null) return;

            var searchText = searchBox.Text?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredClientsWithoutPlans = _clientsWithoutPlans.ToList();
            }
            else
            {
                _filteredClientsWithoutPlans = _clientsWithoutPlans
                    .Where(cs => cs.Client != null && 
                                cs.Client.FullName.ToLower().Contains(searchText))
                    .ToList();
            }

            var withoutPlanList = this.FindControl<ItemsControl>("ClientsWithoutPlanList");
            if (withoutPlanList != null)
            {
                withoutPlanList.ItemsSource = _filteredClientsWithoutPlans;
            }

            var noWithoutPlanMessage = this.FindControl<Border>("NoClientsWithoutPlanMessage");
            if (noWithoutPlanMessage != null)
            {
                noWithoutPlanMessage.IsVisible = !_filteredClientsWithoutPlans.Any();
            }
        }

        private void FilterExistingPlans()
        {
            var searchBox = this.FindControl<TextBox>("SearchWithPlanBox");
            if (searchBox == null || _existingPlans == null) return;

            var searchText = searchBox.Text?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredExistingPlans = _existingPlans.ToList();
            }
            else
            {
                _filteredExistingPlans = _existingPlans
                    .Where(p => p.Client != null && 
                               p.Client.FullName.ToLower().Contains(searchText))
                    .ToList();
            }

            var existingPlansList = this.FindControl<ItemsControl>("ExistingPlansList");
            if (existingPlansList != null)
            {
                existingPlansList.ItemsSource = _filteredExistingPlans;
            }

            var noExistingPlansMessage = this.FindControl<Border>("NoExistingPlansMessage");
            if (noExistingPlansMessage != null)
            {
                noExistingPlansMessage.IsVisible = !_filteredExistingPlans.Any();
            }
        }

        // Обработчики кнопок
        private void OnCreatePlanClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ClientSubscription subscription)
            {
                // Открываем окно создания плана
                var createPlanWindow = new CreateTrainingPlanWindow(_currentTrainer, subscription.Client);
                createPlanWindow.Closed += (s, args) => RefreshView();
                createPlanWindow.Show();
            }
        }

        private void OnViewPlanClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TrainingPlan plan)
            {
                // Открываем окно просмотра плана
                var viewPlanWindow = new ViewTrainingPlanWindow(plan);
                viewPlanWindow.Show();
            }
        }

        private void OnEditPlanClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TrainingPlan plan)
            {
                // Открываем окно редактирования плана
                var editPlanWindow = new CreateTrainingPlanWindow(_currentTrainer, plan.Client, plan);
                editPlanWindow.Closed += (s, args) => RefreshView();
                editPlanWindow.Show();
            }
        }

        public void RefreshView()
        {
            LoadCurrentTrainer();
            LoadData();
        }
    }
}