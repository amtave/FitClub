using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("passport_verification_request")]
    public class PassportVerificationRequest
    {
        [Key]
        [Column("request_id")]
        public int RequestId { get; set; }

        [Column("client_id")]
        public int ClientId { get; set; }

        [Column("passport_series")]
        public string PassportSeries { get; set; }

        [Column("passport_number")]
        public string PassportNumber { get; set; }

        [Column("passport_photo_path")]
        public string PassportPhotoPath { get; set; }
        
        [Column("consent_photo_path")]
        public string? ConsentPhotoPath { get; set; }

        [Column("submitted_at")]
        public DateTime SubmittedAt { get; set; }

        [Column("status_id")]
        public int StatusId { get; set; }

        [Column("rejection_reason")]
        public string? RejectionReason { get; set; }

        [Column("reviewed_by")]
        public int? ReviewedBy { get; set; }

        [Column("reviewed_at")]
        public DateTime? ReviewedAt { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; }

        [NotMapped]
        public string ClientFullName { get; set; }

        [ForeignKey("ReviewedBy")]
        public virtual User Reviewer { get; set; }
        
        [NotMapped]
        public string StatusDisplay
        {
            get
            {
                return StatusId switch
                {
                    1 => "Не отправлено",
                    2 => "На проверке",
                    3 => "Подтверждено",
                    4 => "Отклонено",
                    _ => "Неизвестно"
                };
            }
        }
    }
}