using System;
using System.Linq;
using FitClub.Models;

namespace FitClub.Services
{
    public class ClientHomeService
    {
        private readonly AppDbContext _context;
        
        public ClientHomeService(AppDbContext context)
        {
            _context = context;
        }
        
        // Получить информацию о клубе
        public ClubInfo GetClubInfo()
        {
            return _context.ClubInfos.FirstOrDefault();
        }
    }
}