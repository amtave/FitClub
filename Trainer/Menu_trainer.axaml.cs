using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;
using FitClub.Trainer.Views;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace FitClub.Trainer
{
    public partial class Menu_trainer : Window
    {
        private User _currentUser;
        private Models.Trainer _currentTrainer;

        public Menu_trainer(User user)
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            
            _currentUser = user;
            
            // Получаем данные тренера
            using (var context = new AppDbContext())
            {
                _currentTrainer = context.Trainers
                    .FirstOrDefault(t => t.Email == user.Email);
            }
            
            // Сохраняем email в сессии для использования в других окнах
            UserSession.Login(_currentTrainer != null ? _currentTrainer.Email : user.Email, "Trainer"); // ДОБАВЛЕН ПАРАМЕТР "Trainer"
            
            // Показываем главную страницу при загрузке
            OpenHome_Click(null, null);
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
            individualView.RefreshView(); // Обновляем данные
            MainContentControl.Content = individualView;
        }

        private void OpenGroup_Click(object sender, RoutedEventArgs e)
        {
            var groupView = new GroupTrainingsView();
            groupView.RefreshView(); // Обновляем данные
            MainContentControl.Content = groupView;
        }

        private void OpenClients_Click(object sender, RoutedEventArgs e)
        {
            var clientsView = new TrainerClientsView();
            clientsView.RefreshView(); // Обновляем данные
            MainContentControl.Content = clientsView;
        }

        private void OpenPlan_Click(object sender, RoutedEventArgs e)
        {
            var planView = new TrainingPlanView();
            planView.RefreshView(); // Обновляем данные
            MainContentControl.Content = planView;
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
                // Очищаем сессию при выходе
                UserSession.Logout();
                
                var loginWindow = new Avtoriz();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}