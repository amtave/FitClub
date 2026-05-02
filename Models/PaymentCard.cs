using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("payment_card")]
    public class PaymentCard
    {
        [Key]
        [Column("card_id")]
        public int CardId { get; set; }

        [Column("client_id")]
        public int ClientId { get; set; }

        [Column("card_number")]
        public string CardNumber { get; set; }

        [Column("card_holder_name")]
        public string CardHolderName { get; set; }

        [Column("expiry_month")]
        public int ExpiryMonth { get; set; }

        [Column("expiry_year")]
        public int ExpiryYear { get; set; }

        [Column("cvv")]
        public string CVV { get; set; }

        [Column("is_default")]
        public bool IsDefault { get; set; }

        [Column("is_verified")]
        public bool IsVerified { get; set; }

        [Column("verification_code")]
        public string VerificationCode { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("last_used")]
        public DateTime? LastUsed { get; set; }

        // Навигационное свойство
        [ForeignKey("ClientId")]
        public Client Client { get; set; }

        [NotMapped]
        public string MaskedCardNumber
        {
            get
            {
                if (string.IsNullOrEmpty(CardNumber) || CardNumber.Length < 16)
                    return "**** **** **** ****";
                
                return $"**** **** **** {CardNumber.Substring(CardNumber.Length - 4)}";
            }
        }

        [NotMapped]
        public string FormattedExpiry => $"{ExpiryMonth:D2}/{ExpiryYear % 100:D2}";

        [NotMapped]
        public string CardType
        {
            get
            {
                if (string.IsNullOrEmpty(CardNumber))
                    return "Unknown";
                
                if (CardNumber.StartsWith("4"))
                    return "Visa";
                else if (CardNumber.StartsWith("5"))
                    return "MasterCard";
                else if (CardNumber.StartsWith("2") || CardNumber.StartsWith("6"))
                    return "Mir";
                else
                    return "Bank Card";
            }
        }

        [NotMapped]
        public string CardIcon
        {
            get
            {
                return CardType switch
                {
                    "Visa" => "💳",
                    "MasterCard" => "💳",
                    "Mir" => "💳",
                    _ => "💳"
                };
            }
        }

        // В классе PaymentCard добавьте:
        [NotMapped]
        public string DisplayCardHolderName => CardHolderName?.ToUpper() ?? "CARDHOLDER NAME";

        [NotMapped]
public bool IsValid
{
    get
    {
        try
        {
            var currentDate = DateTime.Now;
            var expiry = new DateTime(ExpiryYear, ExpiryMonth, 
                DateTime.DaysInMonth(ExpiryYear, ExpiryMonth));
            
            return expiry >= currentDate;
        }
        catch
        {
            return false;
        }
    }
}
    }
}