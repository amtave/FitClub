using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace FitClub.Admin.Views
{
    public partial class DuplicateScheduleWindow : Window
    {
        public DateTime? SelectedStartDate { get; private set; }
        public DateTime? SelectedEndDate { get; private set; }

        public DuplicateScheduleWindow(DateTime sourceDate)
        {
            InitializeComponent();
            SourceDateText.Text = $"Копируем расписание за: {sourceDate:dd MMMM yyyy}";
            StartDatePicker.SelectedDate = sourceDate.AddDays(1);
            EndDatePicker.SelectedDate = sourceDate.AddDays(1);
        }

        public DuplicateScheduleWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                return;
            }
            
            SelectedStartDate = StartDatePicker.SelectedDate.Value.Date;
            SelectedEndDate = EndDatePicker.SelectedDate.Value.Date;
            
            Close(true);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close(false);
    }
}