using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using FitClub.Models;
using FitClub.Services;
using Microsoft.EntityFrameworkCore;

namespace FitClub.Trainer.Views
{
    public partial class HomeView : UserControl
    {
        private readonly AppDbContext _context;
        private Models.Trainer _currentTrainer;
        private DateTime _currentDate;

        public HomeView()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _currentDate = DateTime.Today;
            
            LoadCurrentTrainer();
            LoadTodaysTrainings();
            UpdateTrainerInfo();
        }

        private void LoadCurrentTrainer()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки тренера: {ex.Message}");
            }
        }

        private void UpdateTrainerInfo()
        {
            if (_currentTrainer != null)
            {
                var welcomeText = this.FindControl<TextBlock>("WelcomeText");
                if (welcomeText != null)
                {
                    welcomeText.Text = $"Добро пожаловать, {_currentTrainer.FirstName} {_currentTrainer.LastName}!";
                }

                var dateText = this.FindControl<TextBlock>("CurrentDateText");
                if (dateText != null)
                {
                    dateText.Text = $"Сегодня: {_currentDate:dddd, dd MMMM yyyy}";
                }
            }
        }

        private void LoadTodaysTrainings()
        {
            if (_currentTrainer == null) return;

            try
            {
                // Загружаем индивидуальные тренировки на сегодня
                var individualTrainings = _context.IndividualTrainings
                    .Include(it => it.Client)
                    .Where(it => it.TrainerId == _currentTrainer.TrainerId &&
                               it.TrainingDate.Date == _currentDate.Date &&
                               it.IsActive)
                    .OrderBy(it => it.StartTime)
                    .ToList();

                // Загружаем групповые тренировки на сегодня
                var groupTrainings = _context.TrainingSchedules
                    .Include(ts => ts.GroupTraining)
                        .ThenInclude(gt => gt.TrainingType)
                    .Include(ts => ts.GroupTraining)
                        .ThenInclude(gt => gt.IntensityLevel)
                    .Where(ts => ts.TrainerId == _currentTrainer.TrainerId &&
                               ts.TrainingDate.Date == _currentDate.Date &&
                               ts.IsActive)
                    .OrderBy(ts => ts.TrainingTime)
                    .ToList();

                // Создаем ViewModel'ы для индивидуальных тренировок
                var individualViewModels = individualTrainings.Select(t => new IndividualTrainingViewModel
                {
                    Training = t
                }).ToList();

                // Создаем ViewModel'ы для групповых тренировок с возможностью открытия деталей
                var groupViewModels = groupTrainings.Select(ts => new GroupTrainingHomeViewModel(ts, OpenGroupTrainingDetails)).ToList();

                // Обновляем UI
                UpdateTrainingDisplays(individualViewModels, groupViewModels);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки тренировок: {ex.Message}");
            }
        }

        private void OpenGroupTrainingDetails(TrainingSchedule schedule)
        {
            try
            {
                if (schedule == null)
                {
                    Console.WriteLine("Ошибка: расписание не найдено");
                    return;
                }

                // Открываем окно с деталями тренировки
                var detailsWindow = new GroupTrainingDetailsWindow(schedule, _currentTrainer);
                detailsWindow.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка открытия деталей тренировки: {ex.Message}");
            }
        }

        private void UpdateTrainingDisplays(List<IndividualTrainingViewModel> individualTrainings, 
                                          List<GroupTrainingHomeViewModel> groupTrainings)
        {
            // Индивидуальные тренировки
            var individualControl = this.FindControl<ItemsControl>("IndividualTrainingsControl");
            if (individualControl != null)
            {
                individualControl.ItemsSource = individualTrainings;
            }

            var noIndividualMessage = this.FindControl<TextBlock>("NoIndividualTrainingsMessage");
            if (noIndividualMessage != null)
            {
                noIndividualMessage.IsVisible = !individualTrainings.Any();
            }

            var individualCount = this.FindControl<TextBlock>("IndividualCountText");
            if (individualCount != null)
            {
                individualCount.Text = $"{individualTrainings.Count}";
            }

            // Групповые тренировки
            var groupControl = this.FindControl<ItemsControl>("GroupTrainingsControl");
            if (groupControl != null)
            {
                groupControl.ItemsSource = groupTrainings;
            }

            var noGroupMessage = this.FindControl<TextBlock>("NoGroupTrainingsMessage");
            if (noGroupMessage != null)
            {
                noGroupMessage.IsVisible = !groupTrainings.Any();
            }

            var groupCount = this.FindControl<TextBlock>("GroupCountText");
            if (groupCount != null)
            {
                groupCount.Text = $"{groupTrainings.Count}";
            }

            // Общее количество
            var totalCount = this.FindControl<TextBlock>("TotalTrainingsText");
            if (totalCount != null)
            {
                totalCount.Text = $"{individualTrainings.Count + groupTrainings.Count}";
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadTodaysTrainings();
        }
    }

    // ViewModel для отображения индивидуальных тренировок на главной
    public class IndividualTrainingViewModel
    {
        public IndividualTraining Training { get; set; }
        
        public string TimeRangeFormatted => $"{Training.StartTime:hh\\:mm} - {Training.EndTime:hh\\:mm}";
        
        public string DurationFormatted 
        {
            get
            {
                var duration = Training.EndTime - Training.StartTime;
                return $"{(int)duration.TotalMinutes} мин";
            }
        }
        
        public string ClientFullName => Training.Client?.FullName ?? "Клиент не найден";
        public string ClientPhone => Training.Client?.Phone ?? "Телефон не указан";
        
        public Bitmap ClientAvatar 
        {
            get
            {
                try
                {
                    if (Training.Client != null && !string.IsNullOrEmpty(Training.Client.AvatarPath))
                    {
                        string avatarsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Avatars");
                        string avatarPath = Path.Combine(avatarsFolder, Training.Client.AvatarPath);
                        
                        if (File.Exists(avatarPath))
                        {
                            return new Bitmap(avatarPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки аватарки: {ex.Message}");
                }

                return LoadDefaultAvatar();
            }
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
                Console.WriteLine($"Ошибка загрузки дефолтной аватарки: {ex.Message}");
                return null;
            }
        }
    }

    // ViewModel для отображения групповых тренировок на главной
    public class GroupTrainingHomeViewModel
    {
        private readonly TrainingSchedule _schedule;
        private readonly Action<TrainingSchedule> _openDetailsAction;

        public GroupTrainingHomeViewModel(TrainingSchedule schedule, Action<TrainingSchedule> openDetailsAction)
        {
            _schedule = schedule;
            _openDetailsAction = openDetailsAction;
            OpenDetailsCommand = new HomeRelayCommand(OpenDetails);
        }

        public string TimeFormatted => _schedule.TrainingTime.ToString(@"hh\:mm");
        public string DurationFormatted => $"{_schedule.GroupTraining?.DurationMinutes ?? 0} мин";
        public string TrainingName => _schedule.GroupTraining?.Name ?? "Неизвестная тренировка";
        public string TrainingType => _schedule.GroupTraining?.TrainingType?.Name ?? "Тип не указан";
        public string ParticipantsInfo => $"{_schedule.CurrentParticipants}/{_schedule.MaxParticipants}";
        public string IntensityLevel => _schedule.GroupTraining?.IntensityLevel?.Name ?? "Не указана";

        // Команда для открытия деталей
        public ICommand OpenDetailsCommand { get; }

        private void OpenDetails()
        {
            _openDetailsAction?.Invoke(_schedule);
        }
    }

    // Упрощенная реализация RelayCommand для HomeView (чтобы избежать конфликта имен)
    public class HomeRelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public HomeRelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}