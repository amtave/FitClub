using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Linq;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace FitClub.Views
{
    public partial class PremiumTrainingSelectionWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly Tariff _tariff;
        private readonly Models.Client _client;

        public GroupTraining SelectedGroupTraining { get; set; }
        public Models.Trainer SelectedGroupTrainer { get; set; }
        public TrainingType SelectedIndividualTraining { get; set; }
        public Models.Trainer SelectedIndividualTrainer { get; set; }
        public bool PaymentConfirmed { get; set; } = false;

        public PremiumTrainingSelectionWindow(Tariff tariff, Models.Client client)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _tariff = tariff;
            _client = client;

            LoadTrainingTypes();
            SetupEventHandlers();
        }

        private void LoadTrainingTypes()
        {
            var groupTrainings = _context.GroupTrainings
                .Include(gt => gt.TrainingType)
                .Where(gt => gt.IsActive)
                .ToList();

            var trainingTypes = _context.TrainingTypes.OrderBy(t => t.Name).ToList();

            GroupTrainingTypeComboBox.ItemsSource = groupTrainings;
            IndividualTrainingTypeComboBox.ItemsSource = trainingTypes;
        }

        private void SetupEventHandlers()
        {
            GroupTrainingTypeComboBox.SelectionChanged += GroupTrainingTypeComboBox_SelectionChanged;
            GroupTrainerComboBox.SelectionChanged += GroupTrainerComboBox_SelectionChanged;
            IndividualTrainingTypeComboBox.SelectionChanged += IndividualTrainingTypeComboBox_SelectionChanged;
            IndividualTrainerComboBox.SelectionChanged += IndividualTrainerComboBox_SelectionChanged;
        }

        private void GroupTrainingTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedGroupTraining = GroupTrainingTypeComboBox.SelectedItem as GroupTraining;
            if (SelectedGroupTraining != null)
            {
                LoadGroupTrainersForTraining(SelectedGroupTraining.TrainingId);
                UpdateSelectionInfo();
            }
            else
            {
                GroupTrainerComboBox.ItemsSource = null;
                GroupTrainerComboBox.IsEnabled = false;
                UpdateSelectionInfo();
            }
        }

        private void GroupTrainerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedGroupTrainer = GroupTrainerComboBox.SelectedItem as Models.Trainer;
            UpdateSelectionInfo();
        }

        private void IndividualTrainingTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedIndividualTraining = IndividualTrainingTypeComboBox.SelectedItem as TrainingType;
            if (SelectedIndividualTraining != null)
            {
                var trainers = _context.Trainers
                    .Where(t => t.IsActive && t.Specialization != null && t.Specialization.Contains(SelectedIndividualTraining.Name))
                    .ToList();
                IndividualTrainerComboBox.ItemsSource = trainers;
                IndividualTrainerComboBox.IsEnabled = trainers.Any();
                UpdateSelectionInfo();
            }
            else
            {
                IndividualTrainerComboBox.ItemsSource = null;
                IndividualTrainerComboBox.IsEnabled = false;
                UpdateSelectionInfo();
            }
        }

        private void IndividualTrainerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedIndividualTrainer = IndividualTrainerComboBox.SelectedItem as Models.Trainer;
            UpdateSelectionInfo();
        }

        private void LoadGroupTrainersForTraining(int trainingId)
        {
            var trainers = _context.TrainingTrainers
                .Where(tt => tt.TrainingId == trainingId)
                .Include(tt => tt.Trainer)
                .Where(tt => tt.Trainer.IsActive)
                .Select(tt => tt.Trainer)
                .Distinct()
                .ToList();

            GroupTrainerComboBox.ItemsSource = trainers;
            GroupTrainerComboBox.IsEnabled = trainers.Any();
        }

        private void UpdateSelectionInfo()
        {
            bool groupSelected = SelectedGroupTraining != null && SelectedGroupTrainer != null;
            bool individualSelected = SelectedIndividualTraining != null && SelectedIndividualTrainer != null;

            if (groupSelected && individualSelected)
            {
                GroupSelectionText.Text = $"👥 {SelectedGroupTraining.Name} ({SelectedGroupTrainer.LastName})";
                IndividualSelectionText.Text = $"💪 {SelectedIndividualTraining.Name} ({SelectedIndividualTrainer.LastName})";
                SelectionInfoBorder.IsVisible = true;
                SelectionStatusText.IsVisible = false;
                ContinueButton.IsEnabled = true;
            }
            else
            {
                SelectionInfoBorder.IsVisible = false;
                SelectionStatusText.IsVisible = true;
                ContinueButton.IsEnabled = false;
            }
        }

        private async void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGroupTraining == null || SelectedGroupTrainer == null || 
                SelectedIndividualTraining == null || SelectedIndividualTrainer == null)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("Ошибка", "Пожалуйста, выберите тренировки и тренеров", ButtonEnum.Ok)
                    .ShowWindowDialogAsync(this);
                return;
            }

            var paymentWindow = new PaymentWindow(_tariff, _client);
            await paymentWindow.ShowDialog(this);

            if (paymentWindow.PaymentSuccess)
            {
                PaymentConfirmed = true;
                Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentConfirmed = false;
            Close();
        }
    }
}