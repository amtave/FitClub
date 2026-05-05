using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.VisualTree;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace FitClub.Admin.Views
{
    public partial class ClientsView : UserControl
    {
        private AppDbContext _context;
        private List<ClientRequestView> _allClients = new List<ClientRequestView>();
        private ObservableCollection<ClientRequestView> _displayClients = new ObservableCollection<ClientRequestView>();
        private bool _isInitialized = false;

        public ClientsView()
        {
            InitializeComponent();
            _isInitialized = true;
            _context = new AppDbContext();
            
            var list = this.FindControl<ItemsControl>("ClientsList");
            if (list != null) list.ItemsSource = _displayClients;
            
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using var db = new AppDbContext();
                _allClients.Clear();
                var clients = db.Clients.ToList();
                var requests = db.PassportVerificationRequests.ToList();

                foreach (var c in clients)
                {
                    var req = requests.Where(r => r.ClientId == c.ClientId)
                                      .OrderByDescending(r => r.SubmittedAt)
                                      .FirstOrDefault();

                    int statusId = 1;
                    string reason = "";
                    int reqId = 0;
                    DateTime submitted = DateTime.MinValue;

                    if (req != null)
                    {
                        statusId = req.StatusId;
                        reason = req.RejectionReason ?? "";
                        reqId = req.RequestId;
                        submitted = req.SubmittedAt;
                    }
                    else if (!string.IsNullOrEmpty(c.PassportSeries) && !string.IsNullOrEmpty(c.PassportNumber))
                    {
                        statusId = 3;
                    }

                    _allClients.Add(new ClientRequestView {
                        RequestId = reqId,
                        ClientId = c.ClientId,
                        ClientFullName = c.FullName,
                        PassportSeries = c.PassportSeries,
                        PassportNumber = c.PassportNumber,
                        SubmittedAt = submitted,
                        StatusId = statusId,
                        RejectionReason = reason,
                        AvatarPath = c.AvatarPath ?? ""
                    });
                }
                ApplyFilters();
            }
            catch (Exception) { }
        }

        private void ApplyFilters()
        {
            if (!_isInitialized || _displayClients == null) return;

            var search = this.FindControl<TextBox>("SearchBox")?.Text?.ToLower() ?? "";
            var status = this.FindControl<ComboBox>("StatusFilter")?.SelectedIndex ?? 0;
            
            var filtered = _allClients.Where(a => string.IsNullOrEmpty(search) || a.ClientFullName.ToLower().Contains(search));
            
            if (status == 1) filtered = filtered.Where(a => a.StatusId == 2);
            else if (status == 2) filtered = filtered.Where(a => a.StatusId == 3);
            else if (status == 3) filtered = filtered.Where(a => a.StatusId == 1 || a.StatusId == 4);
            
            _displayClients.Clear();
            foreach (var app in filtered.OrderByDescending(x => x.StatusId == 2).ThenByDescending(x => x.SubmittedAt).ThenBy(x => x.ClientFullName)) 
            {
                _displayClients.Add(app);
            }
            
            var emptyMsg = this.FindControl<Border>("EmptyMessage");
            if (emptyMsg != null) emptyMsg.IsVisible = _displayClients.Count == 0;
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e) => ApplyFilters();
        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private async void QuickApprove_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ClientRequestView req)
            {
                var box = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard("Подтверждение", $"Подтвердить паспортные данные {req.ClientFullName}?", MsBox.Avalonia.Enums.ButtonEnum.YesNo);
                if (await box.ShowWindowDialogAsync((Window)this.VisualRoot) == MsBox.Avalonia.Enums.ButtonResult.Yes)
                {
                    try
                    {
                        using var db = new AppDbContext();
                        var dbReq = db.PassportVerificationRequests.Find(req.RequestId);
                        if (dbReq != null)
                        {
                            dbReq.StatusId = 3;
                            dbReq.ReviewedAt = DateTime.Now;
                            var client = db.Clients.Find(req.ClientId);
                            if (client != null)
                            {
                                client.PassportSeries = dbReq.PassportSeries;
                                client.PassportNumber = dbReq.PassportNumber;
                            }
                            await db.SaveChangesAsync();
                            LoadData();
                        }
                    }
                    catch (Exception) { }
                }
            }
        }

        private async void QuickReject_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ClientRequestView req)
            {
                var dialog = new Window { Title = "Причина отказа", Width = 400, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false, Background = Avalonia.Media.Brush.Parse("#F4F7F9") };
                var stack = new StackPanel { Margin = new Avalonia.Thickness(20), Spacing = 10 };
                var box = new TextBox { Watermark = "Введите причину...", Height = 80, AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap, CornerRadius = new Avalonia.CornerRadius(6) };
                var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
                var cancelBtn = new Button { Content = "Отмена", Background = Avalonia.Media.Brush.Parse("#BDC3C7"), Foreground = Avalonia.Media.Brushes.White };
                var confirmBtn = new Button { Content = "Подтвердить", Background = Avalonia.Media.Brush.Parse("#E74C3C"), Foreground = Avalonia.Media.Brushes.White };
                
                bool ok = false;
                cancelBtn.Click += (s, ev) => dialog.Close();
                confirmBtn.Click += (s, ev) => { ok = true; dialog.Close(); };
                buttons.Children.Add(cancelBtn); buttons.Children.Add(confirmBtn);
                stack.Children.Add(new TextBlock { Text = $"Отказ для {req.ClientFullName}:", FontWeight = Avalonia.Media.FontWeight.SemiBold });
                stack.Children.Add(box); stack.Children.Add(buttons);
                dialog.Content = stack;

                await dialog.ShowDialog((Window)this.VisualRoot);
                if (ok)
                {
                    using var db = new AppDbContext();
                    var dbReq = db.PassportVerificationRequests.Find(req.RequestId);
                    if (dbReq != null)
                    {
                        dbReq.StatusId = 4;
                        dbReq.RejectionReason = box.Text ?? "";
                        dbReq.ReviewedAt = DateTime.Now;
                        await db.SaveChangesAsync();
                        LoadData();
                    }
                }
            }
        }

        private async void Client_Tapped(object sender, TappedEventArgs e)
        {
            if (e.Source is Avalonia.Visual v && v.GetVisualAncestors().OfType<Button>().Any()) return;
            
            if ((sender as Border)?.Tag is ClientRequestView req)
            {
                if (req.IsPending)
                {
                    var win = new ClientVerificationWindow(req);
                    await win.ShowDialog((Window)this.VisualRoot);
                    LoadData();
                }
                else
                {
                    using var db = new AppDbContext();
                    var client = db.Clients.FirstOrDefault(c => c.ClientId == req.ClientId);
                    if (client != null)
                    {
                        var dialog = new ClientDetailsWindow(client, req.StatusId);
                        await dialog.ShowDialog((Window)this.VisualRoot);
                        LoadData();
                    }
                }
            }
        }

        public void RefreshView() => LoadData();
    }
}