using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Avalonia.Media;

namespace FitClub.Models
{
    [Table("client_subscription")]
    public class ClientSubscription
    {
        [Key]
        [Column("subscription_id")]
        public int SubscriptionId { get; set; }

        [Required]
        [Column("client_id")]
        public int ClientId { get; set; }

        [Required]
        [Column("tariff_id")]
        public int TariffId { get; set; }

        [Required]
        [Column("start_date", TypeName = "date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [Column("end_date", TypeName = "date")]
        public DateTime EndDate { get; set; } = DateTime.Today;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("purchase_date", TypeName = "date")]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        [Column("visits_type")]
        public string VisitsType { get; set; } = "gym";

        [Column("group_total_visits")]
        public int? GroupTotalVisits { get; set; }

        [Column("group_remaining_visits")]
        public int? GroupRemainingVisits { get; set; }

        [Column("individual_total_visits")]
        public int? IndividualTotalVisits { get; set; }

        [Column("individual_remaining_visits")]
        public int? IndividualRemainingVisits { get; set; }

        [Column("selected_training_type_id")]
        public int? SelectedTrainingTypeId { get; set; }

        [Column("selected_trainer_id")]
        public int? SelectedTrainerId { get; set; }

        [Column("individual_training_type_id")]
        public int? IndividualTrainingTypeId { get; set; }

        [Column("individual_trainer_id")]
        public int? IndividualTrainerId { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; }

        [ForeignKey("TariffId")]
        public virtual Tariff Tariff { get; set; }

        [ForeignKey("SelectedTrainingTypeId")]
        public virtual TrainingType SelectedTrainingType { get; set; }

        [ForeignKey("SelectedTrainerId")]
        public virtual Trainer SelectedTrainer { get; set; }

        [ForeignKey("IndividualTrainingTypeId")]
        public virtual TrainingType IndividualTrainingType { get; set; }

        [ForeignKey("IndividualTrainerId")]
        public virtual Trainer IndividualTrainer { get; set; }

        [NotMapped]
        public string VisitsInfo
        {
            get
            {
                if (VisitsType == "individual" && IndividualTotalVisits.HasValue && IndividualRemainingVisits.HasValue)
                {
                    return $"Осталось: {IndividualRemainingVisits.Value}/{IndividualTotalVisits.Value}";
                }
                else if (VisitsType == "group" && GroupTotalVisits.HasValue && GroupRemainingVisits.HasValue)
                {
                    return $"Осталось: {GroupRemainingVisits.Value}/{GroupTotalVisits.Value}";
                }
                else if (VisitsType == "premium" && 
                        GroupTotalVisits.HasValue && GroupRemainingVisits.HasValue &&
                        IndividualTotalVisits.HasValue && IndividualRemainingVisits.HasValue)
                {
                    return $"Групповые: {GroupRemainingVisits.Value}/{GroupTotalVisits.Value}, " +
                           $"Индивидуальные: {IndividualRemainingVisits.Value}/{IndividualTotalVisits.Value}";
                }
                else if (VisitsType == "combo" && GroupTotalVisits.HasValue && GroupRemainingVisits.HasValue)
                {
                    return $"Комбо: {GroupRemainingVisits.Value}/{GroupTotalVisits.Value}";
                }
                else if (IndividualTotalVisits.HasValue && IndividualRemainingVisits.HasValue)
                {
                    return $"Индив: {IndividualRemainingVisits.Value}/{IndividualTotalVisits.Value}";
                }
                else if (GroupTotalVisits.HasValue && GroupRemainingVisits.HasValue)
                {
                    return $"Групп: {GroupRemainingVisits.Value}/{GroupTotalVisits.Value}";
                }
                
                return "Безлимит";
            }
        }

        [NotMapped]
        public bool HasTrainingPlan { get; set; }

        [NotMapped]
        public bool HasAnyExercisesInPlan { get; set; }

        [NotMapped]
        public string PlanStatusText 
        { 
            get 
            {
                if (!HasTrainingPlan) return "Нужен план";
                if (!HasAnyExercisesInPlan) return "План пустой";
                return "План заполнен";
            } 
        }

        [NotMapped]
        public string PlanStatusColor
        {
            get
            {
                if (!HasTrainingPlan) return "#E74C3C";
                if (!HasAnyExercisesInPlan) return "#F39C12";
                return "#27AE60";
            }
        }

        [NotMapped]
        public string PlanStatusBackgroundColor
        {
            get
            {
                if (!HasTrainingPlan) return "#FDEDEC";
                if (!HasAnyExercisesInPlan) return "#FEF9E7";
                return "#EAFAEE";
            }
        }

        [NotMapped]
        public string ActionButtonText => HasTrainingPlan ? "Просмотр плана" : "Создать план";

        [NotMapped]
        public string TrainingDirectionInfo
        {
            get
            {
                if (IndividualTrainingType != null)
                {
                    return $"Тренировка: {IndividualTrainingType.Name}";
                }
                if (SelectedTrainingType != null)
                {
                    return $"Тренировка: {SelectedTrainingType.Name}";
                }
                return "Направление не выбрано";
            }
        }

        [NotMapped]
        public string IntensityInfo
        {
            get
            {
                using var context = new AppDbContext();
                
                try
                {
                    if (IndividualTrainingTypeId.HasValue && IndividualTrainerId.HasValue)
                    {
                        var groupTraining = context.GroupTrainings
                            .Include(gt => gt.IntensityLevel)
                            .Include(gt => gt.TrainingTrainers)
                            .FirstOrDefault(gt => gt.TypeId == IndividualTrainingTypeId.Value && 
                                                 gt.TrainingTrainers.Any(tt => tt.TrainerId == IndividualTrainerId.Value));
                            
                        if (groupTraining?.IntensityLevel != null)
                        {
                            return $"Интенсивность: {groupTraining.IntensityLevel.Name}";
                        }
                    }
                    
                    if (SelectedTrainingTypeId.HasValue && SelectedTrainerId.HasValue)
                    {
                        var groupTraining = context.GroupTrainings
                            .Include(gt => gt.IntensityLevel)
                            .Include(gt => gt.TrainingTrainers)
                            .FirstOrDefault(gt => gt.TypeId == SelectedTrainingTypeId.Value && 
                                                 gt.TrainingTrainers.Any(tt => tt.TrainerId == SelectedTrainerId.Value));
                            
                        if (groupTraining?.IntensityLevel != null)
                        {
                            return $"Интенсивность: {groupTraining.IntensityLevel.Name}";
                        }
                    }
                    
                    if (IndividualTrainingTypeId.HasValue)
                    {
                        var anyTraining = context.GroupTrainings
                            .Include(gt => gt.IntensityLevel)
                            .FirstOrDefault(gt => gt.TypeId == IndividualTrainingTypeId.Value);
                            
                        if (anyTraining?.IntensityLevel != null)
                        {
                            return $"Интенсивность: {anyTraining.IntensityLevel.Name}";
                        }
                    }
                    
                    if (SelectedTrainingTypeId.HasValue)
                    {
                        var anyTraining = context.GroupTrainings
                            .Include(gt => gt.IntensityLevel)
                            .FirstOrDefault(gt => gt.TypeId == SelectedTrainingTypeId.Value);
                            
                        if (anyTraining?.IntensityLevel != null)
                        {
                            return $"Интенсивность: {anyTraining.IntensityLevel.Name}";
                        }
                    }
                }
                catch (Exception)
                {
                    return "Интенсивность: стандартная";
                }
                
                return "Интенсивность: не указана";
            }
        }

        [NotMapped]
        public string GroupTrainingStats
        {
            get
            {
                if (GroupTotalVisits.HasValue && GroupRemainingVisits.HasValue)
                {
                    return $"Осталось: {GroupRemainingVisits.Value}/{GroupTotalVisits.Value}";
                }
                return "Безлимит";
            }
        }

        [NotMapped]
        public string GroupTrainingTypeInfo
        {
            get
            {
                if (SelectedTrainingType != null)
                {
                    return SelectedTrainingType.Name;
                }
                return "Любые групповые";
            }
        }

        [NotMapped]
        public bool IsExpired => EndDate < DateTime.Today;

        [NotMapped]
        public string StatusDisplay
        {
            get
            {
                if (!IsActive) return "Неактивен";
                if (IsExpired) return "Истек";
                return "Активен";
            }
        }
    }
}