using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using FitClub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using Microsoft.EntityFrameworkCore;

namespace FitClub.Admin.Views
{
    public partial class GroupTrainingsAdminView : UserControl
    {
        private List<GroupTraining> _allTrainings;
        private string _searchText = "";

        public GroupTrainingsAdminView()
        {
            InitializeComponent();
            TypeFilterComboBox.SelectedIndex = 0;
            IntensityFilterComboBox.SelectedIndex = 0;
            LoadTrainings();
        }

        private void LoadTrainings()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    _allTrainings = db.GroupTrainings
                        .Include(t => t.IntensityLevel)
                        .Include(t => t.TrainingType)
                        .Include(t => t.TrainingTrainers)
                        .ThenInclude(tt => tt.Trainer)
                        .OrderBy(t => t.Name)
                        .ToList();
                }
                ApplyFilter();
            }
            catch (Exception)
            {
                ShowStatus($"Ошибка загрузки", false);
            }
        }

        private void ApplyFilter()
        {
            if (_allTrainings == null) return;

            var filtered = _allTrainings.AsEnumerable();

            if (IntensityFilterComboBox?.SelectedItem is ComboBoxItem intensityItem)
            {
                string selectedIntensity = intensityItem.Content.ToString();
                if (selectedIntensity != "Все")
                {
                    filtered = filtered.Where(t => t.IntensityLevel != null && t.IntensityLevel.Name == selectedIntensity);
                }
            }

            if (TypeFilterComboBox?.SelectedItem is ComboBoxItem typeItem)
            {
                string selectedType = typeItem.Content.ToString();
                if (selectedType != "Все")
                {
                    filtered = filtered.Where(t => t.TrainingType != null && t.TrainingType.Name == selectedType);
                }
            }

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filtered = filtered.Where(t => t.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
            }

            var control = this.FindControl<ItemsControl>("TrainingsItemsControl");
            var noMessage = this.FindControl<Border>("NoTrainingsMessage");

            if (control != null)
            {
                var resultList = filtered.ToList();
                control.ItemsSource = resultList;
                if (noMessage != null)
                    noMessage.IsVisible = !resultList.Any();
            }
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

        private async void AddTrainingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new GroupTrainingAddWindow();
                await window.ShowDialog((Window)this.VisualRoot);

                if (window.Tag is bool result && result)
                {
                    LoadTrainings();
                    ShowStatus("Новая тренировка добавлена", true);
                }
            }
            catch (Exception)
            {
                ShowStatus($"Ошибка", false);
            }
        }

        private async void AssignTrainerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var training = (GroupTraining)button.Tag;

                var window = new GroupTrainingAssignTrainerWindow(training.TrainingId);
                await window.ShowDialog((Window)this.VisualRoot);

                if (window.Tag is bool result && result)
                {
                    LoadTrainings();
                    ShowStatus("Тренеры назначены", true);
                }
            }
            catch (Exception)
            {
                ShowStatus($"Ошибка", false);
            }
        }

        private async void EditTrainingButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var training = (GroupTraining)button.Tag;

                var window = new GroupTrainingEditWindow(training);
                await window.ShowDialog((Window)this.VisualRoot);

                if (window.Tag is bool result && result)
                {
                    LoadTrainings();
                    ShowStatus("Данные обновлены", true);
                }
            }
            catch (Exception)
            {
                ShowStatus($"Ошибка", false);
            }
        }

        private async void DeleteTrainingButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var training = (GroupTraining)button.Tag;

            var result = await ShowConfirmDialog("Удаление", $"Удалить тренировку '{training.Name}'?");
            if (!result) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var toDelete = db.GroupTrainings.Find(training.TrainingId);
                    if (toDelete != null)
                    {
                        db.GroupTrainings.Remove(toDelete);
                        await db.SaveChangesAsync();
                    }
                }
                LoadTrainings();
                ShowStatus("Удалено", true);
            }
            catch (Exception)
            {
                ShowStatus($"Ошибка", false);
            }
        }

        private void ShowStatus(string message, bool isSuccess)
        {
            var statusText = this.FindControl<TextBlock>("StatusText");
            var statusBorder = this.FindControl<Border>("StatusBorder");

            if (statusText != null && statusBorder != null)
            {
                statusText.Text = message;
                statusBorder.Background = isSuccess ? Brush.Parse("#27AE60") : Brush.Parse("#E74C3C");
                statusBorder.IsVisible = true;

                var timer = new System.Timers.Timer(3000);
                timer.Elapsed += (s, args) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => { statusBorder.IsVisible = false; });
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        private async Task<bool> ShowConfirmDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title, Width = 350, Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            bool dialogResult = false;
            var stack = new StackPanel { Margin = new Thickness(20), Spacing = 15 };
            stack.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
            var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            var yes = new Button { Content = "Да", Width = 60 };
            var no = new Button { Content = "Нет", Width = 60 };
            yes.Click += (s, e) => { dialogResult = true; dialog.Close(); };
            no.Click += (s, e) => { dialogResult = false; dialog.Close(); };
            buttons.Children.Add(yes);
            buttons.Children.Add(no);
            stack.Children.Add(buttons);
            dialog.Content = stack;
            await dialog.ShowDialog((Window)this.VisualRoot);
            return dialogResult;
        }
    }
}