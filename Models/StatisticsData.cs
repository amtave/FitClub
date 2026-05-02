using System;
using System.Collections.Generic;

namespace FitClub.Models
{
    public class StatisticsData
    {
        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public int TotalTrainers { get; set; }
        public int ActiveTrainers { get; set; }
        public int TotalSubscriptions { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public List<DailySubscriptionData> SubscriptionsByDay { get; set; }
        public List<DailyTrainingData> TrainingsByDay { get; set; }
    }

    public class DailySubscriptionData
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class DailyTrainingData
    {
        public DateTime Date { get; set; }
        public int GroupCount { get; set; }
        public int IndividualCount { get; set; }
        public int TotalCount => GroupCount + IndividualCount;
    }
}