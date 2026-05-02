using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Platform;

namespace FitClub.Views
{
    public class TrainerReviewDisplayItem
    {
        public string ClientName { get; set; }
        public Bitmap ClientPhoto { get; set; }
        public string Comment { get; set; }
        public int Rating { get; set; }
        public DateTime Date { get; set; }
        public string Stars => new string('★', Rating) + new string('☆', 5 - Rating);
        public string RelativeDate => Date.ToString("dd MMMM yyyy");
    }

    public partial class TrainerCardWindow : Window
    {
        private readonly Models.Trainer _trainer;
        private readonly List<TrainerReviewDisplayItem> _allReviews = new();
        private readonly ObservableCollection<TrainerReviewDisplayItem> _visibleReviews = new();
        private int _currentCount = 0;
        private const int PageSize = 5;

        public TrainerCardWindow(Models.Trainer trainer)
        {
            InitializeComponent();
            _trainer = trainer;
            
            TrainerNameText.Text = _trainer.FullName;
            SpecializationText.Text = string.IsNullOrEmpty(_trainer.Specialization) ? "Общий профиль" : _trainer.Specialization;
            ExperienceText.Text = _trainer.ExperienceInfo;
            TrainerPhoto.Source = _trainer.PhotoBitmap;

            LoadData();
        }

        private static Bitmap GetClientAvatar(string avatarPath)
        {
            try
            {
                if (string.IsNullOrEmpty(avatarPath))
                    return new Bitmap(AssetLoader.Open(new Uri("avares://FitClub/Assets/default_avatar.png")));

                if (avatarPath.StartsWith("avares://"))
                    return new Bitmap(AssetLoader.Open(new Uri(avatarPath)));

                return new Bitmap(avatarPath);
            }
            catch
            {
                try
                {
                    return new Bitmap(AssetLoader.Open(new Uri("avares://FitClub/Assets/default_avatar.png")));
                }
                catch
                {
                    return null;
                }
            }
        }

        private void LoadData()
        {
            using var db = new AppDbContext();

            var groupData = db.TrainingBookings
                .Include(b => b.Client)
                .Include(b => b.TrainingSchedule)
                .Where(b => b.TrainingSchedule.TrainerId == _trainer.TrainerId && b.Rating.HasValue)
                .ToList();

            var groupReviews = groupData.Select(b => new TrainerReviewDisplayItem
            {
                ClientName = b.Client.FullName,
                ClientPhoto = GetClientAvatar(b.Client.AvatarPath),
                Comment = b.Review,
                Rating = b.Rating.Value,
                Date = b.BookingDate
            }).ToList();

            var individualData = db.IndividualTrainings
                .Include(it => it.Client)
                .Where(it => it.TrainerId == _trainer.TrainerId && it.Rating.HasValue)
                .ToList();

            var individualReviews = individualData.Select(it => new TrainerReviewDisplayItem
            {
                ClientName = it.Client != null ? it.Client.FullName : "Клиент",
                ClientPhoto = it.Client != null ? GetClientAvatar(it.Client.AvatarPath) : GetClientAvatar(""),
                Comment = it.Review,
                Rating = it.Rating.Value,
                Date = it.TrainingDate
            }).ToList();

            _allReviews.AddRange(groupReviews);
            _allReviews.AddRange(individualReviews);
            
            var sortedReviews = _allReviews.OrderByDescending(r => r.Date).ToList();
            _allReviews.Clear();
            _allReviews.AddRange(sortedReviews);

            if (_allReviews.Any())
            {
                double avg = _allReviews.Average(r => r.Rating);
                AverageRatingText.Text = avg.ToString("0.0");
                TotalReviewsText.Text = $"({_allReviews.Count} отзывов)";
            }
            else
            {
                AverageRatingText.Text = "0.0";
                TotalReviewsText.Text = "(нет отзывов)";
                NoReviewsPanel.IsVisible = true;
            }

            ReviewsControl.ItemsSource = _visibleReviews;
            ShowMore();
        }

        private void ShowMore()
        {
            var toAdd = _allReviews.Skip(_currentCount).Take(_currentCount == 0 ? 3 : PageSize).ToList();
            foreach (var item in toAdd)
            {
                _visibleReviews.Add(item);
            }
            _currentCount += toAdd.Count;
            ShowMoreButton.IsVisible = _currentCount < _allReviews.Count;
        }

        private void ShowMore_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e) => ShowMore();
    }
}