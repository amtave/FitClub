using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FitClub.Trainer.Views
{
    public class ObservableDay
    {
        public int DayNumber { get; set; }
        public ObservableCollection<Exercise> Exercises { get; set; }
    }

    public partial class CreateTrainingPlanWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly Models.Trainer _trainer;
        private readonly Models.Client _client;
        private readonly TrainingPlan _existingPlan;
        private List<TrainingPlanGoal> _goals;
        private List<TrainingPlanDay> _dbTrainingDays;
        private List<ObservableDay> _observableDays;

        public CreateTrainingPlanWindow(Models.Trainer trainer, Models.Client client, TrainingPlan existingPlan = null)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _trainer = trainer;
            _client = client;
            _existingPlan = existingPlan;

            LoadData();
            InitializeView();
        }

        private void LoadData()
        {
            try
            {
                _goals = _context.TrainingPlanGoals.ToList();
                _dbTrainingDays = new List<TrainingPlanDay>();
                _observableDays = new List<ObservableDay>();

                if (_existingPlan != null)
                {
                    var plan = _context.TrainingPlans
                        .Include(p => p.Goal)
                        .Include(p => p.TrainingPlanDays)
                        .FirstOrDefault(p => p.PlanId == _existingPlan.PlanId);

                    if (plan != null)
                    {
                        _dbTrainingDays = plan.TrainingPlanDays.OrderBy(d => d.DayNumber).ToList();
                    }
                }

                for (int i = 1; i <= 12; i++)
                {
                    var existingDbDay = _dbTrainingDays.FirstOrDefault(d => d.DayNumber == i);
                    
                    var obsDay = new ObservableDay
                    {
                        DayNumber = i,
                        Exercises = new ObservableCollection<Exercise>()
                    };

                    if (existingDbDay != null && existingDbDay.Exercises != null)
                    {
                        foreach (var ex in existingDbDay.Exercises)
                        {
                            obsDay.Exercises.Add(new Exercise
                            {
                                Name = ex.Name,
                                Sets = ex.Sets,
                                Reps = ex.Reps,
                                Duration = ex.Duration,
                                Rest = ex.Rest,
                                Notes = ex.Notes
                            });
                        }
                    }

                    _observableDays.Add(obsDay);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка загрузки данных: {ex.Message}", false);
            }
        }

        private void InitializeView()
        {
            try
            {
                if (WindowTitleText != null)
                {
                    WindowTitleText.Text = _existingPlan != null ? "📝 РЕДАКТИРОВАНИЕ ПЛАНА" : "📝 СОЗДАНИЕ ПЛАНА";
                }
                
                if (ClientInfoText != null)
                {
                    ClientInfoText.Text = _client.FullName;
                }

                if (GoalsComboBox != null)
                {
                    GoalsComboBox.ItemsSource = _goals;
                    
                    if (_existingPlan != null && _existingPlan.GoalId > 0)
                    {
                        GoalsComboBox.SelectedItem = _goals.FirstOrDefault(g => g.GoalId == _existingPlan.GoalId);
                    }
                }

                if (NotesTextBox != null && _existingPlan != null)
                {
                    NotesTextBox.Text = _existingPlan.Notes;
                }

                if (TrainingDaysList != null)
                {
                    TrainingDaysList.ItemsSource = _observableDays;
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка инициализации: {ex.Message}", false);
            }
        }

        private void OnAddExerciseClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is ObservableDay day)
                {
                    day.Exercises.Add(new Exercise
                    {
                        Name = "",
                        Sets = 3,
                        Reps = 12,
                        Duration = 45,
                        Rest = 60
                    });
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка добавления упражнения: {ex.Message}", false);
            }
        }

        private void OnRemoveExerciseClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Exercise exercise)
            {
                foreach (var day in _observableDays)
                {
                    if (day.Exercises.Contains(exercise))
                    {
                        day.Exercises.Remove(exercise);
                        break;
                    }
                }
            }
        }

        private async void OnSavePlanClick(object sender, RoutedEventArgs e)
        {
            try
            {
                TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();

                if (GoalsComboBox?.SelectedItem is not TrainingPlanGoal selectedGoal)
                {
                    ShowStatus("Пожалуйста, выберите цель плана", false);
                    return;
                }

                bool hasValidExercise = false;

                foreach (var obsDay in _observableDays)
                {
                    var dbDay = _dbTrainingDays.FirstOrDefault(d => d.DayNumber == obsDay.DayNumber);
                    if (dbDay == null)
                    {
                        dbDay = new TrainingPlanDay { DayNumber = obsDay.DayNumber };
                        _dbTrainingDays.Add(dbDay);
                    }

                    var validExercises = new List<Exercise>();
                    
                    foreach (var exercise in obsDay.Exercises)
                    {
                        if (!string.IsNullOrWhiteSpace(exercise.Name))
                        {
                            validExercises.Add(new Exercise
                            {
                                Name = exercise.Name.Trim(),
                                Sets = exercise.Sets,
                                Reps = exercise.Reps,
                                Duration = exercise.Duration,
                                Rest = exercise.Rest,
                                Notes = exercise.Notes
                            });
                            hasValidExercise = true;
                        }
                    }
                    
                    dbDay.Exercises = validExercises;
                }

                if (!hasValidExercise)
                {
                    ShowStatus("Добавьте хотя бы одно упражнение с названием", false);
                    return;
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                TrainingPlan plan;

                if (_existingPlan != null)
                {
                    plan = await _context.TrainingPlans
                        .Include(p => p.TrainingPlanDays)
                        .FirstOrDefaultAsync(p => p.PlanId == _existingPlan.PlanId);

                    if (plan == null)
                    {
                        ShowStatus("План не найден", false);
                        return;
                    }

                    plan.GoalId = selectedGoal.GoalId;
                    plan.Notes = NotesTextBox?.Text;
                    
                    _context.TrainingPlans.Update(plan);

                    foreach (var dbDay in _dbTrainingDays)
                    {
                        var existingDay = plan.TrainingPlanDays.FirstOrDefault(d => d.DayNumber == dbDay.DayNumber);
                        
                        if (existingDay != null)
                        {
                            existingDay.Exercises = dbDay.Exercises;
                            _context.TrainingPlanDays.Update(existingDay);
                        }
                        else
                        {
                            dbDay.PlanId = plan.PlanId;
                            _context.TrainingPlanDays.Add(dbDay);
                        }
                    }
                }
                else
                {
                    plan = new TrainingPlan
                    {
                        ClientId = _client.ClientId,
                        TrainerId = _trainer.TrainerId,
                        GoalId = selectedGoal.GoalId,
                        Notes = NotesTextBox?.Text,
                        CreatedDate = DateTime.UtcNow,
                        IsActive = true
                    };

                    _context.TrainingPlans.Add(plan);
                    await _context.SaveChangesAsync(); 

                    foreach (var dbDay in _dbTrainingDays)
                    {
                        dbDay.PlanId = plan.PlanId;
                        _context.TrainingPlanDays.Add(dbDay);
                    }
                }

                var notification = new ClientNotification
                {
                    ClientId = _client.ClientId,
                    Message = _existingPlan != null ? "Тренер обновил ваш персональный план тренировок!" : "Тренер составил для вас новый персональный план тренировок!",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };
                _context.ClientNotifications.Add(notification);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ShowStatus("План тренировок успешно сохранен!", true);
                
                await Task.Delay(1500);
                Close();
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка сохранения: {ex.Message}", false);
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ShowStatus(string message, bool isSuccess)
        {
            try
            {
                if (StatusMessage != null && StatusText != null)
                {
                    StatusText.Text = message;
                    StatusText.Foreground = isSuccess ? Brush.Parse("#155724") : Brush.Parse("#721C24");
                    StatusMessage.Background = isSuccess ? Brush.Parse("#D5EDDA") : Brush.Parse("#F8D7DA");
                    StatusMessage.BorderBrush = isSuccess ? Brush.Parse("#C3E6CB") : Brush.Parse("#F5C6CB");
                    StatusMessage.BorderThickness = new Avalonia.Thickness(1);
                    StatusMessage.IsVisible = true;
                }
            }
            catch { }
        }
    }
}