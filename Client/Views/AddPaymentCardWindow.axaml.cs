using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FitClub.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FitClub.Client.Views
{
    public partial class AddPaymentCardWindow : Window
    {
        private Models.Client _client;
        private AppDbContext _context;

        public AddPaymentCardWindow(Models.Client client)
        {
            InitializeComponent();
            _client = client;
            _context = new AppDbContext();
            
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CardHolderNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    // Автоматически преобразуем в верхний регистр
                    string text = textBox.Text.ToUpper();
                    if (text != textBox.Text)
                    {
                        textBox.Text = text;
                        textBox.CaretIndex = text.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CardHolderNameTextBox_TextChanged: {ex.Message}");
            }
        }

        private void CardNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    // Форматирование номера карты: 0000 0000 0000 0000
                    string text = textBox.Text.Replace(" ", "");
                    if (text.Length > 16) text = text.Substring(0, 16);
                    
                    string formatted = "";
                    for (int i = 0; i < text.Length; i++)
                    {
                        if (i > 0 && i % 4 == 0)
                            formatted += " ";
                        formatted += text[i];
                    }
                    
                    if (textBox.Text != formatted)
                    {
                        textBox.Text = formatted;
                        textBox.CaretIndex = formatted.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CardNumberTextBox_TextChanged: {ex.Message}");
            }
        }

        private void ExpiryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    // Ограничиваем ввод только цифрами
                    string text = new string(textBox.Text.Where(char.IsDigit).ToArray());
                    if (textBox.Text != text)
                    {
                        textBox.Text = text;
                        textBox.CaretIndex = text.Length;
                    }

                    // Автопереход между полями
                    if (textBox.Name == "ExpiryMonthTextBox" && text.Length == 2)
                    {
                        var yearTextBox = this.FindControl<TextBox>("ExpiryYearTextBox");
                        yearTextBox?.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in ExpiryTextBox_TextChanged: {ex.Message}");
            }
        }

        private async void VerifyCardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("VerifyCardButton_Click started");
                
                // Находим элементы управления
                var cardHolderNameTextBox = this.FindControl<TextBox>("CardHolderNameTextBox");
                var cardNumberTextBox = this.FindControl<TextBox>("CardNumberTextBox");
                var expiryMonthTextBox = this.FindControl<TextBox>("ExpiryMonthTextBox");
                var expiryYearTextBox = this.FindControl<TextBox>("ExpiryYearTextBox");
                var cvvTextBox = this.FindControl<TextBox>("CVVTextBox");
                var defaultCardCheckBox = this.FindControl<CheckBox>("DefaultCardCheckBox");

                if (cardHolderNameTextBox == null || cardNumberTextBox == null || 
                    expiryMonthTextBox == null || expiryYearTextBox == null || 
                    cvvTextBox == null || defaultCardCheckBox == null)
                {
                    await ShowErrorAsync("Ошибка инициализации формы. Пожалуйста, перезапустите приложение.");
                    return;
                }

                // Валидация данных
                if (!ValidateCardData(cardHolderNameTextBox, cardNumberTextBox, expiryMonthTextBox, expiryYearTextBox, cvvTextBox))
                    return;

                // Проверяем клиента
                if (_client == null)
                {
                    await ShowErrorAsync("Ошибка: данные клиента не загружены");
                    return;
                }

                Console.WriteLine("Creating payment card...");

                // Создаем временную запись карты (не верифицированную)
                var paymentCard = new PaymentCard
                {
                    ClientId = _client.ClientId,
                    CardNumber = cardNumberTextBox.Text.Replace(" ", ""),
                    CardHolderName = cardHolderNameTextBox.Text.Trim(),
                    ExpiryMonth = int.Parse(expiryMonthTextBox.Text),
                    ExpiryYear = 2000 + int.Parse(expiryYearTextBox.Text), // Преобразуем YY в YYYY
                    CVV = cvvTextBox.Text,
                    IsDefault = defaultCardCheckBox.IsChecked ?? false,
                    IsVerified = false,
                    VerificationCode = GenerateVerificationCode(),
                    CreatedAt = DateTime.Now
                };

                Console.WriteLine("Saving payment card to database...");

                // Сохраняем карту (пока не верифицированную)
                _context.PaymentCards.Add(paymentCard);
                _context.SaveChanges();

                Console.WriteLine("Opening verification window...");

                // Показываем окно верификации
                var verificationWindow = new CardVerificationWindow(paymentCard, _client);
                var result = await verificationWindow.ShowDialog<bool>(this);

                if (result)
                {
                    // Карта успешно верифицирована
                    Console.WriteLine("Card verification successful");
                    Close(true);
                }
                else
                {
                    // Удаляем неверифицированную карту
                    Console.WriteLine("Card verification failed, removing card from database");
                    _context.PaymentCards.Remove(paymentCard);
                    _context.SaveChanges();
                    Close(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in VerifyCardButton_Click: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await ShowErrorAsync($"Ошибка при добавлении карты: {ex.Message}");
            }
        }

        private bool ValidateCardData(TextBox cardHolderName, TextBox cardNumber, TextBox expiryMonth, TextBox expiryYear, TextBox cvv)
        {
            try
            {
                // Проверка имени держателя карты
                string holderName = cardHolderName.Text.Trim();
                if (string.IsNullOrEmpty(holderName))
                {
                    ShowError("Введите имя и фамилию держателя карты на латинице");
                    return false;
                }

                if (!Regex.IsMatch(holderName, @"^[A-Z\s]+$"))
                {
                    ShowError("Имя держателя карты должно содержать только латинские буквы и пробелы");
                    return false;
                }

                // Проверка номера карты
                string cardNumberText = cardNumber.Text.Replace(" ", "");
                if (string.IsNullOrEmpty(cardNumberText) || cardNumberText.Length != 16 || !cardNumberText.All(char.IsDigit))
                {
                    ShowError("Введите корректный 16-значный номер карты");
                    return false;
                }

                // Проверка срока действия
                if (!int.TryParse(expiryMonth.Text, out int month) || month < 1 || month > 12)
                {
                    ShowError("Введите корректный месяц (01-12)");
                    return false;
                }

                if (!int.TryParse(expiryYear.Text, out int year) || year < 24 || year > 40)
                {
                    ShowError("Введите корректный год (24-40)");
                    return false;
                }

                // Проверка CVV
                if (string.IsNullOrEmpty(cvv.Text) || cvv.Text.Length != 3 || !cvv.Text.All(char.IsDigit))
                {
                    ShowError("Введите корректный CVV код (3 цифры)");
                    return false;
                }

                // Проверка срока действия карты
                var currentDate = DateTime.Now;
                var cardDate = new DateTime(2000 + year, month, 1).AddMonths(1).AddDays(-1);
                if (cardDate < currentDate)
                {
                    ShowError("Срок действия карты истек");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in ValidateCardData: {ex.Message}");
                ShowError("Ошибка при проверке данных карты");
                return false;
            }
        }

        private string GenerateVerificationCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private async void ShowError(string message)
        {
            try
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", message, ButtonEnum.Ok);
                await box.ShowWindowDialogAsync(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in ShowError: {ex.Message}");
            }
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
    }
}