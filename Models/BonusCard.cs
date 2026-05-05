using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("bonus_card")]
    public class BonusCard
    {
        [Key]
        [Column("card_id")]
        public int CardId { get; set; }

        [Column("client_id")]
        public int ClientId { get; set; }

        [Column("card_number")]
        public string CardNumber { get; set; }

        [Column("points_balance")]
        public int PointsBalance { get; set; }

        [Column("issue_date")]
        public DateTime IssueDate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [ForeignKey("ClientId")]
        public Client Client { get; set; }

        [NotMapped]
        public string FormattedCardNumber => 
            CardNumber?.Length == 16 ? 
            $"{CardNumber.Substring(0, 4)} {CardNumber.Substring(4, 4)} {CardNumber.Substring(8, 4)} {CardNumber.Substring(12, 4)}" : 
            CardNumber;

        [NotMapped]
        public string DisplayName => Client?.FullName ?? "Владелец карты";

        [NotMapped]
        public decimal BonusPointsAsMoney => (decimal)PointsBalance; 
        
        public decimal MaxDiscountForPurchase(decimal purchaseAmount)
        {
            decimal maxDiscount = Math.Min(PointsBalance, purchaseAmount);
            return maxDiscount;
        }
    }
}