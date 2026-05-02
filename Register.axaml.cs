using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FitClub.Models;

namespace FitClub
{
    public partial class Register : Window
    {
        private readonly AppDbContext _context = new AppDbContext();
        private bool _isPasswordVisible = false;
        private bool _isConfirmPasswordVisible = false;

        public Register()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void BackToLogin_Click(object? sender, RoutedEventArgs e)
        {
            var loginWindow = new Avtoriz();
            loginWindow.Show();
            this.Close();
        }

        // Обработчики для паспортных данных
        private void OnPassportSeriesChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                // Оставляем только цифры
                textBox.Text = new string(textBox.Text.Where(char.IsDigit).ToArray());
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        private void OnPassportNumberChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                // Оставляем только цифры
                textBox.Text = new string(textBox.Text.Where(char.IsDigit).ToArray());
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        // Маска для телефона
        private void OnPhoneTextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                string text = textBox.Text;
                
                // Удаляем все нецифровые символы кроме +
                string digits = new string(text.Where(c => char.IsDigit(c) || c == '+').ToArray());
                
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
                if (digits.Length >= 10)
                {
                    textBox.Text = $"+7 {digits.Substring(0, 3)} {digits.Substring(3, 3)}-{digits.Substring(6, 2)}-{digits.Substring(8)}";
                }
                else if (digits.Length >= 6)
                {
                    textBox.Text = $"+7 {digits.Substring(0, 3)} {digits.Substring(3, 3)}-{digits.Substring(6)}";
                }
                else if (digits.Length >= 3)
                {
                    textBox.Text = $"+7 {digits.Substring(0, 3)} {digits.Substring(3)}";
                }
                else if (digits.Length > 0)
                {
                    textBox.Text = $"+7 {digits}";
                }
                else
                {
                    textBox.Text = "";
                }
                
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        // Показать/скрыть пароль
        private void OnShowPasswordClick(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            PasswordTextBox.PasswordChar = _isPasswordVisible ? '\0' : '•';
            ShowPasswordBtn.Content = _isPasswordVisible ? "🙈" : "👁";
        }

        private void OnShowConfirmPasswordClick(object sender, RoutedEventArgs e)
        {
            _isConfirmPasswordVisible = !_isConfirmPasswordVisible;
            ConfirmPasswordTextBox.PasswordChar = _isConfirmPasswordVisible ? '\0' : '•';
            ShowConfirmPasswordBtn.Content = _isConfirmPasswordVisible ? "🙈" : "👁";
        }

        // Валидация пароля в реальном времени
        private void OnPasswordChanged(object sender, TextChangedEventArgs e)
        {
            string password = PasswordTextBox.Text ?? "";
            UpdatePasswordRequirements(password);
        }

        private void OnConfirmPasswordChanged(object sender, TextChangedEventArgs e)
        {
            string password = PasswordTextBox.Text ?? "";
            string confirmPassword = ConfirmPasswordTextBox.Text ?? "";
            
            if (string.IsNullOrEmpty(confirmPassword))
            {
                PasswordMatchText.Text = "";
                PasswordMatchText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Gray);
            }
            else if (password == confirmPassword)
            {
                PasswordMatchText.Text = "✓ Пароли совпадают";
                PasswordMatchText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Green);
            }
            else
            {
                PasswordMatchText.Text = "✗ Пароли не совпадают";
                PasswordMatchText.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Red);
            }
        }

        private void UpdatePasswordRequirements(string password)
        {
            bool hasMinLength = password.Length >= 8;
            bool hasUpperCase = password.Any(char.IsUpper);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecialChar = password.Any(ch => !char.IsLetterOrDigit(ch));

            string requirements = "";
            requirements += hasMinLength ? "✓ 8+ символов " : "✗ 8+ символов ";
            requirements += hasUpperCase ? "✓ Заглавная " : "✗ Заглавная ";
            requirements += hasDigit ? "✓ Цифра " : "✗ Цифра ";
            requirements += hasSpecialChar ? "✓ Спецсимвол" : "✗ Спецсимвол";

            PasswordRequirements.Text = requirements;
            
            if (hasMinLength && hasUpperCase && hasDigit && hasSpecialChar)
            {
                PasswordRequirements.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Green);
            }
            else
            {
                PasswordRequirements.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Red);
            }
        }

        private async void Register_Click(object? sender, RoutedEventArgs e)
        {
            // Получение значений из полей
            string lastName = LastNameTextBox.Text?.Trim() ?? "";
            string firstName = FirstNameTextBox.Text?.Trim() ?? "";
            string middleName = MiddleNameTextBox.Text?.Trim() ?? "";
            string phone = PhoneTextBox.Text?.Trim() ?? "";
            string email = EmailTextBox.Text?.Trim() ?? "";
            string password = PasswordTextBox.Text ?? "";
            string confirmPassword = ConfirmPasswordTextBox.Text ?? "";

            // Валидация обязательных полей
            if (string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(middleName) ||
                string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                await ShowError("Пожалуйста, заполните все обязательные поля");
                return;
            }

            // Проверка совпадения паролей
            if (password != confirmPassword)
            {
                await ShowError("Пароли не совпадают");
                return;
            }

            // Проверка сложности пароля
            if (!IsStrongPassword(password))
            {
                await ShowError("Пароль слишком слабый. Требования: минимум 8 символов, заглавная буква, цифра и специальный символ");
                return;
            }

            // Валидация email
            if (!IsValidEmail(email))
            {
                await ShowError("Неверный формат email");
                return;
            }

            // Валидация телефона
            string cleanPhone = new string(phone.Where(char.IsDigit).ToArray());
            if (cleanPhone.Length != 11 || !cleanPhone.StartsWith("7"))
            {
                await ShowError("Неверный формат телефона. Должно быть 11 цифр");
                return;
            }

            // Проверка уникальности email
            if (_context.Users.Any(u => u.Email == email))
            {
                await ShowError("Пользователь с таким email уже зарегистрирован");
                return;
            }

            try
            {
                // Получаем роль "Client" (RoleId = 3)
                var clientRole = _context.Roles.FirstOrDefault(r => r.Name == "Client");
                if (clientRole == null)
                {
                    await ShowError("Роль 'Client' не найдена в базе данных");
                    return;
                }

                // Создаем пользователя
                var newUser = new User
                {
                    Email = email,
                    Password = password,
                    RoleId = clientRole.RoleId
                };

                _context.Users.Add(newUser);
                _context.SaveChanges();

                // Создаем клиента
                var newClient = new Models.Client
                {
                    LastName = lastName,
                    FirstName = firstName,
                    MiddleName = middleName,
                    Phone = phone,
                    Email = email,
                    PassportSeries = "", // Пустые значения
                    PassportNumber = ""  // Пустые значения
                };

                _context.Clients.Add(newClient);
                _context.SaveChanges();

                await ShowSuccess("Регистрация прошла успешно! Теперь вы можете войти в систему.");
                
                BackToLogin_Click(sender, e);
            }
            catch (System.Exception ex)
            {
                await ShowError($"Ошибка при регистрации: {ex.Message}");
            }
        }

        private bool IsStrongPassword(string password)
        {
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private async System.Threading.Tasks.Task ShowError(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", message, ButtonEnum.Ok);
            await box.ShowWindowDialogAsync(this);
        }

        private async System.Threading.Tasks.Task ShowSuccess(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Успех", message, ButtonEnum.Ok);
            await box.ShowWindowDialogAsync(this);
        }
    }
}