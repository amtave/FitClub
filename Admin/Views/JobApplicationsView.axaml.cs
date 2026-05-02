using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.VisualTree;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using Avalonia;

namespace FitClub.Admin.Views
{
    public partial class JobApplicationsView : UserControl
    {
        private AppDbContext _context;
        private List<JobApplicationView> _allApplications = new List<JobApplicationView>();
        private ObservableCollection<JobApplicationView> _displayApplications = new ObservableCollection<JobApplicationView>();
        private Border _emptyMessage;
        private bool _isInitialized = false;

        public JobApplicationsView()
        {
            InitializeComponent();
            _isInitialized = true;
            _context = new AppDbContext();
            InitializeControls();
            LoadData();
        }

        private void InitializeControls()
        {
            try
            {
                _emptyMessage = this.FindControl<Border>("EmptyMessage");
                var list = this.FindControl<ItemsControl>("ApplicationsList");
                if (list != null) list.ItemsSource = _displayApplications;
            }
            catch (Exception) { }
        }

        private void LoadData()
        {
            try
            {
                _allApplications.Clear();
                var sql = @"
                    SELECT ja.application_id, ja.client_id, ja.specialization, ja.experience_years,
                           ja.created_at, COALESCE(ja.status, 'pending') as status,
                           c.last_name, c.first_name, c.middle_name, c.email, c.phone, c.avatar_path
                    FROM job_application ja
                    LEFT JOIN client c ON ja.client_id = c.client_id";

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    _context.Database.OpenConnection();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _allApplications.Add(new JobApplicationView
                            {
                                ApplicationId = reader.GetInt32(reader.GetOrdinal("application_id")),
                                ClientId = reader.GetInt32(reader.GetOrdinal("client_id")),
                                ClientFullName = $"{reader.GetString(reader.GetOrdinal("last_name"))} {reader.GetString(reader.GetOrdinal("first_name"))} {reader.GetString(reader.GetOrdinal("middle_name"))}",
                                ClientEmail = reader.GetString(reader.GetOrdinal("email")),
                                ClientPhone = reader.GetString(reader.GetOrdinal("phone")),
                                Specialization = reader.GetString(reader.GetOrdinal("specialization")),
                                ExperienceYears = reader.IsDBNull(reader.GetOrdinal("experience_years")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("experience_years")),
                                AvatarPath = reader.IsDBNull(reader.GetOrdinal("avatar_path")) ? "" : reader.GetString(reader.GetOrdinal("avatar_path")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                Status = reader.GetString(reader.GetOrdinal("status"))
                            });
                        }
                    }
                }
                ApplyFilters();
            }
            catch (Exception) { }
        }

        private void ApplyFilters()
        {
            if (!_isInitialized || _displayApplications == null) return;

            var searchText = this.FindControl<TextBox>("SearchBox")?.Text?.ToLower() ?? "";
            var statusFilter = this.FindControl<ComboBox>("StatusFilter")?.SelectedIndex ?? 0;

            var filtered = _allApplications.Where(a => 
                (string.IsNullOrEmpty(searchText) || a.ClientFullName.ToLower().Contains(searchText)));

            if (statusFilter == 1) filtered = filtered.Where(a => a.Status == "pending");
            else if (statusFilter == 2) filtered = filtered.Where(a => a.Status == "accepted");
            else if (statusFilter == 3) filtered = filtered.Where(a => a.Status == "rejected");

            var sorted = filtered.OrderByDescending(a => a.Status == "pending")
                                .ThenByDescending(a => a.CreatedAt).ToList();

            _displayApplications.Clear();
            foreach (var app in sorted) _displayApplications.Add(app);
            if (_emptyMessage != null) _emptyMessage.IsVisible = _displayApplications.Count == 0;
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e) => ApplyFilters();
        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private void BackToTrainers_Click(object sender, RoutedEventArgs e)
        {
            if (this.VisualRoot is Window window)
            {
                var mainContent = window.FindControl<ContentControl>("MainContentControl") ?? window.FindControl<ContentControl>("MainContent");
                if (mainContent != null) mainContent.Content = new TrainersAdminView();
            }
        }

        private async void RejectApplication_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is JobApplicationView app)
            {
                var dialog = new Window
                {
                    Title = "Причина отказа", Width = 400, SizeToContent = SizeToContent.Height,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false,
                    Background = Avalonia.Media.Brush.Parse("#F4F7F9")
                };

                bool confirm = false;
                string reason = "";

                var stack = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 15 };
                stack.Children.Add(new TextBlock { Text = $"Укажите причину отказа для {app.ClientFullName}:", FontWeight = Avalonia.Media.FontWeight.SemiBold });
                
                var reasonBox = new TextBox { Watermark = "Введите причину...", AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Height = 80, CornerRadius = new Avalonia.CornerRadius(6) };
                stack.Children.Add(reasonBox);

                var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
                var cancelBtn = new Button { Content = "Отмена", Background = Avalonia.Media.Brush.Parse("#BDC3C7"), Foreground = Avalonia.Media.Brushes.White, CornerRadius = new Avalonia.CornerRadius(6) };
                var confirmBtn = new Button { Content = "Подтвердить", Background = Avalonia.Media.Brush.Parse("#E74C3C"), Foreground = Avalonia.Media.Brushes.White, CornerRadius = new Avalonia.CornerRadius(6) };

                cancelBtn.Click += (s, ev) => { confirm = false; dialog.Close(); };
                confirmBtn.Click += (s, ev) => { confirm = true; reason = reasonBox.Text ?? ""; dialog.Close(); };

                buttons.Children.Add(cancelBtn);
                buttons.Children.Add(confirmBtn);
                stack.Children.Add(buttons);
                dialog.Content = stack;

                await dialog.ShowDialog((Window)this.VisualRoot);

                if (confirm)
                {
                    try
                    {
                        using (var command = _context.Database.GetDbConnection().CreateCommand())
                        {
                            command.CommandText = "UPDATE job_application SET status = 'rejected', rejection_reason = @reason, reviewed_at = NOW() WHERE application_id = @id";
                            command.Parameters.Add(new Npgsql.NpgsqlParameter("@id", app.ApplicationId));
                            command.Parameters.Add(new Npgsql.NpgsqlParameter("@reason", reason));
                            _context.Database.OpenConnection();
                            command.ExecuteNonQuery();
                        }
                        LoadData();
                    }
                    catch (Exception) { }
                }
            }
        }

        private async void CreateTrainerCard_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is JobApplicationView app)
            {
                var window = new TrainerCreateWindow(app);
                await window.ShowDialog((Window)this.VisualRoot);
                if (window.Tag is bool result && result)
                {
                    LoadData();
                }
            }
        }

        private async void Application_Tapped(object sender, TappedEventArgs e)
        {
            if (e.Source is Avalonia.Visual visual && visual.GetVisualAncestors().OfType<Button>().Any())
            {
                return;
            }

            if (((Border)sender).Tag is JobApplicationView app)
            {
                var dialog = new JobApplicationDetailsWindow(app);
                await dialog.ShowDialog((Window)this.VisualRoot);
                LoadData();
            }
        }

        public void RefreshView() => LoadData();
    }
}