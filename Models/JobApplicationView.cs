using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;

namespace FitClub.Models
{
    public class JobApplicationView
    {
        public int ApplicationId { get; set; }
        public int ClientId { get; set; }
        public string ClientFullName { get; set; }
        public string ClientEmail { get; set; }
        public string ClientPhone { get; set; }
        public string Specialization { get; set; }
        public int? ExperienceYears { get; set; }
        public string ExperienceInfo => ExperienceYears.HasValue ? $"{ExperienceYears} лет" : "Не указано";
        public string Education { get; set; }
        public string Motivation { get; set; }
        public string ProfessionalGoals { get; set; }
        public string OfferToClub { get; set; }
        public string DesiredSchedule { get; set; }
        
        public string DesiredScheduleDisplay
        {
            get
            {
                return DesiredSchedule switch
                {
                    "fulltime" => "Полный день",
                    "shift" => "Сменный график",
                    "morning" => "Только утро",
                    "evening" => "Только вечер",
                    "weekend" => "Выходного дня",
                    _ => DesiredSchedule
                };
            }
        }
        
        public List<string> AgeGroups { get; set; } = new List<string>();
        public string AgeGroupsDisplay => string.Join(", ", AgeGroups);
        public bool HasMedicalBook { get; set; }
        public string Languages { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public string AvatarPath { get; set; }
        
        public List<RatingItem> RatingItems { get; set; } = new List<RatingItem>();

        public bool IsPending => Status == "pending";

        public string StatusDisplay
        {
            get
            {
                return Status switch
                {
                    "pending" => "На рассмотрении",
                    "reviewed" => "Рассмотрено",
                    "accepted" => "Принято",
                    "rejected" => "Отклонено",
                    _ => Status
                };
            }
        }
        
        public string StatusColor
        {
            get
            {
                return Status switch
                {
                    "pending" => "#F39C12",
                    "reviewed" => "#3498DB",
                    "accepted" => "#27AE60",
                    "rejected" => "#E74C3C",
                    _ => "#95A5A6"
                };
            }
        }

        public string StatusIcon
        {
            get
            {
                return Status switch
                {
                    "pending" => "⏳",
                    "reviewed" => "👁️",
                    "accepted" => "✅",
                    "rejected" => "❌",
                    _ => "📋"
                };
            }
        }
        
        public Bitmap AvatarBitmap
        {
            get
            {
                if (string.IsNullOrEmpty(AvatarPath))
                {
                    try
                    {
                        var uri = new Uri("avares://FitClub/Assets/default_avatar.png");
                        using var stream = AssetLoader.Open(uri);
                        return new Bitmap(stream);
                    }
                    catch { return null; }
                }

                try
                {
                    var path = AvatarPath.StartsWith("Assets/") ? AvatarPath : $"Assets/{AvatarPath}";
                    path = path.TrimStart('/');
                    var uri = new Uri($"avares://FitClub/{path}");
                    using var stream = AssetLoader.Open(uri);
                    return new Bitmap(stream);
                }
                catch
                {
                    try
                    {
                        var uri = new Uri("avares://FitClub/Assets/default_avatar.png");
                        using var stream = AssetLoader.Open(uri);
                        return new Bitmap(stream);
                    }
                    catch { return null; }
                }
            }
        }

        public Dictionary<string, int> Ratings { get; set; } = new Dictionary<string, int>();
    }
}