using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Linq;
using FitClub.Trainer.Views;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;

namespace FitClub.Trainer
{
    public partial class Menu_trainer : Window
    {
        private User _currentUser;
        private Models.Trainer _currentTrainer;
        private DispatcherTimer _notificationTimer;

        public Menu_trainer(User user)
        {
            InitializeComponent();
            
            _currentUser = user;
            
            using (var context = new AppDbContext())
            {
                _currentTrainer = context.Trainers
                    .FirstOrDefault(t => t.Email == user.Email);
            }
            
            UserSession.Login(_currentTrainer != null ? _currentTrainer.Email : user.Email, "Trainer");
            
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
                var notifications = db.TrainerNotifications
                    .Where(n => n.TrainerId == _currentTrainer.TrainerId)
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
                var unread = db.TrainerNotifications
                    .Where(n => n.TrainerId == _currentTrainer.TrainerId && !n.IsRead)
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
                var notifications = db.TrainerNotifications
                    .Where(n => n.TrainerId == _currentTrainer.TrainerId)
                    .ToList();
                
                if (notifications.Any())
                {
                    db.TrainerNotifications.RemoveRange(notifications);
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
            using (var context = new AppDbContext())
            {
                Models.Trainer freshTrainer = null;
                if (_currentTrainer != null)
                {
                    freshTrainer = context.Trainers
                        .FirstOrDefault(t => t.TrainerId == _currentTrainer.TrainerId);
                }
                
                var accountView = new AccountView(_currentUser, freshTrainer);
                MainContentControl.Content = accountView;
            }
        }

        private void OpenIndividual_Click(object sender, RoutedEventArgs e)
        {
            var individualView = new IndividualTrainingsView();
            individualView.RefreshView(); 
            MainContentControl.Content = individualView;
        }

        private void OpenGroup_Click(object sender, RoutedEventArgs e)
        {
            var groupView = new GroupTrainingsView();
            groupView.RefreshView(); 
            MainContentControl.Content = groupView;
        }

        private void OpenClients_Click(object sender, RoutedEventArgs e)
        {
            var clientsView = new TrainerClientsView();
            clientsView.RefreshView(); 
            MainContentControl.Content = clientsView;
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
    }
}