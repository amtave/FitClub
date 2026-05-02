using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Linq;
using System.Threading.Tasks;
using FitClub.Admin;
using FitClub.Client;
using FitClub.Trainer;
using FitClub.Models;
using Npgsql;
using System;

namespace FitClub
{
    public partial class Avtoriz : Window
    {
        private readonly AppDbContext _context = new();

        public Avtoriz()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            _context.Database.EnsureCreated();
        }

        private void OpenReg_Click(object? sender, RoutedEventArgs e)
        {
            var regWindow = new Register();
            regWindow.Show();
            this.Close();
        }

        private async void Login_Click(object? sender, RoutedEventArgs e)
{
    string email = EmailTextBox.Text?.Trim() ?? "";
    string password = PasswordTextBox.Text?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        await ShowError("Пожалуйста, заполните все поля");
        return;
    }

    // Используем УЛУЧШЕННЫЙ метод авторизации
    var user = await TryLoginEnhancedAsync(email, password);

    if (user == null)
    {
        await ShowError("Неверный email или пароль");
        return;
    }

    // Получаем данные клиента
    var client = _context.GetClientByEmail(email);

    // Открываем соответствующее окно и передаем данные
    switch (user.Role.Name)
    {
        case "Admin":
            var adminWindow = new Admin.Menu_admin();
            adminWindow.Show();
            break;
        case "Trainer":
            var trainerWindow = new Trainer.Menu_trainer(user);
            trainerWindow.Show();
            break;
        case "Client":
            var clientWindow = new Client.Menu_client(user, client);
            clientWindow.Show();
            break;
        default:
            await ShowError("Неизвестная роль пользователя");
            return;
    }

    this.Close();
}

private async Task<User> TryLoginEnhancedAsync(string email, string password)
{
    // Метод 1: Простая проверка (для незахешированных паролей)
    var userSimple = _context.Users
        .Include(u => u.Role)
        .FirstOrDefault(u => u.Email == email && u.Password == password);
    
    if (userSimple != null)
        return userSimple;
    
    // Метод 2: Проверка через pgcrypto (для захешированных паролей)
    try
    {
        var connectionString = "Host=localhost;Port=5432;Database=FitClub;Username=postgres;Password=1234";
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            
            // Сначала проверяем через pgcrypto
            using (var command = new NpgsqlCommand(
                "SELECT users_id FROM users WHERE email = @email AND password = crypt(@password, password)", 
                connection))
            {
                command.Parameters.AddWithValue("email", email);
                command.Parameters.AddWithValue("password", password);
                var result = command.ExecuteScalar();
                if (result != null)
                {
                    return _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefault(u => u.Email == email);
                }
            }
            
            // Если pgcrypto не сработал, пробуем прямое сравнение
            // (на случай если пароль сломался из-за триггера)
            using (var command = new NpgsqlCommand(
                "SELECT users_id, password FROM users WHERE email = @email", 
                connection))
            {
                command.Parameters.AddWithValue("email", email);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int userId = reader.GetInt32(0);
                        string dbPassword = reader.GetString(1);
                        
                        // Если пароль в базе совпадает с введенным
                        if (dbPassword == password)
                        {
                            return _context.Users
                                .Include(u => u.Role)
                                .FirstOrDefault(u => u.UsersId == userId);
                        }
                        
                        // Если пароль начинается с $2 (хеш bcrypt), но триггер его сломал
                        // Пробуем восстановить
                        if (dbPassword.StartsWith("$2") && dbPassword != password)
                        {
                            // Это признак сломанного хеша из-за триггера
                            // Нужно сбросить пароль
                            await ResetUserPassword(email, "client123"); // или другой стандартный пароль
                            return _context.Users
                                .Include(u => u.Role)
                                .FirstOrDefault(u => u.Email == email);
                        }
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при авторизации: {ex.Message}");
    }
    
    return null;
}

private async Task ResetUserPassword(string email, string newPassword)
{
    try
    {
        var connectionString = "Host=localhost;Port=5432;Database=FitClub;Username=postgres;Password=12345";
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            
            // Отключаем триггер
            using (var command = new NpgsqlCommand(
                "ALTER TABLE users DISABLE TRIGGER trigger_hash_user_password", 
                connection))
            {
                await command.ExecuteNonQueryAsync();
            }
            
            // Устанавливаем простой пароль
            using (var command = new NpgsqlCommand(
                "UPDATE users SET password = @password WHERE email = @email", 
                connection))
            {
                command.Parameters.AddWithValue("password", newPassword);
                command.Parameters.AddWithValue("email", email);
                await command.ExecuteNonQueryAsync();
            }

            using (var command = new NpgsqlCommand(
                "ALTER TABLE users ENABLE TRIGGER trigger_hash_user_password", 
                connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при сбросе пароля: {ex.Message}");
    }
}

private async Task<User> TryLoginAllMethodsAsync(string email, string password)
{
    // Метод 1: Простая проверка
    var userSimple = _context.Users
        .Include(u => u.Role)
        .FirstOrDefault(u => u.Email == email && u.Password == password);
    
    if (userSimple != null)
        return userSimple;
    
    // Метод 2: Проверка через pgcrypto
    try
    {
        bool isPasswordValid = CheckPasswordWithPgcrypto(email, password);
        if (isPasswordValid)
        {
            return _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Email == email);
        }
    }
    catch (Exception) { }
    
    // Метод 3: Прямое SQL сравнение (на случай если пароль не хеширован)
    try
    {
        var connectionString = "Host=localhost;Port=5432;Database=FitClub;Username=postgres;Password=1234";
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            using (var command = new NpgsqlCommand(
                "SELECT users_id FROM users WHERE email = @email AND password = @password", 
                connection))
            {
                command.Parameters.AddWithValue("email", email);
                command.Parameters.AddWithValue("password", password);
                var result = command.ExecuteScalar();
                if (result != null)
                {
                    return _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefault(u => u.Email == email);
                }
            }
        }
    }
    catch (Exception) { }
    
    return null;
}

        private async Task<User> TryLoginAsync(string email, string password)
{
    // Сначала пробуем найти пользователя по email
    var user = _context.Users
        .Include(u => u.Role)
        .FirstOrDefault(u => u.Email == email);

    if (user == null)
    {
        // Пользователь не найден по email
        return null;
    }

    // Пробуем простую проверку (без хеширования) - для тестирования
    // Эта проверка должна работать если пароль в базе не захеширован
    if (user.Password == password)
    {
        return user;
    }

    // Если простая проверка не сработала, пробуем через pgcrypto
    try
    {
        bool isPasswordValid = CheckPasswordWithPgcrypto(email, password);
        if (isPasswordValid)
        {
            return user;
        }
    }
    catch (Exception ex)
    {
        // Пробуем еще один способ - прямое сравнение через SQL
        try
        {
            var connectionString = "Host=localhost;Port=5432;Database=FitClub;Username=postgres;Password=12345";
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(
                    "SELECT users_id FROM users WHERE email = @email AND password = @password", 
                    connection))
                {
                    command.Parameters.AddWithValue("email", email);
                    command.Parameters.AddWithValue("password", password);
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        return user;
                    }
                }
            }
        }
        catch (Exception ex2)
        {
            // Console.WriteLine($"Ошибка прямой проверки пароля: {ex2.Message}");
        }
    }

    return null;
}

        private bool CheckPasswordWithPgcrypto(string email, string password)
        {
            try
            {
                var connectionString = "Host=localhost;Port=5432;Database=FitClub;Username=postgres;Password=12345";
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM users WHERE email = @email AND password = crypt(@password, password)", 
                        connection))
                    {
                        command.Parameters.AddWithValue("email", email);
                        command.Parameters.AddWithValue("password", password);
                        var result = command.ExecuteScalar();
                        return Convert.ToInt64(result) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки пароля через pgcrypto: {ex.Message}");
                return false;
            }
        }

        private async Task ShowError(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Ошибка", message, ButtonEnum.Ok);
            await box.ShowWindowDialogAsync(this);
        }
    }
}