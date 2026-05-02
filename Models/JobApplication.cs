using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("job_application")]
    public class JobApplication
    {
        [Key]
        [Column("application_id")]
        public int ApplicationId { get; set; }

        [Column("client_id")]
        public int ClientId { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; }

        [Column("specialization")]
        public string Specialization { get; set; }

        [Column("specialization_other")]
        public string? SpecializationOther { get; set; }

        [Column("experience_years")]
        public int? ExperienceYears { get; set; }

        [Column("has_certificates")]
        public bool HasCertificates { get; set; }

        [Column("certificates_path")]
        public string? CertificatesPath { get; set; }

        [Column("education")]
        public string? Education { get; set; }

        [Column("motivation")]
        public string? Motivation { get; set; }

        [Column("professional_goals")]
        public string? ProfessionalGoals { get; set; }

        [Column("offer_to_club")]
        public string? OfferToClub { get; set; }

        [Column("desired_schedule")]
        public string? DesiredSchedule { get; set; }

        [Column("age_groups")]
        public string? AgeGroups { get; set; }

        [Column("has_medical_book")]
        public bool HasMedicalBook { get; set; }

        [Column("languages")]
        public string? Languages { get; set; }

        [Column("photo_path")]
        public string? PhotoPath { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("status")]
        public string Status { get; set; } = "pending";

        [Column("reviewed_by")]
        public int? ReviewedBy { get; set; }

        [ForeignKey("ReviewedBy")]
        public virtual User Reviewer { get; set; }

        [Column("reviewed_at")]
        public DateTime? ReviewedAt { get; set; }

        [Column("rejection_reason")]
        public string? RejectionReason { get; set; }

        public virtual ICollection<JobApplicationRating> Ratings { get; set; }

        [NotMapped]
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

        [NotMapped]
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

        [NotMapped]
        public string StatusIcon => Status switch
        {
            "accepted" => "✅",
            "rejected" => "❌",
            "reviewed" => "👀",
            _ => "⏳"
        };

        [NotMapped]
        public bool IsAccepted => Status == "accepted";

        [NotMapped]
        public string FullName => Client?.FullName ?? "";
    }
}