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
    public partial class GroupTrainingEditWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly GroupTraining _training;
        private string _newImagePath = null;

        public GroupTrainingEditWindow(GroupTraining training)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _training = training;
            LoadData();
        }

        private void LoadData()
        {
            var intensities = _context.IntensityLevels.ToList();
            IntensityComboBox.ItemsSource = intensities;
            IntensityComboBox.SelectedItem = intensities.FirstOrDefault(i => i.IntensityId == _training.IntensityId);

            var types = _context.TrainingTypes.ToList();
            TypeComboBox.ItemsSource = types;
            TypeComboBox.SelectedItem = types.FirstOrDefault(t => t.TypeId == _training.TypeId);

            NameTextBox.Text = _training.Name;
            DescriptionTextBox.Text = _training.Description;
            DurationNumeric.Value = _training.DurationMinutes;
            PriceNumeric.Value = _training.Price;
            MaxParticipantsNumeric.Value = _training.MaxParticipants;
            IsActiveCheckBox.IsChecked = _training.IsActive;

            if (_training.TrainingImage != null)
                PreviewImage.Source = _training.TrainingImage;
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
                
                _newImagePath = "Assets/" + fileName;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
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
                using (var db = new AppDbContext())
                {
                    var trainingToUpdate = db.GroupTrainings.Find(_training.TrainingId);
                    if (trainingToUpdate != null)
                    {
                        trainingToUpdate.Name = NameTextBox.Text;
                        trainingToUpdate.Description = DescriptionTextBox.Text;
                        trainingToUpdate.DurationMinutes = (int)(DurationNumeric.Value ?? 0);
                        trainingToUpdate.Price = PriceNumeric.Value ?? 0;
                        trainingToUpdate.MaxParticipants = (int)(MaxParticipantsNumeric.Value ?? 0);
                        trainingToUpdate.IsActive = IsActiveCheckBox.IsChecked ?? false;

                        if (IntensityComboBox.SelectedItem is IntensityLevel selectedIntensity)
                            trainingToUpdate.IntensityId = selectedIntensity.IntensityId;

                        if (TypeComboBox.SelectedItem is TrainingType selectedType)
                            trainingToUpdate.TypeId = selectedType.TypeId;

                        if (!string.IsNullOrEmpty(_newImagePath))
                            trainingToUpdate.ImagePath = _newImagePath;

                        await db.SaveChangesAsync();
                        this.Tag = true;
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Tag = false;
            this.Close();
        }
    }
}