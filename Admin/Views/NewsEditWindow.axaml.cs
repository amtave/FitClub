using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using FitClub.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FitClub.Admin.Views
{
    public partial class NewsEditWindow : Window
    {
        private readonly AppDbContext _context;
        private News _currentNews;
        private string _selectedImagePath;
        private bool _isEditMode;

        // НЕ НУЖНО объявлять поля для элементов управления!
        // Они автоматически генерируются из XAML

        public NewsEditWindow()
        {
            InitializeComponent();
            
            _context = new AppDbContext();
            _isEditMode = false;
            
            // Устанавливаем заголовок
            this.FindControl<TextBlock>("WindowTitle").Text = "➕ Добавление новости";
        }

        public NewsEditWindow(News news) : this()
        {
            _currentNews = news;
            _isEditMode = true;
            
            this.FindControl<TextBlock>("WindowTitle").Text = "✏️ Редактирование новости";
            
            LoadNewsData();
        }

        private void LoadNewsData()
        {
            if (_currentNews != null)
            {
                var titleBox = this.FindControl<TextBox>("TitleBox");
                var descriptionBox = this.FindControl<TextBox>("DescriptionBox");
                var previewImage = this.FindControl<Image>("PreviewImage");
                var imagePlaceholder = this.FindControl<Border>("ImagePlaceholder");
                var selectedFileInfo = this.FindControl<TextBlock>("SelectedFileInfo");
                var selectedFileInfoBorder = this.FindControl<Border>("SelectedFileInfoBorder");

                if (titleBox != null)
                    titleBox.Text = _currentNews.Title;
                    
                if (descriptionBox != null)
                    descriptionBox.Text = _currentNews.Description;

                if (!string.IsNullOrEmpty(_currentNews.ImagePath))
                {
                    try
                    {
                        string fullPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", _currentNews.ImagePath);
                        fullPath = Path.GetFullPath(fullPath);

                        if (File.Exists(fullPath))
                        {
                            if (previewImage != null)
                                previewImage.Source = new Bitmap(fullPath);
                            if (imagePlaceholder != null)
                                imagePlaceholder.IsVisible = false;
                                
                            _selectedImagePath = fullPath;
                            
                            var fileInfo = new FileInfo(fullPath);
                            string fileSize = fileInfo.Length > 1024 * 1024 
                                ? $"{(fileInfo.Length / (1024.0 * 1024.0)):F1} МБ" 
                                : $"{(fileInfo.Length / 1024.0):F0} КБ";
                            
                            if (selectedFileInfo != null)
                                selectedFileInfo.Text = $"Текущее изображение: {Path.GetFileName(_currentNews.ImagePath)} ({fileSize})";
                            if (selectedFileInfoBorder != null)
                                selectedFileInfoBorder.IsVisible = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                    }
                }
            }
        }

        private async void SelectImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new OpenFileDialog();
                dialog.Title = "Выберите изображение для новости";
                dialog.AllowMultiple = false;
                dialog.Filters.Add(new FileDialogFilter 
                { 
                    Name = "Изображения", 
                    Extensions = { "png", "jpg", "jpeg", "bmp", "gif" } 
                });

                var result = await dialog.ShowAsync(this);

                if (result != null && result.Any())
                {
                    _selectedImagePath = result[0];
                    
                    var previewImage = this.FindControl<Image>("PreviewImage");
                    var imagePlaceholder = this.FindControl<Border>("ImagePlaceholder");
                    var selectedFileInfo = this.FindControl<TextBlock>("SelectedFileInfo");
                    var selectedFileInfoBorder = this.FindControl<Border>("SelectedFileInfoBorder");
                    
                    // Показываем превью
                    if (previewImage != null)
                        previewImage.Source = new Bitmap(_selectedImagePath);
                    if (imagePlaceholder != null)
                        imagePlaceholder.IsVisible = false;
                    
                    // Показываем информацию о файле
                    var fileInfo = new FileInfo(_selectedImagePath);
                    string fileSize = fileInfo.Length > 1024 * 1024 
                        ? $"{(fileInfo.Length / (1024.0 * 1024.0)):F1} МБ" 
                        : $"{(fileInfo.Length / 1024.0):F0} КБ";
                    
                    if (selectedFileInfo != null)
                        selectedFileInfo.Text = $"Выбрано: {Path.GetFileName(_selectedImagePath)} ({fileSize})";
                    if (selectedFileInfoBorder != null)
                        selectedFileInfoBorder.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Ошибка", $"Не удалось загрузить изображение: {ex.Message}");
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var titleBox = this.FindControl<TextBox>("TitleBox");
                var descriptionBox = this.FindControl<TextBox>("DescriptionBox");

                // Проверяем заполнение полей
                if (titleBox == null || string.IsNullOrWhiteSpace(titleBox.Text))
                {
                    await ShowMessageBox("Ошибка", "Введите заголовок новости");
                    return;
                }

                if (descriptionBox == null || string.IsNullOrWhiteSpace(descriptionBox.Text))
                {
                    await ShowMessageBox("Ошибка", "Введите текст новости");
                    return;
                }

                string imagePath = _currentNews?.ImagePath;

                // Если выбрано новое изображение
                if (!string.IsNullOrEmpty(_selectedImagePath))
                {
                    // Создаем папку News если её нет
                    string newsFolder = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "News");
                    if (!Directory.Exists(newsFolder))
                        Directory.CreateDirectory(newsFolder);

                    // Генерируем уникальное имя файла
                    string fileName = $"news_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(_selectedImagePath)}";
                    string destinationPath = Path.Combine(newsFolder, fileName);

                    // Копируем файл
                    File.Copy(_selectedImagePath, destinationPath, true);

                    // Сохраняем относительный путь
                    imagePath = $"News/{fileName}";
                }

                if (_isEditMode && _currentNews != null)
                {
                    // Обновляем существующую новость
                    _currentNews.Title = titleBox.Text;
                    _currentNews.Description = descriptionBox.Text;
                    if (!string.IsNullOrEmpty(imagePath))
                        _currentNews.ImagePath = imagePath;

                    _context.News.Update(_currentNews);
                    await _context.SaveChangesAsync();

                    await ShowMessageBox("Успех", "Новость успешно обновлена!");
                }
                else
                {
                    // Создаем новую новость
                    var news = new News
                    {
                        Title = titleBox.Text,
                        Description = descriptionBox.Text,
                        ImagePath = imagePath,
                        CreatedAt = DateTime.Now,
                        IsActive = true
                    };

                    _context.News.Add(news);
                    await _context.SaveChangesAsync();

                    await ShowMessageBox("Успех", "Новость успешно добавлена!");
                }

                Close(true);
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Ошибка", $"Не удалось сохранить новость: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private async Task ShowMessageBox(string title, string message)
        {
            var msgBox = new Window
            {
                Title = title,
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };
            stackPanel.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
            
            var button = new Button 
            { 
                Content = "OK", 
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Padding = new Thickness(20, 5)
            };
            button.Click += (s, e) => msgBox.Close();
            
            stackPanel.Children.Add(button);
            msgBox.Content = stackPanel;
            
            // Показываем как диалог с текущим окном в качестве владельца
            await msgBox.ShowDialog(this);
        }

        public void Close(bool result)
        {
            // Сохраняем результат в свойстве Tag
            Tag = result;
            Close();
        }
    }
}