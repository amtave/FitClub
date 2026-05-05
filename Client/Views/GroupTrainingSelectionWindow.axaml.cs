using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Linq;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using Avalonia.Media;

namespace FitClub.Views
{
    public partial class GroupTrainingSelectionWindow : Window
    {
        private readonly Tariff _tariff;
        private readonly Models.Client _client;
        private readonly AppDbContext _db;
        
        public GroupTraining SelectedTrainingType { get; private set; }
        public Models.Trainer SelectedTrainer { get; private set; }
        public bool PaymentConfirmed { get; private set; }

        public GroupTrainingSelectionWindow(Tariff tariff, Models.Client client)
        {
            InitializeComponent();
            _tariff = tariff;
            _client = client;
            _db = new AppDbContext();
            
            LoadDirections();
        }

        private void LoadDirections()
        {
            DirectionsList.ItemsSource = _db.GroupTrainings.Where(gt => gt.IsActive).ToList();
            EmptyTrainersPanel.IsVisible = true;
        }

        private void DirectionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DirectionsList.SelectedItem is GroupTraining direction)
            {
                SelectedTrainingType = direction;
                SelectedTrainer = null;
                
                var trainers = _db.TrainingTrainers
                    .Include(tt => tt.Trainer)
                    .Where(tt => tt.TrainingId == direction.TypeId && tt.Trainer.IsActive)
                    .Select(tt => tt.Trainer)
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
                SelectionStatusText.Text = $"Выбрано: {SelectedTrainingType.Name} ({SelectedTrainer.FullName})";
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
            var paymentWindow = new FitClub.Views.PaymentWindow(_tariff, _client);
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