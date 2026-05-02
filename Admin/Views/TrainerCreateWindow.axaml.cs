using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using FitClub.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.IO;
using System.Linq;

namespace FitClub.Admin.Views
{
    public partial class TrainerCreateWindow : Window
    {
        private readonly JobApplicationView _application;
        private string _newImagePath = null;
        private bool _isFormattingPhone = false;

        public TrainerCreateWindow(JobApplicationView application)
        {
            InitializeComponent();
            _application = application;
            LoadData();
            GeneratePassword();
        }

        private void LoadData()
        {
            var parts = _application.ClientFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0) LastNameTextBox.Text = parts[0];
            if (parts.Length > 1) FirstNameTextBox.Text = parts[1];
            if (parts.Length > 2) MiddleNameTextBox.Text = parts[2];

            PhoneTextBox.Text = _application.ClientPhone;
            ExpNumeric.Value = _application.ExperienceYears ?? 0;
            
            BioTextBox.Text = _application.Motivation ?? "";
            AchievementsTextBox.Text = _application.OfferToClub ?? "";

            if (!string.IsNullOrEmpty(_application.Specialization))
            {
                var match = SpecComboBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(item => _application.Specialization.Contains(item.Content?.ToString() ?? ""));
                
                if (match != null) SpecComboBox.SelectedItem = match;
            }

            if (_application.AvatarBitmap != null)
            {
                PreviewImage.Source = _application.AvatarBitmap;
                _newImagePath = _application.AvatarPath;
            }
        }

        private void GeneratePassword()
        {
            var random = new Random();
            string emailPrefix = _application.ClientEmail?.Split('@').FirstOrDefault() ?? "trainer";
            
            EmailTextBox.Text = $"trainer_{emailPrefix}{random.Next(10, 99)}@fitness.ru";

            const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            PasswordTextBox.Text = new string(Enumerable.Repeat(chars, 10).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void GeneratePassword_Click(object sender, RoutedEventArgs e) => GeneratePassword();

        private void OnPhoneTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormattingPhone) return;

            var textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                _isFormattingPhone = true;
                string text = textBox.Text;
                string digits = new string(text.Where(c => char.IsDigit(c) || c == '+').ToArray());
                
                if (digits.StartsWith("+7")) digits = digits.Substring(2);
                else if (digits.StartsWith("7") || digits.StartsWith("8")) digits = digits.Substring(1);
                
                digits = new string(digits.Take(10).ToArray());
                string formatted = "";
                
                if (digits.Length >= 10) formatted = $"+7 {digits.Substring(0, 3)} {digits.Substring(3, 3)}-{digits.Substring(6, 2)}-{digits.Substring(8)}";
                else if (digits.Length >= 6) formatted = $"+7 {digits.Substring(0, 3)} {digits.Substring(3, 3)}-{digits.Substring(6)}";
                else if (digits.Length >= 3) formatted = $"+7 {digits.Substring(0, 3)} {digits.Substring(3)}";
                else if (digits.Length > 0) formatted = $"+7 {digits}";
                
                if (textBox.Text != formatted)
                {
                    textBox.Text = formatted;
                    textBox.CaretIndex = textBox.Text.Length;
                }
                _isFormattingPhone = false;
            }
        }

        private async void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            var storage = this.StorageProvider;
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите фото тренера",
                FileTypeFilter = new[] { FilePickerFileTypes.ImageAll }
            });

            if (files.Any())
            {
                var file = files.First();
                var stream = await file.OpenReadAsync();
                PreviewImage.Source = new Bitmap(stream);
                
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.Name);
                string destinationPath = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                
                using (var destStream = File.Create(destinationPath))
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(destStream);
                }
                _newImagePath = "Assets/" + fileName;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var db = new AppDbContext();
                
                var newUser = new User
                {
                    Email = EmailTextBox.Text ?? "",
                    Password = PasswordTextBox.Text ?? "",
                    RoleId = 2 
                };
                db.Users.Add(newUser);

                var newTrainer = new Models.Trainer
                {
                    LastName = LastNameTextBox.Text ?? "",
                    FirstName = FirstNameTextBox.Text ?? "",
                    MiddleName = MiddleNameTextBox.Text ?? "",
                    Phone = PhoneTextBox.Text ?? "",
                    Email = EmailTextBox.Text ?? "",
                    ExperienceYears = (int)(ExpNumeric.Value ?? 0),
                    IndividualTrainingPrice = PriceNumeric.Value ?? 2000,
                    IsActive = true,
                    WorkSchedule = ScheduleComboBox.SelectedIndex == 1 ? "wed-sun" : "mon-fri",
                    PhotoPath = _newImagePath ?? "",
                    Bio = BioTextBox.Text ?? "",
                    Achievements = AchievementsTextBox.Text ?? ""
                };

                if (SpecComboBox.SelectedItem is ComboBoxItem specItem)
                {
                    newTrainer.Specialization = specItem.Content.ToString();
                }

                db.Trainers.Add(newTrainer);

                var jobApp = db.JobApplications.Find(_application.ApplicationId);
                if (jobApp != null)
                {
                    jobApp.Status = "accepted";
                    jobApp.ReviewedAt = DateTime.Now;
                }

                await db.SaveChangesAsync();
                Tag = true;
                Close();
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Ошибка сохранения", 
                    $"Не удалось сохранить тренера.\n{ex.Message}\n{ex.InnerException?.Message}", 
                    ButtonEnum.Ok);
                await box.ShowWindowDialogAsync(this);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Tag = false;
            Close();
        }
    }
}