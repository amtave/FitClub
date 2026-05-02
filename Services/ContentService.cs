using Microsoft.EntityFrameworkCore;
using FitClub.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FitClub.Services
{
    public class ContentService
    {
        private readonly AppDbContext _context;

        public ContentService(AppDbContext context)
        {
            _context = context;
        }

        // Получение активных новостей - БЕЗ ОГРАНИЧЕНИЯ
        public List<News> GetActiveNews()
        {
            return _context.News
                .Where(n => n.IsActive)
                .OrderByDescending(n => n.CreatedAt)
                .ToList(); // Убрали Take(10)
        }

        // Получение активных акций - БЕЗ ОГРАНИЧЕНИЯ
        public List<Promotion> GetActivePromotions()
        {
            var today = DateTime.Today;
            
            return _context.Promotions
                .Where(p => p.IsActive && 
                           (!p.ValidUntil.HasValue || p.ValidUntil.Value >= today))
                .OrderByDescending(p => p.CreatedAt)
                .ToList(); // Убрали Take(8)
        }

        // Получение новости по ID
        public News GetNewsById(int newsId)
        {
            return _context.News.FirstOrDefault(n => n.NewsId == newsId && n.IsActive);
        }

        // Получение акции по ID
        public Promotion GetPromotionById(int promotionId)
        {
            var today = DateTime.Today;
            
            return _context.Promotions.FirstOrDefault(p => 
                p.PromotionId == promotionId && 
                p.IsActive && 
                (!p.ValidUntil.HasValue || p.ValidUntil.Value >= today));
        }

        // Добавление новости
        public bool AddNews(News news)
        {
            try
            {
                news.CreatedAt = DateTime.Now;
                news.IsActive = true;
                
                _context.News.Add(news);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении новости: {ex.Message}");
                return false;
            }
        }

        // Обновление новости
        public bool UpdateNews(News news)
        {
            try
            {
                var existingNews = _context.News.Find(news.NewsId);
                if (existingNews != null)
                {
                    existingNews.Title = news.Title;
                    existingNews.Description = news.Description;
                    existingNews.ImagePath = news.ImagePath;
                    
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении новости: {ex.Message}");
                return false;
            }
        }

        // Мягкое удаление новости
        public bool DeleteNews(int newsId)
        {
            try
            {
                var news = _context.News.Find(newsId);
                if (news != null)
                {
                    news.IsActive = false;
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении новости: {ex.Message}");
                return false;
            }
        }

        // Добавление акции
        public bool AddPromotion(Promotion promotion)
        {
            try
            {
                promotion.CreatedAt = DateTime.Now;
                promotion.IsActive = true;
                
                _context.Promotions.Add(promotion);
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении акции: {ex.Message}");
                return false;
            }
        }

        // Обновление акции
        public bool UpdatePromotion(Promotion promotion)
        {
            try
            {
                var existingPromotion = _context.Promotions.Find(promotion.PromotionId);
                if (existingPromotion != null)
                {
                    existingPromotion.Title = promotion.Title;
                    existingPromotion.Description = promotion.Description;
                    existingPromotion.DiscountPercent = promotion.DiscountPercent;
                    existingPromotion.ValidUntil = promotion.ValidUntil;
                    existingPromotion.ImagePath = promotion.ImagePath;
                    
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении акции: {ex.Message}");
                return false;
            }
        }

        // Мягкое удаление акции
        public bool DeletePromotion(int promotionId)
        {
            try
            {
                var promotion = _context.Promotions.Find(promotionId);
                if (promotion != null)
                {
                    promotion.IsActive = false;
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении акции: {ex.Message}");
                return false;
            }
        }
    }
}