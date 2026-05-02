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
    public partial class TrainerClientsView : UserControl
    {
        private readonly AppDbContext _context;
        private Models.Trainer _currentTrainer;
        private List<ClientSubscription> _allIndividualSubscriptions;
        private List<ClientSubscription> _allGroupSubscriptions;
        private List<ClientSubscription> _filteredIndividualSubscriptions;
        private List<ClientSubscription> _filteredGroupSubscriptions;

        public TrainerClientsView()
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

            UpdateTrainerInfo();
        }

        private void UpdateTrainerInfo()
        {
            if (_currentTrainer != null)
            {
                var trainerInfoText = this.FindControl<TextBlock>("TrainerInfoText");
                if (trainerInfoText != null)
                {
                    trainerInfoText.Text = $"Тренер: {_currentTrainer.LastName} {_currentTrainer.FirstName} {_currentTrainer.MiddleName}";
                }
            }
        }

       private void LoadData()
{
    if (_currentTrainer == null) return;

    try
    {
        Console.WriteLine($"=== ЗАГРУЗКА ДАННЫХ ДЛЯ ТРЕНЕРА ID: {_currentTrainer.TrainerId} ===");

        // Загружаем ВСЕ активные абонементы
        var allSubscriptions = _context.ClientSubscriptions
            .Include(cs => cs.Client)
            .Include(cs => cs.Tariff)
            .Include(cs => cs.IndividualTrainingType)
            .Include(cs => cs.IndividualTrainer)
            .Include(cs => cs.SelectedTrainingType)
            .Include(cs => cs.SelectedTrainer)
            .Where(cs => cs.IsActive && cs.EndDate >= DateTime.Today)
            .ToList();

        Console.WriteLine($"Всего активных абонементов: {allSubscriptions.Count}");

        // Загружаем все планы тренировок для этого тренера с днями
        var trainingPlans = _context.TrainingPlans
            .Include(tp => tp.TrainingPlanDays) // ВАЖНО: включаем дни
            .Where(tp => tp.TrainerId == _currentTrainer.TrainerId && tp.IsActive)
            .ToList();

        foreach (var sub in allSubscriptions)
        {
            // Находим план для этого клиента
            var clientPlan = trainingPlans.FirstOrDefault(tp => tp.ClientId == sub.ClientId);
            
            // Устанавливаем статус плана
            sub.HasTrainingPlan = clientPlan != null;
            sub.HasAnyExercisesInPlan = clientPlan?.HasAnyExercises ?? false; // Используем свойство из TrainingPlan
        }

        // ОТЛАДКА: выводим информацию о каждом абонементе
        foreach (var sub in allSubscriptions)
        {
            Console.WriteLine($"Абонемент ID: {sub.SubscriptionId}, " +
                            $"Тип: {sub.VisitsType}, " +
                            $"Тариф: {sub.Tariff?.Name}, " +
                            $"IndTrainerId: {sub.IndividualTrainerId}, " +
                            $"SelTrainerId: {sub.SelectedTrainerId}");

            // Находим план для этого клиента
            var clientPlan = trainingPlans.FirstOrDefault(tp => tp.ClientId == sub.ClientId);
            
            // Устанавливаем статус плана
            sub.HasTrainingPlan = clientPlan != null;
            sub.HasAnyExercisesInPlan = clientPlan?.HasAnyExercises ?? false;

            Console.WriteLine($"Клиент {sub.Client?.FullName}: план={sub.HasTrainingPlan}, упражнения={sub.HasAnyExercisesInPlan}");
        }

        // УПРОЩЕННАЯ ФИЛЬТРАЦИЯ - проверяем оба поля тренера
        _allIndividualSubscriptions = allSubscriptions
            .Where(cs => 
                (cs.IndividualTrainerId == _currentTrainer.TrainerId ||
                 cs.SelectedTrainerId == _currentTrainer.TrainerId) &&
                (cs.VisitsType == "individual" || 
                 cs.IndividualTotalVisits.HasValue ||
                 (cs.Tariff != null && cs.Tariff.Name.ToLower().Contains("индивидуаль")))
            )
            .ToList();

        _allGroupSubscriptions = allSubscriptions
            .Where(cs => 
                cs.SelectedTrainerId == _currentTrainer.TrainerId &&
                (cs.VisitsType == "group" || 
                 cs.GroupTotalVisits.HasValue ||
                 (cs.Tariff != null && cs.Tariff.Name.ToLower().Contains("групп")))
            )
            .ToList();

        Console.WriteLine($"Найдено индивидуальных: {_allIndividualSubscriptions.Count}");
        Console.WriteLine($"Найдено групповых: {_allGroupSubscriptions.Count}");

        // Загружаем аватары клиентов
        foreach (var subscription in allSubscriptions)
        {
            if (subscription.Client != null)
            {
                subscription.Client.AvatarBitmap = LoadClientAvatar(subscription.Client);
            }
        }

        UpdateDisplay();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка загрузки данных: {ex.Message}");
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
                // Если загрузка не удалась, возвращаем null
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
            _filteredIndividualSubscriptions = _allIndividualSubscriptions ?? new List<ClientSubscription>();
            _filteredGroupSubscriptions = _allGroupSubscriptions ?? new List<ClientSubscription>();

            var individualList = this.FindControl<ItemsControl>("IndividualClientsList");
            var groupList = this.FindControl<ItemsControl>("GroupClientsList");

            if (individualList != null)
            {
                individualList.ItemsSource = _filteredIndividualSubscriptions;
            }

            if (groupList != null)
            {
                groupList.ItemsSource = _filteredGroupSubscriptions;
            }

            var noIndividualMessage = this.FindControl<Border>("NoIndividualClientsMessage");
            var noGroupMessage = this.FindControl<Border>("NoGroupClientsMessage");

            if (noIndividualMessage != null)
            {
                noIndividualMessage.IsVisible = !_filteredIndividualSubscriptions.Any();
            }

            if (noGroupMessage != null)
            {
                noGroupMessage.IsVisible = !_filteredGroupSubscriptions.Any();
            }
        }

        // МЕТОДЫ ПОИСКА
        private void OnIndividualSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            FilterIndividualSubscriptions();
        }

        private void OnGroupSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            FilterGroupSubscriptions();
        }

        private void FilterIndividualSubscriptions()
        {
            var searchBox = this.FindControl<TextBox>("IndividualSearchBox");
            if (searchBox == null || _allIndividualSubscriptions == null) return;

            var searchText = searchBox.Text?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredIndividualSubscriptions = _allIndividualSubscriptions.ToList();
            }
            else
            {
                _filteredIndividualSubscriptions = _allIndividualSubscriptions
                    .Where(cs => cs.Client != null && 
                                cs.Client.FullName.ToLower().Contains(searchText))
                    .ToList();
            }

            var individualList = this.FindControl<ItemsControl>("IndividualClientsList");
            if (individualList != null)
            {
                individualList.ItemsSource = _filteredIndividualSubscriptions;
            }

            var noIndividualMessage = this.FindControl<Border>("NoIndividualClientsMessage");
            if (noIndividualMessage != null)
            {
                noIndividualMessage.IsVisible = !_filteredIndividualSubscriptions.Any();
            }
        }

        private void FilterGroupSubscriptions()
        {
            var searchBox = this.FindControl<TextBox>("GroupSearchBox");
            if (searchBox == null || _allGroupSubscriptions == null) return;

            var searchText = searchBox.Text?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredGroupSubscriptions = _allGroupSubscriptions.ToList();
            }
            else
            {
                _filteredGroupSubscriptions = _allGroupSubscriptions
                    .Where(cs => cs.Client != null && 
                                cs.Client.FullName.ToLower().Contains(searchText))
                    .ToList();
            }

            var groupList = this.FindControl<ItemsControl>("GroupClientsList");
            if (groupList != null)
            {
                groupList.ItemsSource = _filteredGroupSubscriptions;
            }

            var noGroupMessage = this.FindControl<Border>("NoGroupClientsMessage");
            if (noGroupMessage != null)
            {
                noGroupMessage.IsVisible = !_filteredGroupSubscriptions.Any();
            }
        }

        private void RefreshIndividualData(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void RefreshGroupData(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

       private void OnIndividualActionClick(object sender, RoutedEventArgs e)
{
    if (sender is Button button && button.Tag is ClientSubscription subscription)
    {
        // Проверяем, есть ли уже план у клиента
        var existingPlan = _context.TrainingPlans
            .Include(tp => tp.TrainingPlanDays)
            .FirstOrDefault(tp => tp.ClientId == subscription.ClientId && 
                                 tp.TrainerId == _currentTrainer.TrainerId && 
                                 tp.IsActive);

        if (existingPlan != null)
        {
            // Если план есть - открываем окно просмотра
            var viewPlanWindow = new ViewTrainingPlanWindow(existingPlan);
            viewPlanWindow.Show();
        }
        else
        {
            // Если плана нет - открываем окно создания
            var createPlanWindow = new CreateTrainingPlanWindow(_currentTrainer, subscription.Client);
            createPlanWindow.Closed += (s, args) => RefreshView();
            createPlanWindow.Show();
        }
    }
}

        private void OnGroupDetailsClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ClientSubscription subscription)
            {
                // Логика для просмотра деталей групповых тренировок
            }
        }

        public void RefreshView()
        {
            LoadCurrentTrainer();
            LoadData();
        }
    }
}