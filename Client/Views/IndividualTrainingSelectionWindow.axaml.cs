using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Linq;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using Avalonia.Media;

namespace FitClub.Views
{
    public partial class IndividualTrainingSelectionWindow : Window
    {
        private readonly Tariff _tariff;
        private readonly Models.Client _client;
        private readonly AppDbContext _db;
        
        public TrainingType SelectedTrainingType { get; private set; }
        public Models.Trainer SelectedTrainer { get; private set; }
        public bool PaymentConfirmed { get; private set; }

        public IndividualTrainingSelectionWindow(Tariff tariff, Models.Client client)
        {
            InitializeComponent();
            _tariff = tariff;
            _client = client;
            _db = new AppDbContext();
            
            LoadTypes();
        }

        private void LoadTypes()
        {
            TypesList.ItemsSource = _db.TrainingTypes.OrderBy(t => t.Name).ToList();
            EmptyTrainersPanel.IsVisible = true;
        }

        private void TypesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TypesList.SelectedItem is TrainingType type)
            {
                SelectedTrainingType = type;
                SelectedTrainer = null;
                
                var trainers = _db.Trainers
                    .Where(t => t.IsActive && t.Specialization != null && t.Specialization.Contains(type.Name))
                    .ToList();
                
                TrainersItemsControl.ItemsSource = trainers;
                EmptyTrainersPanel.IsVisible = !trainers.Any();
                UpdateStatus();
            }
        }

        private void TrainerSelect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Models.Trainer trainer)
            {
                SelectedTrainer = trainer;
                UpdateStatus();
            }
        }

        private void UpdateStatus()
        {
            if (SelectedTrainingType != null && SelectedTrainer != null)
            {
                SelectionStatusText.Text = $"Выбрано: {SelectedTrainingType.Name} (Тренер: {SelectedTrainer.LastName})";
                SelectionStatusText.Foreground = Brushes.Green;
                ConfirmButton.IsEnabled = true;
            }
            else
            {
                SelectionStatusText.Text = "Выберите направление и тренера";
                SelectionStatusText.Foreground = Brushes.Red;
                ConfirmButton.IsEnabled = false;
            }
        }

        private async void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var paymentWindow = new PaymentWindow(_tariff, _client);
            await paymentWindow.ShowDialog(this);

            if (paymentWindow.PaymentSuccess)
            {
                PaymentConfirmed = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}