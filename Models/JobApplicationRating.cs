using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("job_application_ratings")]
    public class JobApplicationRating
    {
        [Key]
        [Column("rating_id")]
        public int RatingId { get; set; }

        [Column("application_id")]
        public int ApplicationId { get; set; }

        [ForeignKey("ApplicationId")]
        public virtual JobApplication JobApplication { get; set; }

        [Column("quality_name")]
        public string QualityName { get; set; }

        [Column("rating_value")]
        public int RatingValue { get; set; } // 1-10
    }
}