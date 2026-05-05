using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;

namespace FitClub.Admin.Views
{
    public partial class ClientVerificationWindow : Window
    {
        private ClientRequestView _requestView;
        private PassportVerificationRequest _request;
        private AppDbContext _context;
        private string _passportsFolder;

        public ClientVerificationWindow()
        {
            InitializeComponent();
        }

        public ClientVerificationWindow(ClientRequestView req) : this()
        {
            _requestView = req;
            _context = new AppDbContext();
            _passportsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Passports");
            LoadDataFromDb();
        }

        private void LoadDataFromDb()
        {
            _request = _context.PassportVerificationRequests
                .Include(r => r.Client)
                .FirstOrDefault(r => r.RequestId == _requestView.RequestId);

            if (_request != null)
            {
                LoadData();
            }
        }

        private void LoadData()
        {
            TitleText.Text = "Новая заявка на проверку";
            TitleBorder.Background = new SolidColorBrush(Color.Parse("#F39C12"));

            StatusBadge.Background = new SolidColorBrush(Color.Parse("#F39C12"));
            StatusText.Text = "⏳ На проверке";
            
            SubmittedAtText.Text = _request.SubmittedAt.ToString("dd.MM.yyyy HH:mm");

            FullNameText.Text = _request.Client?.FullName ?? "—";
            EmailText.Text = _request.Client?.Email ?? "—";
            PhoneText.Text = _request.Client?.Phone ?? "—";

            PassportSeriesText.Text = _request.PassportSeries ?? "—";
            PassportNumberText.Text = _request.PassportNumber ?? "—";

            LoadPhotos();
        }

        private void LoadPhotos()
        {
            try
            {
                if (!string.IsNullOrEmpty(_request.PassportPhotoPath))
                {
                    string pPath = Path.Combine(_passportsFolder, _request.PassportPhotoPath);
                    if (File.Exists(pPath))
                    {
                        PassportPhotoImage.Source = new Bitmap(pPath);
                        PhotoStatusText.Text = "Фото загружено";
                    }
                    else
                    {
                        PhotoStatusText.Text = "❌ Файл не найден";
                    }
                }
                else
                {
                    PhotoStatusText.Text = "Нет фото";
                }

                if (!string.IsNullOrEmpty(_request.ConsentPhotoPath))
                {
                    string cPath = Path.Combine(_passportsFolder, _request.ConsentPhotoPath);
                    if (File.Exists(cPath))
                    {
                        ConsentPhotoImage.Source = new Bitmap(cPath);
                        ConsentStatusText.Text = "Скан загружен";
                    }
                    else
                    {
                        ConsentStatusText.Text = "❌ Файл не найден";
                    }
                }
                else
                {
                    ConsentStatusText.Text = "Нет фото";
                }
            }
            catch
            {
                PhotoStatusText.Text = "❌ Ошибка загрузки";
                ConsentStatusText.Text = "❌ Ошибка загрузки";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
            Close(false);
        }
    }
}