using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using FitClub.Models;
using System;
using System.IO;
using System.Linq;

namespace FitClub.Admin.Views
{
    public partial class TrainerEditWindow : Window
    {
        private readonly Models.Trainer _trainer;
        private string _newImagePath = null;
        private bool _isFormattingPhone = false;

        public TrainerEditWindow(Models.Trainer trainer)
        {
            InitializeComponent();
            _trainer = trainer;
            LoadData();
        }

        private void LoadData()
        {
            LastNameTextBox.Text = _trainer.LastName;
            FirstNameTextBox.Text = _trainer.FirstName;
            MiddleNameTextBox.Text = _trainer.MiddleName;
            PhoneTextBox.Text = _trainer.Phone;
            EmailTextBox.Text = _trainer.Email;
            ExpNumeric.Value = _trainer.ExperienceYears;
            PriceNumeric.Value = _trainer.IndividualTrainingPrice;
            IsActiveCheckBox.IsChecked = _trainer.IsActive;

            BioTextBox.Text = _trainer.Bio;
            AchievementsTextBox.Text = _trainer.Achievements;

            if (!string.IsNullOrEmpty(_trainer.Specialization))
            {
                var match = SpecComboBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(item => _trainer.Specialization.Contains(item.Content?.ToString() ?? ""));
                
                if (match != null)
                {
                    SpecComboBox.SelectedItem = match;
                }
            }

            ScheduleComboBox.SelectedIndex = _trainer.WorkSchedule == "wed-sun" ? 1 : 0;

            if (_trainer.PhotoBitmap != null)
                PreviewImage.Source = _trainer.PhotoBitmap;
        }

        private void OnPhoneTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormattingPhone) return;

            var textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                _isFormattingPhone = true;
                
                string text = textBox.Text;
                string digits = new string(text.Where(c => char.IsDigit(c) || c == '+').ToArray());
                
                if (digits.StartsWith("+7"))
                {
                    digits = digits.Substring(2);
                }
                else if (digits.StartsWith("7") || digits.StartsWith("8"))
                {
                    digits = digits.Substring(1);
                }
                
                digits = new string(digits.Take(10).ToArray());
                string formatted = "";
                
                if (digits.Length >= 10)
                {
                    formatted = $"+7 {digits.Substring(0, 3)} {digits.Substring(3, 3)}-{digits.Substring(6, 2)}-{digits.Substring(8)}";
                }
                else if (digits.Length >= 6)
                {
                    formatted = $"+7 {digits.Substring(0, 3)} {digits.Substring(3, 3)}-{digits.Substring(6)}";
                }
                else if (digits.Length >= 3)
                {
                    formatted = $"+7 {digits.Substring(0, 3)} {digits.Substring(3)}";
                }
                else if (digits.Length > 0)
                {
                    formatted = $"+7 {digits}";
                }
                
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
                var t = db.Trainers.Find(_trainer.TrainerId);
                if (t != null)
                {
                    t.LastName = LastNameTextBox.Text ?? "";
                    t.FirstName = FirstNameTextBox.Text ?? "";
                    t.MiddleName = MiddleNameTextBox.Text ?? "";
                    t.Phone = PhoneTextBox.Text ?? "";
                    t.Email = EmailTextBox.Text ?? "";
                    
                    if (SpecComboBox.SelectedItem is ComboBoxItem specItem)
                        t.Specialization = specItem.Content.ToString();

                    t.ExperienceYears = (int)(ExpNumeric.Value ?? 0);
                    t.IndividualTrainingPrice = PriceNumeric.Value ?? 2000;
                    t.IsActive = IsActiveCheckBox.IsChecked ?? true;
                    t.WorkSchedule = ScheduleComboBox.SelectedIndex == 1 ? "wed-sun" : "mon-fri";
                    
                    t.Bio = BioTextBox.Text ?? "";
                    t.Achievements = AchievementsTextBox.Text ?? "";

                    if (!string.IsNullOrEmpty(_newImagePath))
                        t.PhotoPath = _newImagePath;

                    var user = db.Users.FirstOrDefault(u => u.Email == _trainer.Email);
                    if (user != null && t.Email != _trainer.Email)
                        user.Email = t.Email;

                    await db.SaveChangesAsync();
                    Tag = true;
                    Close();
                }
            }
            catch (Exception) { }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Tag = false;
            Close();
        }
    }
}