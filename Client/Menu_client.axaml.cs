using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Linq;
using FitClub.Client.Views;
using FitClub.Models;
using FitClub.Views;

namespace FitClub.Client
{
    public partial class Menu_client : Window
    {
        private User _currentUser;
        private Models.Client _currentClient;
        private DispatcherTimer _notificationTimer;

        public Menu_client(User user, Models.Client client)
        {
            InitializeComponent();
            
            _currentUser = user;
            _currentClient = client;
            
            UserSession.Login(_currentClient.Email, "Client");
            
            LoadNotifications();
            StartNotificationTimer();
            
            OpenHome_Click(null, null);
        }

        private void StartNotificationTimer()
        {
            _notificationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _notificationTimer.Tick += (sender, args) => LoadNotifications();
            _notificationTimer.Start();
        }

        private void LoadNotifications()
        {
            try
            {
                using var db = new AppDbContext();
                var notifications = db.ClientNotifications
                    .Where(n => n.ClientId == _currentClient.ClientId)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToList();

                int unreadCount = notifications.Count(n => !n.IsRead);
                
                NotificationBadge.IsVisible = unreadCount > 0;
                NotificationCountText.Text = unreadCount.ToString();
                NotificationsList.ItemsSource = notifications;
            }
            catch { }
        }

        private void Notifications_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext();
                var unread = db.ClientNotifications
                    .Where(n => n.ClientId == _currentClient.ClientId && !n.IsRead)
                    .ToList();
                
                if (unread.Any())
                {
                    foreach (var n in unread)
                    {
                        n.IsRead = true;
                    }
                    db.SaveChanges();
                    
                    NotificationBadge.IsVisible = false;
                }
            }
            catch { }
        }

        private void ClearNotifications_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext();
                var notifications = db.ClientNotifications
                    .Where(n => n.ClientId == _currentClient.ClientId)
                    .ToList();
                
                if (notifications.Any())
                {
                    db.ClientNotifications.RemoveRange(notifications);
                    db.SaveChanges();
                    LoadNotifications();
                }
            }
            catch { }
        }

        private void OpenHome_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new HomeView();
        }

        private void OpenAccount_Click(object sender, RoutedEventArgs e)
        {
            var context = new AppDbContext();
            var freshClient = context.Clients.Find(_currentClient.ClientId);
            var accountView = new AccountView(_currentUser, freshClient);
            MainContentControl.Content = accountView;
        }

        private void OpenTariffs_Click(object sender, RoutedEventArgs e)
        {
            var tariffsView = new TariffsView();
            tariffsView.RefreshView(); 
            MainContentControl.Content = tariffsView;
        }

        private void OpenSchedule_Click(object sender, RoutedEventArgs e)
        {
            var trainingsView = new GroupTrainingsView();
            trainingsView.RefreshView(); 
            MainContentControl.Content = trainingsView;
        }

        private void OpenMyWorkouts_Click(object sender, RoutedEventArgs e)
        {
            var workoutsView = new MyWorkoutsView();
            workoutsView.RefreshView(); 
            MainContentControl.Content = workoutsView;
        }

        private void OpenTrainers_Click(object sender, RoutedEventArgs e)
        {
            MainContentControl.Content = new TrainersView();
        }

        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Подтверждение", 
                "Вы уверены, что хотите выйти?", 
                ButtonEnum.YesNo);
            
            var result = await box.ShowWindowDialogAsync(this);
            
            if (result == ButtonResult.Yes)
            {
                _notificationTimer?.Stop();
                UserSession.Logout();
                
                var loginWindow = new Avtoriz();
                loginWindow.Show();
                this.Close();
            }
        }
        
        public void NavigateToTrainersView(string searchText = null)
        {
            var trainersView = new FitClub.Client.Views.TrainersView();
            
            if (!string.IsNullOrEmpty(searchText))
            {
                trainersView.SearchAndShowTrainer(searchText);
            }
            
            MainContentControl.Content = trainersView;
        }
        
        public void RefreshMyWorkouts()
        {
            try
            {
                if (MainContentControl.Content is MyWorkoutsView myWorkoutsView)
                {
                    myWorkoutsView.RefreshView();
                }
            }
            catch { }
        }
    }
}