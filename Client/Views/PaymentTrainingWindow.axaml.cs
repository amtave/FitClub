using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using FitClub.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Linq;
using FitClub.Services;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Avalonia.Media;

namespace FitClub.Client.Views
{
    public partial class PaymentTrainingWindow : Window
    {
        private readonly GroupTraining _training;
        private readonly TrainingSchedule _schedule;
        private readonly Models.Client _client;
        private readonly TrainingService _trainingService;
        private List<PaymentCard> _savedCards;
        private PaymentCard _selectedCard = null;
        private bool _useNewCard = false;
        private BonusCard _bonusCard = null;
        private decimal _finalPrice;
        private int _usedBonusPoints = 0;

        public bool PaymentSuccess { get; private set; }

        public PaymentTrainingWindow(GroupTraining training, TrainingSchedule schedule, Models.Client client, TrainingService trainingService)
        {
            InitializeComponent();
            _training = training;
            _schedule = schedule;
            _client = client;
            _trainingService = trainingService;
            _finalPrice = _training.Price;

            TrainingTypeText.Text = _training.Name;
            TrainerNameText.Text = $"Тренер: {_schedule.Trainer.FullName}";
            var endTime = _schedule.TrainingTime.Add(TimeSpan.FromMinutes(_training.DurationMinutes));
            TrainingDateTimeText.Text = $"{_schedule.TrainingDate:dd.MM.yyyy} | {_schedule.TrainingTime:hh\\:mm} - {endTime:hh\\:mm}";
            TrainingPriceText.Text = $"{_finalPrice:N0} ₽";

            LoadSavedCards();
            LoadBonusCard();
            UpdatePriceDisplay();
        }

        private void LoadSavedCards()
        {
            try
            {
                using var context = new AppDbContext();
                _savedCards = context.PaymentCards
                    .Where(pc => pc.ClientId == _client.ClientId && pc.IsVerified)
                    .OrderByDescending(pc => pc.IsDefault)
                    .ThenByDescending(pc => pc.LastUsed)
                    .ToList();

                SavedCardsComboBox.Items.Clear();
                SavedCardsComboBox.Items.Add(new ComboBoxItem { Content = "Выберите карту", Tag = null });

                if (_savedCards.Any())
                {
                    foreach (var card in _savedCards)
                    {
                        SavedCardsComboBox.Items.Add(new ComboBoxItem
                        {
                            Content = $"{card.CardIcon} {card.CardType} {card.MaskedCardNumber}",
                            Tag = card
                        });
                    }
                    SavedCardsComboBox.SelectedIndex = 0;
                }
                else
                {
                    SavedCardsComboBox.IsVisible = false;
                }
            }
            catch { }
            ValidatePaymentForm();
        }

        private void LoadBonusCard()
        {
            try
            {
                using var context = new AppDbContext();
                _bonusCard = context.BonusCards.FirstOrDefault(bc => bc.ClientId == _client.ClientId && bc.IsActive);

                if (_bonusCard != null)
                {
                    HasBonusCardPanel.IsVisible = true;
                    NoBonusCardPanel.IsVisible = false;
                    BonusCardInfoText.Text = $"Карта {_bonusCard.FormattedCardNumber}";
                    BonusCardBalanceText.Text = $"{_bonusCard.PointsBalance} Б";
                    
                    int maxBonusPoints = (int)Math.Min(_bonusCard.PointsBalance, _finalPrice);
                    BonusPointsSlider.Maximum = maxBonusPoints;
                    BonusPointsSlider.Value = 0;
                    BonusPointsTextBox.Text = "0";
                }
                else
                {
                    HasBonusCardPanel.IsVisible = false;
                    NoBonusCardPanel.IsVisible = true;
                }
            }
            catch { }
        }

        private void UpdatePriceDisplay()
        {
            if (_usedBonusPoints > 0)
                FinalPriceText.Text = $"{_finalPrice:N0} ₽";
            else
                FinalPriceText.Text = $"{_finalPrice:N0} ₽";
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
            _finalPrice = _training.Price;
            UpdatePriceDisplay();
            ValidatePaymentForm();
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
            _finalPrice = Math.Max(0, _training.Price - _usedBonusPoints);
            BonusPointsUsedText.Text = $"- {_usedBonusPoints} ₽";
            FinalPriceText.Text = $"{_finalPrice:N0} ₽";
            ValidatePaymentForm();
        }

        private void IncreaseBonusPoints_Click(object sender, RoutedEventArgs e) => BonusPointsSlider.Value = Math.Min(_usedBonusPoints + 10, (int)BonusPointsSlider.Maximum);
        private void DecreaseBonusPoints_Click(object sender, RoutedEventArgs e) => BonusPointsSlider.Value = Math.Max(_usedBonusPoints - 10, 0);

        private void NewCardButton_Click(object sender, RoutedEventArgs e)
        {
            _useNewCard = true;
            SavedCardsSection.IsVisible = false;
            NewCardSection.IsVisible = true;
            _selectedCard = null;
            CardNumberTextBox.Text = "";
            ExpiryDateTextBox.Text = "";
            CvvTextBox.Text = "";
            HideExpiryDateError();
            ValidateForm();
        }

        private void BackToSavedCardsButton_Click(object sender, RoutedEventArgs e)
        {
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
                    SelectedCardNumber.Text = _selectedCard.MaskedCardNumber;
                    SelectedCardExpiry.Text = _selectedCard.FormattedExpiry;
                    SelectedCardInfo.IsVisible = true;
                }
                else SelectedCardInfo.IsVisible = false;
            }
            ValidatePaymentForm();
        }

        private void ValidatePaymentForm() => PayButton.IsEnabled = _selectedCard != null && !_useNewCard && _finalPrice >= 0;

        private void ValidateForm()
        {
            if (_useNewCard)
            {
                var cardNumber = CardNumberTextBox.Text?.Replace(" ", "") ?? "";
                var expiryDate = ExpiryDateTextBox.Text?.Replace("/", "") ?? "";
                var cvv = CvvTextBox.Text ?? "";
                bool isExpiryDateValid = ValidateExpiryDate(ExpiryDateTextBox.Text);
                PayButton.IsEnabled = cardNumber.Length == 16 && expiryDate.Length == 4 && cvv.Length == 3 && isExpiryDateValid && _finalPrice >= 0;
            }
            else ValidatePaymentForm();
        }

        private void CardNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var text = textBox.Text?.Replace(" ", "") ?? "";
            if (text.Length > 16) text = text.Substring(0, 16);
            if (text.Length > 0)
            {
                var formatted = "";
                for (int i = 0; i < text.Length; i++)
                {
                    if (i > 0 && i % 4 == 0) formatted += " ";
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
            if (text.Length > 4) text = text.Substring(0, 4);
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
            if (string.IsNullOrEmpty(expiryDate) || expiryDate.Length < 5) { HideExpiryDateError(); return false; }
            try
            {
                var parts = expiryDate.Split('/');
                if (parts.Length == 2 && int.TryParse(parts[0], out int month) && int.TryParse(parts[1], out int year))
                {
                    if (month < 1 || month > 12) { ShowExpiryDateError("Неверный месяц"); return false; }
                    if (year < 100) year += 2000;
                    if (year < 2025) { ShowExpiryDateError("Неверный год"); return false; }
                    if (new DateTime(year, month, DateTime.DaysInMonth(year, month)) < DateTime.Now) { ShowExpiryDateError("Карта просрочена"); return false; }
                    HideExpiryDateError(); return true;
                }
                return false;
            }
            catch { return false; }
        }

        private void ShowExpiryDateError(string message) { ExpiryDateTextBox.BorderBrush = Brush.Parse("#E74C3C"); ExpiryDateErrorText.Text = message; ExpiryDateErrorText.IsVisible = true; }
        private void HideExpiryDateError() { ExpiryDateTextBox.BorderBrush = Brush.Parse("#BDC3C7"); ExpiryDateErrorText.IsVisible = false; }
        private void CvvTextBox_TextChanged(object sender, TextChangedEventArgs e) { var textBox = (TextBox)sender; var text = textBox.Text ?? ""; if (text.Length > 3) textBox.Text = text.Substring(0, 3); ValidateForm(); }

        private bool IsExpiryDateValid(string expiryDate)
        {
            if (string.IsNullOrEmpty(expiryDate) || expiryDate.Length != 4) return false;
            try
            {
                var month = int.Parse(expiryDate.Substring(0, 2));
                var year = int.Parse(expiryDate.Substring(2, 2));
                if (year < 100) year += 2000;
                if (month < 1 || month > 12 || year < 2025) return false;
                return new DateTime(year, month, DateTime.DaysInMonth(year, month)) >= DateTime.Now;
            }
            catch { return false; }
        }

        private bool IsSavedCardValid(PaymentCard card)
        {
            try { return new DateTime(card.ExpiryYear, card.ExpiryMonth, DateTime.DaysInMonth(card.ExpiryYear, card.ExpiryMonth)) >= DateTime.Now; }
            catch { return false; }
        }

        private async void PayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_useNewCard && _selectedCard == null) return;
                if (_useNewCard && !IsExpiryDateValid(ExpiryDateTextBox.Text?.Replace("/", ""))) return;
                if (!_useNewCard && !IsSavedCardValid(_selectedCard)) return;

                if (_bonusCard != null && _usedBonusPoints > 0)
                {
                    if (_usedBonusPoints > _bonusCard.PointsBalance) return;
                }

                PayButton.IsVisible = false;
                LoadingBorder.IsVisible = true;
                await Task.Delay(1500);

                using (var context = new AppDbContext())
                {
                    var sched = context.TrainingSchedules.FirstOrDefault(s => s.ScheduleId == _schedule.ScheduleId);
                    if (sched == null || sched.CurrentParticipants >= sched.MaxParticipants)
                    {
                        throw new Exception("К сожалению, свободные места закончились.");
                    }

                    bool already = context.TrainingBookings.Any(b => b.ClientId == _client.ClientId && b.ScheduleId == _schedule.ScheduleId);
                    if (already)
                    {
                        throw new Exception("Вы уже записаны на это занятие.");
                    }

                    var bonusCardInDb = context.BonusCards.FirstOrDefault(bc => bc.ClientId == _client.ClientId && bc.IsActive);
                    if (bonusCardInDb != null)
                    {
                        if (_usedBonusPoints > 0) bonusCardInDb.PointsBalance -= _usedBonusPoints;
                        int earnedPoints = (int)Math.Floor(_finalPrice * 0.02m);
                        if (earnedPoints > 0) bonusCardInDb.PointsBalance += earnedPoints;
                    }

                    var newBooking = new TrainingBooking
                    {
                        ClientId = _client.ClientId,
                        ScheduleId = _schedule.ScheduleId,
                        TrainingId = _training.TrainingId,
                        BookingDate = DateTime.Now,
                        Status = "confirmed"
                    };
                    
                    context.TrainingBookings.Add(newBooking);
                    sched.CurrentParticipants++;
                    await context.SaveChangesAsync();

                    PaymentSuccess = true;
                    LoadingBorder.IsVisible = false;
                    SuccessBorder.IsVisible = true;
                    
                    await SaveReceiptWithDialog(newBooking, bonusCardInDb, bonusCardInDb?.PointsBalance ?? 0);
                }
            }
            catch (Exception ex)
            {
                LoadingBorder.IsVisible = false;
                PayButton.IsVisible = true;
                var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", ex.Message, ButtonEnum.Ok);
                await box.ShowWindowDialogAsync(this);
            }
        }

        private async Task SaveReceiptWithDialog(TrainingBooking booking, BonusCard bonusCard, int finalBalance)
        {
            try
            {
                int earnedPoints = (int)Math.Floor(_finalPrice * 0.02m);
                string bonusCardInfo = bonusCard != null ? $"Списано: {_usedBonusPoints} Б\nНачислено: {earnedPoints} Б\nОстаток: {finalBalance} Б\n" : "";
                string statusText = booking.Status == "confirmed" ? "Оплачено" : "Отменена";
                string receiptText = $"ЧЕК ОБ ОПЛАТЕ\n\nДата: {DateTime.Now:dd.MM.yyyy HH:mm}\nУслуга: Групповая тренировка\nНаправление: {_training.Name}\nКлиент: {_client.FullName}\nТренер: {_schedule.Trainer.FullName}\nВремя: {_schedule.TrainingDate:dd.MM.yyyy} ({_schedule.TrainingTime:hh\\:mm})\nСтатус: {statusText}\n\nК оплате: {_finalPrice:N0} ₽\n{bonusCardInfo}";

                var receiptBox = MessageBoxManager.GetMessageBoxStandard("Электронный чек", receiptText, ButtonEnum.Ok);
                await receiptBox.ShowWindowDialogAsync(this);
            }
            catch { }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close(false);
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close(PaymentSuccess);
    }
}