using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FitClub.Client.Views
{
    public partial class MyApplicationsView : UserControl
    {
        public MyApplicationsView()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.VisualRoot is Window window)
                {
                    var mainContent = window.FindControl<ContentControl>("MainContentControl");
                    if (mainContent != null)
                    {
                        mainContent.Content = new HomeView();
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"Ошибка при возврате: {ex.Message}");
            }
        }
    }
}