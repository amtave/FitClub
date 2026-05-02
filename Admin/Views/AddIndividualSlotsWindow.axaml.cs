using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FitClub.Admin.Views
{
    public partial class AddIndividualSlotsWindow : Window
    {
        public List<TimeSpan> SelectedTimes { get; private set; } = new();

        public AddIndividualSlotsWindow(List<(TimeSpan Start, TimeSpan End)> occupiedRanges)
        {
            InitializeComponent();
            foreach (var child in SlotsStack.Children)
            {
                if (child is CheckBox cb && cb.Tag is string timeStr)
                {
                    var slotStart = TimeSpan.Parse(timeStr);
                    var slotEnd = slotStart.Add(TimeSpan.FromHours(1));

                    bool conflict = occupiedRanges.Any(r => slotStart < r.End && slotEnd > r.Start);
                    if (conflict)
                    {
                        cb.IsEnabled = false;
                        cb.Content += " (Занято)";
                        cb.Foreground = Avalonia.Media.Brushes.Gray;
                    }
                }
            }
        }

        public AddIndividualSlotsWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in SlotsStack.Children)
            {
                if (child is CheckBox cb && cb.IsChecked == true && cb.IsEnabled && cb.Tag is string timeStr)
                {
                    SelectedTimes.Add(TimeSpan.Parse(timeStr));
                }
            }
            Close(true);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close(false);
    }
}