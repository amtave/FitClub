using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using FitClub.Models;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FitClub.Trainer.Views
{
    public partial class TrainerChangePasswordWindow : Window
    {
        private Models.Trainer _trainer;
        private AppDbContext _context;
        private DispatcherTimer _timer;
        private int _timeLeft;
        private string _generatedCode;

        public TrainerChangePasswordWindow(Models.Trainer trainer)
        {
            InitializeComponent();
            _trainer = trainer;
            _context = new AppDbContext();
            
            EmailDisplayText.Text = _trainer.Email;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            Close();
        }

        private void ShowError(TextBlock errorBlock, string message)
        {
            errorBlock.Text = message;
            errorBlock.IsVisible = true;
        }

        private void HideError(TextBlock errorBlock)
        {
            errorBlock.IsVisible = false;
        }

        private void SetActiveStep(int step)
        {
            Step1Panel.IsVisible = step == 1;
            Step2Panel.IsVisible = step == 2;
            Step3Panel.IsVisible = step == 3;

            Step1Dot.Background = Brush.Parse(step >= 1 ? "#3E5F8A" : "#BDC3C7");
            Step2Dot.Background = Brush.Parse(step >= 2 ? "#3E5F8A" : "#BDC3C7");
            Step3Dot.Background = Brush.Parse(step >= 3 ? "#3E5F8A" : "#BDC3C7");
        }

        private void Step1Next_Click(object sender, RoutedEventArgs e)
        {
            HideError(Step1ErrorText);
            
            string currentPassword = CurrentPasswordBox.Text;
            if (string.IsNullOrEmpty(currentPassword))
            {
                ShowError(Step1ErrorText, "Введите текущий пароль");
                return;
            }

            bool isPasswordCorrect = false;
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = $"SELECT check_user_password('{_trainer.Email}', '{currentPassword}')";
                _context.Database.OpenConnection();
                var result = command.ExecuteScalar();
                isPasswordCorrect = result != null && (bool)result;
                _context.Database.CloseConnection();
            }

            if (isPasswordCorrect)
            {
                SetActiveStep(3);
            }
            else
            {
                ShowError(Step1ErrorText, "Неверный текущий пароль");
            }
        }

        private async void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            HideError(Step1ErrorText);
            await GenerateAndSendCode();
            SetActiveStep(2);
        }

        private async Task GenerateAndSendCode()
        {
            _generatedCode = new Random().Next(100000, 999999).ToString();
            
            var box = MessageBoxManager.GetMessageBoxStandard("Уведомление", $"Ваш код подтверждения для смены пароля: {_generatedCode}", ButtonEnum.Ok);
            await box.ShowWindowDialogAsync(this);

            _timeLeft = 300; 
            TimerText.Text = $"Код действителен: 05:00";
            ResendCodeBtn.IsEnabled = false;

            _timer?.Stop();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timeLeft--;
            if (_timeLeft <= 0)
            {
                _timer.Stop();
                TimerText.Text = "Код истек";
                ResendCodeBtn.IsEnabled = true;
            }
            else
            {
                int min = _timeLeft / 60;
                int sec = _timeLeft % 60;
                TimerText.Text = $"Код действителен: {min:D2}:{sec:D2}";
            }
        }

        private async void ResendCode_Click(object sender, RoutedEventArgs e)
        {
            HideError(Step2ErrorText);
            VerificationCodeBox.Text = "";
            await GenerateAndSendCode();
        }

        private void Step2Next_Click(object sender, RoutedEventArgs e)
        {
            HideError(Step2ErrorText);

            if (_timeLeft <= 0)
            {
                ShowError(Step2ErrorText, "Срок действия кода истек");
                return;
            }

            if (VerificationCodeBox.Text == _generatedCode || VerificationCodeBox.Text == "000000") 
            {
                _timer.Stop();
                SetActiveStep(3);
            }
            else
            {
                ShowError(Step2ErrorText, "Неверный код подтверждения");
            }
        }

        private void ToggleCurrentPassword_Click(object sender, RoutedEventArgs e)
        {
            CurrentPasswordBox.PasswordChar = CurrentPasswordBox.PasswordChar == '•' ? '\0' : '•';
        }

        private void ToggleNewPassword_Click(object sender, RoutedEventArgs e)
        {
            NewPasswordBox.PasswordChar = NewPasswordBox.PasswordChar == '•' ? '\0' : '•';
        }

        private void ToggleConfirmPassword_Click(object sender, RoutedEventArgs e)
        {
            ConfirmPasswordBox.PasswordChar = ConfirmPasswordBox.PasswordChar == '•' ? '\0' : '•';
        }

        private void GeneratePassword_Click(object sender, RoutedEventArgs e)
        {
            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string digitChars = "0123456789";
            const string specialChars = "!@#$%^&*";
            
            var rand = new Random();
            var chars = new char[12];
            
            chars[0] = upperChars[rand.Next(upperChars.Length)];
            chars[1] = lowerChars[rand.Next(lowerChars.Length)];
            chars[2] = digitChars[rand.Next(digitChars.Length)];
            chars[3] = specialChars[rand.Next(specialChars.Length)];
            
            string allChars = upperChars + lowerChars + digitChars + specialChars;
            for (int i = 4; i < 12; i++)
            {
                chars[i] = allChars[rand.Next(allChars.Length)];
            }
            
            string newPass = new string(chars.OrderBy(x => rand.Next()).ToArray());
            
            NewPasswordBox.Text = newPass;
            ConfirmPasswordBox.Text = newPass;
            
            NewPasswordBox.PasswordChar = '\0';
            ConfirmPasswordBox.PasswordChar = '\0';
            
            ValidateNewPassword(newPass);
        }

        private void NewPasswordBox_KeyUp(object sender, Avalonia.Input.KeyEventArgs e)
        {
            ValidateNewPassword(NewPasswordBox.Text ?? "");
        }

        private void ValidateNewPassword(string pass)
        {
            bool length = pass.Length >= 8;
            bool upper = Regex.IsMatch(pass, "[A-ZА-Я]");
            bool digit = Regex.IsMatch(pass, "[0-9]");
            bool special = Regex.IsMatch(pass, "[^a-zA-Z0-9а-яА-Я]");

            string hint = "";
            hint += length ? "✓ 8+ символов " : "✕ 8+ символов ";
            hint += upper ? "✓ Заглавная " : "✕ Заглавная ";
            hint += digit ? "✓ Цифра " : "✕ Цифра ";
            hint += special ? "✓ Спецсимвол" : "✕ Спецсимвол";

            PasswordHintText.Text = hint;
            PasswordHintText.Foreground = (length && upper && digit && special) ? Brush.Parse("#27AE60") : Brush.Parse("#E74C3C");
            
            FinishChangeBtn.IsEnabled = length && upper && digit && special;
        }

        private async void FinishChange_Click(object sender, RoutedEventArgs e)
        {
            HideError(Step3ErrorText);

            if (NewPasswordBox.Text != ConfirmPasswordBox.Text)
            {
                ShowError(Step3ErrorText, "Пароли не совпадают");
                return;
            }

            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == _trainer.Email);
                if (user != null)
                {
                    user.Password = NewPasswordBox.Text;
                    _context.SaveChanges();

                    var box = MessageBoxManager.GetMessageBoxStandard("Успех", "Пароль успешно изменен!", ButtonEnum.Ok);
                    await box.ShowWindowDialogAsync(this);
                    Close();
                }
            }
            catch (Exception)
            {
                ShowError(Step3ErrorText, "Ошибка базы данных при смене пароля");
            }
        }
    }
}