using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;

namespace FitClub.Admin.Views
{
    public partial class ClientDetailsWindow : Window
    {
        private Models.Client _client;
        private int _statusId;
        private PassportVerificationRequest _request;
        private string _passportsFolder;

        public ClientDetailsWindow()
        {
            InitializeComponent();
        }

        public ClientDetailsWindow(Models.Client client, int statusId) : this()
        {
            _client = client;
            _statusId = statusId;
            _passportsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Passports");
            LoadData();
        }

        private void LoadData()
        {
            using var db = new AppDbContext();
            _request = db.PassportVerificationRequests
                .Where(r => r.ClientId == _client.ClientId)
                .OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefault();

            if (_statusId == 3)
            {
                TitleText.Text = "Профиль клиента";
                TitleBorder.Background = new SolidColorBrush(Color.Parse("#27AE60"));
                StatusBadge.Background = new SolidColorBrush(Color.Parse("#27AE60"));
                StatusText.Text = "✅ Данные подтверждены";
            }
            else if (_statusId == 4)
            {
                TitleText.Text = "Профиль клиента";
                TitleBorder.Background = new SolidColorBrush(Color.Parse("#E74C3C"));
                StatusBadge.Background = new SolidColorBrush(Color.Parse("#E74C3C"));
                StatusText.Text = "❌ Заявка отклонена";
                
                if (_request != null && !string.IsNullOrEmpty(_request.RejectionReason))
                {
                    RejectionReasonBorder.IsVisible = true;
                    RejectionReasonText.Text = _request.RejectionReason;
                }
            }
            else
            {
                TitleText.Text = "Профиль клиента";
                TitleBorder.Background = new SolidColorBrush(Color.Parse("#95A5A6"));
                StatusBadge.Background = new SolidColorBrush(Color.Parse("#95A5A6"));
                StatusText.Text = "⚠️ Данные не предоставлены";
                _request = null;
                RejectionReasonBorder.IsVisible = false;
            }

            FullNameText.Text = _client.FullName;
            EmailText.Text = _client.Email;
            PhoneText.Text = _client.Phone;

            if (_statusId == 3)
            {
                PassportSeriesText.Text = _client.PassportSeries;
                PassportNumberText.Text = _client.PassportNumber;
            }
            else
            {
                PassportSeriesText.Text = _request?.PassportSeries ?? "—";
                PassportNumberText.Text = _request?.PassportNumber ?? "—";
            }

            SubmittedAtText.Text = _request != null ? _request.SubmittedAt.ToString("dd.MM.yyyy HH:mm") : "—";
            ReviewedAtText.Text = (_request != null && _request.ReviewedAt.HasValue) ? _request.ReviewedAt.Value.ToString("dd.MM.yyyy HH:mm") : "—";

            LoadPhotos();
        }

        private void LoadPhotos()
        {
            try
            {
                if (_request != null && !string.IsNullOrEmpty(_request.PassportPhotoPath))
                {
                    string pPath = Path.Combine(_passportsFolder, _request.PassportPhotoPath);
                    if (File.Exists(pPath))
                    {
                        PassportPhotoImage.Source = new Bitmap(pPath);
                        PhotoStatusText.Text = "Фотография загружена";
                    }
                    else
                    {
                        PassportPhotoImage.Source = null;
                        PhotoStatusText.Text = "❌ Файл не найден";
                    }
                }
                else
                {
                    PassportPhotoImage.Source = null;
                    PhotoStatusText.Text = "Фотография отсутствует";
                }

                if (_request != null && !string.IsNullOrEmpty(_request.ConsentPhotoPath))
                {
                    string cPath = Path.Combine(_passportsFolder, _request.ConsentPhotoPath);
                    if (File.Exists(cPath))
                    {
                        ConsentPhotoImage.Source = new Bitmap(cPath);
                        ConsentStatusText.Text = "Скан загружен";
                    }
                    else
                    {
                        ConsentPhotoImage.Source = null;
                        ConsentStatusText.Text = "❌ Файл не найден";
                    }
                }
                else
                {
                    ConsentPhotoImage.Source = null;
                    ConsentStatusText.Text = "Скан отсутствует";
                }
            }
            catch 
            { 
                PhotoStatusText.Text = "Ошибка чтения файла";
                ConsentStatusText.Text = "Ошибка чтения файла";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}