using Avalonia.Controls;
using Avalonia.Interactivity;
using FitClub.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace FitClub.Manager
{
    public partial class Menu_manager : Window
    {
        private User _currentUser;

        public Menu_manager(User user)
        {
            InitializeComponent();
            _currentUser = user;
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