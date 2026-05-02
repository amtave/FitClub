using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FitClub.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FitClub.Client.Views
{
    public partial class CardVerificationWindow : Window
    {
        private PaymentCard _paymentCard;
        private Models.Client _client;
        private AppDbContext _context;
        private CancellationTokenSource _timerCancellation;
        private int _timeRemaining = 300; // 5 минут в секундах

        public CardVerificationWindow(PaymentCard paymentCard, Models.Client client)
        {
            InitializeComponent();
            _paymentCard = paymentCard;
            _client = client;
            _context = new AppDbContext();
            
            Console.WriteLine($"CardVerificationWindow created - Card: {paymentCard?.CardNumber ?? "NULL"}, Client: {client?.FullName ?? "NULL"}");
            
            // Ждем инициализации компонентов
            this.AttachedToVisualTree += (s, e) => LoadCardData();
            StartTimer();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadCardData()
        {
            try
            {
                if (_paymentCard == null)
                {
                    Console.WriteLine("ERROR: PaymentCard is null in LoadCardData");
                    return;
                }

                if (_client == null)
                {
                    Console.WriteLine("ERROR: Client is null in LoadCardData");
                    return;
                }

                // Находим элементы после полной инициализации
                var cardInfoText = this.FindControl<TextBlock>("CardInfoText");
                var phoneNumberText = this.FindControl<TextBlock>("PhoneNumberText");

                if (cardInfoText == null || phoneNumberText == null)
                {
                    Console.WriteLine("ERROR: TextBlock controls are not found in CardVerificationWindow");
                    return;
                }

                cardInfoText.Text = $"Карта: {_paymentCard.MaskedCardNumber}";
                phoneNumberText.Text = $"На номер: {_client.Phone}";
                
                Console.WriteLine($"Card data loaded: {_paymentCard.MaskedCardNumber}, {_client.Phone}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in LoadCardData: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
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
                TimerText.Text = "Время истекло!";
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
                var timerText = this.FindControl<TextBlock>("TimerText");
                if (timerText != null)
                {
                    var minutes = _timeRemaining / 60;
                    var seconds = _timeRemaining % 60;
                    timerText.Text = $"Код действителен: {minutes:D2}:{seconds:D2}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UpdateTimerText: {ex.Message}");
            }
        }

        private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var verificationCodeTextBox = this.FindControl<TextBox>("VerificationCodeTextBox");
                if (verificationCodeTextBox == null)
                {
                    await ShowErrorAsync("Ошибка инициализации формы");
                    return;
                }

                string enteredCode = verificationCodeTextBox.Text.Trim();
                
                if (string.IsNullOrEmpty(enteredCode) || enteredCode.Length != 6)
                {
                    await ShowErrorAsync("Введите 6-значный код из SMS");
                    return;
                }

                // В реальном приложении здесь была бы проверка с платежной системой
                // Для демонстрации считаем, что код всегда верный
                bool isCodeValid = true; // enteredCode == _paymentCard.VerificationCode;

                if (isCodeValid)
                {
                    // Помечаем карту как верифицированную
                    _paymentCard.IsVerified = true;
                    _paymentCard.LastUsed = DateTime.Now;

                    // Если это карта по умолчанию, снимаем флаг с других карт
                    if (_paymentCard.IsDefault)
                    {
                        var otherCards = _context.PaymentCards
                            .Where(pc => pc.ClientId == _client.ClientId && pc.CardId != _paymentCard.CardId && pc.IsDefault)
                            .ToList();
                        
                        foreach (var card in otherCards)
                        {
                            card.IsDefault = false;
                        }
                    }

                    _context.PaymentCards.Update(_paymentCard);
                    _context.SaveChanges();

                    _timerCancellation?.Cancel();
                    await ShowSuccessAsync("Карта успешно подтверждена и добавлена!");
                    Close(true);
                }
                else
                {
                    await ShowErrorAsync("Неверный код подтверждения. Пожалуйста, проверьте SMS и попробуйте снова.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in ConfirmButton_Click: {ex.Message}");
                await ShowErrorAsync($"Ошибка при подтверждении карты: {ex.Message}");
            }
        }

        private async void ResendCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Генерируем новый код
                _paymentCard.VerificationCode = GenerateVerificationCode();
                _context.PaymentCards.Update(_paymentCard);
                _context.SaveChanges();

                // Сбрасываем таймер
                _timerCancellation?.Cancel();
                _timeRemaining = 300;
                StartTimer();

                var verificationCodeTextBox = this.FindControl<TextBox>("VerificationCodeTextBox");
                verificationCodeTextBox.Text = "";
                
                await ShowMessageAsync("Код подтверждения отправлен повторно");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in ResendCodeButton_Click: {ex.Message}");
                await ShowErrorAsync($"Ошибка при отправке кода: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _timerCancellation?.Cancel();
            Close(false);
        }

        private string GenerateVerificationCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
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
                Console.WriteLine($"ERROR in ShowErrorAsync: {ex.Message}");
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
                Console.WriteLine($"ERROR in ShowSuccessAsync: {ex.Message}");
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
                Console.WriteLine($"ERROR in ShowMessageAsync: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _timerCancellation?.Cancel();
            _timerCancellation?.Dispose();
            base.OnClosed(e);
        }
    }
}