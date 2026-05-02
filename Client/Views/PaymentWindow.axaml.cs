using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using FitClub.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Linq;
using FitClub.Services;
using System.IO;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FitClub.Views
{
    public partial class PaymentWindow : Window
    {
        private readonly Tariff _tariff;
        private readonly Models.Client _client;
        private readonly AppDbContext _context;
        
        private List<PaymentCard> _savedCards;
        private PaymentCard _selectedCard = null;
        private bool _useNewCard = false;
        private BonusCard _bonusCard = null;
        private decimal _finalPrice;
        private int _usedBonusPoints = 0;

        public bool PaymentSuccess { get; private set; }
        public ClientSubscription NewSubscription { get; private set; }

        public PaymentWindow(Tariff tariff, Models.Client client)
        {
            InitializeComponent();
            _tariff = tariff;
            _client = client;
            _context = new AppDbContext();
            _finalPrice = _tariff.Price;

            // Заполняем информацию о тарифе
            TariffNameText.Text = tariff.Name;
            TariffDescriptionText.Text = tariff.Description ?? "";
            TariffPriceText.Text = $"Стоимость: {tariff.Price:0.00} ₽";
            TariffDurationText.Text = $"Срок действия: {tariff.DurationDays} дней";

            // Изначально показываем только выбор сохраненной карты
            NewCardSection.IsVisible = false;
            SavedCardsSection.IsVisible = true;
            
            // Загружаем сохранённые карты и бонусную карту
            LoadSavedCards();
            LoadBonusCard();
            
            UpdatePriceDisplay();
        }

        private void LoadSavedCards()
        {
            try
            {
                _savedCards = _context.PaymentCards
                    .Where(pc => pc.ClientId == _client.ClientId && pc.IsVerified)
                    .OrderByDescending(pc => pc.IsDefault)
                    .ThenByDescending(pc => pc.LastUsed)
                    .ToList();

                // Обновляем ComboBox
                SavedCardsComboBox.Items.Clear();
                
                // Добавляем первый элемент "Выберите карту"
                SavedCardsComboBox.Items.Add(new ComboBoxItem 
                { 
                    Content = "-- Выберите карту --",
                    Tag = null 
                });

                if (_savedCards.Any())
                {
                    // Заполняем список карт
                    foreach (var card in _savedCards)
                    {
                        var comboBoxItem = new ComboBoxItem
                        {
                            Content = $"{card.CardIcon} {card.CardType} {card.MaskedCardNumber}" +
                                      (card.IsDefault ? " (основная)" : ""),
                            Tag = card // Сохраняем объект карты в Tag
                        };
                        SavedCardsComboBox.Items.Add(comboBoxItem);
                    }

                    // Выбираем первый элемент
                    SavedCardsComboBox.SelectedIndex = 0;
                }
                else
                {
                    // Нет сохранённых карт - скрываем выбор
                    SavedCardsComboBox.IsVisible = false;
                    var noCardsText = new TextBlock
                    {
                        Text = "💳 У вас нет сохраненных карт. Добавьте карту в разделе 'Мой аккаунт' или введите данные вручную.",
                        FontSize = 11,
                        Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(127, 140, 141)),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        Margin = new Thickness(0, 5, 0, 10)
                    };
                    
                    // Добавляем текстовое сообщение
                    var stackPanel = SavedCardsSection.Child as StackPanel;
                    if (stackPanel != null)
                    {
                        stackPanel.Children.Insert(1, noCardsText);
                    }
                }
            }
            catch (Exception ex)
            {
                _savedCards = new List<PaymentCard>();
                Console.WriteLine($"Ошибка загрузки карт: {ex.Message}");
            }
            
            // Обновляем состояние кнопки оплаты
            ValidatePaymentForm();
        }

        private void LoadBonusCard()
        {
            try
            {
                _bonusCard = _context.BonusCards
                    .FirstOrDefault(bc => bc.ClientId == _client.ClientId && bc.IsActive);

                if (_bonusCard != null)
                {
                    // Показываем панель с бонусной картой
                    HasBonusCardPanel.IsVisible = true;
                    NoBonusCardPanel.IsVisible = false;
                    
                    // Обновляем информацию о карте
                    BonusCardInfoText.Text = $"Бонусная карта {_bonusCard.FormattedCardNumber}";
                    BonusCardBalanceText.Text = $"Баланс: {_bonusCard.PointsBalance} баллов ({(decimal)_bonusCard.PointsBalance} ₽)";
                    
                    // Настраиваем слайдер
                    int maxBonusPoints = (int)Math.Min(_bonusCard.PointsBalance, _finalPrice);
                    BonusPointsSlider.Maximum = maxBonusPoints;
                    BonusPointsSlider.Value = 0;
                    BonusPointsTextBox.Text = "0";
                }
                else
                {
                    // Показываем панель без бонусной карты
                    HasBonusCardPanel.IsVisible = false;
                    NoBonusCardPanel.IsVisible = true;
                    BonusPointsSliderBorder.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                _bonusCard = null;
                HasBonusCardPanel.IsVisible = false;
                NoBonusCardPanel.IsVisible = true;
                Console.WriteLine($"Ошибка загрузки бонусной карты: {ex.Message}");
            }
        }

        private void UpdatePriceDisplay()
        {
            TariffPriceText.Text = $"Стоимость: {_tariff.Price:0.00} ₽";
            if (_usedBonusPoints > 0)
            {
                FinalPriceText.Text = $"Итоговая стоимость: {_finalPrice:0.00} ₽ (списано {_usedBonusPoints} баллов)";
            }
            else
            {
                FinalPriceText.Text = $"Итоговая стоимость: {_finalPrice:0.00} ₽";
            }
        }

        private void UseBonusPointsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_bonusCard != null)
            {
                BonusPointsSliderBorder.IsVisible = true;
                UpdateBonusPointsDisplay();
            }
        }

        private void UseBonusPointsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            BonusPointsSliderBorder.IsVisible = false;
            _usedBonusPoints = 0;
            _finalPrice = _tariff.Price;
            UpdatePriceDisplay();
        }

        private void BonusPointsSlider_ValueChanged(object sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            _usedBonusPoints = (int)BonusPointsSlider.Value;
            BonusPointsTextBox.Text = _usedBonusPoints.ToString();
            UpdateBonusPointsDisplay();
        }

        private void BonusPointsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(BonusPointsTextBox.Text, out int points))
            {
                points = Math.Max(0, Math.Min(points, (int)BonusPointsSlider.Maximum));
                _usedBonusPoints = points;
                BonusPointsSlider.Value = points;
                UpdateBonusPointsDisplay();
            }
        }

        private void UpdateBonusPointsDisplay()
        {
            _finalPrice = Math.Max(0, _tariff.Price - _usedBonusPoints);
            
            BonusPointsUsedText.Text = $"Списано баллов: {_usedBonusPoints} (скидка {_usedBonusPoints} ₽)";
            FinalPriceText.Text = $"Итоговая стоимость: {_finalPrice:0.00} ₽";
            
            // Обновляем кнопку оплаты
            ValidatePaymentForm();
        }

        private void IncreaseBonusPoints_Click(object sender, RoutedEventArgs e)
        {
            int newValue = Math.Min(_usedBonusPoints + 10, (int)BonusPointsSlider.Maximum);
            BonusPointsSlider.Value = newValue;
        }

        private void DecreaseBonusPoints_Click(object sender, RoutedEventArgs e)
        {
            int newValue = Math.Max(_usedBonusPoints - 10, 0);
            BonusPointsSlider.Value = newValue;
        }

        private void NewCardButton_Click(object sender, RoutedEventArgs e)
        {
            // Показываем форму для ввода новой карты
            _useNewCard = true;
            SavedCardsSection.IsVisible = false;
            NewCardSection.IsVisible = true;
            _selectedCard = null;
            
            // Сбрасываем поля новой карты
            CardNumberTextBox.Text = "";
            ExpiryDateTextBox.Text = "";
            CvvTextBox.Text = "";
            HideExpiryDateError(); // Скрываем сообщение об ошибке
            SelectedCardInfo.IsVisible = false;
            
            // Валидируем форму
            ValidateForm();
        }

        private void BackToSavedCardsButton_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся к выбору сохраненной карты
            _useNewCard = false;
            NewCardSection.IsVisible = false;
            SavedCardsSection.IsVisible = true;
            
            ValidatePaymentForm();
        }

        private void SavedCardsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SavedCardsComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                _selectedCard = selectedItem.Tag as PaymentCard;
                
                if (_selectedCard != null)
                {
                    UpdateSelectedCardInfo();
                    
                    // Обновляем LastUsed
                    _selectedCard.LastUsed = DateTime.Now;
                    _context.PaymentCards.Update(_selectedCard);
                    _context.SaveChanges();
                }
                else
                {
                    SelectedCardInfo.IsVisible = false;
                }
            }
            
            ValidatePaymentForm();
        }

        private void UpdateSelectedCardInfo()
        {
            if (_selectedCard != null)
            {
                SelectedCardNumber.Text = _selectedCard.MaskedCardNumber;
                SelectedCardHolder.Text = _selectedCard.DisplayCardHolderName;
                SelectedCardExpiry.Text = _selectedCard.FormattedExpiry;
                SelectedCardInfo.IsVisible = true;
            }
            else
            {
                SelectedCardInfo.IsVisible = false;
            }
        }

        private void ValidatePaymentForm()
        {
            // Кнопка оплаты активна, если выбрана карта и цена больше 0
            PayButton.IsEnabled = _selectedCard != null && !_useNewCard && _finalPrice >= 0;
        }

        private void ValidateForm()
        {
            if (_useNewCard)
            {
                // Для новой карты
                var cardNumber = CardNumberTextBox.Text?.Replace(" ", "") ?? "";
                var expiryDate = ExpiryDateTextBox.Text?.Replace("/", "") ?? "";
                var cvv = CvvTextBox.Text ?? "";

                bool isExpiryDateValid = IsExpiryDateValid(expiryDate);

                PayButton.IsEnabled = cardNumber.Length == 16 &&
                                     expiryDate.Length == 4 &&
                                     cvv.Length == 3 &&
                                     isExpiryDateValid &&
                                     _finalPrice >= 0;
            }
            else
            {
                // Для сохранённой карты
                ValidatePaymentForm();
            }
        }

        // Методы валидации карты
        private void CardNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var text = textBox.Text?.Replace(" ", "") ?? "";

            if (text.Length > 16)
                text = text.Substring(0, 16);

            if (text.Length > 0)
            {
                var formatted = "";
                for (int i = 0; i < text.Length; i++)
                {
                    if (i > 0 && i % 4 == 0)
                        formatted += " ";
                    formatted += text[i];
                }
                textBox.Text = formatted;
                textBox.CaretIndex = formatted.Length;
            }

            ValidateForm();
        }

        private void ExpiryDateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var text = textBox.Text?.Replace("/", "") ?? "";

            if (text.Length > 4)
                text = text.Substring(0, 4);

            if (text.Length >= 2)
            {
                textBox.Text = text.Substring(0, 2) + "/" + (text.Length > 2 ? text.Substring(2) : "");
                textBox.CaretIndex = textBox.Text.Length;
            }

            ValidateExpiryDate(textBox.Text);
            ValidateForm();
        }

        private bool ValidateExpiryDate(string expiryDate)
        {
            if (string.IsNullOrEmpty(expiryDate) || expiryDate.Length < 5)
            {
                HideExpiryDateError();
                return false;
            }

            try
            {
                var parts = expiryDate.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int year))
                {
                    if (month < 1 || month > 12)
                    {
                        ShowExpiryDateError("Месяц должен быть от 01 до 12");
                        return false;
                    }

                    if (year < 100)
                        year += 2000;

                    if (year < 2025)
                    {
                        ShowExpiryDateError("Год должен быть не менее 2025");
                        return false;
                    }

                    var currentDate = DateTime.Now;
                    var expiry = new DateTime(year, month, DateTime.DaysInMonth(year, month));

                    if (expiry < currentDate)
                    {
                        ShowExpiryDateError("Срок действия карты истек");
                        return false;
                    }

                    HideExpiryDateError();
                    return true;
                }
                else
                {
                    ShowExpiryDateError("Неверный формат");
                    return false;
                }
            }
            catch (Exception)
            {
                ShowExpiryDateError("Ошибка валидации");
                return false;
            }
        }

        private void ShowExpiryDateError(string message)
        {
            ExpiryDateTextBox.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E74C3C"));
            ExpiryDateErrorText.Text = message;
            ExpiryDateErrorText.IsVisible = true;
        }

        private void HideExpiryDateError()
        {
            ExpiryDateTextBox.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#BDC3C7"));
            ExpiryDateErrorText.IsVisible = false;
        }

        private void CvvTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var text = textBox.Text ?? "";

            if (text.Length > 3)
                textBox.Text = text.Substring(0, 3);

            ValidateForm();
        }

        private bool IsExpiryDateValid(string expiryDate)
        {
            if (string.IsNullOrEmpty(expiryDate) || expiryDate.Length != 4)
                return false;

            try
            {
                var month = int.Parse(expiryDate.Substring(0, 2));
                var year = int.Parse(expiryDate.Substring(2, 2));

                if (year < 100)
                    year += 2000;

                if (month < 1 || month > 12)
                    return false;
                if (year < 2025)
                    return false;

                var currentDate = DateTime.Now;
                var expiry = new DateTime(year, month, DateTime.DaysInMonth(year, month));

                return expiry >= currentDate;
            }
            catch
            {
                return false;
            }
        }

        private bool IsSavedCardValid(PaymentCard card)
        {
            try
            {
                var currentDate = DateTime.Now;
                var expiry = new DateTime(card.ExpiryYear, card.ExpiryMonth, 
                    DateTime.DaysInMonth(card.ExpiryYear, card.ExpiryMonth));
                
                return expiry >= currentDate;
            }
            catch
            {
                return false;
            }
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok);
            await box.ShowWindowDialogAsync(this);
        }

        private async void PayButton_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // Валидация
        if (!_useNewCard && _selectedCard == null)
        {
            await ShowMessageAsync("Ошибка", "Выберите карту для оплаты");
            return;
        }
        
        if (_useNewCard)
        {
            // Валидация новой карты
            var expiryDate = ExpiryDateTextBox.Text?.Replace("/", "") ?? "";
            if (!IsExpiryDateValid(expiryDate))
            {
                var errorBox = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    "Срок действия карты невалиден. Пожалуйста, проверьте данные.",
                    ButtonEnum.Ok);
                await errorBox.ShowWindowDialogAsync(this);
                return;
            }
        }
        else
        {
            // Проверяем срок действия сохранённой карты
            if (!IsSavedCardValid(_selectedCard))
            {
                await ShowMessageAsync("Ошибка", 
                    "Срок действия выбранной карты истек. Пожалуйста, выберите другую карту.");
                return;
            }
        }

        // Проверяем использование бонусных баллов
        if (_bonusCard != null && _usedBonusPoints > 0)
        {
            if (_usedBonusPoints > _bonusCard.PointsBalance)
            {
                await ShowMessageAsync("Ошибка", 
                    $"Недостаточно бонусных баллов. На вашей карте только {_bonusCard.PointsBalance} баллов.");
                return;
            }
            
            if (_finalPrice <= 0)
            {
                var confirmBox = MessageBoxManager.GetMessageBoxStandard(
                    "Подтверждение",
                    "Вы можете оплатить всю сумму бонусными баллами. Продолжить?",
                    ButtonEnum.YesNo);
                
                var result = await confirmBox.ShowWindowDialogAsync(this);
                if (result != ButtonResult.Yes)
                {
                    return;
                }
            }
        }

        // Показываем загрузку
        PayButton.IsVisible = false;
        LoadingBorder.IsVisible = true;

        // Имитируем обработку платежа
        await Task.Delay(2000);

        Console.WriteLine($"=== ОПЛАТА АБОНЕМЕНТА ===");
        Console.WriteLine($"Клиент ID: {_client.ClientId}");
        Console.WriteLine($"Тариф: {_tariff.Name}");
        Console.WriteLine($"Базовая стоимость: {_tariff.Price:0.00} ₽");
        Console.WriteLine($"Списано баллов: {_usedBonusPoints}");
        Console.WriteLine($"Итоговая стоимость: {_finalPrice:0.00} ₽");
        
        if (_useNewCard)
        {
            Console.WriteLine($"Использована новая карта");
        }
        else
        {
            Console.WriteLine($"Использована сохранённая карта: {_selectedCard.MaskedCardNumber}");
        }

        // 1. Обработка бонусных баллов - ИСПОЛЬЗУЕМ ОДИН КОНТЕКСТ
        using (var context = new AppDbContext())
        {
            try
            {
                // ПРОВЕРКА: есть ли активный абонемент
                var activeSubscription = context.ClientSubscriptions
                    .Where(cs => cs.ClientId == _client.ClientId &&
                                cs.IsActive &&
                                cs.EndDate >= DateTime.Today)
                    .FirstOrDefault();

                if (activeSubscription != null)
                {
                    throw new InvalidOperationException(
                        $"У вас уже есть активный абонемент '{activeSubscription.Tariff.Name}', " +
                        $"действующий до {activeSubscription.EndDate:dd.MM.yyyy}. " +
                        $"Новый абонемент можно приобрести после окончания текущего.");
                }

                // 2. ОБРАБОТКА БОНУСОВ - Загружаем актуальную бонусную карту из БД
                var bonusCardInDb = context.BonusCards
                    .FirstOrDefault(bc => bc.ClientId == _client.ClientId && bc.IsActive);

                if (bonusCardInDb != null)
                {
                    // Списание баллов
                    if (_usedBonusPoints > 0)
                    {
                        bonusCardInDb.PointsBalance -= _usedBonusPoints;
                        Console.WriteLine($"Списано {_usedBonusPoints} бонусных баллов");
                        Console.WriteLine($"Остаток на карте: {bonusCardInDb.PointsBalance} баллов");
                    }

                    // Начисление новых бонусных баллов (2% от итоговой суммы)
                    int earnedPoints = (int)Math.Floor(_finalPrice * 0.02m); // 2%
                    if (earnedPoints > 0)
                    {
                        bonusCardInDb.PointsBalance += earnedPoints;
                        Console.WriteLine($"Начислено {earnedPoints} бонусных баллов (2% от {_finalPrice:0.00} ₽)");
                        Console.WriteLine($"Итоговый баланс: {bonusCardInDb.PointsBalance} баллов");
                    }
                }
                else
                {
                    Console.WriteLine("Бонусной карты нет, баллы не начисляются");
                }

                // 3. Создаем запись о подписке
                var subscription = new ClientSubscription
                {
                    ClientId = _client.ClientId,
                    TariffId = _tariff.TariffId,
                    StartDate = DateTime.Today,
                    IsActive = true,
                    PurchaseDate = DateTime.Today
                };

                context.ClientSubscriptions.Add(subscription);
                await context.SaveChangesAsync(); // Сохраняем все изменения (и бонусы, и подписку)

                // 4. Загружаем полные данные подписки
                var savedSubscription = await context.ClientSubscriptions
                    .Include(s => s.Tariff)
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscription.SubscriptionId);

                if (savedSubscription != null)
                {
                    PaymentSuccess = true;
                    NewSubscription = savedSubscription;

                    LoadingBorder.IsVisible = false;
                    SuccessBorder.IsVisible = true;

                    // 5. Получаем актуальный баланс для чека
                    int finalBalance = 0;
                    if (bonusCardInDb != null)
                    {
                        // Перезагружаем карту для получения актуального баланса
                        await context.Entry(bonusCardInDb).ReloadAsync();
                        finalBalance = bonusCardInDb.PointsBalance;
                    }

                    // ✅ СОХРАНЯЕМ ЧЕК С ПРАВИЛЬНЫМИ ДАННЫМИ О БОНУСАХ
                    await SaveReceiptWithDialog(savedSubscription, bonusCardInDb, finalBalance);
                }
                else
                {
                    throw new Exception("Не удалось загрузить данные подписки после сохранения.");
                }
            }
            catch (Exception ex)
            {
                LoadingBorder.IsVisible = false;
                PayButton.IsVisible = true;

                // Выводим полную информацию об ошибке в консоль
                Console.WriteLine("=== ПОЛНАЯ ИНФОРМАЦИЯ ОБ ОШИБКЕ ===");
                Console.WriteLine($"Сообщение: {ex.Message}");
                Console.WriteLine($"Тип исключения: {ex.GetType()}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Внутреннее исключение: {ex.InnerException.Message}");
                }

                Console.WriteLine("=== КОНЕЦ ИНФОРМАЦИИ ОБ ОШИБКЕ ===");

                // Формируем понятное сообщение для пользователя
                string errorMessage;

                if (ex is InvalidOperationException)
                {
                    // Наша кастомная ошибка из кода
                    errorMessage = ex.Message;
                }
                else if (ex.Message.Contains("активный абонемент"))
                {
                    // Ошибка из триггера базы данных
                    errorMessage = ex.Message;
                }
                else
                {
                    // Другие ошибки
                    errorMessage = $"Произошла ошибка при оформлении подписки:\n{ex.Message}";

                    if (ex.InnerException != null)
                    {
                        errorMessage += $"\n\nДетали: {ex.InnerException.Message}";
                    }
                }

                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Ошибка",
                    errorMessage,
                    ButtonEnum.Ok);
                await box.ShowWindowDialogAsync(this);
            }
        }
    }
    catch (Exception ex)
    {
        LoadingBorder.IsVisible = false;
        PayButton.IsVisible = true;

        Console.WriteLine($"❌ ОШИБКА ОПЛАТЫ: {ex.Message}");
        Console.WriteLine($"StackTrace: {ex.StackTrace}");

        var box = MessageBoxManager.GetMessageBoxStandard(
            "Ошибка",
            $"Не удалось выполнить оплату: {ex.Message}",
            ButtonEnum.Ok);
        await box.ShowWindowDialogAsync(this);
    }
}

        private async Task SaveReceiptWithDialog(ClientSubscription subscription, BonusCard bonusCard, int finalBalance)
{
    try
    {
        int earnedPoints = (int)Math.Floor(_finalPrice * 0.02m);
        string bonusCardInfo = bonusCard != null 
            ? $"Бонусная карта: {bonusCard.FormattedCardNumber}\n" +
              $"Начислено баллов: {earnedPoints}\n" +
              $"Итоговый баланс: {finalBalance} баллов\n"
            : "Бонусной карты нет, баллы не начислены\n";

        string receiptText = $"Чек об оплате абонемента\n" +
                            $"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}\n" +
                            $"Клиент: {_client.FullName}\n" +
                            $"Тариф: {_tariff.Name}\n" +
                            $"Базовая стоимость: {_tariff.Price:0.00} ₽\n" +
                            $"Списано бонусных баллов: {_usedBonusPoints}\n" +
                            $"Итоговая стоимость: {_finalPrice:0.00} ₽\n" +
                            $"{bonusCardInfo}" +
                            $"Дата начала: {subscription.StartDate:dd.MM.yyyy}\n" +
                            $"Дата окончания: {subscription.EndDate:dd.MM.yyyy}\n" +
                            $"Номер чека: {subscription.SubscriptionId}";

        // Просто показываем чек в сообщении
        var receiptBox = MessageBoxManager.GetMessageBoxStandard(
            "Чек об оплате",
            receiptText,
            ButtonEnum.Ok);
        await receiptBox.ShowWindowDialogAsync(this);
    }
    catch (Exception)
    {
        // Игнорируем ошибки с чеком
    }
}

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close(PaymentSuccess);
        }
    }
}