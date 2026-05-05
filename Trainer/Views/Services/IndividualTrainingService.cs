using System;
using System.Collections.Generic;
using System.Linq;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;

namespace FitClub.Trainer.Services
{
    public class IndividualTrainingService
    {
        private readonly AppDbContext _context;

        public IndividualTrainingService(AppDbContext context)
        {
            _context = context;
        }

        // Только активные записи индивидуальных тренировок
        public List<IndividualTraining> GetTrainerIndividualTrainingsForDate(int trainerId, DateTime date)
{
    try
    {
        var trainings = _context.IndividualTrainings
            .Include(it => it.Client)
            .Where(it => it.TrainerId == trainerId &&
                       it.TrainingDate.Date == date.Date &&
                       it.IsActive)
            .OrderBy(it => it.StartTime)
            .ToList();

        System.Diagnostics.Debug.WriteLine($"Сервис: найдено {trainings.Count} тренировок для тренера {trainerId} на {date:dd.MM.yyyy}");
        return trainings;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"❌ Ошибка в сервисе: {ex.Message}");
        return new List<IndividualTraining>();
    }
}
    }
}