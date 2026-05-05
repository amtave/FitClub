using System;

namespace FitClub.Models
{
    public class IndividualTrainingSlot
    {
        public DateTime TrainingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int TrainerId { get; set; }
        public bool IsAvailable { get; set; }

        public string TimeFormatted => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    }
}