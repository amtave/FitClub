using System;
using System.Linq;
using FitClub.Models;

namespace FitClub.Services
{
    public class ClubInfoService
    {
        private readonly AppDbContext _context;
        
        public ClubInfoService(AppDbContext context)
        {
            _context = context;
        }
        
        // Получить текущую информацию о клубе
        public ClubInfo GetClubInfo()
        {
            // Предполагаем, что у нас одна запись
            var info = _context.ClubInfos.FirstOrDefault();
            
            // Если записи нет, создаем с дефолтными значениями
            if (info == null)
            {
                info = new ClubInfo
                {
                    ClubName = "FitClub",
                    Address = "г. Москва, ул. Фитнесная, 15",
                    Phone = "+7 (495) 123-45-67",
                    WorkingHours = "Пн-Вс 7:00 - 23:00",
                    WelcomeText = "Современный фитнес-клуб с лучшим оборудованием и профессиональными тренерами"
                };
                
                _context.ClubInfos.Add(info);
                _context.SaveChanges();
            }
            
            return info;
        }
        
        // Обновить информацию о клубе
        public bool UpdateClubInfo(ClubInfo updatedInfo, int userId)
        {
            try
            {
                var info = _context.ClubInfos.FirstOrDefault();
                if (info == null)
                {
                    info = new ClubInfo();
                    _context.ClubInfos.Add(info);
                }
                
                info.ClubName = updatedInfo.ClubName;
                info.Address = updatedInfo.Address;
                info.Phone = updatedInfo.Phone;
                info.WorkingHours = updatedInfo.WorkingHours;
                info.WelcomeText = updatedInfo.WelcomeText;
                
                if (!string.IsNullOrEmpty(updatedInfo.LogoPath))
                {
                    info.LogoPath = updatedInfo.LogoPath;
                }
                
                info.UpdatedAt = DateTime.Now;
                info.UpdatedBy = userId;
                
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении информации о клубе: {ex.Message}");
                return false;
            }
        }
        
        // Обновить только логотип
        public bool UpdateLogo(string logoPath, int userId)
        {
            try
            {
                var info = _context.ClubInfos.FirstOrDefault();
                if (info == null)
                {
                    info = new ClubInfo();
                    _context.ClubInfos.Add(info);
                }
                
                info.LogoPath = logoPath;
                info.UpdatedAt = DateTime.Now;
                info.UpdatedBy = userId;
                
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении логотипа: {ex.Message}");
                return false;
            }
        }
    }
}