using Microsoft.EntityFrameworkCore;
using FitClub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;

namespace FitClub.Services
{
    public class TrainingService
    {
        private readonly AppDbContext _context;
        private Dictionary<int, HashSet<DateTime>> _emptyDaysCache = null;

        public TrainingService(AppDbContext context)
        {
            _context = context;
        }

        private void BuildEmptyDaysCache(DateTime startDate, DateTime endDate)
        {
            if (_emptyDaysCache != null) return;
            
            _emptyDaysCache = new Dictionary<int, HashSet<DateTime>>();
            var trainers = _context.Trainers.Where(t => t.IsActive).ToList();
            
            foreach(var t in trainers)
            {
                var absences = _context.TrainerAbsences
                    .Where(a => a.TrainerId == t.TrainerId && a.AbsenceDate >= startDate && a.AbsenceDate <= endDate)
                    .Select(a => a.AbsenceDate.Date)
                    .ToList();
                    
                var groups = _context.TrainingSchedules
                    .Where(s => s.TrainerId == t.TrainerId && s.TrainingDate >= startDate && s.TrainingDate <= endDate)
                    .Select(s => s.TrainingDate.Date)
                    .ToList();
                    
                var inds = _context.IndividualTrainings
                    .Where(i => i.TrainerId == t.TrainerId && i.TrainingDate >= startDate && i.TrainingDate <= endDate)
                    .Select(i => i.TrainingDate.Date)
                    .ToList();

                var emptyDays = new HashSet<DateTime>();
                for (var d = startDate.Date; d <= endDate.Date; d = d.AddDays(1))
                {
                    if (!absences.Contains(d) && !groups.Contains(d) && !inds.Contains(d))
                    {
                        emptyDays.Add(d);
                    }
                }
                _emptyDaysCache[t.TrainerId] = emptyDays;
            }
        }

        public void CreateMonthlyIndividualSchedules()
        {
            var today = DateTime.Today;
            var endDate = today.AddDays(30);
            BuildEmptyDaysCache(today, endDate);

            var trainers = _context.Trainers.Where(t => t.IsActive).ToList();

            var individualTimeSlots = new[] {
                new TimeSpan(8, 0, 0), new TimeSpan(10, 0, 0),
                new TimeSpan(12, 0, 0), new TimeSpan(14, 0, 0),
                new TimeSpan(16, 0, 0), new TimeSpan(18, 0, 0),
                new TimeSpan(20, 0, 0)
            };

            foreach (var trainer in trainers)
            {
                var emptyDays = _emptyDaysCache.ContainsKey(trainer.TrainerId) ? _emptyDaysCache[trainer.TrainerId] : new HashSet<DateTime>();

                foreach (var d in emptyDays)
                {
                    if (IsTrainerWorking(trainer, d.DayOfWeek))
                    {
                        var localGroups = _context.TrainingSchedules.Local
                            .Where(ts => ts.TrainerId == trainer.TrainerId && ts.TrainingDate.Date == d.Date)
                            .ToList();

                        foreach(var time in individualTimeSlots)
                        {
                            var endTime = time.Add(TimeSpan.FromHours(1));
                            bool conflict = false;

                            foreach(var gt in localGroups)
                            {
                                if (gt.GroupTraining == null)
                                {
                                    gt.GroupTraining = _context.GroupTrainings.FirstOrDefault(x => x.TrainingId == gt.TrainingId);
                                }
                                var duration = gt.GroupTraining?.DurationMinutes ?? 60;
                                var gStart = gt.TrainingTime;
                                var gEnd = gStart.Add(TimeSpan.FromMinutes(duration));

                                if (time < gEnd && endTime > gStart)
                                {
                                    conflict = true;
                                    break;
                                }
                            }

                            if (!conflict)
                            {
                                bool exists = _context.IndividualTrainings.Local.Any(i =>
                                    i.TrainerId == trainer.TrainerId &&
                                    i.TrainingDate.Date == d.Date &&
                                    i.StartTime == time);

                                if (!exists)
                                {
                                    _context.IndividualTrainings.Add(new IndividualTraining
                                    {
                                        TrainerId = trainer.TrainerId,
                                        TrainingDate = d.Date,
                                        StartTime = time,
                                        EndTime = endTime,
                                        ClientId = null,
                                        IsActive = true,
                                        CreatedAt = DateTime.UtcNow,
                                        Price = trainer.IndividualTrainingPrice
                                    });
                                }
                            }
                        }
                    }
                }
            }
            _context.SaveChanges();
        }

        public void CreateMonthlySchedules()
        {
            try
            {
                var today = DateTime.Today;
                var endDate = today.AddDays(30);
                BuildEmptyDaysCache(today, endDate);
                
                var activeTrainings = GetActiveGroupTrainings();

                foreach (var training in activeTrainings)
                {
                    CreateScheduleForTraining(training.TrainingId, today, endDate);
                }
            }
            catch (Exception) { }
        }

        private void CreateScheduleForTraining(int trainingId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var training = _context.GroupTrainings
                    .AsNoTracking()
                    .FirstOrDefault(gt => gt.TrainingId == trainingId);

                if (training == null) return;
                
                BuildEmptyDaysCache(startDate, endDate);

                var trainers = GetTrainersForTraining(trainingId);

                foreach (var trainer in trainers)
                {
                    var emptyDays = _emptyDaysCache.ContainsKey(trainer.TrainerId) ? _emptyDaysCache[trainer.TrainerId] : new HashSet<DateTime>();
                    var timeSlots = GetTimeSlotsForTraining(training.Name);

                    foreach (var d in emptyDays.Where(date => date >= startDate.Date && date <= endDate.Date))
                    {
                        if (IsTrainerWorking(trainer, d.DayOfWeek))
                        {
                            foreach (var timeSlot in timeSlots)
                            {
                                bool exists = _context.TrainingSchedules.Local.Any(ts => 
                                    ts.TrainerId == trainer.TrainerId && 
                                    ts.TrainingDate == d && 
                                    ts.TrainingTime == timeSlot);
                                    
                                if (!exists)
                                {
                                    _context.TrainingSchedules.Add(new TrainingSchedule
                                    {
                                        TrainingId = training.TrainingId,
                                        TrainerId = trainer.TrainerId,
                                        TrainingDate = d,
                                        TrainingTime = timeSlot,
                                        MaxParticipants = training.MaxParticipants,
                                        CurrentParticipants = 0,
                                        IsActive = true
                                    });
                                }
                            }
                        }
                    }
                }
                _context.SaveChanges();
            }
            catch (Exception) { }
        }

        public void CheckTrainingTrainersRelations()
        {
            try
            {
                var trainingTrainers = _context.TrainingTrainers
                    .Include(tt => tt.Training)
                    .Include(tt => tt.Trainer)
                    .ToList();
            }
            catch (Exception)
            {
                var trainingTrainersSimple = _context.TrainingTrainers.ToList();
            }
        }

        public bool CreateTrainingBooking(TrainingBooking booking)
        {
            try
            {
                bool alreadyBooked = _context.TrainingBookings
                    .Any(b => b.ClientId == booking.ClientId &&
                             b.ScheduleId == booking.ScheduleId &&
                             b.Status == "confirmed");

                if (alreadyBooked)
                {
                    return false;
                }

                var schedule = _context.TrainingSchedules
                    .FirstOrDefault(s => s.ScheduleId == booking.ScheduleId);

                if (schedule == null)
                {
                    return false;
                }

                if (schedule.CurrentParticipants >= schedule.MaxParticipants)
                {
                    return false;
                }

                var newBooking = new TrainingBooking
                {
                    ClientId = booking.ClientId,
                    TrainingId = booking.TrainingId,
                    ScheduleId = booking.ScheduleId,
                    Status = "confirmed",
                    BookingDate = DateTime.Now
                };

                _context.TrainingBookings.Add(newBooking);
                _context.SaveChanges();

                _context.Entry(schedule).Reload();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool CheckIfClientAlreadyBooked(int clientId, int scheduleId)
        {
            try
            {
                var exists = _context.TrainingBookings
                    .Any(b => b.ClientId == clientId &&
                             b.ScheduleId == scheduleId &&
                             b.Status == "confirmed");
                return exists;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void RecreateAllSchedules()
        {
            try
            {
                var today = DateTime.Today;
                var endDate = today.AddDays(60);

                var futureSchedules = _context.TrainingSchedules
                    .Where(ts => ts.TrainingDate >= today)
                    .ToList();

                if (futureSchedules.Any())
                {
                    _context.TrainingSchedules.RemoveRange(futureSchedules);
                    _context.SaveChanges();
                }

                CreateMonthlySchedules();
                CheckTrainingSchedules();
            }
            catch (Exception) { }
        }

        private void CheckCreatedSchedules(int trainingId, DateTime startDate, List<Models.Trainer> trainers)
        {
            try
            {
                var createdSchedules = _context.TrainingSchedules
                    .AsNoTracking()
                    .Where(ts => ts.TrainingId == trainingId && ts.TrainingDate >= startDate)
                    .ToList();
            }
            catch (Exception) { }
        }

        public List<Models.Trainer> GetTrainersForTraining(int trainingId)
        {
            var trainerIds = _context.TrainingTrainers
                .Where(tt => tt.TrainingId == trainingId)
                .Select(tt => tt.TrainerId)
                .ToList();

            return _context.Trainers
                .Where(t => trainerIds.Contains(t.TrainerId) && t.IsActive)
                .ToList();
        }

        private List<Models.Trainer> GetTrainersForTrainingFallback(int trainingId)
        {
            var trainingTrainerPairs = new Dictionary<int, List<int>>
            {
                { 1, new List<int> { 1, 7 } },
                { 2, new List<int> { 2, 8 } },
                { 3, new List<int> { 3, 9 } },
                { 4, new List<int> { 4, 10 } },
                { 5, new List<int> { 5, 11 } },
                { 6, new List<int> { 6, 12 } }
            };

            if (trainingTrainerPairs.TryGetValue(trainingId, out var trainerIds))
            {
                var trainers = _context.Trainers
                    .AsNoTracking()
                    .Where(t => trainerIds.Contains(t.TrainerId) && t.IsActive)
                    .ToList();
                return trainers;
            }
            return new List<Models.Trainer>();
        }

        public void CheckTrainingSchedules()
        {
            try
            {
                var today = DateTime.Today;
                var endDate = today.AddDays(30);
                var trainings = GetActiveGroupTrainings();

                foreach (var training in trainings)
                {
                    var trainers = GetTrainersForTraining(training.TrainingId);
                    var schedules = _context.TrainingSchedules
                        .Include(ts => ts.Trainer)
                        .Where(ts => ts.TrainingId == training.TrainingId &&
                                    ts.TrainingDate >= today &&
                                    ts.TrainingDate <= endDate)
                        .ToList();
                }
            }
            catch (Exception) { }
        }

        private bool IsTrainerWorking(Models.Trainer trainer, DayOfWeek dayOfWeek)
        {
            var workingDays = trainer.WorkSchedule switch
            {
                "mon-fri" => new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                "wed-sun" => new[] { DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                _ => Array.Empty<DayOfWeek>()
            };

            return workingDays.Contains(dayOfWeek);
        }

        private List<TimeSpan> GetTimeSlotsForTraining(string trainingName)
        {
            return trainingName.ToLower() switch
            {
                var name when name.Contains("virtual") => new List<TimeSpan> { new TimeSpan(9, 0, 0), new TimeSpan(14, 0, 0), new TimeSpan(19, 0, 0) },
                var name when name.Contains("gymtime") => new List<TimeSpan> { new TimeSpan(10, 0, 0), new TimeSpan(15, 0, 0), new TimeSpan(20, 0, 0) },
                var name when name.Contains("yoga") => new List<TimeSpan> { new TimeSpan(8, 0, 0), new TimeSpan(13, 0, 0), new TimeSpan(18, 0, 0) },
                var name when name.Contains("cross") => new List<TimeSpan> { new TimeSpan(11, 0, 0), new TimeSpan(16, 0, 0), new TimeSpan(21, 0, 0) },
                var name when name.Contains("zumba") => new List<TimeSpan> { new TimeSpan(17, 0, 0), new TimeSpan(19, 30, 0) },
                var name when name.Contains("boxing") => new List<TimeSpan> { new TimeSpan(10, 0, 0), new TimeSpan(18, 0, 0), new TimeSpan(20, 0, 0) },
                _ => new List<TimeSpan> { new TimeSpan(9, 0, 0), new TimeSpan(14, 0, 0), new TimeSpan(18, 0, 0) }
            };
        }

        public List<TrainingSchedule> GetSchedulesForTrainer(int trainingId, int trainerId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var schedules = _context.TrainingSchedules
                    .Include(ts => ts.GroupTraining)
                    .Include(ts => ts.Trainer)
                    .Where(ts => ts.TrainingId == trainingId &&
                                ts.TrainerId == trainerId &&
                                ts.IsActive &&
                                ts.TrainingDate >= startDate &&
                                ts.TrainingDate <= endDate)
                    .Where(ts => ts.CurrentParticipants < ts.MaxParticipants)
                    .Where(ts => ts.TrainingDate > DateTime.Now.Date ||
                                (ts.TrainingDate == DateTime.Today && ts.TrainingTime > DateTime.Now.TimeOfDay))
                    .OrderBy(ts => ts.TrainingDate)
                    .ThenBy(ts => ts.TrainingTime)
                    .ToList();

                return schedules;
            }
            catch (Exception)
            {
                return new List<TrainingSchedule>();
            }
        }

        public List<TrainingSchedule> GetAvailableSchedulesForPeriod(int trainingId, DateTime startDate, DateTime endDate)
        {
            try
            {
                EnsureSchedulesExist(trainingId, startDate, endDate);

                var schedules = _context.TrainingSchedules
                    .Include(ts => ts.GroupTraining)
                    .Where(ts => ts.TrainingId == trainingId &&
                                ts.IsActive &&
                                ts.TrainingDate >= startDate &&
                                ts.TrainingDate <= endDate)
                    .Where(ts => ts.CurrentParticipants < ts.MaxParticipants)
                    .Where(ts => ts.TrainingDate > DateTime.Now.Date ||
                                (ts.TrainingDate == DateTime.Today && ts.TrainingTime > DateTime.Now.TimeOfDay))
                    .OrderBy(ts => ts.TrainingDate)
                    .ThenBy(ts => ts.TrainingTime)
                    .ToList();

                return schedules;
            }
            catch (Exception)
            {
                return new List<TrainingSchedule>();
            }
        }

        public void DebugClientBookings(int clientId)
        {
            try
            {
                var bookings = _context.TrainingBookings
                    .Include(b => b.GroupTraining)
                    .Include(b => b.TrainingSchedule)
                    .ThenInclude(ts => ts.Trainer)
                    .Where(b => b.ClientId == clientId)
                    .ToList();
            }
            catch (Exception) { }
        }

        public List<TrainingSchedule> GetAvailableSchedulesForTraining(int trainingId, DateTime startDate, DateTime endDate)
        {
            try
            {
                return _context.TrainingSchedules
                    .Include(ts => ts.GroupTraining)
                    .Include(ts => ts.Trainer)
                    .Where(ts => ts.TrainingId == trainingId &&
                                ts.IsActive &&
                                ts.TrainingDate >= startDate &&
                                ts.TrainingDate <= endDate)
                    .Where(ts => ts.CurrentParticipants < ts.MaxParticipants)
                    .Where(ts => ts.TrainingDate > DateTime.Now.Date ||
                                (ts.TrainingDate == DateTime.Today && ts.TrainingTime > DateTime.Now.TimeOfDay))
                    .OrderBy(ts => ts.TrainingDate)
                    .ThenBy(ts => ts.TrainingTime)
                    .ToList();
            }
            catch (Exception)
            {
                return new List<TrainingSchedule>();
            }
        }

        private void EnsureSchedulesExist(int trainingId, DateTime startDate, DateTime endDate)
        {
            var existingCount = _context.TrainingSchedules
                .Count(ts => ts.TrainingId == trainingId &&
                            ts.IsActive &&
                            ts.TrainingDate >= startDate &&
                            ts.TrainingDate <= endDate);

            if (existingCount < 10)
            {
                CreateScheduleForTraining(trainingId, startDate, endDate);
            }
        }

        public List<TrainingSchedule> GetAvailableSchedules(int trainingId)
        {
            var today = DateTime.Today;
            var endDate = today.AddDays(30);
            return GetAvailableSchedulesForPeriod(trainingId, today, endDate);
        }

        public List<GroupTraining> GetActiveGroupTrainings()
        {
            var trainings = _context.GroupTrainings
                .AsNoTracking()
                .Where(gt => gt.IsActive)
                .Include(gt => gt.IntensityLevel)
                .Include(gt => gt.TrainingType)
                .ToList();

            foreach (var training in trainings)
            {
                training.AvailableTrainers = GetTrainersForTraining(training.TrainingId);
            }

            return trainings;
        }

        public List<GroupTraining> GetGroupTrainingsByTrainer(int trainerId)
        {
            try
            {
                var trainingIds = _context.TrainingTrainers
                    .Where(tt => tt.TrainerId == trainerId)
                    .Select(tt => tt.TrainingId)
                    .ToList();

                if (!trainingIds.Any())
                {
                    return new List<GroupTraining>();
                }

                var trainings = _context.GroupTrainings
                    .Where(gt => gt.IsActive && trainingIds.Contains(gt.TrainingId))
                    .Include(gt => gt.IntensityLevel)
                    .Include(gt => gt.TrainingType)
                    .ToList();

                foreach (var training in trainings)
                {
                    training.AvailableTrainers = GetTrainersForTraining(training.TrainingId);
                }

                return trainings;
            }
            catch (Exception)
            {
                return new List<GroupTraining>();
            }
        }

        public void CheckTrainingData()
        {
            var trainingTrainers = _context.TrainingTrainers.ToList();
            var schedules = _context.TrainingSchedules
                .Include(ts => ts.GroupTraining)
                .Include(ts => ts.Trainer)
                .Where(ts => ts.TrainingDate >= DateTime.Today)
                .ToList();
        }

        public List<IndividualTrainingSlot> GetAvailableIndividualSlotsForClient(int clientId, int trainerId, DateTime startDate, DateTime endDate)
        {
            var slots = new List<IndividualTrainingSlot>();
            var currentDate = startDate;

            var individualTimeSlots = new[]
            {
                new TimeSpan(8, 0, 0),
                new TimeSpan(10, 0, 0),
                new TimeSpan(12, 0, 0),
                new TimeSpan(14, 0, 0),
                new TimeSpan(16, 0, 0),
                new TimeSpan(18, 0, 0),
                new TimeSpan(20, 0, 0),
            };

            var trainerGroupSchedules = _context.TrainingSchedules
                .Include(ts => ts.GroupTraining)
                .Where(ts => ts.TrainerId == trainerId &&
                            ts.IsActive &&
                            ts.TrainingDate >= startDate &&
                            ts.TrainingDate <= endDate)
                .ToList();

            while (currentDate <= endDate)
            {
                var trainer = _context.Trainers.FirstOrDefault(t => t.TrainerId == trainerId);
                if (trainer != null && IsTrainerWorking(trainer, currentDate.DayOfWeek))
                {
                    var groupTrainingsOnDate = trainerGroupSchedules
                        .Where(ts => ts.TrainingDate.Date == currentDate.Date)
                        .ToList();

                    foreach (var startTime in individualTimeSlots)
                    {
                        var endTime = startTime.Add(TimeSpan.FromHours(1));
                        bool conflictsWithGroupTraining = false;

                        foreach (var gt in groupTrainingsOnDate)
                        {
                            if (gt.GroupTraining == null) continue;

                            var groupStart = gt.TrainingTime;
                            var groupEnd = groupStart.Add(TimeSpan.FromMinutes(gt.GroupTraining.DurationMinutes));

                            bool conflict = (startTime < groupEnd && endTime > groupStart);

                            if (conflict)
                            {
                                conflictsWithGroupTraining = true;
                                break;
                            }
                        }

                        if (!conflictsWithGroupTraining)
                        {
                            bool isAvailable = IsIndividualTimeSlotAvailable(clientId, trainerId, currentDate, startTime, endTime);

                            if (isAvailable)
                            {
                                slots.Add(new IndividualTrainingSlot
                                {
                                    TrainingDate = currentDate,
                                    StartTime = startTime,
                                    EndTime = endTime,
                                    TrainerId = trainerId,
                                    IsAvailable = true
                                });
                            }
                        }
                    }
                }
                currentDate = currentDate.AddDays(1);
            }

            return slots;
        }

        public void CheckTrainingDurations()
        {
            var trainings = _context.GroupTrainings.ToList();
        }

        public bool IsIndividualTimeSlotAvailable(int clientId, int trainerId, DateTime trainingDate, TimeSpan startTime, TimeSpan endTime)
        {
            try
            {
                var clientIndividualTrainings = _context.IndividualTrainings
                    .Where(it => it.ClientId == clientId &&
                                it.IsActive &&
                                it.TrainingDate.Date == trainingDate.Date)
                    .ToList();

                bool hasIndividualConflict = clientIndividualTrainings
                    .Any(it => (it.StartTime < endTime && it.EndTime > startTime));

                if (hasIndividualConflict) return false;

                var clientGroupBookings = _context.TrainingBookings
                    .Include(tb => tb.TrainingSchedule)
                    .Include(tb => tb.GroupTraining)
                    .Where(tb => tb.ClientId == clientId &&
                                tb.Status == "confirmed" &&
                                tb.TrainingSchedule != null &&
                                tb.TrainingSchedule.TrainingDate.Date == trainingDate.Date)
                    .ToList();

                bool hasGroupConflict = clientGroupBookings
                    .Any(tb =>
                    {
                        if (tb.TrainingSchedule == null || tb.GroupTraining == null) return false;
                        var scheduleStart = tb.TrainingSchedule.TrainingTime;
                        var scheduleEnd = scheduleStart.Add(TimeSpan.FromMinutes(tb.GroupTraining.DurationMinutes));
                        return (startTime < scheduleEnd && endTime > scheduleStart);
                    });

                if (hasGroupConflict) return false;

                var trainerIndividualTrainings = _context.IndividualTrainings
                    .Where(it => it.TrainerId == trainerId &&
                                it.ClientId != null && 
                                it.IsActive &&
                                it.TrainingDate.Date == trainingDate.Date)
                    .ToList();

                bool hasTrainerIndividualConflict = trainerIndividualTrainings
                    .Any(it => (it.StartTime < endTime && it.EndTime > startTime));

                if (hasTrainerIndividualConflict) return false;

                var trainerGroupSchedules = _context.TrainingSchedules
                    .Include(ts => ts.GroupTraining)
                    .Where(ts => ts.TrainerId == trainerId &&
                                ts.IsActive &&
                                ts.TrainingDate.Date == trainingDate.Date)
                    .ToList();

                bool hasTrainerGroupConflict = trainerGroupSchedules
                    .Any(ts =>
                    {
                        if (ts.GroupTraining == null) return false;
                        var groupStart = ts.TrainingTime;
                        var groupEnd = groupStart.Add(TimeSpan.FromMinutes(ts.GroupTraining.DurationMinutes));
                        return (startTime < groupEnd && endTime > groupStart);
                    });

                if (hasTrainerGroupConflict) return false;

                var otherClientsIndividualTrainings = _context.IndividualTrainings
                    .Where(it => it.TrainerId == trainerId &&
                                it.ClientId != null && 
                                it.ClientId != clientId &&
                                it.IsActive &&
                                it.TrainingDate.Date == trainingDate.Date)
                    .ToList();

                bool hasOtherClientsConflict = otherClientsIndividualTrainings
                    .Any(it => (it.StartTime < endTime && it.EndTime > startTime));

                if (hasOtherClientsConflict) return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void BookIndividualTraining(int clientId, int trainerId, IndividualTrainingSlot slot)
        {
            try
            {
                if (!IsIndividualTimeSlotAvailable(clientId, trainerId, slot.TrainingDate, slot.StartTime, slot.EndTime))
                {
                    throw new Exception("Выбранное время стало недоступно. Возможно, вы или тренер уже заняты другой тренировкой в это время.");
                }

                var trainer = _context.Trainers.FirstOrDefault(t => t.TrainerId == trainerId);
                if (trainer == null) throw new Exception("Тренер не найден");

                var existingSlot = _context.IndividualTrainings.FirstOrDefault(it => 
                    it.TrainerId == trainerId && 
                    it.TrainingDate.Date == slot.TrainingDate.Date && 
                    it.StartTime == slot.StartTime && 
                    it.ClientId == null && 
                    it.IsActive);

                if (existingSlot != null)
                {
                    existingSlot.ClientId = clientId;
                    existingSlot.Price = trainer.IndividualTrainingPrice;
                    existingSlot.CreatedAt = DateTime.UtcNow;
                }
                else
                {
                    var individualTraining = new IndividualTraining
                    {
                        ClientId = clientId,
                        TrainerId = trainerId,
                        TrainingDate = slot.TrainingDate,
                        StartTime = slot.StartTime,
                        EndTime = slot.EndTime,
                        Price = trainer.IndividualTrainingPrice,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.IndividualTrainings.Add(individualTraining);
                }

                _context.SaveChanges();
            }
            catch (DbUpdateException dbEx)
            {
                if (dbEx.InnerException is PostgresException postgresEx && postgresEx.SqlState == "23505")
                {
                    throw new Exception("Не удалось записаться на тренировку. Возможно, это время уже занято другим клиентом.");
                }

                string errorMessage = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                throw new Exception($"Ошибка базы данных при записи на индивидуальную тренировку: {errorMessage}");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public IndividualTraining GetLastIndividualTraining(int clientId, int trainerId)
        {
            return _context.IndividualTrainings
                .Where(it => it.ClientId == clientId && it.TrainerId == trainerId)
                .OrderByDescending(it => it.CreatedAt)
                .FirstOrDefault();
        }

        public bool CancelBooking(int bookingId)
        {
            try
            {
                var booking = _context.TrainingBookings
                    .Include(b => b.TrainingSchedule)
                    .FirstOrDefault(b => b.BookingId == bookingId);

                if (booking != null)
                {
                    if (booking.TrainingSchedule != null)
                    {
                        booking.TrainingSchedule.CurrentParticipants -= 1;
                        if (booking.TrainingSchedule.CurrentParticipants < 0)
                            booking.TrainingSchedule.CurrentParticipants = 0;
                    }

                    _context.TrainingBookings.Remove(booking);
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool CancelIndividualTraining(int individualTrainingId)
        {
            try
            {
                var training = _context.IndividualTrainings
                    .FirstOrDefault(it => it.IndividualTrainingId == individualTrainingId);

                if (training != null)
                {
                    training.ClientId = null;
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public TrainingSchedule GetScheduleById(int scheduleId)
        {
            try
            {
                return _context.TrainingSchedules
                    .FirstOrDefault(s => s.ScheduleId == scheduleId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public TrainingBooking GetLastBooking(int clientId, int scheduleId)
        {
            try
            {
                return _context.TrainingBookings
                    .Include(tb => tb.GroupTraining)
                    .Include(tb => tb.TrainingSchedule)
                    .Where(tb => tb.ClientId == clientId && tb.ScheduleId == scheduleId)
                    .OrderByDescending(tb => tb.BookingDate)
                    .FirstOrDefault();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool HasActiveSubscription(int clientId)
        {
            var today = DateTime.UtcNow.Date;
            return _context.ClientSubscriptions
                .Any(cs => cs.ClientId == clientId &&
                          cs.IsActive &&
                          cs.EndDate.Date >= today);
        }

        public ClientSubscription GetActiveSubscription(int clientId)
        {
            var today = DateTime.UtcNow.Date;
            return _context.ClientSubscriptions
                .Where(cs => cs.ClientId == clientId &&
                           cs.IsActive &&
                           cs.EndDate.Date >= today)
                .Include(cs => cs.Tariff)
                .FirstOrDefault();
        }

        public List<TrainingBooking> GetClientBookings(int clientId)
        {
            try
            {
                return _context.TrainingBookings
                    .Include(tb => tb.GroupTraining)
                    .Include(tb => tb.TrainingSchedule)
                    .ThenInclude(ts => ts.Trainer)
                    .Where(tb => tb.ClientId == clientId)
                    .OrderByDescending(tb => tb.BookingDate)
                    .ToList();
            }
            catch (Exception)
            {
                return new List<TrainingBooking>();
            }
        }

        public void CheckDatabaseConnection()
        {
            try
            {
                var test = _context.TrainingBookings.FirstOrDefault();
            }
            catch (Exception) { }
        }

        public void MarkPastTrainingsAsInactive()
        {
            try
            {
                var now = DateTime.Now;
                var pastTrainings = _context.IndividualTrainings
                    .Where(it => it.IsActive &&
                                (it.TrainingDate < now.Date ||
                                (it.TrainingDate == now.Date && it.EndTime < now.TimeOfDay)))
                    .ToList();

                foreach (var training in pastTrainings)
                {
                    training.IsActive = false;
                }

                if (pastTrainings.Any())
                {
                    _context.SaveChanges();
                }
            }
            catch (Exception) { }
        }
    }
}