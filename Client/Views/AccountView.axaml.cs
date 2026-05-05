using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.IO;
using System.Threading.Tasks;
using FitClub.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace FitClub.Client.Views
{
    public partial class AccountView : UserControl
    {
        private User _currentUser;
        private Models.Client _currentClient;
        private AppDbContext _context;
        private string _avatarsFolder;
        private BonusCard _currentBonusCard;
        private List<PaymentCard> _paymentCards;
        private List<TrainingPlanGoal> _availableGoals;
        private TrainingPlanGoal _selectedGoal;
        private bool _isEditingAdditionalInfo = false;
        private bool _isEditingBasicInfo = false;
        
        public AccountView(User user, Models.Client client)
{
    InitializeComponent();
    
    _currentUser = user;
    _currentClient = client;
    _context = new AppDbContext();

    // Загружаем связанные данные
    if (_currentClient != null)
    {
        // Явно загружаем цель, если она есть
        if (_currentClient.GoalId.HasValue)
        {
            _currentClient.Goal = _context.TrainingPlanGoals
                .FirstOrDefault(g => g.GoalId == _currentClient.GoalId);
        }
    }
    
    // Папка для хранения аватаров в проекте
    _avatarsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Avatars");
    if (!Directory.Exists(_avatarsFolder))
    {
        Directory.CreateDirectory(_avatarsFolder);
    }

    LoadUserData();
    UpdatePassportDataDisplay();
    LoadBonusCard();
    LoadPaymentCards();
}

        private void LoadPaymentCards()
        {
            try
            {
                _paymentCards = _context.PaymentCards
                    .Where(pc => pc.ClientId == _currentClient.ClientId && pc.IsVerified)
                    .OrderByDescending(pc => pc.IsDefault)
                    .ThenByDescending(pc => pc.LastUsed)
                    .ToList();

                // Проверяем, что только одна карта помечена как "по умолчанию"
                var defaultCards = _paymentCards.Where(c => c.IsDefault).ToList();
                if (defaultCards.Count > 1)
                {
                    // Оставляем только первую карту как карту по умолчанию
                    for (int i = 1; i < defaultCards.Count; i++)
                    {
                        defaultCards[i].IsDefault = false;
                        _context.PaymentCards.Update(defaultCards[i]);
                    }
                    _context.SaveChanges();
                    
                    // Перезагружаем карты после исправления
                    _paymentCards = _context.PaymentCards
                        .Where(pc => pc.ClientId == _currentClient.ClientId && pc.IsVerified)
                        .OrderByDescending(pc => pc.IsDefault)
                        .ThenByDescending(pc => pc.LastUsed)
                        .ToList();
                }

                UpdatePaymentCardsDisplay();
            }
            catch (Exception ex)
            {
                _paymentCards = new List<PaymentCard>();
                UpdatePaymentCardsDisplay();
            }
        }

        private void UpdatePaymentCardsDisplay()
        {
            // Очищаем панель карт
            PaymentCardsPanel.Children.Clear();

            if (_paymentCards.Any())
            {
                foreach (var card in _paymentCards)
                {
                    var cardControl = CreatePaymentCardControl(card);
                    PaymentCardsPanel.Children.Add(cardControl);
                }

                // Проверяем максимальное количество карт
                if (_paymentCards.Count >= 2)
                {
                    AddCardButton.IsVisible = false;
                    MaxCardsMessage.IsVisible = true;
                }
                else
                {
                    AddCardButton.IsVisible = true;
                    MaxCardsMessage.IsVisible = false;
                }
            }
            else
            {
                // Показываем сообщение, если карт нет
                var noCardsText = new TextBlock
                {
                    Text = "💳 Карты не добавлены",
                    FontSize = 12,
                    Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(127, 140, 141)),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    TextAlignment = Avalonia.Media.TextAlignment.Center
                };
                PaymentCardsPanel.Children.Add(noCardsText);

                AddCardButton.IsVisible = true;
                MaxCardsMessage.IsVisible = false;
            }
        }

        private Border CreatePaymentCardControl(PaymentCard card)
        {
            var border = new Border
            {
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(248, 249, 250)),
                BorderBrush = card.IsDefault ? 
                    new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(52, 152, 219)) : 
                    new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(189, 195, 199)),
                BorderThickness = new Thickness(card.IsDefault ? 3 : 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Левая часть - информация о карте
            var leftStackPanel = new StackPanel();
            Grid.SetColumn(leftStackPanel, 0);

            // Первая строка: иконка, тип карты, статус по умолчанию
            var firstRow = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8
            };

            firstRow.Children.Add(new TextBlock
            {
                Text = card.CardIcon,
                FontSize = 14
            });

            firstRow.Children.Add(new TextBlock
            {
                Text = card.CardType,
                FontSize = 12,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(44, 62, 80))
            });

            if (card.IsDefault)
            {
                firstRow.Children.Add(new Border
                {
                    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(52, 152, 219)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2),
                    Child = new TextBlock
                    {
                        Text = "По умолчанию",
                        FontSize = 9,
                        FontWeight = Avalonia.Media.FontWeight.Bold,
                        Foreground = Avalonia.Media.Brushes.White
                    }
                });
            }

            leftStackPanel.Children.Add(firstRow);

            // Вторая строка: номер карты
            leftStackPanel.Children.Add(new TextBlock
            {
                Text = card.MaskedCardNumber,
                FontSize = 14,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(44, 62, 80)),
                Margin = new Thickness(0, 4, 0, 0)
            });

            // Третья строка: держатель карты
            leftStackPanel.Children.Add(new TextBlock
            {
                Text = card.DisplayCardHolderName,
                FontSize = 10,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(127, 140, 141)),
                Margin = new Thickness(0, 2, 0, 0)
            });

            // Четвертая строка: срок действия
            leftStackPanel.Children.Add(new TextBlock
            {
                Text = $"Срок: {card.FormattedExpiry}",
                FontSize = 10,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(127, 140, 141))
            });

            // Кнопка "Сделать по умолчанию" (если карта не является картой по умолчанию)
            if (!card.IsDefault)
            {
                var setDefaultButton = new Button
                {
                    Content = "⭐ Сделать основной",
                    Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(241, 196, 15)),
                    Foreground = Avalonia.Media.Brushes.White,
                    FontSize = 9,
                    FontWeight = Avalonia.Media.FontWeight.Bold,
                    Padding = new Thickness(8, 2),
                    Margin = new Thickness(0, 5, 0, 0),
                    Tag = card.CardId
                };
                setDefaultButton.Click += SetDefaultCardButton_Click;
                leftStackPanel.Children.Add(setDefaultButton);
            }

            // Правая часть - кнопка удаления
            var deleteButton = new Button
            {
                Content = "🗑️",
                Background = Avalonia.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Tag = card.CardId,
                Margin = new Thickness(5, 0, 0, 0)
            };
            deleteButton.Click += DeleteCardButton_Click;

            // Контейнер для правой части
            var rightStackPanel = new StackPanel
            {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            rightStackPanel.Children.Add(deleteButton);

            Grid.SetColumn(rightStackPanel, 1);

            mainGrid.Children.Add(leftStackPanel);
            mainGrid.Children.Add(rightStackPanel);

            border.Child = mainGrid;
            return border;
        }

        private async void SetDefaultCardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is int cardId)
                {
                    var selectedCard = _paymentCards.FirstOrDefault(c => c.CardId == cardId);
                    if (selectedCard != null)
                    {
                        // Снимаем флаг "по умолчанию" со всех карт
                        foreach (var card in _paymentCards)
                        {
                            if (card.IsDefault)
                            {
                                card.IsDefault = false;
                                _context.PaymentCards.Update(card);
                            }
                        }

                        // Устанавливаем выбранную карту как карту по умолчанию
                        selectedCard.IsDefault = true;
                        _context.PaymentCards.Update(selectedCard);
                        _context.SaveChanges();

                        // Перезагружаем карты
                        LoadPaymentCards();
                        await ShowMessage("Успех", $"Карта {selectedCard.MaskedCardNumber} теперь используется по умолчанию");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка при установке карты по умолчанию: {ex.Message}");
            }
        }

        private async void DeleteCardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag is int cardId)
                {
                    var card = _paymentCards.FirstOrDefault(c => c.CardId == cardId);
                    if (card != null)
                    {
                        var box = MessageBoxManager.GetMessageBoxStandard(
                            "Подтверждение удаления",
                            $"Вы уверены, что хотите удалить карту {card.MaskedCardNumber}?",
                            ButtonEnum.YesNo,
                            Icon.Question);

                        var result = await box.ShowWindowDialogAsync((Window)this.VisualRoot);

                        if (result == ButtonResult.Yes)
                        {
                            bool wasDefault = card.IsDefault;
                            
                            _context.PaymentCards.Remove(card);
                            _context.SaveChanges();

                            // Если удалили карту по умолчанию и есть другие карты, назначаем первую как карту по умолчанию
                            if (wasDefault && _paymentCards.Count > 1)
                            {
                                var remainingCards = _paymentCards.Where(c => c.CardId != cardId).ToList();
                                if (remainingCards.Any())
                                {
                                    var newDefaultCard = remainingCards.First();
                                    newDefaultCard.IsDefault = true;
                                    _context.PaymentCards.Update(newDefaultCard);
                                    _context.SaveChanges();
                                }
                            }

                            // Перезагружаем карты
                            LoadPaymentCards();
                            await ShowMessage("Успех", "Карта успешно удалена!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка при удалении карты: {ex.Message}");
            }
        }

        private async void AddPaymentCard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentClient == null)
                {
                    await ShowError("Ошибка: данные клиента не загружены");
                    return;
                }

                var dialog = new AddPaymentCardWindow(_currentClient);
                var result = await dialog.ShowDialog<bool>((Window)this.VisualRoot);

                if (result)
                {
                    LoadPaymentCards();
                    await ShowMessage("Успех", "Карта успешно добавлена и проверена!");
                }
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка при добавлении карты: {ex.Message}");
            }
        }

        private void LoadBonusCard()
        {
            try
            {
                _currentBonusCard = _context.BonusCards
                    .FirstOrDefault(bc => bc.ClientId == _currentClient.ClientId && bc.IsActive);

                UpdateBonusCardDisplay();
            }
            catch (Exception)
            {
                _currentBonusCard = null;
                UpdateBonusCardDisplay();
            }
        }

        private void UpdateBonusCardDisplay()
        {
            if (_currentBonusCard != null && _currentBonusCard.IsActive)
            {
                NoBonusCardBorder.IsVisible = false;
                BonusCardBorder.IsVisible = true;

                BonusCardNumberText.Text = _currentBonusCard.FormattedCardNumber;
                BonusCardHolderText.Text = _currentClient.FullName.ToUpper();
                BonusCardIssueDateText.Text = $"Выпуск: {_currentBonusCard.IssueDate:dd.MM.yyyy}";
                BonusCardPointsText.Text = $"{_currentBonusCard.PointsBalance} баллов";
            }
            else
            {
                NoBonusCardBorder.IsVisible = true;
                BonusCardBorder.IsVisible = false;
            }
        }

        private async void ApplyForBonusCard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_currentClient.HasPassportData)
                {
                    await ShowError("Для оформления бонусной карты необходимо заполнить паспортные данные.");
                    return;
                }

                var result = await ShowBonusCardApplicationDialog();
                
                if (result)
                {
                    var newBonusCard = new BonusCard
                    {
                        ClientId = _currentClient.ClientId,
                        CardNumber = GenerateCardNumber(),
                        PointsBalance = 100,
                        IssueDate = DateTime.Now,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    _context.BonusCards.Add(newBonusCard);
                    _context.SaveChanges();

                    _currentBonusCard = newBonusCard;
                    UpdateBonusCardDisplay();

                    await ShowMessage("Поздравляем!", 
                        $"Бонусная карта успешно оформлена!\n" +
                        $"Номер карты: {newBonusCard.FormattedCardNumber}\n" +
                        $"Вам начислено: {newBonusCard.PointsBalance} баллов");
                }
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка при оформлении бонусной карты: {ex.Message}");
            }
        }

        private async Task<bool> ShowBonusCardApplicationDialog()
        {
            var dialog = new BonusCardApplicationWindow(_currentClient);
            return await dialog.ShowDialog<bool>((Window)this.VisualRoot);
        }

        private string GenerateCardNumber()
        {
            var datePart = DateTime.Now.ToString("yyMMdd");
            var randomPart = new Random().Next(1000, 9999).ToString();
            return $"FC-{datePart}-{randomPart}";
        }

        private void LoadUserData()
        {
            if (_currentClient == null)
            {
                return;
            }
            
            FullNameText.Text = $"{_currentClient.LastName} {_currentClient.FirstName} {_currentClient.MiddleName}";
            EmailText.Text = _currentClient.Email;
            PhoneText.Text = _currentClient.Phone;

            PassportSeriesText.Text = _currentClient.PassportSeries;
            PassportNumberText.Text = _currentClient.PassportNumber;

            LoadAdditionalInfo();
            LoadAvatar();
            LoadClientSubscription();
        }

        private void AddAdditionalInfo_Click(object sender, RoutedEventArgs e)
        {
            StartEditingAdditionalInfo();
        }

        private void EditAdditionalInfo_Click(object sender, RoutedEventArgs e)
        {
            StartEditingAdditionalInfo();
        }

        private void StartEditingAdditionalInfo()
        {
            _isEditingAdditionalInfo = true;
            
            try
            {
                // Заполняем поля для редактирования
                HeightEditTextBox.Text = _currentClient.HeightCm?.ToString() ?? string.Empty;
                WeightEditTextBox.Text = _currentClient.WeightKg?.ToString() ?? string.Empty;
                
                // УСТАНАВЛИВАЕМ ЗНАЧЕНИЕ ПОЛА
                string currentGender = _currentClient.Gender ?? "";
                
                if (currentGender == "Мужской")
                {
                    GenderComboBox.SelectedIndex = 1; // Второй элемент (индекс 1)
                }
                else if (currentGender == "Женский")
                {
                    GenderComboBox.SelectedIndex = 2; // Третий элемент (индекс 2)
                }
                else
                {
                    GenderComboBox.SelectedIndex = 0; // Первый элемент (индекс 0) - "Не указан"
                }
                
                // БЛОКИРУЕМ ComboBox если пол уже выбран (Мужской или Женский)
                if (!string.IsNullOrEmpty(currentGender) && currentGender != "Не указан")
                {
                    GenderComboBox.IsEnabled = false;
                    
                    // Альтернатива ToolTip - добавляем текстовое сообщение под ComboBox
                    // Создаем текстовый блок с сообщением
                    var messageTextBlock = new TextBlock
                    {
                        Text = "⚠️ Пол нельзя изменить после первого выбора",
                        FontSize = 10,
                        Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(231, 76, 60)),
                        Margin = new Thickness(0, 5, 0, 0),
                        IsVisible = true
                    };
                    
                    // Добавляем сообщение в родительский контейнер
                    var parentPanel = GenderComboBox.Parent as StackPanel;
                    if (parentPanel != null)
                    {
                        // Ищем уже существующее сообщение
                        var existingMessage = parentPanel.Children.OfType<TextBlock>()
                            .FirstOrDefault(tb => tb.Name == "GenderLockedMessage");
                            
                        if (existingMessage == null)
                        {
                            messageTextBlock.Name = "GenderLockedMessage";
                            parentPanel.Children.Add(messageTextBlock);
                        }
                    }
                }
                else
                {
                    GenderComboBox.IsEnabled = true;
                    
                    // Убираем сообщение если оно есть
                    var parentPanel = GenderComboBox.Parent as StackPanel;
                    if (parentPanel != null)
                    {
                        var existingMessage = parentPanel.Children.OfType<TextBlock>()
                            .FirstOrDefault(tb => tb.Name == "GenderLockedMessage");
                            
                        if (existingMessage != null)
                        {
                            parentPanel.Children.Remove(existingMessage);
                        }
                    }
                }
                
                // Настраиваем ComboBox для цели
                if (_availableGoals == null || !_availableGoals.Any())
                {
                    _availableGoals = _context.TrainingPlanGoals.ToList();
                }
                
                GoalComboBox.ItemsSource = _availableGoals;
                GoalComboBox.ItemTemplate = new FuncDataTemplate<TrainingPlanGoal>((goal, _) =>
                    new TextBlock { Text = goal?.Name ?? "Не выбрано", FontSize = 14 });
                
                // Устанавливаем выбранную цель
                if (_currentClient.Goal != null)
                {
                    GoalComboBox.SelectedItem = _availableGoals.FirstOrDefault(g => g.GoalId == _currentClient.Goal.GoalId);
                }
                else
                {
                    GoalComboBox.SelectedItem = null;
                }
                
                UpdateAdditionalInfoDisplay();
            }
            catch (Exception ex)
            {
                // Console.WriteLine($"Ошибка в StartEditingAdditionalInfo: {ex.Message}");
            }
        }

        private async void SaveAdditionalInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ПРОВЕРЯЕМ, МОЖНО ЛИ ИЗМЕНИТЬ ПОЛ
                string currentGender = _currentClient.Gender ?? "";
                bool genderCanBeChanged = string.IsNullOrEmpty(currentGender) || currentGender == "Не указан";
                
                // Сохраняем пол только если он еще не выбран или равен "Не указан"
                if (genderCanBeChanged)
                {
                    int selectedIndex = GenderComboBox.SelectedIndex;
                    
                    // Сохраняем пол по индексу
                    switch (selectedIndex)
                    {
                        case 1: // "Мужской"
                            _currentClient.Gender = "Мужской";
                            break;
                        case 2: // "Женский"
                            _currentClient.Gender = "Женский";
                            break;
                        case 0: // "Не указан"
                        default:
                            _currentClient.Gender = null;
                            break;
                    }
                }
                else
                {
                    // Если пол уже выбран (Мужской или Женский), оставляем прежнее значение
                    // Ничего не делаем - поле остается как есть
                }
                
                // Валидация роста
                if (!string.IsNullOrEmpty(HeightEditTextBox.Text))
                {
                    if (int.TryParse(HeightEditTextBox.Text, out int height))
                    {
                        if (height < 100 || height > 250)
                        {
                            await ShowError("Рост должен быть от 100 до 250 см");
                            return;
                        }
                        _currentClient.HeightCm = height;
                    }
                    else
                    {
                        await ShowError("Введите корректное значение роста");
                        return;
                    }
                }
                else
                {
                    _currentClient.HeightCm = null;
                }
                
                // Валидация веса
                if (!string.IsNullOrEmpty(WeightEditTextBox.Text))
                {
                    if (decimal.TryParse(WeightEditTextBox.Text, out decimal weight))
                    {
                        if (weight < 30 || weight > 500)
                        {
                            await ShowError("Вес должен быть от 30 до 500 кг");
                            return;
                        }
                        _currentClient.WeightKg = weight;
                    }
                    else
                    {
                        await ShowError("Введите корректное значение веса");
                        return;
                    }
                }
                else
                {
                    _currentClient.WeightKg = null;
                }
                
                // Обновляем цель
                var selectedGoal = GoalComboBox.SelectedItem as TrainingPlanGoal;
                if (selectedGoal != null)
                {
                    _currentClient.GoalId = selectedGoal.GoalId;
                    _currentClient.Goal = selectedGoal;
                }
                else
                {
                    _currentClient.GoalId = null;
                    _currentClient.Goal = null;
                }
                
                // Обновляем дату изменения
                _currentClient.AdditionalInfoUpdatedAt = DateTime.Now;
                
                // Сохраняем в базу
                _context.Clients.Update(_currentClient);
                await _context.SaveChangesAsync();
                
                // Сбрасываем состояние ComboBox (на случай отмены или повторного редактирования)
                GenderComboBox.IsEnabled = true;
                
                // Убираем сообщение если оно есть
                var parentPanel = GenderComboBox.Parent as StackPanel;
                if (parentPanel != null)
                {
                    var existingMessage = parentPanel.Children.OfType<TextBlock>()
                        .FirstOrDefault(tb => tb.Name == "GenderLockedMessage");
                        
                    if (existingMessage != null)
                    {
                        parentPanel.Children.Remove(existingMessage);
                    }
                }
                
                // Завершаем редактирование
                _isEditingAdditionalInfo = false;
                UpdateAdditionalInfoDisplay();
                
                await ShowMessage("Успех", "Дополнительная информация успешно сохранена!");
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void CancelAdditionalInfo_Click(object sender, RoutedEventArgs e)
        {
            _isEditingAdditionalInfo = false;
            
            // Сбрасываем состояние ComboBox
            GenderComboBox.IsEnabled = true;
            
            // Убираем сообщение если оно есть
            var parentPanel = GenderComboBox.Parent as StackPanel;
            if (parentPanel != null)
            {
                var existingMessage = parentPanel.Children.OfType<TextBlock>()
                    .FirstOrDefault(tb => tb.Name == "GenderLockedMessage");
                    
                if (existingMessage != null)
                {
                    parentPanel.Children.Remove(existingMessage);
                }
            }
            
            // Загружаем актуальные данные из базы (отменяем изменения)
            try
            {
                _context = new AppDbContext();
                _currentClient = _context.Clients
                    .Include(c => c.Goal)
                    .FirstOrDefault(c => c.ClientId == _currentClient.ClientId);
                
                UpdateAdditionalInfoDisplay();
            }
            catch (Exception ex)
            {
                UpdateAdditionalInfoDisplay();
            }
        }

        private void LoadAdditionalInfo()
        {
            try
            {
                // Гарантируем, что Gender соответствует ограничениям
                if (_currentClient.Gender != "Мужской" && _currentClient.Gender != "Женский")
                {
                    _currentClient.Gender = null; // Или "", в зависимости от ограничения
                }
                
                // Загружаем цель тренировок, если есть
                if (_currentClient.GoalId.HasValue && _currentClient.Goal == null)
                {
                    _currentClient.Goal = _context.TrainingPlanGoals
                        .FirstOrDefault(g => g.GoalId == _currentClient.GoalId);
                }

                // Загружаем доступные цели для ComboBox
                _availableGoals = _context.TrainingPlanGoals.ToList();
                
                UpdateAdditionalInfoDisplay();
            }
            catch (Exception ex)
            {
                _availableGoals = new List<TrainingPlanGoal>();
                _currentClient.Gender = null; // Или ""
                UpdateAdditionalInfoDisplay();
            }
        }

        // Маска для телефона
        private void EditPhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                string text = textBox.Text;
                int cursorPosition = textBox.CaretIndex;
                
                // Сохраняем курсор до форматирования
                bool wasAtEnd = cursorPosition == text.Length;
                
                // Удаляем все нецифровые символы кроме +
                string digits = new string(text.Where(c => char.IsDigit(c) || c == '+').ToArray());
                
                // Убираем префиксы
                if (digits.StartsWith("+7"))
                {
                    digits = digits.Substring(2);
                }
                else if (digits.StartsWith("7"))
                {
                    digits = digits.Substring(1);
                }
                else if (digits.StartsWith("8"))
                {
                    digits = digits.Substring(1);
                }
                
                // Ограничиваем 10 цифрами
                digits = new string(digits.Take(10).ToArray());
                
                // Форматируем номер
                string formattedText;
                if (digits.Length >= 10)
                {
                    formattedText = $"+7 {digits.Substring(0, 3)} {digits.Substring(3, 3)}-{digits.Substring(6, 2)}-{digits.Substring(8)}";
                }
                else if (digits.Length >= 6)
                {
                    formattedText = $"+7 {digits.Substring(0, 3)} {digits.Substring(3, 3)}-{digits.Substring(6)}";
                }
                else if (digits.Length >= 3)
                {
                    formattedText = $"+7 {digits.Substring(0, 3)} {digits.Substring(3)}";
                }
                else if (digits.Length > 0)
                {
                    formattedText = $"+7 {digits}";
                }
                else
                {
                    formattedText = "";
                }
                
                // Устанавливаем текст только если он изменился
                if (textBox.Text != formattedText)
                {
                    textBox.Text = formattedText;
                    
                    // Восстанавливаем позицию курсора
                    if (wasAtEnd)
                    {
                        textBox.CaretIndex = textBox.Text.Length;
                    }
                    else
                    {
                        // Пытаемся сохранить позицию курсора
                        textBox.CaretIndex = Math.Min(cursorPosition, formattedText.Length);
                    }
                }
            }
        }

        // Маска для email с валидацией в реальном времени
        private void EditEmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                // Простая визуальная валидация
                string email = textBox.Text.Trim();
                
                if (!string.IsNullOrEmpty(email))
                {
                    bool isValid = IsValidEmail(email);
                    
                    // Меняем цвет рамки в зависимости от валидности
                    if (isValid)
                    {
                        textBox.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(39, 174, 96)); // Зеленый
                    }
                    else
                    {
                        textBox.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(231, 76, 60)); // Красный
                    }
                }
                else
                {
                    // Сбрасываем цвет, если поле пустое
                    textBox.ClearValue(TextBox.BorderBrushProperty);
                }
            }
        }

        // Метод для проверки email (уже есть у вас)
        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private void UpdateAdditionalInfoDisplay()
        {
            if (_currentClient.HasAdditionalInfo && !_isEditingAdditionalInfo)
            {
                NoAdditionalInfoPanel.IsVisible = false;
                AdditionalInfoPanel.IsVisible = true;
                
                GenderText.Text = _currentClient.GenderDisplay;
                HeightText.Text = _currentClient.HeightDisplay;
                WeightText.Text = _currentClient.WeightDisplay;
                GoalText.Text = _currentClient.GoalDisplay;
                
                AdditionalInfoViewGrid.IsVisible = true;
                AdditionalInfoEditGrid.IsVisible = false;
                EditAdditionalInfoButton.IsVisible = true;
            }
            else if (_isEditingAdditionalInfo)
            {
                NoAdditionalInfoPanel.IsVisible = false;
                AdditionalInfoPanel.IsVisible = true;
                
                AdditionalInfoViewGrid.IsVisible = false;
                AdditionalInfoEditGrid.IsVisible = true;
                EditAdditionalInfoButton.IsVisible = false;
            }
            else
            {
                NoAdditionalInfoPanel.IsVisible = true;
                AdditionalInfoPanel.IsVisible = false;
            }
        }

        private async void UpdatePassportDataDisplay()
{
    // Получаем последний запрос на верификацию
    var lastRequest = _context.PassportVerificationRequests
        .Where(r => r.ClientId == _currentClient.ClientId)
        .OrderByDescending(r => r.SubmittedAt)
        .FirstOrDefault();

    // Находим элементы управления
    var infoBorder = this.FindControl<Border>("InfoBorder");
    var infoTitle = this.FindControl<TextBlock>("InfoTitle");
    var infoText = this.FindControl<TextBlock>("InfoText");
    var passportDataPanel = this.FindControl<StackPanel>("PassportDataPanel");
    var actionButton = this.FindControl<Button>("ActionButton");

    // Проверяем, есть ли новый запрос на проверке
    bool hasPendingRequest = lastRequest != null && lastRequest.StatusId == PassportVerificationStatus.PENDING_ID;

    // Случай 1: Есть подтвержденные данные в таблице client
    if (_currentClient.HasPassportData)
    {
        // ВСЕГДА показываем подтвержденные данные, даже если есть новый запрос
        passportDataPanel.IsVisible = true;
        PassportSeriesText.Text = _currentClient.PassportSeries;
        PassportNumberText.Text = _currentClient.PassportNumber;

        if (hasPendingRequest)
        {
            // Есть новые данные на проверке
            infoBorder.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(254, 247, 235));
            infoTitle.Text = "⏳ Есть данные на проверке";
            infoTitle.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(243, 156, 18));
            infoText.Text = $"Новые паспортные данные отправлены на проверку {lastRequest.SubmittedAt:dd.MM.yyyy HH:mm}. Старые данные продолжают действовать до подтверждения новых.";
            
            // Кнопка для редактирования (открывает пустую форму)
            actionButton.Content = "✏️ Редактировать";
            actionButton.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(52, 152, 219));
            actionButton.IsVisible = true;
        }
        else
        {
            // Только подтвержденные данные, нет новых запросов
            infoBorder.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(232, 246, 243));
            infoTitle.Text = "✅ Паспортные данные подтверждены";
            infoTitle.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(39, 174, 96));
            infoText.Text = "Ваши паспортные данные прошли проверку.";

            // Кнопка для редактирования (открывает пустую форму)
            actionButton.Content = "✏️ Редактировать паспортные данные";
            actionButton.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(52, 152, 219));
            actionButton.IsVisible = true;
        }
    }
    // Случай 2: Есть запрос на проверке (pending), но нет подтвержденных данных
    else if (hasPendingRequest)
    {
        // Скрываем панель с данными
        passportDataPanel.IsVisible = false;

        // Информационное уведомление
        infoBorder.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(254, 247, 235));
        infoTitle.Text = "⏳ Документы на проверке";
        infoTitle.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(243, 156, 18));
        infoText.Text = $"Ваши документы отправлены на проверку {lastRequest.SubmittedAt:dd.MM.yyyy HH:mm}. Ожидайте решения администратора.";

        // Без кнопки
        actionButton.IsVisible = false;
    }
    // Случай 3: Запрос был отклонен
    else if (lastRequest != null && lastRequest.StatusId == PassportVerificationStatus.REJECTED_ID)
    {
        // Скрываем панель с данными
        passportDataPanel.IsVisible = false;

        // Информационное уведомление
        infoBorder.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(253, 237, 236));
        infoTitle.Text = "❌ Документы отклонены";
        infoTitle.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(231, 76, 60));
        infoText.Text = $"Причина: {lastRequest.RejectionReason}\n\nПожалуйста, исправьте данные и отправьте заявку снова.";

        // Кнопка для повторной отправки
        actionButton.Content = "✏️ Отправить заново";
        actionButton.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(231, 76, 60));
        actionButton.IsVisible = true;
    }
    // Случай 4: Нет данных и нет запросов
    else
    {
        // Скрываем панель с данными
        passportDataPanel.IsVisible = false;

        // Информационное уведомление
        infoBorder.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(255, 249, 230));
        infoTitle.Text = "ℹ️ Паспортные данные не заполнены";
        infoTitle.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(230, 126, 34));
        infoText.Text = "Для оформления абонемента и записи на тренировки необходимо добавить паспортные данные.";

        // Кнопка для добавления
        actionButton.Content = "➕ Добавить паспортные данные";
        actionButton.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(62, 95, 138));
        actionButton.IsVisible = true;
    }
}

private async void ActionButton_Click(object sender, RoutedEventArgs e)
{
    var button = sender as Button;
    if (button == null) return;

    // Отписываемся временно, чтобы избежать двойного вызова
    button.Click -= ActionButton_Click;
    
    await ShowPassportDataDialog();
    
    // Подписываемся обратно
    button.Click += ActionButton_Click;
}

        private void LoadClientSubscription()
{
    if (_currentClient != null)
    {
        // Проверяем, есть ли подтвержденные паспортные данные
        bool hasVerifiedPassport = !string.IsNullOrEmpty(_currentClient.PassportSeries) && 
                                   !string.IsNullOrEmpty(_currentClient.PassportNumber);
        
        if (!hasVerifiedPassport)
        {
            ShowNoSubscription("Для покупки абонемента необходимо подтверждение паспортных данных");
            return;
        }

        try
        {
            var activeSubscription = _context.ClientSubscriptions
                .Include(cs => cs.Tariff)
                .Include(cs => cs.SelectedTrainingType)
                .Include(cs => cs.SelectedTrainer)
                .Include(cs => cs.IndividualTrainingType)
                .Include(cs => cs.IndividualTrainer)
                .Where(cs => cs.ClientId == _currentClient.ClientId && cs.IsActive && cs.EndDate >= DateTime.Today)
                .OrderByDescending(cs => cs.EndDate)
                .FirstOrDefault();

            if (activeSubscription != null)
            {
                ActiveSubscriptionText.Text = FormatSubscriptionInfo(activeSubscription);
                ActiveSubscriptionBorder.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(39, 174, 96));
                ActiveSubscriptionBorder.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(232, 246, 243));
                ActiveSubscriptionBorder.IsVisible = true;
            }
            else
            {
                ShowNoSubscription();
            }
        }
        catch (Exception)
        {
            ShowNoSubscription();
        }
    }
    else
    {
        ShowNoSubscription();
    }
}

        private void ShowNoSubscription(string message = "Абонемент не активен")
        {
            ActiveSubscriptionText.Text = message;
            ActiveSubscriptionBorder.BorderBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(52, 152, 219));
            ActiveSubscriptionBorder.Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(235, 245, 251));
            ActiveSubscriptionBorder.IsVisible = true;
        }


        private async Task ShowPassportDataDialog()
{
    try
    {
        var dialog = new PassportDataWindow(_currentClient);
        var result = await dialog.ShowDialog<bool>((Window)this.VisualRoot);

        if (result)
        {
            _context = new AppDbContext();
            _currentClient = _context.Clients.Find(_currentClient.ClientId);

            LoadUserData();
            UpdatePassportDataDisplay();

            await ShowMessage("Успех", "Паспортные данные успешно отправлены на проверку!");
        }
    }
    catch (Exception ex)
    {
        await ShowError($"Ошибка при сохранении паспортных данных: {ex.Message}");
    }
}


        private void LoadAvatar()
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentClient.AvatarPath))
                {
                    string avatarPath = Path.Combine(_avatarsFolder, _currentClient.AvatarPath);
                    if (File.Exists(avatarPath))
                    {
                        var bitmap = new Bitmap(avatarPath);
                        AvatarImage.Source = bitmap;
                        return;
                    }
                }

                LoadDefaultAvatar();
            }
            catch (Exception)
            {
                LoadDefaultAvatar();
            }
        }

        private void LoadDefaultAvatar()
        {
            try
            {
                string defaultAvatarPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "default_avatar.png");
                if (File.Exists(defaultAvatarPath))
                {
                    var bitmap = new Bitmap(defaultAvatarPath);
                    AvatarImage.Source = bitmap;
                }
                else
                {
                    AvatarImage.Source = null;
                }
            }
            catch (Exception)
            {
                AvatarImage.Source = null;
            }
        }

        private async void ChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog();
                dialog.Title = "Выберите изображение для аватара";
                dialog.Filters.Add(new FileDialogFilter
                {
                    Name = "Изображения",
                    Extensions = { "jpg", "jpeg", "png", "bmp" }
                });
                dialog.AllowMultiple = false;

                var result = await dialog.ShowAsync((Window)this.VisualRoot);

                if (result != null && result.Length > 0)
                {
                    string selectedFile = result[0];

                    string extension = Path.GetExtension(selectedFile).ToLower();
                    string newFileName = $"{_currentClient.ClientId}{extension}";
                    string newFilePath = Path.Combine(_avatarsFolder, newFileName);

                    File.Copy(selectedFile, newFilePath, true);

                    _currentClient.AvatarPath = newFileName;
                    _context.Clients.Update(_currentClient);
                    _context.SaveChanges();

                    LoadAvatar();

                    await ShowMessage("Успех", "Аватар успешно обновлен!");
                }
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка при изменении аватара: {ex.Message}");
            }
        }

        private async void RemoveAvatar_Click(object sender, RoutedEventArgs e)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                "Подтверждение",
                "Вы уверены, что хотите удалить аватар?",
                ButtonEnum.YesNo);

            var result = await box.ShowWindowDialogAsync((Window)this.VisualRoot);

            if (result == ButtonResult.Yes)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_currentClient.AvatarPath))
                    {
                        string avatarPath = Path.Combine(_avatarsFolder, _currentClient.AvatarPath);
                        if (File.Exists(avatarPath))
                        {
                            File.Delete(avatarPath);
                        }
                    }

                    _currentClient.AvatarPath = "";
                    _context.Clients.Update(_currentClient);
                    _context.SaveChanges();

                    LoadAvatar();

                    await ShowMessage("Успех", "Аватар успешно удален!");
                }
                catch (Exception ex)
                {
                    await ShowError($"Ошибка при удалении аватара: {ex.Message}");
                }
            }
        }

        private async void EditData_Click(object sender, RoutedEventArgs e)
        {
            StartEditingBasicInfo();
        }

        private void StartEditingBasicInfo()
        {
            _isEditingBasicInfo = true;
            
            // Показываем блок редактирования
            EditBasicInfoBorder.IsVisible = true;
            
            // Заполняем поля текущими данными
            EditEmailTextBox.Text = _currentClient.Email;
            EditPhoneTextBox.Text = _currentClient.Phone;
        }

        private async void SaveBasicInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация email
                string newEmail = EditEmailTextBox.Text.Trim();
                
                if (string.IsNullOrEmpty(newEmail))
                {
                    await ShowError("Email не может быть пустым");
                    return;
                }
                
                if (!IsValidEmail(newEmail))
                {
                    await ShowError("Введите корректный email адрес в формате example@domain.com");
                    return;
                }
                
                // Проверяем, не используется ли email другим клиентом
                if (newEmail != _currentClient.Email)
                {
                    var existingClient = _context.Clients.FirstOrDefault(c => c.Email == newEmail && c.ClientId != _currentClient.ClientId);
                    if (existingClient != null)
                    {
                        await ShowError("Этот email уже используется другим пользователем");
                        return;
                    }
                    
                    var existingUser = _context.Users.FirstOrDefault(u => u.Email == newEmail);
                    if (existingUser != null && existingUser.UsersId != _currentUser.UsersId)
                    {
                        await ShowError("Этот email уже используется другим пользователем");
                        return;
                    }
                }
                
                // Валидация телефона
                string newPhone = EditPhoneTextBox.Text.Trim();
                
                if (string.IsNullOrEmpty(newPhone))
                {
                    await ShowError("Телефон не может быть пустым");
                    return;
                }
                
                // Убираем форматирование для проверки
                string phoneDigits = new string(newPhone.Where(c => char.IsDigit(c)).ToArray());
                
                if (phoneDigits.Length != 11 && phoneDigits.Length != 10)
                {
                    await ShowError("Введите корректный номер телефона (10 цифр без кода страны или 11 цифр с кодом)");
                    return;
                }
                
                // Используем транзакцию для безопасного обновления
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Обновляем данные клиента
                        _currentClient.Email = newEmail;
                        _currentClient.Phone = newPhone;
                        _context.Clients.Update(_currentClient);
                        
                        // Если email изменился, обновляем и в таблице users
                        if (newEmail != _currentUser.Email)
                        {
                            // Получаем текущий пароль пользователя
                            var currentPassword = _context.Users
                                .AsNoTracking()
                                .Where(u => u.UsersId == _currentUser.UsersId)
                                .Select(u => u.Password)
                                .FirstOrDefault();
                            
                            // Обновляем email БЕЗ использования Entity Framework, чтобы избежать триггера
                            await _context.Database.ExecuteSqlRawAsync(
                                "UPDATE users SET email = {0} WHERE users_id = {1}",
                                newEmail, _currentUser.UsersId);
                            
                            // Обновляем локальную переменную
                            _currentUser.Email = newEmail;
                        }
                        
                        // Сохраняем все изменения
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        
                        // Завершаем редактирование
                        _isEditingBasicInfo = false;
                        EditBasicInfoBorder.IsVisible = false;
                        
                        // Обновляем отображение
                        EmailText.Text = _currentClient.Email;
                        PhoneText.Text = _currentClient.Phone;
                        
                        await ShowMessage("Успех", "Основные данные успешно обновлены!");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void CancelBasicInfo_Click(object sender, RoutedEventArgs e)
        {
            _isEditingBasicInfo = false;
            EditBasicInfoBorder.IsVisible = false;
        }

        private async void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentUser == null)
                {
                    await ShowError("Ошибка: данные пользователя не загружены");
                    return;
                }
                
                if (_currentClient == null)
                {
                    await ShowError("Ошибка: данные клиента не загружены");
                    return;
                }
                
                var changePasswordWindow = new ChangePasswordWindow(_currentUser, _currentClient);
                var result = await changePasswordWindow.ShowDialog<bool>((Window)this.VisualRoot);
                
                if (result)
                {
                    await ShowMessage("Успех", "Пароль успешно изменен!");
                }
            }
            catch (Exception ex)
            {
                await ShowError($"Ошибка при смене пароля: {ex.Message}");
            }
        }

        private async void PurchaseHistory_Click(object sender, RoutedEventArgs e)
        {
            await ShowMessage("Информация", "Функция 'История покупок' будет реализована в ближайшем обновлении. Здесь вы сможете просмотреть все свои покупки и скачать чеки.");
        }

        private async Task ShowMessage(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok);
            await box.ShowWindowDialogAsync((Window)this.VisualRoot);
        }

        private async Task ShowError(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", message, ButtonEnum.Ok);
            await box.ShowWindowDialogAsync((Window)this.VisualRoot);
        }

        public void RefreshView()
        {
            _context = new AppDbContext();
            if (_currentClient != null)
            {
                _currentClient = _context.Clients.Find(_currentClient.ClientId);
            }

            LoadUserData();
            UpdatePassportDataDisplay();
        }

        private string FormatSubscriptionInfo(ClientSubscription subscription)
        {
            var tariff = subscription.Tariff;
            var info = new List<string>
            {
                $"Тариф: {tariff.Name}",
                $"Дата приобретения: {subscription.PurchaseDate:dd.MM.yyyy}",
                $"Действует до: {subscription.EndDate:dd.MM.yyyy}",
                $"Осталось дней: {(subscription.EndDate - DateTime.Today).Days}"
            };

            if (subscription.VisitsType == "premium" &&
                subscription.GroupTotalVisits.HasValue && subscription.GroupRemainingVisits.HasValue &&
                subscription.IndividualTotalVisits.HasValue && subscription.IndividualRemainingVisits.HasValue)
            {
                info.Add($"Групповые тренировки: {subscription.GroupRemainingVisits.Value}/{subscription.GroupTotalVisits.Value} посещений осталось");
                info.Add($"Индивидуальные тренировки: {subscription.IndividualRemainingVisits.Value}/{subscription.IndividualTotalVisits.Value} посещений осталось");
            }
            else if (subscription.VisitsType == "group" && subscription.GroupTotalVisits.HasValue && subscription.GroupRemainingVisits.HasValue)
            {
                info.Add($"Групповые тренировки: {subscription.GroupRemainingVisits.Value}/{subscription.GroupTotalVisits.Value} посещений осталось");
            }
            else if (subscription.VisitsType == "individual" && subscription.IndividualTotalVisits.HasValue && subscription.IndividualRemainingVisits.HasValue)
            {
                info.Add($"Индивидуальные тренировки: {subscription.IndividualRemainingVisits.Value}/{subscription.IndividualTotalVisits.Value} посещений осталось");
            }
            else if (subscription.VisitsType == "combo" && subscription.GroupTotalVisits.HasValue && subscription.GroupRemainingVisits.HasValue)
            {
                info.Add($"Посещений осталось: {subscription.GroupRemainingVisits.Value}/{subscription.GroupTotalVisits.Value}");
            }

            if (subscription.SelectedTrainingType != null)
            {
                info.Add($"Групповые: {subscription.SelectedTrainingType.Name}");
            }
            if (subscription.SelectedTrainer != null)
            {
                info.Add($"Тренер (групповые): {subscription.SelectedTrainer.FullName}");
            }

            if (subscription.IndividualTrainingType != null)
            {
                info.Add($"Индивидуальные: {subscription.IndividualTrainingType.Name}");
            }
            if (subscription.IndividualTrainer != null)
            {
                info.Add($"Тренер (индивидуальные): {subscription.IndividualTrainer.FullName}");
            }

            return string.Join("\n", info);
        }
    }
}