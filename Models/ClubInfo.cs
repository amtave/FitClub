using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("club_info")]
    public class ClubInfo
    {
        [Key]
        [Column("info_id")]
        public int InfoId { get; set; }
        
        [Column("club_name")]
        public string ClubName { get; set; }
        
        [Column("address")]
        public string Address { get; set; }
        
        [Column("phone")]
        public string Phone { get; set; }
        
        [Column("working_hours")]
        public string WorkingHours { get; set; }
        
        [Column("logo_path")]
        public string? LogoPath { get; set; }
        
        [Column("welcome_text")]
        public string WelcomeText { get; set; }
        
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        
        [Column("updated_by")]
        public int? UpdatedBy { get; set; }
    }
}