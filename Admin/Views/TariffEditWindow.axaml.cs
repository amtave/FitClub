using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using FitClub.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FitClub.Admin.Views
{
    public partial class TariffEditWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly Tariff _currentTariff;
        private readonly bool _isEditMode;
        private string _selectedImagePath = string.Empty;
        private bool _isLoaded = false; // Флаг для отслеживания загрузки

        public TariffEditWindow()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _isEditMode = false;
            _currentTariff = new Tariff 
            { 
                IsActive = true,
                CreatedAt = DateTime.Now,
                ImagePath = string.Empty 
            };

            Title = "Добавление нового тарифа";
            WindowTitleText.Text = "Добавление нового тарифа";

            // Подписываемся на событие загрузки окна
            this.Loaded += TariffEditWindow_Loaded;
        }

        public TariffEditWindow(Tariff tariff)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _isEditMode = true;
            _currentTariff = tariff ?? throw new ArgumentNullException(nameof(tariff));

            Title = "Редактирование тарифа";
            WindowTitleText.Text = "Редактирование тарифа";

            // Подписываемся на событие загрузки окна
            this.Loaded += TariffEditWindow_Loaded;
        }

        private void TariffEditWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
            {
                LoadCategories();
                if (_isEditMode)
                {
                    LoadTariffData();
                }
                else
                {
                    // Устанавливаем значения по умолчанию для нового тарифа
                    TariffPriceBox.Text = "0";
                    TariffDurationBox.Text = "30";
                    IsActiveCheckBox.IsChecked = true;
                }
                _isLoaded = true;
            }
        }

        private void LoadCategories()
        {
            try
            {
                var categories = _context.TariffCategories.ToList();
                CategoryComboBox.ItemsSource = categories;

                // Настраиваем отображение элементов в ComboBox
                CategoryComboBox.ItemTemplate = new FuncDataTemplate<TariffCategory>((value, namescope) =>
                {
                    return new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 10,
                        Children =
                        {
                            new TextBlock { Text = value?.Icon ?? "📦" },
                            new TextBlock { Text = value?.Name ?? "Категория", 
                                           VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center }
                        }
                    };
                });

                // Для нового тарифа автоматически выбираем первую категорию (обычно тренажерный зал)
                if (!_isEditMode && categories.Any())
                {
                    // Пробуем найти категорию "Тренажерный зал"
                    var gymCategory = categories.FirstOrDefault(c => c.Name.Contains("Тренажерный") || c.CategoryId == 1);
                    CategoryComboBox.SelectedItem = gymCategory ?? categories.First();
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка загрузки категорий: {ex.Message}", false);
            }
        }

        private void LoadTariffData()
        {
            try
            {
                TariffNameBox.Text = _currentTariff.Name;
                TariffDescriptionBox.Text = _currentTariff.Description;
                TariffPriceBox.Text = _currentTariff.Price.ToString("F0");
                TariffDurationBox.Text = _currentTariff.DurationDays.ToString();
                IsActiveCheckBox.IsChecked = _currentTariff.IsActive;

                // Выбираем категорию
                if (_currentTariff.CategoryId.HasValue && CategoryComboBox.ItemsSource != null)
                {
                    foreach (var item in CategoryComboBox.Items)
                    {
                        if (item is TariffCategory category && category.CategoryId == _currentTariff.CategoryId)
                        {
                            CategoryComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }

                // Загружаем изображение, если есть
                if (!string.IsNullOrEmpty(_currentTariff.ImagePath))
                {
                    _selectedImagePath = _currentTariff.ImagePath;
                    LoadImagePreview(_currentTariff.ImagePath);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка загрузки данных: {ex.Message}", false);
            }
        }

        private async void LoadImageButton_Click(object sender, RoutedEventArgs e)
{
    try
    {
        var dialog = new OpenFileDialog();
        dialog.Title = "Выберите изображение для тарифа";
        dialog.AllowMultiple = false;
        dialog.Filters.Add(new FileDialogFilter
        {
            Name = "Изображения",
            Extensions = { "png", "jpg", "jpeg", "bmp", "gif" }
        });

        var result = await dialog.ShowAsync(this);

        if (result != null && result.Any())
        {
            string selectedFile = result[0];
            
            // Простой путь - копируем в папку сборки
            string fileName = $"tariff_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(selectedFile)}";
            string destPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Tariffs", fileName);
            
            // Создаем папку
            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
            
            // Копируем файл
            File.Copy(selectedFile, destPath, true);
            
            // Сохраняем относительный путь
            _selectedImagePath = $"Assets/Tariffs/{fileName}";
            ImagePathBox.Text = _selectedImagePath;
            
            // Показываем превью
            LoadImagePreview(_selectedImagePath);
            
            ShowStatus("Изображение загружено", true);
        }
    }
    catch (Exception ex)
    {
        ShowStatus($"Ошибка: {ex.Message}", false);
    }
}

        private void LoadImagePreview(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    TariffImagePreview.Source = null;
                    ImagePlaceholder.IsVisible = true;
                    return;
                }

                string fullPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", imagePath);
                fullPath = Path.GetFullPath(fullPath);

                if (File.Exists(fullPath))
                {
                    TariffImagePreview.Source = new Bitmap(fullPath);
                    ImagePlaceholder.IsVisible = false;
                }
                else
                {
                    TariffImagePreview.Source = null;
                    ImagePlaceholder.IsVisible = true;
                }
            }
            catch
            {
                TariffImagePreview.Source = null;
                ImagePlaceholder.IsVisible = true;
            }
        }

        private void TariffPriceBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Пропускаем обработку, если окно еще не загружено полностью
            if (!_isLoaded) return;

            var textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                // Оставляем только цифры
                string digits = new string(textBox.Text.Where(c => char.IsDigit(c)).ToArray());
                if (digits != textBox.Text)
                {
                    var caretIndex = textBox.CaretIndex;
                    textBox.Text = digits;
                    textBox.CaretIndex = Math.Min(caretIndex, textBox.Text.Length);
                }

                // Автоматически устанавливаем категорию для тест-драйва
                if (decimal.TryParse(digits, out decimal price) && price == 0)
                {
                    // Проверяем, что TariffNameBox не null и содержит нужный текст
                    if (TariffNameBox != null && 
                        (TariffNameBox.Text?.Contains("Тест-драйв") == true || 
                         TariffNameBox.Text?.Contains("Пробный") == true))
                    {
                        // Проверяем, что CategoryComboBox и его Items не null
                        if (CategoryComboBox != null && CategoryComboBox.Items != null)
                        {
                            // Ищем категорию "Тренажерный зал"
                            foreach (var item in CategoryComboBox.Items)
                            {
                                if (item is TariffCategory category &&
                                    (category.Name.Contains("Тренажерный") || category.CategoryId == 1))
                                {
                                    CategoryComboBox.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void TariffDurationBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Пропускаем обработку, если окно еще не загружено полностью
            if (!_isLoaded) return;

            var textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                string digits = new string(textBox.Text.Where(c => char.IsDigit(c)).ToArray());
                if (digits != textBox.Text)
                {
                    var caretIndex = textBox.CaretIndex;
                    textBox.Text = digits;
                    textBox.CaretIndex = Math.Min(caretIndex, textBox.Text.Length);
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Блокируем кнопку, чтобы предотвратить повторное нажатие
                var saveButton = sender as Button;
                if (saveButton != null)
                    saveButton.IsEnabled = false;

                // Валидация
                if (string.IsNullOrWhiteSpace(TariffNameBox.Text))
                {
                    ShowStatus("Введите название тарифа", false);
                    if (saveButton != null)
                        saveButton.IsEnabled = true;
                    return;
                }

                if (CategoryComboBox.SelectedItem == null)
                {
                    ShowStatus("Выберите категорию тарифа", false);
                    if (saveButton != null)
                        saveButton.IsEnabled = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(TariffPriceBox.Text))
                {
                    ShowStatus("Введите цену тарифа", false);
                    if (saveButton != null)
                        saveButton.IsEnabled = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(TariffDurationBox.Text))
                {
                    ShowStatus("Введите срок действия тарифа", false);
                    if (saveButton != null)
                        saveButton.IsEnabled = true;
                    return;
                }

                // Проверяем, не пытаемся ли мы сохранить тариф с ценой 0 (тест-драйв) с неправильной категорией
                decimal price = decimal.Parse(TariffPriceBox.Text);
                var selectedCategory = CategoryComboBox.SelectedItem as TariffCategory;

                // Автоматически корректируем категорию для тест-драйва
                if (price == 0 && TariffNameBox.Text.Contains("Тест-драйв"))
                {
                    // Убеждаемся, что тест-драйв в категории "Тренажерный зал"
                    var gymCategory = _context.TariffCategories.FirstOrDefault(c => c.Name.Contains("Тренажерный") || c.CategoryId == 1);
                    if (gymCategory != null)
                    {
                        selectedCategory = gymCategory;
                        CategoryComboBox.SelectedItem = gymCategory;
                    }
                }

                // Сохраняем данные
                _currentTariff.Name = TariffNameBox.Text.Trim();
                _currentTariff.Description = TariffDescriptionBox.Text?.Trim();
                _currentTariff.Price = price;
                _currentTariff.DurationDays = int.Parse(TariffDurationBox.Text);

                if (selectedCategory != null)
                {
                    _currentTariff.CategoryId = selectedCategory.CategoryId;
                }

                _currentTariff.IsActive = IsActiveCheckBox.IsChecked ?? true;

                if (!string.IsNullOrEmpty(_selectedImagePath))
                {
                    _currentTariff.ImagePath = _selectedImagePath;
                }

                // Используем отдельный контекст для сохранения, чтобы избежать конфликтов отслеживания
                using (var saveContext = new AppDbContext())
                {
                    if (_isEditMode)
                    {
                        var existingTariff = await saveContext.Tariffs.FindAsync(_currentTariff.TariffId);
                        if (existingTariff != null)
                        {
                            // Обновляем существующий тариф
                            existingTariff.Name = _currentTariff.Name;
                            existingTariff.Description = _currentTariff.Description;
                            existingTariff.Price = _currentTariff.Price;
                            existingTariff.DurationDays = _currentTariff.DurationDays;
                            existingTariff.CategoryId = _currentTariff.CategoryId;
                            existingTariff.IsActive = _currentTariff.IsActive;
                            existingTariff.ImagePath = _currentTariff.ImagePath;

                            saveContext.Tariffs.Update(existingTariff);
                        }
                        else
                        {
                            // Если тариф не найден (возможно был удален), создаем новый
                            _currentTariff.CreatedAt = DateTime.Now;
                            await saveContext.Tariffs.AddAsync(_currentTariff);
                        }
                    }
                    else
                    {
                        _currentTariff.CreatedAt = DateTime.Now;
                        await saveContext.Tariffs.AddAsync(_currentTariff);
                    }

                    await saveContext.SaveChangesAsync();
                }

                // Очищаем кэш контекста
                _context.ChangeTracker.Clear();

                // Отмечаем успешное сохранение
                Tag = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowStatus($"Ошибка сохранения: {ex.Message}", false);

                // Разблокируем кнопку в случае ошибки
                var saveButton = sender as Button;
                if (saveButton != null)
                    saveButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Tag = false;
            Close();
        }

        private void ShowStatus(string message, bool isSuccess)
        {
            StatusText.Text = message;
            StatusBorder.Background = isSuccess ?
                Avalonia.Media.Brush.Parse("#27AE60") :
                Avalonia.Media.Brush.Parse("#E74C3C");
            StatusBorder.IsVisible = true;

            var timer = new System.Timers.Timer(3000);
            timer.Elapsed += (s, args) =>
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    StatusBorder.IsVisible = false;
                });
                timer.Dispose();
            };
            timer.Start();
        }
    }
}