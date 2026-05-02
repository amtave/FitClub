using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Linq;
using FitClub.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FitClub.Views
{
    public partial class TariffsView : UserControl
    {
        private readonly AppDbContext _context;
        private Models.Client _currentClient;
        private bool _hasActiveSubscription = false;

        public TariffsView()
        {
            InitializeComponent();
            _context = new AppDbContext();
            LoadCurrentClient();
            LoadTariffsByCategories();
            LoadClientSubscription();
            UpdatePassportWarning();
        }

        private void LoadCurrentClient()
        {
            if (UserSession.IsLoggedIn)
            {
                _currentClient = _context.GetClientByEmail(UserSession.CurrentUserEmail);
            }

            if (_currentClient == null)
            {
                ActiveSubscriptionBorder.IsVisible = false;
            }
        }

        private void LoadTariffsByCategories()
        {
            try
            {
                var activeTariffs = _context.Tariffs
                    .Include(t => t.Category)
                    .Where(t => t.IsActive)
                    .ToList() 
                    .Select(t => new Tariff
                    {
                        TariffId = t.TariffId,
                        Name = t.Name,
                        Description = t.Description,
                        Price = t.Price,
                        DurationDays = t.DurationDays,
                        CategoryId = t.CategoryId,
                        IsActive = t.IsActive,
                        Category = t.Category,
                        ImagePath = t.ImagePath ?? string.Empty 
                    })
                    .ToList();

                GymTariffsItemsControl.ItemsSource = activeTariffs.Where(t => t.CategoryId == 1).ToList();
                GroupTrainingTariffsItemsControl.ItemsSource = activeTariffs.Where(t => t.CategoryId == 2).ToList();
                IndividualTrainingTariffsItemsControl.ItemsSource = activeTariffs.Where(t => t.CategoryId == 3).ToList();
                CombinedTariffsItemsControl.ItemsSource = activeTariffs.Where(t => t.CategoryId == 4).ToList();
            }
            catch { }
        }

        private void LoadClientSubscription()
        {
            if (_currentClient != null)
            {
                try
                {
                    var activeSubscription = _context.ClientSubscriptions
                        .Include(cs => cs.Tariff)
                        .Include(cs => cs.SelectedTrainer)
                        .Include(cs => cs.IndividualTrainingType)
                        .Include(cs => cs.IndividualTrainer)
                        .Where(cs => cs.ClientId == _currentClient.ClientId && cs.IsActive && cs.EndDate >= DateTime.Today)
                        .OrderByDescending(cs => cs.EndDate)
                        .FirstOrDefault();

                    if (activeSubscription != null)
                    {
                        ActiveSubscriptionText.Text = FormatSubscriptionInfo(activeSubscription);
                        _hasActiveSubscription = true;
                        ActiveSubscriptionBorder.IsVisible = true;
                    }
                    else
                    {
                        _hasActiveSubscription = false;
                        ActiveSubscriptionBorder.IsVisible = false;
                    }
                }
                catch 
                {
                    _hasActiveSubscription = false;
                    ActiveSubscriptionBorder.IsVisible = false;
                }
            }
        }

        private string FormatSubscriptionInfo(ClientSubscription subscription)
        {
            var tariff = subscription.Tariff;
            var info = new List<string>
            {
                $"Тариф: {tariff.Name}",
                $"Действует до: {subscription.EndDate:dd.MM.yyyy}",
                $"Осталось дней: {(subscription.EndDate - DateTime.Today).Days}"
            };

            if (subscription.SelectedTrainingTypeId.HasValue)
            {
                var groupTraining = _context.GroupTrainings.Find(subscription.SelectedTrainingTypeId.Value);
                if (groupTraining != null)
                {
                    info.Add($"Направление (Групп.): {groupTraining.Name}");
                }
            }
            if (subscription.SelectedTrainer != null)
            {
                info.Add($"Тренер (Групп.): {subscription.SelectedTrainer.FullName}");
            }
            if (subscription.IndividualTrainingType != null)
            {
                info.Add($"Направление (Инд.): {subscription.IndividualTrainingType.Name}");
            }
            if (subscription.IndividualTrainer != null)
            {
                info.Add($"Тренер (Инд.): {subscription.IndividualTrainer.FullName}");
            }

            if (subscription.VisitsType == "premium" &&
                subscription.GroupTotalVisits.HasValue && subscription.GroupRemainingVisits.HasValue &&
                subscription.IndividualTotalVisits.HasValue && subscription.IndividualRemainingVisits.HasValue)
            {
                info.Add($"Групповые: {subscription.GroupRemainingVisits.Value}/{subscription.GroupTotalVisits.Value} | Индивидуальные: {subscription.IndividualRemainingVisits.Value}/{subscription.IndividualTotalVisits.Value}");
            }
            else if (subscription.VisitsType == "group" && subscription.GroupTotalVisits.HasValue && subscription.GroupRemainingVisits.HasValue)
            {
                info.Add($"Групповые тренировки: {subscription.GroupRemainingVisits.Value}/{subscription.GroupTotalVisits.Value} посещений");
            }
            else if (subscription.VisitsType == "individual" && subscription.IndividualTotalVisits.HasValue && subscription.IndividualRemainingVisits.HasValue)
            {
                info.Add($"Индивидуальные тренировки: {subscription.IndividualRemainingVisits.Value}/{subscription.IndividualTotalVisits.Value} посещений");
            }

            return string.Join("\n", info);
        }

        private void UpdatePassportWarning()
        {
            if (_currentClient != null) PassportWarningBorder.IsVisible = !_currentClient.HasPassportData;
            else PassportWarningBorder.IsVisible = false;
        }

        private async void BuyTariffButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentClient == null)
            {
                await ShowMessage("Ошибка", "Для покупки тарифа необходимо авторизоваться в системе!");
                return;
            }

            if (!_currentClient.HasPassportData)
            {
                await ShowMessage("Паспортные данные не заполнены", "Для оформления абонемента необходимо заполнить паспортные данные в разделе 'Мой аккаунт'.");
                return;
            }

            var button = (Button)sender;
            var tariff = (Tariff)button.Tag;

            bool isSingleVisit = tariff.Name.Contains("Разовое посещение");

            if (!isSingleVisit)
            {
                var activeSubscription = _context.ClientSubscriptions
                    .Where(cs => cs.ClientId == _currentClient.ClientId && cs.IsActive && cs.EndDate >= DateTime.Today)
                    .FirstOrDefault();

                if (activeSubscription != null)
                {
                    await ShowMessage("Недоступно", "У вас уже есть активный абонемент. Новый можно приобрести после окончания текущего.");
                    return;
                }
            }

            if (isSingleVisit)
            {
                var dateWindow = new SingleVisitDateWindow(tariff, _currentClient, _context);
                await dateWindow.ShowDialog((Window)this.VisualRoot);
                if (dateWindow.PaymentConfirmed) await ShowMessage("Успех", "Разовое посещение успешно приобретено!");
                return;
            }

            bool isGroupTraining = tariff.Name.Contains("групповые");
            bool isIndividualTraining = tariff.Name.Contains("индивидуальные");
            bool isCombo = tariff.Name.Contains("Комбо");
            bool isPremium = tariff.Name.Contains("Все включено");

            int? finalGroupTrainingId = null;
            int? finalGroupTrainerId = null;
            int? finalIndTypeId = null;
            int? finalIndTrainerId = null;
            bool success = false;

            if (isPremium)
            {
                var premiumWindow = new PremiumTrainingSelectionWindow(tariff, _currentClient);
                await premiumWindow.ShowDialog((Window)this.VisualRoot);
                success = premiumWindow.PaymentConfirmed;
                finalGroupTrainingId = premiumWindow.SelectedGroupTraining?.TrainingId;
                finalGroupTrainerId = premiumWindow.SelectedGroupTrainer?.TrainerId;
                finalIndTypeId = premiumWindow.SelectedIndividualTraining?.TypeId;
                finalIndTrainerId = premiumWindow.SelectedIndividualTrainer?.TrainerId;
            }
            else if (isGroupTraining || (isCombo && tariff.Name.Contains("групповые")))
            {
                var groupWindow = new GroupTrainingSelectionWindow(tariff, _currentClient);
                await groupWindow.ShowDialog((Window)this.VisualRoot);
                success = groupWindow.PaymentConfirmed;
                finalGroupTrainingId = groupWindow.SelectedTrainingType?.TrainingId;
                finalGroupTrainerId = groupWindow.SelectedTrainer?.TrainerId;
            }
            else if (isIndividualTraining || (isCombo && tariff.Name.Contains("индивидуальные")))
            {
                var individualWindow = new IndividualTrainingSelectionWindow(tariff, _currentClient);
                await individualWindow.ShowDialog((Window)this.VisualRoot);
                success = individualWindow.PaymentConfirmed;
                finalIndTypeId = individualWindow.SelectedTrainingType?.TypeId;
                finalIndTrainerId = individualWindow.SelectedTrainer?.TrainerId;
            }
            else
            {
                var paymentWindow = new PaymentWindow(tariff, _currentClient);
                await paymentWindow.ShowDialog((Window)this.VisualRoot);
                success = paymentWindow.PaymentSuccess;
            }

            if (success)
            {
                using (var freshContext = new AppDbContext())
                {
                    var latestSubscription = freshContext.ClientSubscriptions
                        .Where(cs => cs.ClientId == _currentClient.ClientId && cs.IsActive && cs.EndDate >= DateTime.Today)
                        .OrderByDescending(cs => cs.SubscriptionId)
                        .FirstOrDefault();

                    if (latestSubscription != null)
                    {
                        latestSubscription.VisitsType = isPremium ? "premium" : (isGroupTraining ? "group" : (isIndividualTraining ? "individual" : "gym"));
                        latestSubscription.GroupTotalVisits = (isPremium || isGroupTraining || isCombo) ? 12 : (int?)null;
                        latestSubscription.GroupRemainingVisits = (isPremium || isGroupTraining || isCombo) ? 12 : (int?)null;
                        latestSubscription.IndividualTotalVisits = (isPremium || isIndividualTraining) ? 12 : (int?)null;
                        latestSubscription.IndividualRemainingVisits = (isPremium || isIndividualTraining) ? 12 : (int?)null;
                        
                        latestSubscription.SelectedTrainingTypeId = finalGroupTrainingId;
                        latestSubscription.SelectedTrainerId = finalGroupTrainerId;
                        latestSubscription.IndividualTrainingTypeId = finalIndTypeId;
                        latestSubscription.IndividualTrainerId = finalIndTrainerId;

                        freshContext.ClientSubscriptions.Update(latestSubscription);
                        freshContext.SaveChanges();
                    }
                }
                
                _context.ChangeTracker.Clear();
                _currentClient = _context.Clients.Find(_currentClient.ClientId);
                LoadClientSubscription();
                await ShowMessage("Успех", "Абонемент успешно приобретен!");
            }
        }

        private async Task ShowMessage(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message, ButtonEnum.Ok);
            await box.ShowWindowDialogAsync((Window)this.VisualRoot);
        }

        public void RefreshView()
        {
            LoadCurrentClient();
            LoadClientSubscription();
            UpdatePassportWarning();
        }
    }
}