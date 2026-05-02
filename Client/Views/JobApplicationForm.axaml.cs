using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Linq;
using FitClub.Models;
using System;
using System.Collections.Generic;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace FitClub.Client.Views
{
    public partial class JobApplicationForm : UserControl
    {
        private AppDbContext _context;
        private Models.Client _currentClient;
        private List<string> _selectedAgeGroups = new List<string>();

        public JobApplicationForm()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadCurrentClient();
            LoadUserAvatar();
            LoadSpecializations();
            LoadSchedules();
            SetupSliderEvents();
        }

        private void LoadCurrentClient()
        {
            try
            {
                if (UserSession.IsLoggedIn)
                {
                    _currentClient = _context.GetClientByEmail(UserSession.CurrentUserEmail);

                    if (_currentClient != null)
                    {
                        UserFullNameText.Text = _currentClient.FullName;
                        UserEmailText.Text = _currentClient.Email;
                        UserPhoneText.Text = _currentClient.Phone;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки данных клиента: {ex.Message}");
            }
        }

        private void LoadSpecializations()
        {
            try
            {
                var specializations = TrainerSpecializations.GetAll();
                SpecializationComboBox.ItemsSource = specializations;
                SpecializationComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки специализаций: {ex.Message}");
            }
        }

        private void LoadSchedules()
        {
            try
            {
                var schedules = DesiredSchedules.GetAll();
                ScheduleComboBox.ItemsSource = schedules.Select(s => s.Display).ToList();
                ScheduleComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки графиков: {ex.Message}");
            }
        }

        private void SetupSliderEvents()
        {
            DisciplineSlider.ValueChanged += (s, e) =>
            {
                DisciplineValueText.Text = ((int)DisciplineSlider.Value).ToString();
            };

            CommunicationSlider.ValueChanged += (s, e) =>
            {
                CommunicationValueText.Text = ((int)CommunicationSlider.Value).ToString();
            };

            ResponsibilitySlider.ValueChanged += (s, e) =>
            {
                ResponsibilityValueText.Text = ((int)ResponsibilitySlider.Value).ToString();
            };

            EmpathySlider.ValueChanged += (s, e) =>
            {
                EmpathyValueText.Text = ((int)EmpathySlider.Value).ToString();
            };
        }

        private void SpecializationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SpecializationComboBox.SelectedItem?.ToString() == "Другое")
            {
                SpecializationOtherTextBox.IsVisible = true;
            }
            else
            {
                SpecializationOtherTextBox.IsVisible = false;
                SpecializationOtherTextBox.Text = "";
            }
        }

        private void HasCertificates_Checked(object sender, RoutedEventArgs e)
        {
            UploadCertificateButton.IsVisible = true;
        }

        private void HasCertificates_Unchecked(object sender, RoutedEventArgs e)
        {
            UploadCertificateButton.IsVisible = false;
        }

        private async void UploadCertificateButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Реализовать загрузку файлов
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Информация",
                "Функция загрузки сертификатов будет доступна позже",
                ButtonEnum.Ok);
            await box.ShowWindowDialogAsync((Window)this.VisualRoot);
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка обязательных полей
                if (_currentClient == null)
                {
                    await ShowError("Ошибка", "Не удалось определить пользователя");
                    return;
                }

                if (SpecializationComboBox.SelectedItem == null)
                {
                    await ShowError("Ошибка", "Выберите специализацию");
                    return;
                }

                if (string.IsNullOrWhiteSpace(MotivationTextBox.Text))
                {
                    await ShowError("Ошибка", "Напишите, почему вы хотите работать у нас");
                    return;
                }

                // Создаем заявку
                var application = new JobApplication
                {
                    ClientId = _currentClient.ClientId,
                    Specialization = SpecializationComboBox.SelectedItem?.ToString(),
                    SpecializationOther = SpecializationComboBox.SelectedItem?.ToString() == "Другое" ? SpecializationOtherTextBox.Text : null,
                    ExperienceYears = (int?)ExperienceYearsNumeric.Value,
                    Education = EducationTextBox.Text,
                    HasCertificates = HasCertificatesCheckBox.IsChecked ?? false,
                    Motivation = MotivationTextBox.Text,
                    ProfessionalGoals = GoalsTextBox.Text,
                    OfferToClub = OfferTextBox.Text,
                    DesiredSchedule = ScheduleComboBox.SelectedItem?.ToString(),
                    HasMedicalBook = MedicalBookCheckBox.IsChecked ?? false,
                    Languages = LanguagesTextBox.Text,
                    CreatedAt = DateTime.Now,
                    Status = "pending"
                };

                // Сохраняем возрастные группы
                var ageGroups = new List<string>();
                if (ChildrenGroupCheckBox.IsChecked == true) ageGroups.Add("children");
                if (TeensGroupCheckBox.IsChecked == true) ageGroups.Add("teens");
                if (AdultsGroupCheckBox.IsChecked == true) ageGroups.Add("adults");
                if (SeniorsGroupCheckBox.IsChecked == true) ageGroups.Add("seniors");

                application.AgeGroups = string.Join(",", ageGroups);

                // Добавляем в БД
                _context.JobApplications.Add(application);
                await _context.SaveChangesAsync();

                // Сохраняем оценки
                var ratings = new List<JobApplicationRating>
        {
            new JobApplicationRating {
                ApplicationId = application.ApplicationId,
                QualityName = "Дисциплинированность",
                RatingValue = (int)DisciplineSlider.Value
            },
            new JobApplicationRating {
                ApplicationId = application.ApplicationId,
                QualityName = "Коммуникабельность",
                RatingValue = (int)CommunicationSlider.Value
            },
            new JobApplicationRating {
                ApplicationId = application.ApplicationId,
                QualityName = "Ответственность",
                RatingValue = (int)ResponsibilitySlider.Value
            },
            new JobApplicationRating {
                ApplicationId = application.ApplicationId,
                QualityName = "Эмпатия к клиентам",
                RatingValue = (int)EmpathySlider.Value
            }
        };

                _context.JobApplicationRatings.AddRange(ratings);
                await _context.SaveChangesAsync();

                // Показываем сообщение об успехе
                var successBox = MessageBoxManager.GetMessageBoxStandard(
                    "Заявка отправлена",
                    "Ваша заявка успешно отправлена! Мы рассмотрим её и свяжемся с вами в ближайшее время.",
                    ButtonEnum.Ok);
                await successBox.ShowWindowDialogAsync((Window)this.VisualRoot);

                // Возвращаемся на главную и обновляем ее
                if (this.VisualRoot is Window window)
                {
                    var mainContent = window.FindControl<ContentControl>("MainContentControl");
                    if (mainContent != null)
                    {
                        // Создаем новый экземпляр HomeView и обновляем его
                        var homeView = new HomeView();
                        // Принудительно вызываем проверку заявок через RefreshView
                        homeView.RefreshView();
                        mainContent.Content = homeView;
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowError("Ошибка", $"Не удалось отправить заявку: {ex.Message}");
                Console.WriteLine($"Ошибка отправки заявки: {ex}");
            }
        }

        private async Task ShowError(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok);
            await box.ShowWindowDialogAsync((Window)this.VisualRoot);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ReturnToHome();
        }

        private void LoadUserAvatar()
        {
            try
            {
                if (_currentClient != null && !string.IsNullOrEmpty(_currentClient.AvatarPath))
                {
                    var imagePath = _currentClient.AvatarPath.StartsWith("Assets/")
                        ? _currentClient.AvatarPath
                        : $"Assets/{_currentClient.AvatarPath}";

                    imagePath = imagePath.TrimStart('/');
                    var resourceUri = new Uri($"avares://FitClub/{imagePath}");
                    using var stream = AssetLoader.Open(resourceUri);
                    UserAvatarImage.Source = new Avalonia.Media.Imaging.Bitmap(stream);
                }
                else
                {
                    // Загружаем аватар по умолчанию
                    try
                    {
                        var resourceUri = new Uri($"avares://FitClub/Assets/default_avatar.png");
                        using var stream = AssetLoader.Open(resourceUri);
                        UserAvatarImage.Source = new Avalonia.Media.Imaging.Bitmap(stream);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки аватара: {ex.Message}");
            }
        }

        private void ReturnToHome()
        {
            try
            {
                if (this.VisualRoot is Window window)
                {
                    var mainContent = window.FindControl<ContentControl>("MainContentControl");
                    if (mainContent != null)
                    {
                        mainContent.Content = new HomeView();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при закрытии формы: {ex.Message}");
            }
        }
    }
}