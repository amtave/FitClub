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
using System.IO;

namespace FitClub.Admin.Views
{
    public partial class TariffsAdminView : UserControl
    {
        private List<Tariff> _allTariffs;
        private int _currentFilter = 0;
        private string _searchText = "";

        public TariffsAdminView()
        {
            InitializeComponent();
            CategoryFilterComboBox.SelectedIndex = 0;
            LoadTariffs();
        }

        private void LoadTariffs()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    _allTariffs = db.Tariffs
                        .Include(t => t.Category)
                        .OrderByDescending(t => t.CreatedAt)
                        .ToList();
                }
                ApplyFilter();
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка загрузки тарифов: {ex.Message}", false);
            }
        }

        private void ApplyFilter()
        {
            if (_allTariffs == null) return;

            var filtered = _allTariffs.AsEnumerable();

            if (_currentFilter != 0)
            {
                filtered = filtered.Where(t => t.CategoryId == _currentFilter);
            }

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filtered = filtered.Where(t => t.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
            }

            var tariffsControl = this.FindControl<ItemsControl>("TariffsItemsControl");
            var noTariffsMessage = this.FindControl<Border>("NoTariffsMessage");

            if (tariffsControl != null)
            {
                var resultList = filtered.ToList();
                tariffsControl.ItemsSource = resultList;
                if (noTariffsMessage != null)
                    noTariffsMessage.IsVisible = !resultList.Any();
            }
        }

        private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentFilter = CategoryFilterComboBox.SelectedIndex;
            ApplyFilter();
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            _searchText = SearchTextBox.Text ?? "";
            ApplyFilter();
        }

        private async void AddTariffButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new TariffEditWindow();
            await window.ShowDialog((Window)this.VisualRoot);

            if (window.Tag is bool result && result)
            {
                LoadTariffs();
                ShowStatus("Тариф успешно добавлен!", true);
            }
        }

        private async void EditTariffButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var tariff = (Tariff)button.Tag;

            var window = new TariffEditWindow(tariff);
            await window.ShowDialog((Window)this.VisualRoot);

            if (window.Tag is bool result && result)
            {
                LoadTariffs();
                ShowStatus("Тариф успешно обновлен!", true);
            }
        }

        private async void DeleteTariffButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var tariff = (Tariff)button.Tag;

            var confirmResult = await ShowConfirmDialog("Подтверждение", $"Удалить тариф '{tariff.Name}'?");
            if (!confirmResult) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    var toDelete = db.Tariffs.Find(tariff.TariffId);
                    if (toDelete != null)
                    {
                        db.Tariffs.Remove(toDelete);
                        await db.SaveChangesAsync();
                    }
                }
                LoadTariffs();
                ShowStatus("Тариф удален", true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка: {ex.Message}", false);
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
    }
}