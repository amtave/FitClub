using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Linq;
using System.Collections.Generic;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;

namespace FitClub.Trainer.Views
{
    public partial class ViewTrainingPlanWindow : Window
    {
        private TrainingPlan _plan;

        public string ClientInfo => _plan?.Client?.FullName ?? "Неизвестно";
        public string GoalInfo => $"Цель: {_plan?.Goal?.Name ?? "Не указана"}";
        public string TrainerInfo => $"Тренер: {_plan?.Trainer?.FullName ?? "Неизвестно"}";
        public string CreatedDate => $"Создан: {_plan?.CreatedDate:dd.MM.yyyy}";
        public string StatusText => $"{_plan?.CompletedDays ?? 0} из 12 дней заполнено";
        public SolidColorBrush StatusColor => new SolidColorBrush((_plan?.CompletedDays ?? 0) == 12 ? Color.Parse("#27AE60") : Color.Parse("#F39C12"));
        public string Notes => string.IsNullOrEmpty(_plan?.Notes) ? "Без дополнительных примечаний" : _plan.Notes;

        public ViewTrainingPlanWindow(TrainingPlan plan)
        {
            InitializeComponent();
            _plan = plan;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
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
                            if (existingDay.Exercises == null)
                            {
                                existingDay.Exercises = new List<Exercise>();
                            }
                        }
                    }

                    _plan.TrainingPlanDays = _plan.TrainingPlanDays
                        .OrderBy(d => d.DayNumber)
                        .ToList();

                    this.DataContext = this;

                    var trainingDaysList = this.FindControl<ItemsControl>("TrainingDaysList");
                    if (trainingDaysList != null)
                    {
                        trainingDaysList.ItemsSource = _plan.TrainingPlanDays;
                    }
                }
            }
            catch
            {
            }
        }

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            var owner = this.Owner as Window;
            var editWindow = new CreateTrainingPlanWindow(_plan.Trainer, _plan.Client, _plan);
            Close();
            
            if (owner != null)
            {
                editWindow.ShowDialog(owner);
            }
            else
            {
                editWindow.Show();
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}