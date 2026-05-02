using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;

namespace FitClub.Trainer.Views
{
    public partial class CreateTrainingPlanWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly Models.Trainer _trainer;
        private readonly Models.Client _client;
        private readonly TrainingPlan _existingPlan;
        private List<TrainingPlanGoal> _goals;
        private List<TrainingPlanDay> _trainingDays;

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
                // Загружаем цели плана
                _goals = _context.TrainingPlanGoals.ToList();
                _trainingDays = new List<TrainingPlanDay>();

                if (_existingPlan != null)
                {
                    // Загружаем существующий план
                    var plan = _context.TrainingPlans
                        .Include(p => p.Goal)
                        .Include(p => p.TrainingPlanDays)
                        .FirstOrDefault(p => p.PlanId == _existingPlan.PlanId);

                    if (plan != null)
                    {
                        _trainingDays = plan.TrainingPlanDays
                            .OrderBy(d => d.DayNumber)
                            .ToList();
                    }
                }

                // Создаем дни, если их нет или меньше 12
                for (int i = 1; i <= 12; i++)
                {
                    var existingDay = _trainingDays.FirstOrDefault(d => d.DayNumber == i);
                    if (existingDay == null)
                    {
                        _trainingDays.Add(new TrainingPlanDay
                        {
                            DayNumber = i,
                            Exercises = new List<Exercise>(),
                            Notes = ""
                        });
                    }
                    else
                    {
                        // Убедимся, что Exercises инициализирован
                        if (existingDay.Exercises == null)
                            existingDay.Exercises = new List<Exercise>();
                    }
                }

                // Сортируем по номеру дня
                _trainingDays = _trainingDays
                    .OrderBy(d => d.DayNumber)
                    .ToList();
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
        // Устанавливаем заголовки
        var windowTitleText = this.FindControl<TextBlock>("WindowTitleText");
        var clientInfoText = this.FindControl<TextBlock>("ClientInfoText");
        
        if (windowTitleText != null)
        {
            windowTitleText.Text = _existingPlan != null ? 
                "Редактирование плана тренировок" : "Создание плана тренировок";
        }
        
        if (clientInfoText != null)
        {
            clientInfoText.Text = $"Клиент: {_client.FullName}";
        }

        // Заполняем ComboBox целей
        var goalsComboBox = this.FindControl<ComboBox>("GoalsComboBox");
        if (goalsComboBox != null)
        {
            goalsComboBox.ItemsSource = _goals;
            
            // НЕ УСТАНАВЛИВАЕМ выбранный элемент по умолчанию
            // Пользователь должен выбрать цель самостоятельно
            if (_existingPlan != null && _existingPlan.GoalId > 0)
            {
                goalsComboBox.SelectedItem = _goals.FirstOrDefault(g => g.GoalId == _existingPlan.GoalId);
            }
            // Для нового плана - не выбираем ничего
        }

        // Устанавливаем дни тренировок
        var trainingDaysList = this.FindControl<ItemsControl>("TrainingDaysList");
        if (trainingDaysList != null)
        {
            trainingDaysList.ItemsSource = _trainingDays;
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
        if (sender is Button button && button.Tag is TrainingPlanDay day)
        {
            // Создаем новое упражнение
            var newExercise = new Exercise
            {
                Name = "Новое упражнение", // Даем начальное название
                Sets = 3,
                Reps = 12,
                Duration = 45,
                Rest = 60
            };
            
            day.AddExercise(newExercise);
            
            // Обновляем отображение
            RefreshDaysList();
            ShowStatus("Упражнение добавлено", true);
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
        foreach (var day in _trainingDays)
        {
            if (day.Exercises.Contains(exercise))
            {
                day.RemoveExercise(exercise);
                RefreshDaysList();
                ShowStatus("Упражнение удалено", true);
                break;
            }
        }
    }
}

        private void RefreshDaysList()
{
    var trainingDaysList = this.FindControl<ItemsControl>("TrainingDaysList");
    if (trainingDaysList != null)
    {
        trainingDaysList.ItemsSource = null;
        trainingDaysList.ItemsSource = _trainingDays;
    }
}

        private async void OnSavePlanClick(object sender, RoutedEventArgs e)
{
    try
    {
        // 1. ПРОВЕРЯЕМ ЦЕЛЬ
        var goalsComboBox = this.FindControl<ComboBox>("GoalsComboBox");
        if (goalsComboBox?.SelectedItem is not TrainingPlanGoal selectedGoal)
        {
            ShowStatus("Пожалуйста, выберите цель плана", false);
            return;
        }

        // 2. РУЧНОЙ СБОР ДАННЫХ ИЗ ВСЕХ ПОЛЕЙ
        Console.WriteLine("=== РУЧНОЙ СБОР ДАННЫХ ===");
        
        var allExercises = new List<(int dayNumber, Exercise exercise)>();
        bool hasValidExercise = false;

        // Проходим по всем дням и упражнениям
        foreach (var day in _trainingDays)
        {
            Console.WriteLine($"--- День {day.DayNumber} ---");
            
            // Создаем новый список для непустых упражнений
            var validExercises = new List<Exercise>();
            
            foreach (var exercise in day.Exercises ?? new List<Exercise>())
            {
                Console.WriteLine($"Упражнение до обработки: '{exercise.Name}'");
                
                // Если упражнение имеет название - оно валидно
                if (!string.IsNullOrWhiteSpace(exercise.Name))
                {
                    // Создаем КОПИЮ упражнения с гарантированно правильными данными
                    var validExercise = new Exercise
                    {
                        Name = exercise.Name.Trim(),
                        Sets = exercise.Sets,
                        Reps = exercise.Reps,
                        Duration = exercise.Duration,
                        Rest = exercise.Rest,
                        Notes = exercise.Notes
                    };
                    
                    validExercises.Add(validExercise);
                    allExercises.Add((day.DayNumber, validExercise));
                    hasValidExercise = true;
                    
                    Console.WriteLine($"СОХРАНЕНО: '{validExercise.Name}' - {validExercise.Sets}x{validExercise.Reps}");
                }
                else
                {
                    Console.WriteLine("ПРОПУЩЕНО: пустое название");
                }
            }
            
            // Обновляем день только валидными упражнениями
            day.Exercises = validExercises;
        }

        // 3. ПРОВЕРКА - есть ли хоть одно упражнение с названием
        if (!hasValidExercise)
        {
            ShowStatus("Добавьте хотя бы одно упражнение с названием", false);
            return;
        }

        // 4. СОХРАНЕНИЕ В БАЗУ
        using var transaction = await _context.Database.BeginTransactionAsync();

        TrainingPlan plan;

        if (_existingPlan != null)
        {
            // Редактирование существующего плана
            plan = await _context.TrainingPlans
                .Include(p => p.TrainingPlanDays)
                .FirstOrDefaultAsync(p => p.PlanId == _existingPlan.PlanId);

            if (plan == null)
            {
                ShowStatus("План не найден", false);
                return;
            }

            plan.GoalId = selectedGoal.GoalId;

            // Обновляем дни
            foreach (var day in _trainingDays)
            {
                var existingDay = plan.TrainingPlanDays.FirstOrDefault(d => d.DayNumber == day.DayNumber);
                
                if (existingDay != null)
                {
                    // ОБНОВЛЯЕМ СУЩЕСТВУЮЩИЙ ДЕНЬ
                    existingDay.Exercises = day.Exercises;
                    Console.WriteLine($"Обновлен день {day.DayNumber}: {day.Exercises.Count} упражнений");
                }
                else
                {
                    // ДОБАВЛЯЕМ НОВЫЙ ДЕНЬ
                    day.PlanId = plan.PlanId;
                    _context.TrainingPlanDays.Add(day);
                    Console.WriteLine($"Добавлен новый день {day.DayNumber}: {day.Exercises.Count} упражнений");
                }
            }
        }
        else
        {
            // СОЗДАНИЕ НОВОГО ПЛАНА
            plan = new TrainingPlan
            {
                ClientId = _client.ClientId,
                TrainerId = _trainer.TrainerId,
                GoalId = selectedGoal.GoalId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            _context.TrainingPlans.Add(plan);
            await _context.SaveChangesAsync(); // Сохраняем, чтобы получить PlanId

            // Добавляем все дни
            foreach (var day in _trainingDays)
            {
                day.PlanId = plan.PlanId;
                _context.TrainingPlanDays.Add(day);
                Console.WriteLine($"Добавлен день {day.DayNumber}: {day.Exercises.Count} упражнений");
            }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        ShowStatus("План тренировок успешно сохранен!", true);
        
        // Даем время увидеть сообщение
        await System.Threading.Tasks.Task.Delay(1500);
        Close();
    }
    catch (Exception ex)
    {
        ShowStatus($"Ошибка сохранения: {ex.Message}", false);
        Console.WriteLine($"ОШИБКА: {ex}");
    }
}

private void UpdateBindings()
{
    // Принудительно обновляем фокус - это заставит TextBox обновить привязки
    var trainingDaysList = this.FindControl<ItemsControl>("TrainingDaysList");
    if (trainingDaysList != null)
    {
        // Переводим фокус на сам ItemsControl и обратно
        trainingDaysList.Focus();
        
        // Имитируем потерю фокуса для всех TextBox
        // Это заставит Avalonia обновить привязки данных
        foreach (var day in _trainingDays)
        {
            foreach (var exercise in day.Exercises)
            {
                // Данные уже должны быть в модели благодаря TwoWay привязкам
                // Но на всякий случай логируем
                Console.WriteLine($"Упражнение: '{exercise.Name}', Подходы: {exercise.Sets}, Повторения: {exercise.Reps}");
            }
        }
    }
}


private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
{
    if (sender is TextBox textBox && textBox.DataContext is Exercise exercise)
    {
        // Принудительно обновляем свойство в модели
        var text = textBox.Text;
        
        // Обновляем свойство вручную на основе Watermark или контекста
        if (textBox.Watermark != null)
        {
            switch (textBox.Watermark)
            {
                case "Название упражнения":
                    exercise.Name = text;
                    Console.WriteLine($"Обновлено название: '{exercise.Name}'");
                    break;
                case "3":
                    exercise.Sets = int.TryParse(text, out int sets) ? sets : (int?)null;
                    Console.WriteLine($"Обновлены подходы: {exercise.Sets}");
                    break;
                case "12":
                    exercise.Reps = int.TryParse(text, out int reps) ? reps : (int?)null;
                    Console.WriteLine($"Обновлены повторения: {exercise.Reps}");
                    break;
                case "45":
                    exercise.Duration = int.TryParse(text, out int duration) ? duration : (int?)null;
                    Console.WriteLine($"Обновлена длительность: {exercise.Duration}");
                    break;
                case "60":
                    exercise.Rest = int.TryParse(text, out int rest) ? rest : (int?)null;
                    Console.WriteLine($"Обновлен отдых: {exercise.Rest}");
                    break;
            }
        }
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
                var statusMessage = this.FindControl<Border>("StatusMessage");
                var statusText = this.FindControl<TextBlock>("StatusText");

                if (statusMessage != null && statusText != null)
                {
                    statusText.Text = message;
                    statusMessage.Background = isSuccess ? 
                        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#D5EDDA")) :
                        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#F8D7DA"));
                    statusMessage.IsVisible = true;
                }
            }
            catch
            {
                // Игнорируем ошибки отображения статуса
            }
        }
    }
}