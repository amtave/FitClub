using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace FitClub.Models
{
    [Table("passport_verification_status")]
    public class PassportVerificationStatus
    {
        [Key]
        [Column("status_id")]
        public int StatusId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("color_code")]
        public string ColorCode { get; set; }

        // Навигационное свойство
        public ICollection<Client> Clients { get; set; }

        // Константы для удобства
        public const string NOT_SUBMITTED = "not_submitted";
        public const string PENDING = "pending";
        public const string VERIFIED = "verified";
        public const string REJECTED = "rejected";

        // Статические ID статусов
        public const int NOT_SUBMITTED_ID = 1;
        public const int PENDING_ID = 2;
        public const int VERIFIED_ID = 3;
        public const int REJECTED_ID = 4;

        // Отображение для пользователя
        public string DisplayName
        {
            get
            {
                return Name switch
                {
                    NOT_SUBMITTED => "Не отправлено",
                    PENDING => "Ожидает проверки",
                    VERIFIED => "Подтверждено",
                    REJECTED => "Отклонено",
                    _ => Name
                };
            }
        }
    }
}