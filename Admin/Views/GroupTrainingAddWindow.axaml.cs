using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using FitClub.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FitClub.Admin.Views
{
    public partial class GroupTrainingAddWindow : Window
    {
        private readonly AppDbContext _context;
        private string _imagePath = null;

        public GroupTrainingAddWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadData();
        }

        private void LoadData()
        {
            var intensities = _context.IntensityLevels.ToList();
            IntensityComboBox.ItemsSource = intensities;
            if (intensities.Any())
                IntensityComboBox.SelectedIndex = 0;

            var types = _context.TrainingTypes.ToList();
            TypeComboBox.ItemsSource = types;
            if (types.Any())
                TypeComboBox.SelectedIndex = 0;
        }

        private async void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            var storage = this.StorageProvider;
            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите изображение",
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
                
                _imagePath = "Assets/" + fileName;
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                NameTextBox.BorderBrush = Brushes.Red;
                NameErrorText.IsVisible = true;
                isValid = false;
            }
            else
            {
                NameTextBox.BorderBrush = null;
                NameErrorText.IsVisible = false;
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                DescriptionTextBox.BorderBrush = Brushes.Red;
                DescriptionErrorText.IsVisible = true;
                isValid = false;
            }
            else
            {
                DescriptionTextBox.BorderBrush = null;
                DescriptionErrorText.IsVisible = false;
            }

            if (!isValid) return;

            try
            {
                var newTraining = new GroupTraining
                {
                    Name = NameTextBox.Text,
                    Description = DescriptionTextBox.Text,
                    DurationMinutes = (int)(DurationNumeric.Value ?? 60),
                    Price = PriceNumeric.Value ?? 0,
                    IsActive = IsActiveCheckBox.IsChecked ?? true,
                    IntensityId = (IntensityComboBox.SelectedItem as IntensityLevel)?.IntensityId ?? 1,
                    TypeId = (TypeComboBox.SelectedItem as TrainingType)?.TypeId ?? 2,
                    MaxParticipants = (int)(MaxParticipantsNumeric.Value ?? 20),
                    ImagePath = _imagePath ?? "Assets/default_image.png"
                };

                _context.GroupTrainings.Add(newTraining);
                await _context.SaveChangesAsync();
                
                this.Tag = true;
                this.Close();
            }
            catch (Exception ex)
            {
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Tag = false;
            this.Close();
        }
    }
}