using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace FitClub.Services
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _smtpUsername = "your-email@gmail.com";
        private readonly string _smtpPassword = "your-password";
        
        public async Task<bool> SendVerificationCodeAsync(string toEmail, string verificationCode)
        {
            try
            {
                using (var client = new SmtpClient(_smtpServer, _smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(_smtpUsername, _smtpPassword);
                    
                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_smtpUsername),
                        Subject = "Код подтверждения смены пароля - Fitness Club",
                        Body = $@"
<html>
<body>
    <h2>Подтверждение смены пароля</h2>
    <p>Вы запросили смену пароля в фитнес-клубе.</p>
    <p><strong>Ваш код подтверждения: {verificationCode}</strong></p>
    <p>Код действителен в течение 5 минут.</p>
    <p>Если вы не запрашивали смену пароля, проигнорируйте это письмо.</p>
    <hr>
    <p style='font-size: 12px; color: #666;'>
        С уважением,<br>
        Команда Fitness Club
    </p>
</body>
</html>",
                        IsBodyHtml = true
                    };
                    
                    mailMessage.To.Add(toEmail);
                    
                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}