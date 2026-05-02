using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using FitClub.Models;
using System.Linq;

namespace FitClub.Views
{
    public partial class WorkoutRatingWindow : Window
    {
        private int _currentRating = 0;
        private readonly WorkoutDisplayItem _workoutItem;
        private readonly List<Button> _stars;

        public WorkoutRatingWindow(WorkoutDisplayItem item)
        {
            InitializeComponent();
            _workoutItem = item;
            WorkoutNameText.Text = item.Title;
            _stars = new List<Button> { Star1, Star2, Star3, Star4, Star5 };
            UpdateStars();
        }

        private void Star_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Tag?.ToString(), out int rating))
            {
                _currentRating = rating;
                UpdateStars();
            }
        }

        private void UpdateStars()
        {
            for (int i = 0; i < _stars.Count; i++)
            {
                _stars[i].Foreground = (i < _currentRating) ? Brush.Parse("#FFD700") : Brush.Parse("#BDC3C7");
            }
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRating == 0) return;
            if (string.IsNullOrWhiteSpace(CommentTextBox.Text)) return;

            try
            {
                using var db = new AppDbContext();
                if (_workoutItem.WorkoutType == "Group" && _workoutItem.OriginalObject is TrainingBooking gb)
                {
                    var booking = db.TrainingBookings.FirstOrDefault(b => b.BookingId == gb.BookingId);
                    if (booking != null)
                    {
                        booking.Rating = _currentRating;
                        booking.Review = CommentTextBox.Text;
                    }
                }
                else if (_workoutItem.WorkoutType == "Individual" && _workoutItem.OriginalObject is IndividualTraining it)
                {
                    var training = db.IndividualTrainings.FirstOrDefault(t => t.IndividualTrainingId == it.IndividualTrainingId);
                    if (training != null)
                    {
                        training.Rating = _currentRating;
                        training.Review = CommentTextBox.Text;
                    }
                }

                await db.SaveChangesAsync();
                Close(true);
            }
            catch { }
        }

        private void Back_Click(object sender, RoutedEventArgs e) => Close(false);
    }
}