using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    [Table("training_type")]
    public class TrainingType
    {
        [Key]
        [Column("type_id")]
        public int TypeId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

    }
}