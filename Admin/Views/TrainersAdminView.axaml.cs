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

namespace FitClub.Admin.Views
{
    public partial class TrainersAdminView : UserControl
    {
        private AppDbContext _context;
        private List<Models.Trainer> _allTrainers;
        private string _searchText = "";

        public TrainersAdminView()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadFilters();
            SpecFilterComboBox.SelectedIndex = 0;
            GroupFilterComboBox.SelectedIndex = 0;
            ScheduleFilterComboBox.SelectedIndex = 0;
            LoadTrainers();
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
                    filtered = filtered.Where(t => t.GroupTrainingsList.Contains(selectedGroup, StringComparison.OrdinalIgnoreCase));
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
            NoTrainersBorder.IsVisible = !resultList.Any();
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            _searchText = (sender as TextBox)?.Text ?? "";
            ApplyFilter();
        }

        private async void OpenJobApplications_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.VisualRoot is Window window)
                {
                    var mainContent = window.FindControl<ContentControl>("MainContentControl") ?? window.FindControl<ContentControl>("MainContent");
                    if (mainContent != null)
                    {
                        mainContent.Content = new JobApplicationsView();
                    }
                }
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", ex.Message, ButtonEnum.Ok);
                await box.ShowWindowDialogAsync((Window)this.VisualRoot);
            }
        }

        private void OpenSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Models.Trainer trainer)
            {
                var window = TopLevel.GetTopLevel(this) as Window;
                if (window != null)
                {
                    var mainContent = window.FindControl<ContentControl>("MainContentControl") ?? window.FindControl<ContentControl>("MainContent");
                    if (mainContent != null)
                    {
                        var scheduleView = new TrainerScheduleView();
                        mainContent.Content = scheduleView;

                        var combo = scheduleView.FindControl<ComboBox>("TrainerComboBox");
                        if (combo != null)
                        {
                            var trainers = combo.ItemsSource as IEnumerable<Models.Trainer>;
                            if (trainers != null)
                            {
                                var t = trainers.FirstOrDefault(x => x.TrainerId == trainer.TrainerId);
                                if (t != null) combo.SelectedItem = t;
                            }
                        }
                    }
                }
            }
        }

        private async void FireTrainer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Models.Trainer trainer)
            {
                var dialog = new Window
                {
                    Title = "Увольнение", Width = 400, SizeToContent = SizeToContent.Height,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false,
                    Background = Avalonia.Media.Brush.Parse("#F4F7F9")
                };
                
                bool confirm = false;
                var stack = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 15 };
                stack.Children.Add(new TextBlock { Text = $"Уволить тренера {trainer.FullName}?\nБудущие тренировки будут удалены, а профиль заблокирован.", TextWrapping = Avalonia.Media.TextWrapping.Wrap, FontWeight = Avalonia.Media.FontWeight.SemiBold });
                
                var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
                var no = new Button { Content = "Отмена", Background = Avalonia.Media.Brush.Parse("#BDC3C7"), Foreground = Avalonia.Media.Brushes.White, CornerRadius = new Avalonia.CornerRadius(6) };
                var yes = new Button { Content = "Уволить", Background = Avalonia.Media.Brush.Parse("#E74C3C"), Foreground = Avalonia.Media.Brushes.White, CornerRadius = new Avalonia.CornerRadius(6) };
                
                yes.Click += (s, ev) => { confirm = true; dialog.Close(); };
                no.Click += (s, ev) => { confirm = false; dialog.Close(); };
                
                buttons.Children.Add(no);
                buttons.Children.Add(yes);
                stack.Children.Add(buttons);
                dialog.Content = stack;

                await dialog.ShowDialog((Window)this.VisualRoot);

                if (confirm)
                {
                    try
                    {
                        using var db = new AppDbContext();
                        var today = DateTime.Today;
                        
                        var futureSchedules = db.TrainingSchedules
                            .Include(ts => ts.TrainingBookings)
                            .Where(ts => ts.TrainerId == trainer.TrainerId && ts.TrainingDate >= today)
                            .ToList();

                        foreach (var fs in futureSchedules)
                        {
                            db.TrainingBookings.RemoveRange(fs.TrainingBookings);
                        }
                        db.TrainingSchedules.RemoveRange(futureSchedules);

                        var futureInds = db.IndividualTrainings
                            .Where(it => it.TrainerId == trainer.TrainerId && it.TrainingDate >= today)
                            .ToList();
                        db.IndividualTrainings.RemoveRange(futureInds);

                        var links = db.TrainingTrainers.Where(tt => tt.TrainerId == trainer.TrainerId).ToList();
                        db.TrainingTrainers.RemoveRange(links);

                        var user = db.Users.FirstOrDefault(u => u.Email == trainer.Email);
                        if (user != null) db.Users.Remove(user);

                        var dbTrainer = db.Trainers.Find(trainer.TrainerId);
                        if (dbTrainer != null)
                        {
                            dbTrainer.IsActive = false;
                        }

                        await db.SaveChangesAsync();
                        
                        var box = MessageBoxManager.GetMessageBoxStandard("Успех", "Тренер успешно уволен.", ButtonEnum.Ok);
                        await box.ShowWindowDialogAsync((Window)this.VisualRoot);
                        
                        LoadTrainers();
                    }
                    catch (Exception ex)
                    {
                        var box = MessageBoxManager.GetMessageBoxStandard("Ошибка БД", 
                            $"Не удалось уволить тренера.\n{ex.Message}\n{ex.InnerException?.Message}", 
                            ButtonEnum.Ok);
                        await box.ShowWindowDialogAsync((Window)this.VisualRoot);
                    }
                }
            }
        }
        
        private async void OpenTrainerDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Models.Trainer trainer)
            {
                var window = new TrainerDetailsWindow(trainer);
                await window.ShowDialog((Window)this.VisualRoot);
            }
        }

        private async void EditTrainer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Models.Trainer trainer)
            {
                var window = new TrainerEditWindow(trainer);
                await window.ShowDialog((Window)this.VisualRoot);
                
                if (window.Tag is bool result && result)
                {
                    LoadTrainers();
                }
            }
        }

        public void RefreshView()
        {
            LoadTrainers();
        }
    }
}