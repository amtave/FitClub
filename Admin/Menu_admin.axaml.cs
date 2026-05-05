using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FitClub.Admin.Views;
using FitClub.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;

namespace FitClub.Admin
{
    public partial class Menu_admin : Window
    {
        public Menu_admin()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            OpenHome_Click(null, null);
        }

        private void OpenHome_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Открываем главную страницу администратора");
                MainContentControl.Content = new AdminHomeView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии главной: {ex.Message}");
            }
        }

        private void OpenClients_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Открываем страницу клиентов");
                MainContentControl.Content = new ClientsView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии клиентов: {ex.Message}");
            }
        }

        private void OpenTrainers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Открываем страницу управления тренерами");
                MainContentControl.Content = new Views.TrainersAdminView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии страницы тренеров: {ex.Message}");
            }
        }

        private void OpenTariffs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Открываем страницу управления тарифами");
                MainContentControl.Content = new Views.TariffsAdminView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии страницы тарифов: {ex.Message}");
            }
        }

        private void OpenStatistics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Открываем страницу статистики");
                MainContentControl.Content = new Views.StatisticsView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии статистики: {ex.Message}");
            }
        }

        private void OpenGroupTrainings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainContentControl.Content = new GroupTrainingsAdminView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии групповых тренировок: {ex.Message}");
            }
        }

        private void OpenSchedule_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("Открываем страницу расписания");
                MainContentControl.Content = new TrainerScheduleView();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при открытии расписания: {ex.Message}");
            }
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
                var loginWindow = new Avtoriz();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}