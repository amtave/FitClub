using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Linq;
using System.Collections.Generic;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;

namespace FitClub.Client.Views
{
    public partial class ClientViewTrainingPlanWindow : Window
    {
        private TrainingPlan _plan;

        public ClientViewTrainingPlanWindow(TrainingPlan plan)
        {
            InitializeComponent();
            _plan = plan;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Загружаем полные данные плана
                using var context = new AppDbContext();
                var fullPlan = context.TrainingPlans
                    .Include(p => p.Client)
                    .Include(p => p.Trainer)
                    .Include(p => p.Goal)
                    .Include(p => p.TrainingPlanDays)
                    .FirstOrDefault(p => p.PlanId == _plan.PlanId);

                if (fullPlan != null)
                {
                    _plan = fullPlan;

                    // Убедимся, что все дни инициализированы
                    for (int i = 1; i <= 12; i++)
                    {
                        var existingDay = _plan.TrainingPlanDays.FirstOrDefault(d => d.DayNumber == i);
                        if (existingDay == null)
                        {
                            _plan.TrainingPlanDays.Add(new TrainingPlanDay
                            {
                                DayNumber = i,
                                Exercises = new List<Exercise>()
                            });
                        }
                        else
                        {
                            // Убедимся, что Exercises инициализирован
                            if (existingDay.Exercises == null)
                                existingDay.Exercises = new List<Exercise>();
                        }
                    }

                    _plan.TrainingPlanDays = _plan.TrainingPlanDays
                        .OrderBy(d => d.DayNumber)
                        .ToList();

                    // Устанавливаем данные в интерфейс
                    SetInterfaceData();
                }
                else
                {
                    ShowError("План тренировок не найден");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void SetInterfaceData()
        {
            // Основная информация
            ClientInfoText.Text = $"Клиент: {_plan.Client?.FullName ?? "Неизвестно"}";
            GoalInfoText.Text = $"Цель: {_plan.Goal?.Name ?? "Не указана"}";
            TrainerInfoText.Text = _plan.Trainer?.FullName ?? "Неизвестно";
            CreatedDateText.Text = _plan.CreatedDate.ToString("dd.MM.yyyy");

            // Статус
            StatusText.Text = $"{_plan.CompletedDays}/12 дней заполнено";
            StatusBorder.Background = new SolidColorBrush(
                _plan.CompletedDays == 12 ? Color.Parse("#27AE60") : 
                _plan.CompletedDays > 0 ? Color.Parse("#F39C12") : Color.Parse("#BDC3C7"));

            // Дни тренировок
            TrainingDaysList.ItemsSource = _plan.TrainingPlanDays;
        }

        private void ShowError(string message)
        {
            // Можно добавить отображение ошибки в интерфейсе
            Console.WriteLine($"Ошибка: {message}");
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}