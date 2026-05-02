using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitClub.Models
{
    public class Exercise
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("sets")]
        public int? Sets { get; set; }

        [JsonPropertyName("reps")]
        public int? Reps { get; set; }

        [JsonPropertyName("duration")]
        public int? Duration { get; set; }

        [JsonPropertyName("rest")]
        public int? Rest { get; set; }

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = "";

        [NotMapped]
        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrWhiteSpace(Name) && 
                              !Sets.HasValue && 
                              !Reps.HasValue && 
                              !Duration.HasValue && 
                              !Rest.HasValue;

        [NotMapped]
        [JsonIgnore]
        public bool HasName => !string.IsNullOrWhiteSpace(Name);

        // ВЫЧИСЛЯЕМЫЕ СВОЙСТВА ДЛЯ ОТОБРАЖЕНИЯ
        [NotMapped]
        [JsonIgnore]
        public string SetsDisplay => Sets.HasValue && Sets > 0 ? $"{Sets} подходов" : "";
        
        [NotMapped]
        [JsonIgnore]
        public string RepsDisplay => Reps.HasValue && Reps > 0 ? $"{Reps} повторений" : "";
        
        [NotMapped]
        [JsonIgnore]
        public string DurationDisplay => Duration.HasValue && Duration > 0 ? $"{Duration} мин" : "";
        
        [NotMapped]
        [JsonIgnore]
        public string RestDisplay => Rest.HasValue && Rest > 0 ? $"{Rest} сек отдых" : "";
    }
}