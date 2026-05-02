using Avalonia.Controls;
using Avalonia.Interactivity;
using FitClub.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FitClub.Admin.Views
{
    public partial class AddGroupTrainingSlotWindow : Window
    {
        public GroupTraining SelectedTraining { get; private set; }
        public TimeSpan SelectedTime { get; private set; }

        public AddGroupTrainingSlotWindow(List<GroupTraining> trainings, List<(TimeSpan Start, TimeSpan End)> occupiedRanges)
        {
            InitializeComponent();
            
            TrainingComboBox.ItemsSource = trainings;
            if (trainings.Any()) TrainingComboBox.SelectedIndex = 0;

            var timeSlots = new[] { 
                new TimeSpan(8,0,0), new TimeSpan(9,0,0), new TimeSpan(10,0,0),
                new TimeSpan(11,0,0), new TimeSpan(12,0,0), new TimeSpan(13,0,0),
                new TimeSpan(14,0,0), new TimeSpan(15,0,0), new TimeSpan(16,0,0),
                new TimeSpan(17,0,0), new TimeSpan(18,0,0), new TimeSpan(19,0,0),
                new TimeSpan(20,0,0)
            };

            foreach (var time in timeSlots)
            {
                var endTime = time.Add(TimeSpan.FromHours(1));
                bool conflict = occupiedRanges.Any(r => time < r.End && endTime > r.Start);
                
                var rb = new RadioButton 
                { 
                    Content = time.ToString(@"hh\:mm"), 
                    Tag = time
                };

                if (conflict)
                {
                    rb.IsEnabled = false;
                    rb.Content += " (Занято)";
                    rb.Foreground = Avalonia.Media.Brushes.Gray;
                }
                
                TimeRadioStack.Children.Add(rb);
            }
        }

        public AddGroupTrainingSlotWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (TrainingComboBox.SelectedItem is GroupTraining training)
            {
                var selectedRb = TimeRadioStack.Children.OfType<RadioButton>().FirstOrDefault(r => r.IsChecked == true);
                if (selectedRb != null && selectedRb.Tag is TimeSpan time)
                {
                    SelectedTraining = training;
                    SelectedTime = time;
                    Close(true);
                    return;
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close(false);
    }
}