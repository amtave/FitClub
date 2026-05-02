using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using FitClub.Models;
using FitClub.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;

namespace FitClub.Client.Views
{
    public partial class HomeView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly ContentService _contentService;

        public ClubInfo ClubInfo { get; set; }
        public string GreetingText { get; set; }

        public HomeView()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _contentService = new ContentService(_context);

            LoadData();
            LoadPromotions();
            LoadNews();
            CheckUserApplications();
        }

        private void LoadData()
        {
            string currentEmail = UserSession.CurrentUserEmail;
            var client = _context.Clients.FirstOrDefault(c => c.Email == currentEmail);
            
            if (client != null)
            {
                GreetingText = $"Отличный день для новых рекордов, {client.FirstName}! 🏆";
            }
            else
            {
                GreetingText = "Время становиться лучше с каждой тренировкой! 🏆";
            }

            ClubInfo = _context.ClubInfos.FirstOrDefault();

            if (ClubInfo == null)
            {
                ClubInfo = new ClubInfo
                {
                    ClubName = "FitClub",
                    WelcomeText = "Современный фитнес-клуб для тех, кто ценит комфорт и результат. Достигайте своих целей вместе с нами!",
                    Address = "ул. Спортивная, д. 1",
                    Phone = "+7 (999) 000-00-00",
                    WorkingHours = "08:00 - 23:00"
                };
            }

            LoadLogo(ClubInfo.LogoPath);

            DataContext = null;
            DataContext = this;
        }

        private void LoadLogo(string path)
        {
            try
            {
                var logoImage = this.FindControl<Image>("LogoImage");
                var logoPlaceholder = this.FindControl<TextBlock>("LogoPlaceholder");

                if (!string.IsNullOrEmpty(path))
                {
                    string fullPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", path);
                    fullPath = Path.GetFullPath(fullPath);

                    if (File.Exists(fullPath))
                    {
                        if (logoImage != null) logoImage.Source = new Bitmap(fullPath);
                        if (logoPlaceholder != null) logoPlaceholder.IsVisible = false;
                    }
                    else
                    {
                        if (logoImage != null) logoImage.Source = null;
                        if (logoPlaceholder != null) logoPlaceholder.IsVisible = true;
                    }
                }
                else
                {
                    if (logoImage != null) logoImage.Source = null;
                    if (logoPlaceholder != null) logoPlaceholder.IsVisible = true;
                }
            }
            catch
            {
                var logoImage = this.FindControl<Image>("LogoImage");
                var logoPlaceholder = this.FindControl<TextBlock>("LogoPlaceholder");
                if (logoImage != null) logoImage.Source = null;
                if (logoPlaceholder != null) logoPlaceholder.IsVisible = true;
            }
        }

        private void LoadPromotions()
        {
            try
            {
                var promotions = _contentService.GetActivePromotions();
                var promotionsControl = this.FindControl<ItemsControl>("PromotionsItemsControl");

                if (promotionsControl != null)
                {
                    promotionsControl.ItemsSource = promotions.Any() ? promotions : null;
                }
            }
            catch (Exception) { }
        }

        private void LoadNews()
        {
            try
            {
                var news = _context.News.Where(n => n.IsActive).OrderByDescending(n => n.CreatedAt).ToList();
                var newsControl = this.FindControl<ItemsControl>("NewsItemsControl");

                if (newsControl != null)
                {
                    newsControl.ItemsSource = news.Any() ? news : null;
                }
            }
            catch (Exception) { }
        }

        private void CheckUserApplications()
        {
            try
            {
                string currentEmail = UserSession.CurrentUserEmail;
                var client = _context.Clients.FirstOrDefault(c => c.Email == currentEmail);
                
                if (client != null)
                {
                    bool hasApplications = _context.JobApplications.Any(ja => ja.ClientId == client.ClientId);
                    var myApplicationsButton = this.FindControl<Button>("MyApplicationsButton");
                    
                    if (myApplicationsButton != null)
                    {
                        myApplicationsButton.IsVisible = hasApplications;
                    }
                }
            }
            catch (Exception) { }
        }

        private void OpenJobApplicationForm_Click(object sender, RoutedEventArgs e)
        {
            if (this.VisualRoot is Window window)
            {
                var mainContent = window.FindControl<ContentControl>("MainContentControl");
                if (mainContent != null)
                {
                    mainContent.Content = new JobApplicationForm();
                }
            }
        }

        private void OpenMyApplications_Click(object sender, RoutedEventArgs e)
        {
            if (this.VisualRoot is Window window)
            {
                var mainContent = window.FindControl<ContentControl>("MainContentControl");
                if (mainContent != null)
                {
                    mainContent.Content = new MyApplicationsView();
                }
            }
        }

        public void RefreshView()
        {
            CheckUserApplications();
        }
    }
}