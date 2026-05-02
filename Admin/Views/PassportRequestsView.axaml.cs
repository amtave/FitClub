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

namespace FitClub.Admin.Views
{
    public partial class PassportRequestsView : UserControl
    {
        private AppDbContext _context;
        private List<ClientRequestView> _allRequests = new List<ClientRequestView>();
        private ObservableCollection<ClientRequestView> _displayRequests = new ObservableCollection<ClientRequestView>();
        private bool _isInitialized = false;

        public PassportRequestsView()
        {
            InitializeComponent();
            _isInitialized = true;
            _context = new AppDbContext();
            
            var list = this.FindControl<ItemsControl>("RequestsList");
            if (list != null) list.ItemsSource = _displayRequests;
            
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _allRequests.Clear();
                var sql = @"SELECT r.request_id, r.client_id, r.passport_series, r.passport_number, 
                                   r.submitted_at, r.status_id, r.rejection_reason,
                                   c.last_name, c.first_name, c.middle_name, c.avatar_path
                            FROM passport_verification_request r
                            JOIN client c ON r.client_id = c.client_id";

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    _context.Database.OpenConnection();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _allRequests.Add(new ClientRequestView {
                                RequestId = reader.GetInt32(reader.GetOrdinal("request_id")),
                                ClientId = reader.GetInt32(reader.GetOrdinal("client_id")),
                                ClientFullName = $"{reader.GetString(reader.GetOrdinal("last_name"))} {reader.GetString(reader.GetOrdinal("first_name"))} {reader.GetString(reader.GetOrdinal("middle_name"))}",
                                PassportSeries = reader.GetString(reader.GetOrdinal("passport_series")),
                                PassportNumber = reader.GetString(reader.GetOrdinal("passport_number")),
                                SubmittedAt = reader.GetDateTime(reader.GetOrdinal("submitted_at")),
                                StatusId = reader.GetInt32(reader.GetOrdinal("status_id")),
                                RejectionReason = reader.IsDBNull(reader.GetOrdinal("rejection_reason")) ? "" : reader.GetString(reader.GetOrdinal("rejection_reason")),
                                AvatarPath = reader.IsDBNull(reader.GetOrdinal("avatar_path")) ? "" : reader.GetString(reader.GetOrdinal("avatar_path"))
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
            if (!_isInitialized || _displayRequests == null) return;

            var search = this.FindControl<TextBox>("SearchBox")?.Text?.ToLower() ?? "";
            var status = this.FindControl<ComboBox>("StatusFilter")?.SelectedIndex ?? 0;
            
            var filtered = _allRequests.Where(a => string.IsNullOrEmpty(search) || a.ClientFullName.ToLower().Contains(search));
            
            if (status == 1) filtered = filtered.Where(a => a.StatusId == 2);
            else if (status == 2) filtered = filtered.Where(a => a.StatusId == 3);
            else if (status == 3) filtered = filtered.Where(a => a.StatusId == 4);
            
            _displayRequests.Clear();
            
            foreach (var app in filtered.OrderByDescending(x => x.StatusId == 2).ThenByDescending(x => x.SubmittedAt)) 
            {
                _displayRequests.Add(app);
            }
            
            var emptyMsg = this.FindControl<Border>("EmptyMessage");
            if (emptyMsg != null) emptyMsg.IsVisible = !_displayRequests.Any();
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e) => ApplyFilters();
        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        
        private void Back_Click(object sender, RoutedEventArgs e) 
        { 
            if (this.VisualRoot is Window w) 
            { 
                var c = w.FindControl<ContentControl>("MainContentControl") ?? w.FindControl<ContentControl>("MainContent"); 
                if (c != null) c.Content = new ClientsView();
            } 
        }

        private async void Reject_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ClientRequestView req)
            {
                var dialog = new Window { Title = "Причина отказа", Width = 400, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false, Background = Avalonia.Media.Brush.Parse("#F4F7F9") };
                var stack = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 10 };
                var box = new TextBox { Watermark = "Введите причину...", Height = 80, AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap, CornerRadius = new Avalonia.CornerRadius(6) };
                var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
                var cancelBtn = new Button { Content = "Отмена", Background = Avalonia.Media.Brush.Parse("#BDC3C7"), Foreground = Avalonia.Media.Brushes.White, CornerRadius = new Avalonia.CornerRadius(6) };
                var confirmBtn = new Button { Content = "Подтвердить", Background = Avalonia.Media.Brush.Parse("#E74C3C"), Foreground = Avalonia.Media.Brushes.White, CornerRadius = new Avalonia.CornerRadius(6) };
                
                bool ok = false;
                cancelBtn.Click += (s, ev) => dialog.Close();
                confirmBtn.Click += (s, ev) => { ok = true; dialog.Close(); };
                
                buttons.Children.Add(cancelBtn);
                buttons.Children.Add(confirmBtn);
                stack.Children.Add(new TextBlock { Text = $"Укажите причину отказа для {req.ClientFullName}:", FontWeight = Avalonia.Media.FontWeight.SemiBold });
                stack.Children.Add(box);
                stack.Children.Add(buttons);
                dialog.Content = stack;
                
                await dialog.ShowDialog((Window)this.VisualRoot);
                if (ok)
                {
                    try
                    {
                        using (var command = _context.Database.GetDbConnection().CreateCommand())
                        {
                            command.CommandText = "UPDATE passport_verification_request SET status_id = 4, rejection_reason = @r, reviewed_at = NOW() WHERE request_id = @id";
                            command.Parameters.Add(new Npgsql.NpgsqlParameter("@r", box.Text ?? ""));
                            command.Parameters.Add(new Npgsql.NpgsqlParameter("@id", req.RequestId));
                            _context.Database.OpenConnection();
                            command.ExecuteNonQuery();
                        }
                        LoadData();
                    }
                    catch (Exception) { }
                }
            }
        }

        private async void Review_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ClientRequestView req)
            {
                var win = new ClientVerificationWindow(req);
                await win.ShowDialog((Window)this.VisualRoot);
                if (win.Tag is bool r && r) LoadData();
            }
        }

        private async void Request_Tapped(object sender, TappedEventArgs e)
        {
            if (e.Source is Avalonia.Visual v && v.GetVisualAncestors().OfType<Button>().Any()) return;
            if ((sender as Border)?.Tag is ClientRequestView req)
            {
                var win = new ClientVerificationWindow(req);
                await win.ShowDialog((Window)this.VisualRoot);
                if (win.Tag is bool r && r) LoadData();
            }
        }
    }
}