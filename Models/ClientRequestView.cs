using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace FitClub.Models
{
    public class ClientRequestView
    {
        public int RequestId { get; set; }
        public int ClientId { get; set; }
        public string ClientFullName { get; set; }
        public string PassportSeries { get; set; }
        public string PassportNumber { get; set; }
        public DateTime SubmittedAt { get; set; }
        public int StatusId { get; set; }
        public string RejectionReason { get; set; }
        public string AvatarPath { get; set; }
        
        public bool IsPending => StatusId == 2;

        public string StatusDisplay => StatusId switch
        {
            1 => "Не отправлено",
            2 => "На проверке",
            3 => "Подтверждены",
            4 => "Отклонены",
            _ => "Неизвестно"
        };
        
        public string StatusColor => StatusId switch
        {
            2 => "#F39C12",
            3 => "#27AE60", 
            4 => "#E74C3C",
            _ => "#7F8C8D"
        };

        public string StatusIcon => StatusId switch
        {
            2 => "⏳",
            3 => "✅",
            4 => "❌",
            _ => "📋"
        };

        public Bitmap AvatarBitmap
        {
            get
            {
                if (string.IsNullOrEmpty(AvatarPath))
                {
                    try { return new Bitmap(AssetLoader.Open(new Uri("avares://FitClub/Assets/default_avatar.png"))); }
                    catch { return null; }
                }
                try
                {
                    var path = AvatarPath.StartsWith("Assets/") ? AvatarPath : $"Assets/{AvatarPath}";
                    path = path.TrimStart('/');
                    return new Bitmap(AssetLoader.Open(new Uri($"avares://FitClub/{path}")));
                }
                catch
                {
                    try { return new Bitmap(AssetLoader.Open(new Uri("avares://FitClub/Assets/default_avatar.png"))); }
                    catch { return null; }
                }
            }
        }
    }
}