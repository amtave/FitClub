using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FitClub.Models;
using FitClub.Services;
using System;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Input;
using Microsoft.EntityFrameworkCore;

namespace FitClub.Admin.Views
{
    public partial class AdminHomeView : UserControl
    {
        private readonly AppDbContext _context;
        private readonly ClubInfoService _clubInfoService;
        private readonly ContentService _contentService;

        public ClubInfo ClubInfo { get; set; }
        public ClubInfo EditClubInfo { get; set; }

        public AdminHomeView()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _clubInfoService = new ClubInfoService(_context);
            _contentService = new ContentService(_context);
            
            LoadData();
            LoadPromotions();
            LoadNews();
        }

        private void LoadData()
        {
            ClubInfo = _context.ClubInfos.FirstOrDefault();

            if (ClubInfo == null)
            {
                ClubInfo = new ClubInfo
                {
                    ClubName = "FitClub",
                    WelcomeText = "Современный фитнес-клуб для тех, кто ценит комфорт и результат. Достигайте своих целей вместе с нами!",
                    Address = "ул. Спортивная, д. 1",
                    Phone = "+7 (999) 000-00-00",
                    WorkingHours = "08:00 - 23:00",
                    UpdatedAt = DateTime.Now
                };
                _context.ClubInfos.Add(ClubInfo);
                _context.SaveChanges();
            }

            EditClubInfo = new ClubInfo
            {
                InfoId = ClubInfo.InfoId,
                ClubName = ClubInfo.ClubName,
                WelcomeText = ClubInfo.WelcomeText,
                Address = ClubInfo.Address,
                Phone = ClubInfo.Phone,
                WorkingHours = ClubInfo.WorkingHours,
                LogoPath = ClubInfo.LogoPath
            };

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
                var editLogoImage = this.FindControl<Image>("EditLogoImage");
                var editLogoPlaceholder = this.FindControl<TextBlock>("EditLogoPlaceholder");

                if (!string.IsNullOrEmpty(path))
                {
                    string fullPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", path);
                    fullPath = Path.GetFullPath(fullPath);
                    
                    if (File.Exists(fullPath))
                    {
                        var bitmap = new Bitmap(fullPath);
                        if (logoImage != null) logoImage.Source = bitmap;
                        if (logoPlaceholder != null) logoPlaceholder.IsVisible = false;
                        if (editLogoImage != null) editLogoImage.Source = bitmap;
                        if (editLogoPlaceholder != null) editLogoPlaceholder.IsVisible = false;
                    }
                    else
                    {
                        if (logoImage != null) logoImage.Source = null;
                        if (logoPlaceholder != null) logoPlaceholder.IsVisible = true;
                        if (editLogoImage != null) editLogoImage.Source = null;
                        if (editLogoPlaceholder != null) editLogoPlaceholder.IsVisible = true;
                    }
                }
                else
                {
                    if (logoImage != null) logoImage.Source = null;
                    if (logoPlaceholder != null) logoPlaceholder.IsVisible = true;
                    if (editLogoImage != null) editLogoImage.Source = null;
                    if (editLogoPlaceholder != null) editLogoPlaceholder.IsVisible = true;
                }
            }
            catch
            {
                var logoImage = this.FindControl<Image>("LogoImage");
                var logoPlaceholder = this.FindControl<TextBlock>("LogoPlaceholder");
                var editLogoImage = this.FindControl<Image>("EditLogoImage");
                var editLogoPlaceholder = this.FindControl<TextBlock>("EditLogoPlaceholder");
                
                if (logoImage != null) logoImage.Source = null;
                if (logoPlaceholder != null) logoPlaceholder.IsVisible = true;
                if (editLogoImage != null) editLogoImage.Source = null;
                if (editLogoPlaceholder != null) editLogoPlaceholder.IsVisible = true;
            }
        }

        private async void LoadLogoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog();
                dialog.Title = "Выберите логотип";
                dialog.AllowMultiple = false;
                dialog.Filters.Add(new FileDialogFilter { Name = "Изображения", Extensions = { "png", "jpg", "jpeg", "bmp", "gif" } });
                
                var result = await dialog.ShowAsync((Window)this.VisualRoot);
                
                if (result != null && result.Any())
                {
                    string selectedFile = result[0];
                    string fileName = $"club_logo_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(selectedFile)}";
                    string assetsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets");
                    string destinationPath = Path.Combine(assetsPath, fileName);
                    
                    if (!Directory.Exists(assetsPath))
                        Directory.CreateDirectory(assetsPath);
                    
                    File.Copy(selectedFile, destinationPath, true);
                    
                    EditClubInfo.LogoPath = $"Assets/{fileName}";
                    LoadLogo(EditClubInfo.LogoPath);
                    
                    ShowStatus("Логотип загружен. Нажмите 'Сохранить изменения'", true);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка загрузки логотипа: {ex.Message}", false);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModePanel.IsVisible = false;
            EditModePanel.IsVisible = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            ViewModePanel.IsVisible = true;
            EditModePanel.IsVisible = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var dbInfo = _context.ClubInfos.FirstOrDefault(c => c.InfoId == EditClubInfo.InfoId);

            if (dbInfo != null)
            {
                dbInfo.ClubName = EditClubInfo.ClubName ?? "";
                dbInfo.WelcomeText = EditClubInfo.WelcomeText ?? "";
                dbInfo.Address = EditClubInfo.Address ?? "";
                dbInfo.Phone = EditClubInfo.Phone ?? "";
                dbInfo.WorkingHours = EditClubInfo.WorkingHours ?? "";
                dbInfo.LogoPath = EditClubInfo.LogoPath;
                dbInfo.UpdatedAt = DateTime.Now;

                _context.SaveChanges();
            }
            else
            {
                _context.ClubInfos.Add(new ClubInfo
                {
                    ClubName = EditClubInfo.ClubName ?? "",
                    WelcomeText = EditClubInfo.WelcomeText ?? "",
                    Address = EditClubInfo.Address ?? "",
                    Phone = EditClubInfo.Phone ?? "",
                    WorkingHours = EditClubInfo.WorkingHours ?? "",
                    LogoPath = EditClubInfo.LogoPath,
                    UpdatedAt = DateTime.Now
                });
                _context.SaveChanges();
            }

            ViewModePanel.IsVisible = true;
            EditModePanel.IsVisible = false;
            LoadData();
            ShowStatus("Информация о клубе успешно сохранена!", true);
        }

        private void PhoneBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                string text = textBox.Text;
                string digits = new string(text.Where(c => char.IsDigit(c) || c == '+').ToArray());

                if (digits.StartsWith("+7"))
                    digits = digits.Substring(2);
                else if (digits.StartsWith("7") || digits.StartsWith("8"))
                    digits = digits.Substring(1);

                digits = new string(digits.Take(10).ToArray());
                string formatted = "";

                if (digits.Length >= 10)
                    formatted = $"+7 {digits.Substring(0, 3)} {digits.Substring(3, 3)}-{digits.Substring(6, 2)}-{digits.Substring(8)}";
                else if (digits.Length >= 6)
                    formatted = $"+7 {digits.Substring(0, 3)} {digits.Substring(3, 3)}-{digits.Substring(6)}";
                else if (digits.Length >= 3)
                    formatted = $"+7 {digits.Substring(0, 3)} {digits.Substring(3)}";
                else if (digits.Length > 0)
                    formatted = $"+7 {digits}";

                if (textBox.Text != formatted)
                {
                    textBox.Text = formatted;
                    textBox.CaretIndex = textBox.Text.Length;
                }
            }
        }

        private void LoadPromotions()
        {
            try
            {
                var promotions = _contentService.GetActivePromotions();
                var promotionsControl = this.FindControl<ItemsControl>("PromotionsItemsControl");
                var noPromotionsMessage = this.FindControl<Border>("NoPromotionsMessage");

                if (promotionsControl != null)
                {
                    if (promotions.Any())
                    {
                        promotionsControl.ItemsSource = promotions;
                        if (noPromotionsMessage != null) noPromotionsMessage.IsVisible = false;
                    }
                    else
                    {
                        promotionsControl.ItemsSource = null;
                        if (noPromotionsMessage != null) noPromotionsMessage.IsVisible = true;
                    }
                }
            }
            catch (Exception) { }
        }

        private void AddPromotionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowStatus("Функция добавления акции будет доступна позже", true);
        }

        private void EditPromotionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowStatus("Редактирование акции будет доступно позже", true);
        }

        private async void DeletePromotionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var promotion = (Promotion)button.Tag;

                var result = await ShowConfirmDialog("Подтверждение удаления", $"Вы уверены, что хотите удалить акцию '{promotion.Title}'?");

                if (result)
                {
                    promotion.IsActive = false;
                    _context.Promotions.Update(promotion);
                    await _context.SaveChangesAsync();

                    LoadPromotions();
                    ShowStatus("Акция удалена", true);
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
                var noNewsMessage = this.FindControl<Border>("NoNewsMessage");

                if (newsControl != null)
                {
                    if (news.Any())
                    {
                        newsControl.ItemsSource = news;
                        if (noNewsMessage != null) noNewsMessage.IsVisible = false;
                    }
                    else
                    {
                        newsControl.ItemsSource = null;
                        if (noNewsMessage != null) noNewsMessage.IsVisible = true;
                    }
                }
            }
            catch (Exception) { }
        }

        private async void AddNewsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new NewsEditWindow();
                await window.ShowDialog((Window)this.VisualRoot);

                if (window.Tag is bool result && result)
                {
                    LoadNews();
                    ShowStatus("Новость успешно добавлена!", true);
                }
            }
            catch (Exception) { }
        }

        private async void EditNewsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var news = (News)button.Tag;

                var window = new NewsEditWindow(news);
                await window.ShowDialog((Window)this.VisualRoot);

                if (window.Tag is bool result && result)
                {
                    LoadNews();
                    ShowStatus("Новость успешно обновлена!", true);
                }
            }
            catch (Exception) { }
        }

        private async void DeleteNewsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = (Button)sender;
                var news = (News)button.Tag;

                var result = await ShowConfirmDialog("Подтверждение удаления", $"Вы уверены, что хотите удалить новость '{news.Title}'?");

                if (result)
                {
                    news.IsActive = false;
                    _context.News.Update(news);
                    await _context.SaveChangesAsync();

                    LoadNews();
                    ShowStatus("Новость удалена", true);
                }
            }
            catch (Exception) { }
        }

        private async Task<bool> ShowConfirmDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title, Width = 350, Height = 200, WindowStartupLocation = WindowStartupLocation.CenterOwner, CanResize = false
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };
            stackPanel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 10) });

            var buttonPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Spacing = 10 };
            var yesButton = new Button { Content = "Да", Padding = new Thickness(25, 5), Background = Brush.Parse("#E74C3C"), Foreground = Brushes.White };
            var noButton = new Button { Content = "Нет", Padding = new Thickness(25, 5), Background = Brush.Parse("#95A5A6"), Foreground = Brushes.White };

            bool result = false;
            yesButton.Click += (s, e) => { result = true; dialog.Close(); };
            noButton.Click += (s, e) => { result = false; dialog.Close(); };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
            stackPanel.Children.Add(buttonPanel);
            dialog.Content = stackPanel;

            await dialog.ShowDialog((Window)this.VisualRoot);
            return result;
        }

        private void ShowStatus(string message, bool isSuccess)
        {
            var statusBorder = this.FindControl<Border>("StatusBorder");
            var statusText = this.FindControl<TextBlock>("StatusText");

            if (statusBorder != null && statusText != null)
            {
                statusText.Text = message;
                statusBorder.Background = isSuccess ? Brush.Parse("#27AE60") : Brush.Parse("#E74C3C");
                statusBorder.IsVisible = true;

                var timer = new System.Timers.Timer(3000);
                timer.Elapsed += (s, args) =>
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => { statusBorder.IsVisible = false; });
                    timer.Dispose();
                };
                timer.Start();
            }
        }
    }
}