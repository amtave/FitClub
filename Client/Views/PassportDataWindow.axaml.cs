using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Platform;
using FitClub.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.IO;
using System.Linq;

namespace FitClub.Client.Views
{
    public partial class PassportDataWindow : Window
    {
        private Models.Client _client;
        private AppDbContext _context;
        private string _selectedPassportPath;
        private string _selectedConsentPath;
        private string _passportsFolder;

        public PassportDataWindow()
        {
            InitializeComponent();
        }

        public PassportDataWindow(Models.Client client) : this()
        {
            _client = client;
            _context = new AppDbContext();
            _passportsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Passports");
            if (!Directory.Exists(_passportsFolder)) Directory.CreateDirectory(_passportsFolder);

            PassportSeriesTextBox.Text = string.Empty;
            PassportNumberTextBox.Text = string.Empty;
            SubmitButton.IsEnabled = false;
        }

        private void PassportSeriesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                string digits = new string(tb.Text.Where(char.IsDigit).ToArray());
                if (digits.Length > 4) digits = digits.Substring(0, 4);
                if (tb.Text != digits) { int pos = tb.CaretIndex; tb.Text = digits; tb.CaretIndex = Math.Min(pos, digits.Length); }
            }
        }

        private void PassportNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                string digits = new string(tb.Text.Where(char.IsDigit).ToArray());
                if (digits.Length > 6) digits = digits.Substring(0, 6);
                if (tb.Text != digits) { int pos = tb.CaretIndex; tb.Text = digits; tb.CaretIndex = Math.Min(pos, digits.Length); }
            }
        }

        private async void SelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Фото паспорта", AllowMultiple = false, FileTypeFilter = new[] { new FilePickerFileType("Images") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png" } } } });
            if (files.Count > 0)
            {
                _selectedPassportPath = files[0].Path.LocalPath;
                PassportPhotoPreview.Source = new Bitmap(_selectedPassportPath);
                PhotoStatusText.Text = "✅ Файл выбран";
                ClearPhotoButton.IsVisible = true;
            }
        }

        private void ClearPhoto_Click(object sender, RoutedEventArgs e)
        {
            _selectedPassportPath = null; PassportPhotoPreview.Source = null; PhotoStatusText.Text = ""; ClearPhotoButton.IsVisible = false;
        }

        private async void SelectConsent_Click(object sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = "Фото согласия", AllowMultiple = false, FileTypeFilter = new[] { new FilePickerFileType("Images") { Patterns = new[] { "*.jpg", "*.jpeg", "*.png" } } } });
            if (files.Count > 0)
            {
                _selectedConsentPath = files[0].Path.LocalPath;
                ConsentPhotoPreview.Source = new Bitmap(_selectedConsentPath);
                ConsentStatusText.Text = "✅ Файл выбран";
                ClearConsentButton.IsVisible = true;
            }
        }

        private void ClearConsent_Click(object sender, RoutedEventArgs e)
        {
            _selectedConsentPath = null; ConsentPhotoPreview.Source = null; ConsentStatusText.Text = ""; ClearConsentButton.IsVisible = false;
        }

        private void ConsentCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            SubmitButton.IsEnabled = ConsentCheckBox.IsChecked ?? false;
        }

        private async void DownloadConsent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var uri = new Uri("avares://FitClub/Assets/consent_form.pdf");
                
                if (!AssetLoader.Exists(uri))
                {
                    var boxError = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Бланк согласия не найден в системе (убедитесь, что файл называется consent_form.pdf и имеет тип сборки AvaloniaResource).", ButtonEnum.Ok);
                    await boxError.ShowWindowDialogAsync(this);
                    return;
                }

                var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Выберите папку для сохранения бланка" });
                if (folder.Count > 0)
                {
                    string destPath = Path.Combine(folder[0].Path.LocalPath, "Consent_Form_FitClub.pdf");
                    
                    using (var assetStream = AssetLoader.Open(uri))
                    using (var fileStream = File.Create(destPath))
                    {
                        await assetStream.CopyToAsync(fileStream);
                    }
                    
                    var box = MessageBoxManager.GetMessageBoxStandard("Успех", $"Бланк успешно сохранен в:\n{destPath}", ButtonEnum.Ok);
                    await box.ShowWindowDialogAsync(this);
                }
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", ex.Message, ButtonEnum.Ok);
                await box.ShowWindowDialogAsync(this);
            }
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(PassportSeriesTextBox.Text) || PassportSeriesTextBox.Text.Length != 4) { var b = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Серия должна содержать 4 цифры", ButtonEnum.Ok); await b.ShowWindowDialogAsync(this); return; }
                if (string.IsNullOrEmpty(PassportNumberTextBox.Text) || PassportNumberTextBox.Text.Length != 6) { var b = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Номер должен содержать 6 цифр", ButtonEnum.Ok); await b.ShowWindowDialogAsync(this); return; }
                if (string.IsNullOrEmpty(_selectedPassportPath)) { var b = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Выберите фото паспорта", ButtonEnum.Ok); await b.ShowWindowDialogAsync(this); return; }
                if (string.IsNullOrEmpty(_selectedConsentPath)) { var b = MessageBoxManager.GetMessageBoxStandard("Ошибка", "Выберите фото заполненного согласия", ButtonEnum.Ok); await b.ShowWindowDialogAsync(this); return; }

                var box = MessageBoxManager.GetMessageBoxStandard("Подтверждение отправки", "Отправить данные на проверку?", ButtonEnum.YesNo);
                if (await box.ShowWindowDialogAsync(this) == ButtonResult.Yes)
                {
                    string pExt = Path.GetExtension(_selectedPassportPath);
                    string cExt = Path.GetExtension(_selectedConsentPath);
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                    
                    string pFileName = $"passport_{_client.ClientId}_{timestamp}{pExt}";
                    string cFileName = $"consent_{_client.ClientId}_{timestamp}{cExt}";

                    File.Copy(_selectedPassportPath, Path.Combine(_passportsFolder, pFileName), true);
                    File.Copy(_selectedConsentPath, Path.Combine(_passportsFolder, cFileName), true);

                    var request = new PassportVerificationRequest
                    {
                        ClientId = _client.ClientId,
                        PassportSeries = PassportSeriesTextBox.Text,
                        PassportNumber = PassportNumberTextBox.Text,
                        PassportPhotoPath = pFileName,
                        ConsentPhotoPath = cFileName,
                        SubmittedAt = DateTime.Now,
                        StatusId = 2 
                    };

                    _context.PassportVerificationRequests.Add(request);
                    await _context.SaveChangesAsync();

                    var okBox = MessageBoxManager.GetMessageBoxStandard("Успех", "Документы отправлены на проверку!", ButtonEnum.Ok);
                    await okBox.ShowWindowDialogAsync(this);
                    Close(true);
                }
            }
            catch (Exception) { }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close(false);
    }
}