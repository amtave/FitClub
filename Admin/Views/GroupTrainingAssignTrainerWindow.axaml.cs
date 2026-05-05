using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Media;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FitClub.Admin.Views
{
    public partial class GroupTrainingAssignTrainerWindow : Window
    {
        private readonly int _trainingId;
        private readonly AppDbContext _db;
        private List<TrainerSelectionItem> _allTrainers;
        private string _searchText = "";

        public GroupTrainingAssignTrainerWindow(int trainingId)
        {
            InitializeComponent();
            _trainingId = trainingId;
            _db = new AppDbContext();
            SpecComboBox.SelectedIndex = 0;
            LoadTrainers();
        }

        public GroupTrainingAssignTrainerWindow()
        {
            InitializeComponent();
        }

        private void LoadTrainers()
        {
            var allTrainersList = _db.Trainers.Where(t => t.IsActive).OrderBy(t => t.LastName).ToList();
            var assignedTrainerIds = _db.TrainingTrainers
                .Where(tt => tt.TrainingId == _trainingId)
                .Select(tt => tt.TrainerId)
                .ToList();

            _allTrainers = allTrainersList.Select(t => new TrainerSelectionItem
            {
                Trainer = t,
                IsSelected = assignedTrainerIds.Contains(t.TrainerId)
            }).ToList();

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_allTrainers == null) return;

            var filtered = _allTrainers.AsEnumerable();

            if (SpecComboBox?.SelectedItem is ComboBoxItem specItem)
            {
                string selectedSpec = specItem.Content.ToString();
                if (selectedSpec != "Все специализации")
                {
                    filtered = filtered.Where(t => t.Trainer.Specialization != null && t.Trainer.Specialization.Contains(selectedSpec, System.StringComparison.OrdinalIgnoreCase));
                }
            }

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                filtered = filtered.Where(t => t.Trainer.FullName.Contains(_searchText, System.StringComparison.OrdinalIgnoreCase));
            }

            TrainersItemsControl.ItemsSource = filtered.ToList();
        }

        private void SpecComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            _searchText = (sender as TextBox)?.Text ?? "";
            ApplyFilter();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            var existingLinks = _db.TrainingTrainers.Where(tt => tt.TrainingId == _trainingId).ToList();
            _db.TrainingTrainers.RemoveRange(existingLinks);

            var selectedTrainers = _allTrainers.Where(t => t.IsSelected).ToList();
            foreach (var st in selectedTrainers)
            {
                _db.TrainingTrainers.Add(new TrainingTrainer
                {
                    TrainingId = _trainingId,
                    TrainerId = st.Trainer.TrainerId,
                    IsPrimary = true 
                });
            }

            await _db.SaveChangesAsync();
            Tag = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Tag = false;
            Close();
        }
    }

    public class TrainerSelectionItem : INotifyPropertyChanged
    {
        public Models.Trainer Trainer { get; set; }
        
        private bool _isSelected;
        public bool IsSelected 
        { 
            get => _isSelected; 
            set 
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BorderColor));
                }
            }
        }

        public IBrush BorderColor => IsSelected ? Brush.Parse("#27AE60") : Brush.Parse("#E0E0E0");

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}