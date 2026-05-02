using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.IO;

namespace FitClub.Admin.Views
{
    public partial class TrainerAbsenceWindow : Window
    {
        private DateTime _startDate;
        private string _selectedPhotoPath;
        public bool Confirmed { get; private set; }
        public DateTime EndDate { get; private set; }
        public string ReasonType { get; private set; }
        public string Comment { get; private set; }
        public string FinalPhotoPath { get; private set; }

        public TrainerAbsenceWindow()
        {
            InitializeComponent();
        }

        public TrainerAbsenceWindow(DateTime date) : this()
        {
            _startDate = date;
            EndDate = date;
            DateText.Text = $"Начиная с: {date:dd MMMM yyyy}";
            
            ReasonComboBox.SelectionChanged += (s, e) => 
            {
                DaysCountPanel.IsVisible = ReasonComboBox.SelectedIndex == 0;
            };
            ReasonComboBox.SelectedIndex = 0;
        }

        private async void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions 
            { 
                Title = "Фото документа", AllowMultiple = false, 
                FileTypeFilter = new[] { new FilePickerFileType("Images") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png" } } } 
            });
            
            if (files.Count > 0)
            {
                _selectedPhotoPath = files[0].Path.LocalPath;
                DocumentPhotoPreview.Source = new Bitmap(_selectedPhotoPath);
                
                PhotoBorder.BorderBrush = Avalonia.Media.Brush.Parse("#BDC3C7");
                PhotoStatusText.Foreground = Avalonia.Media.Brush.Parse("#7F8C8D");
                PhotoStatusText.Text = "✅ Документ прикреплен";
                ClearPhotoButton.IsVisible = true;
            }
        }

        private void ClearPhoto_Click(object sender, RoutedEventArgs e)
        {
            _selectedPhotoPath = null;
            DocumentPhotoPreview.Source = null;
            PhotoStatusText.Text = "";
            ClearPhotoButton.IsVisible = false;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedPhotoPath))
            {
                PhotoBorder.BorderBrush = Avalonia.Media.Brushes.Red;
                PhotoStatusText.Text = "Обязательно прикрепите документ (справку/заявление/объяснительную)!";
                PhotoStatusText.Foreground = Avalonia.Media.Brushes.Red;
                return;
            }

            ReasonType = ReasonComboBox.SelectedIndex switch
            {
                0 => "sick",
                1 => "leave",
                _ => "truancy"
            };

            int days = ReasonType == "sick" ? (int)(DaysCountPicker.Value ?? 1) : 1;
            EndDate = _startDate.AddDays(days - 1);
            Comment = CommentTextBox.Text ?? "";

            try
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "AbsenceDocs");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                
                string ext = Path.GetExtension(_selectedPhotoPath);
                string fileName = $"doc_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                FinalPhotoPath = fileName;
                
                File.Copy(_selectedPhotoPath, Path.Combine(folder, fileName), true);
            }
            catch { FinalPhotoPath = null; }

            Confirmed = true;
            Close(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close(false);
    }
}