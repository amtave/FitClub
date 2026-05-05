using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FitClub.Models;
using System;

namespace FitClub.Client.Views
{
    public partial class BonusCardApplicationWindow : Window
    {
        private Models.Client _client;

        public BonusCardApplicationWindow(Models.Client client)
        {
            InitializeComponent();
            _client = client;
            LoadClientData();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadClientData()
        {
            // Заполняем автоматически данные клиента
            this.FindControl<TextBlock>("FullNameText").Text = _client.FullName;
            this.FindControl<TextBlock>("PhoneText").Text = _client.Phone;
            this.FindControl<TextBlock>("PassportText").Text = 
                $"{_client.PassportSeries} {_client.PassportNumber}";
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            Close(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}