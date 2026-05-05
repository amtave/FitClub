using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FitClub.Admin.Views
{
    public partial class JobApplicationDetailsWindow : Window
    {
        private JobApplicationView _applicationView;
        private JobApplication _application;
        private AppDbContext _context;
        private User _currentAdmin;

        public JobApplicationDetailsWindow()
        {
            InitializeComponent();
        }

        public JobApplicationDetailsWindow(JobApplicationView applicationView) : this()
        {
            _applicationView = applicationView;
            _context = new AppDbContext();
            _currentAdmin = _context.Users.FirstOrDefault(u => u.Email == "admin@fitness.ru");
            LoadApplicationFromDb();
        }

        private void LoadApplicationFromDb()
        {
            try
            {
                var sql = @"
                    SELECT 
                        ja.application_id,
                        ja.client_id,
                        ja.specialization,
                        COALESCE(ja.specialization_other, '') as specialization_other,
                        COALESCE(ja.experience_years, 0) as experience_years,
                        COALESCE(ja.education, '') as education,
                        COALESCE(ja.motivation, '') as motivation,
                        COALESCE(ja.professional_goals, '') as professional_goals,
                        COALESCE(ja.offer_to_club, '') as offer_to_club,
                        COALESCE(ja.desired_schedule, '') as desired_schedule,
                        COALESCE(ja.age_groups, '') as age_groups,
                        ja.has_medical_book,
                        COALESCE(ja.languages, '') as languages,
                        COALESCE(ja.certificates_path, '') as certificates_path,
                        ja.created_at,
                        COALESCE(ja.status, 'pending') as status,
                        c.last_name,
                        c.first_name,
                        c.middle_name,
                        c.email,
                        c.phone
                    FROM job_application ja
                    LEFT JOIN client c ON ja.client_id = c.client_id
                    WHERE ja.application_id = @id";

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    command.Parameters.Add(new Npgsql.NpgsqlParameter("@id", _applicationView.ApplicationId));
                    
                    _context.Database.OpenConnection();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var client = new Models.Client
                            {
                                ClientId = reader.GetInt32(reader.GetOrdinal("client_id")),
                                LastName = reader.GetString(reader.GetOrdinal("last_name")),
                                FirstName = reader.GetString(reader.GetOrdinal("first_name")),
                                MiddleName = reader.GetString(reader.GetOrdinal("middle_name")),
                                Email = reader.GetString(reader.GetOrdinal("email")),
                                Phone = reader.GetString(reader.GetOrdinal("phone"))
                            };

                            _application = new JobApplication
                            {
                                ApplicationId = reader.GetInt32(reader.GetOrdinal("application_id")),
                                ClientId = reader.GetInt32(reader.GetOrdinal("client_id")),
                                Client = client,
                                Specialization = reader.GetString(reader.GetOrdinal("specialization")),
                                SpecializationOther = reader.GetString(reader.GetOrdinal("specialization_other")),
                                ExperienceYears = reader.GetInt32(reader.GetOrdinal("experience_years")),
                                Education = reader.GetString(reader.GetOrdinal("education")),
                                Motivation = reader.GetString(reader.GetOrdinal("motivation")),
                                ProfessionalGoals = reader.GetString(reader.GetOrdinal("professional_goals")),
                                OfferToClub = reader.GetString(reader.GetOrdinal("offer_to_club")),
                                DesiredSchedule = reader.GetString(reader.GetOrdinal("desired_schedule")),
                                AgeGroups = reader.GetString(reader.GetOrdinal("age_groups")),
                                HasMedicalBook = reader.GetBoolean(reader.GetOrdinal("has_medical_book")),
                                Languages = reader.GetString(reader.GetOrdinal("languages")),
                                CertificatesPath = reader.GetString(reader.GetOrdinal("certificates_path")),
                                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                                Status = reader.GetString(reader.GetOrdinal("status"))
                            };
                        }
                    }
                }

                if (_application != null)
                {
                    LoadData();
                }
            }
            catch (Exception) { }
        }

        private void LoadData()
        {
            if (_application == null) return;

            TitleText.Text = GetStatusDisplay(_application.Status);
            TitleBorder.Background = new SolidColorBrush(Color.Parse(GetStatusColor(_application.Status)));

            StatusBadge.Background = new SolidColorBrush(Color.Parse(GetStatusColor(_application.Status)));
            StatusIconText.Text = GetStatusIcon(_application.Status);
            StatusTextBadge.Background = new SolidColorBrush(Color.Parse(GetStatusColor(_application.Status)));
            StatusText.Text = GetStatusDisplay(_application.Status);
            CreatedAtText.Text = $"Подана: {_application.CreatedAt:dd.MM.yyyy HH:mm}";

            FullNameText.Text = _application.Client?.FullName ?? "Неизвестно";
            EmailText.Text = _application.Client?.Email ?? "Не указан";
            PhoneText.Text = _application.Client?.Phone ?? "Не указан";
            DateText.Text = _application.CreatedAt.ToString("dd.MM.yyyy HH:mm");

            string specialization = _application.Specialization;
            if (_application.Specialization == "Другое" && !string.IsNullOrEmpty(_application.SpecializationOther))
            {
                specialization += $" ({_application.SpecializationOther})";
            }
            SpecializationText.Text = specialization;
            
            ExperienceText.Text = _application.ExperienceYears.HasValue && _application.ExperienceYears > 0 
                ? $"{_application.ExperienceYears} лет" 
                : "Не указано";
            
            EducationText.Text = !string.IsNullOrEmpty(_application.Education) 
                ? _application.Education 
                : "Не указано";
            
            CertificatesText.Text = _application.HasCertificates || !string.IsNullOrEmpty(_application.CertificatesPath)
                ? "Есть сертификаты" 
                : "Нет сертификатов";

            MotivationText.Text = !string.IsNullOrEmpty(_application.Motivation) 
                ? _application.Motivation 
                : "Не указано";
            
            GoalsText.Text = !string.IsNullOrEmpty(_application.ProfessionalGoals) 
                ? _application.ProfessionalGoals 
                : "Не указано";
            
            OfferText.Text = !string.IsNullOrEmpty(_application.OfferToClub) 
                ? _application.OfferToClub 
                : "Не указано";

            ScheduleText.Text = !string.IsNullOrEmpty(_application.DesiredSchedule)
                ? GetScheduleDisplay(_application.DesiredSchedule)
                : "Не указано";

            if (!string.IsNullOrEmpty(_application.AgeGroups))
            {
                var groups = _application.AgeGroups.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var groupNames = groups.Select(g => GetAgeGroupDisplay(g));
                AgeGroupsText.Text = string.Join(", ", groupNames);
            }
            else
            {
                AgeGroupsText.Text = "Не указано";
            }

            MedicalBookText.Text = _application.HasMedicalBook ? "Есть" : "Нет";
            LanguagesText.Text = !string.IsNullOrEmpty(_application.Languages) 
                ? _application.Languages 
                : "Не указано";

            LoadRatings();
        }

        private void LoadRatings()
        {
            try
            {
                var sql = @"
                    SELECT quality_name, rating_value
                    FROM job_application_ratings
                    WHERE application_id = @id
                    ORDER BY quality_name";

                var ratings = new List<RatingItem>();

                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = sql;
                    command.Parameters.Add(new Npgsql.NpgsqlParameter("@id", _application.ApplicationId));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ratings.Add(new RatingItem
                            {
                                QualityName = reader.GetString(reader.GetOrdinal("quality_name")),
                                RatingValue = reader.GetInt32(reader.GetOrdinal("rating_value"))
                            });
                        }
                    }
                }

                var ratingsControl = this.FindControl<ItemsControl>("RatingsItemsControl");
                if (ratingsControl != null)
                {
                    ratingsControl.ItemsSource = ratings;
                }
            }
            catch (Exception) { }
        }

        private string GetStatusDisplay(string status)
        {
            return status switch
            {
                "pending" => "На рассмотрении",
                "reviewed" => "Рассмотрено",
                "accepted" => "Принято",
                "rejected" => "Отклонено",
                _ => status
            };
        }

        private string GetStatusColor(string status)
        {
            return status switch
            {
                "pending" => "#F39C12",
                "reviewed" => "#3498DB",
                "accepted" => "#27AE60",
                "rejected" => "#E74C3C",
                _ => "#95A5A6"
            };
        }

        private string GetStatusIcon(string status)
        {
            return status switch
            {
                "pending" => "⏳",
                "reviewed" => "👁️",
                "accepted" => "✅",
                "rejected" => "❌",
                _ => "📋"
            };
        }

        private string GetScheduleDisplay(string schedule)
        {
            return schedule switch
            {
                "fulltime" => "Полный день",
                "shift" => "Сменный график",
                "morning" => "Только утро",
                "evening" => "Только вечер",
                "weekend" => "Выходного дня",
                _ => schedule
            };
        }

        private string GetAgeGroupDisplay(string group)
        {
            return group switch
            {
                "children" => "Дети (6-12 лет)",
                "teens" => "Подростки (13-17 лет)",
                "adults" => "Взрослые (18-45 лет)",
                "seniors" => "Старшее поколение (45+)",
                _ => group
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _context?.Dispose();
            Close(false);
        }
    }
}