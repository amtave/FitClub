using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("single_gym_visit")]
    public class SingleGymVisit
    {
        [Key]
        [Column("visit_id")]
        public int VisitId { get; set; }

        [Required]
        [Column("client_id")]
        public int ClientId { get; set; }

        [Required]
        [Column("visit_date", TypeName = "date")]
        public DateTime VisitDate { get; set; }

        [Required]
        [Column("price")]
        public decimal Price { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Навигационное свойство
        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; }
    }
}