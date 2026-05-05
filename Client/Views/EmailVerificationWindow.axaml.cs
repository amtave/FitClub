using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FitClub.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace FitClub.Client.Views
{
    public partial class EmailVerificationWindow : Window
    {
        private readonly string _expectedCode;
        private readonly string _email;
        private readonly User _user;
        private readonly bool _isRecoveryMode;
        private string _newPassword = null;
        private AppDbContext _context;
        
        private CancellationTokenSource _timerCancellation;
        private int _timeRemaining = 300; // 5 минут в секундах
        
        // Элементы управления
        private TextBlock _emailText;
        private TextBox _verificationCodeTextBox;
        private TextBlock _timerText;
        private Button _confirmButton;
        
        public EmailVerificationWindow(string verificationCode, string email, User user, bool isRecoveryMode = false, string newPassword = null)
        {
            InitializeComponent();
            _expectedCode = verificationCode;
            _email = email;
            _user = user;
            _isRecoveryMode = isRecoveryMode;
            _newPassword = newPassword;
            _context = new AppDbContext();
            
            InitializeControls();
            
            Loaded += OnWindowLoaded;
            Closing += OnWindowClosing;
        }
        
        private void InitializeControls()
        {
            try
            {
                _emailText = this.FindControl<TextBlock>("EmailText");
                _verificationCodeTextBox = this.FindControl<TextBox>("VerificationCodeTextBox");
                _timerText = this.FindControl<TextBlock>("TimerText");
                _confirmButton = this.FindControl<Button>("ConfirmButton");
                
                if (_emailText != null)
                    _emailText.Text = _email;
                    
                // Настраиваем кнопку в зависимости от режима
                if (_confirmButton != null && _isRecoveryMode)
                {
                    _confirmButton.Content = "✅ Продолжить";
                }
            }
            catch (Exception ex)
            {
                // Без вывода в консоль
            }
        }
        
        private void OnWindowLoaded(object sender, EventArgs e)
        {
            StartTimer();
        }
        
        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _timerCancellation?.Cancel();
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        private async void StartTimer()
        {
            _timerCancellation = new CancellationTokenSource();
            
            try
            {
                while (_timeRemaining > 0)
                {
                    UpdateTimerText();
                    await Task.Delay(1000, _timerCancellation.Token);
                    _timeRemaining--;
                }
                
                // Время вышло
                if (_timerText != null)
                {
                    _timerText.Text = "Время истекло!";
                }
                
                await ShowErrorAsync("Время для ввода кода истекло. Пожалуйста, запросите новый код.");
                Close(false);
            }
            catch (TaskCanceledException)
            {
                // Таймер был отменен
            }
        }
        
        private void UpdateTimerText()
        {
            try
            {
                if (_timerText != null)
                {
                    var minutes = _timeRemaining / 60;
                    var seconds = _timeRemaining % 60;
                    _timerText.Text = $"Код действителен: {minutes:D2}:{seconds:D2}";
                }
            }
            catch (Exception ex)
            {
                // Без вывода в консоль
            }
        }
        
        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_verificationCodeTextBox == null)
                {
                    await ShowErrorAsync("Ошибка инициализации формы");
                    return;
                }

                string enteredCode = _verificationCodeTextBox.Text.Trim();
                
                if (string.IsNullOrEmpty(enteredCode) || enteredCode.Length != 6)
                {
                    await ShowErrorAsync("Введите 6-значный код из email");
                    return;
                }

                if (enteredCode == _expectedCode)
                {
                    _timerCancellation?.Cancel();
                    
                    if (_isRecoveryMode)
                    {
                        // В режиме восстановления просто закрываем окно с успехом
                        // Создание нового пароля будет в основном окне
                        await ShowSuccessAsync("Email подтвержден! Теперь вы можете создать новый пароль.");
                        Close(true);
                    }
                    else
                    {
                        // В обычном режиме меняем пароль сразу
                        if (string.IsNullOrEmpty(_newPassword))
                        {
                            await ShowErrorAsync("Ошибка: новый пароль не указан");
                            return;
                        }
                        
                        bool passwordChanged = await ChangePasswordInDatabaseAsync(_newPassword);
                        
                        if (passwordChanged)
                        {
                            await ShowSuccessAsync("Пароль успешно изменен!");
                            Close(true);
                        }
                        else
                        {
                            await ShowErrorAsync("Ошибка при изменении пароля");
                        }
                    }
                }
                else
                {
                    await ShowErrorAsync("Неверный код подтверждения. Проверьте email и попробуйте снова.");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка: {ex.Message}");
            }
        }
        
        private async Task<bool> ChangePasswordInDatabaseAsync(string newPassword)
        {
            try
            {
                // Находим пользователя
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UsersId == _user.UsersId);
                    
                if (user == null) return false;
                
                // Обновляем пароль - хеширование произойдет в триггере PostgreSQL
                user.Password = newPassword;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        
        private async void ResendCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // В реальном приложении здесь отправка email
                // Для демонстрации просто показываем сообщение
                
                // Сбрасываем таймер
                _timerCancellation?.Cancel();
                _timeRemaining = 300;
                StartTimer();

                if (_verificationCodeTextBox != null)
                {
                    _verificationCodeTextBox.Text = "";
                }
                
                await ShowMessageAsync("Код подтверждения отправлен повторно на ваш email");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Ошибка при отправке кода: {ex.Message}");
            }
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _timerCancellation?.Cancel();
            Close(false);
        }
        
        private async Task ShowErrorAsync(string message)
        {
            try
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", message, ButtonEnum.Ok);
                await box.ShowWindowDialogAsync(this);
            }
            catch (Exception ex)
            {
                // Без вывода в консоль
            }
        }
        
        private async Task ShowSuccessAsync(string message)
        {
            try
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Успех", message, ButtonEnum.Ok);
                await box.ShowWindowDialogAsync(this);
            }
            catch (Exception ex)
            {
                // Без вывода в консоль
            }
        }
        
        private async Task ShowMessageAsync(string message)
        {
            try
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Информация", message, ButtonEnum.Ok);
                await box.ShowWindowDialogAsync(this);
            }
            catch (Exception ex)
            {
                // Без вывода в консоль
            }
        }
    }
}