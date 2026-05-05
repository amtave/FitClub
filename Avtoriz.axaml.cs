using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.EntityFrameworkCore;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Linq;
using System.Threading.Tasks;
using FitClub.Admin;
using FitClub.Client;
using FitClub.Trainer;
using FitClub.Manager;
using FitClub.Models;
using Npgsql;
using System;

namespace FitClub
{
    public partial class Avtoriz : Window
    {
        private readonly AppDbContext _context = new();
        private int _loginAttempts = 0;
        private const int MaxAttempts = 3;

        public Avtoriz()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            _context.Database.EnsureCreated();
        }

        private void TogglePassword_Click(object? sender, RoutedEventArgs e)
        {
            PasswordTextBox.PasswordChar = PasswordTextBox.PasswordChar == '•' ? '\0' : '•';
        }

        private void OpenReg_Click(object? sender, RoutedEventArgs e)
        {
            var regWindow = new Register();
            regWindow.Show();
            this.Close();
        }

        private async void Login_Click(object? sender, RoutedEventArgs e)
        {
            if (_loginAttempts >= MaxAttempts)
            {
                await ShowError("Попытки исчерпаны. Доступ заблокирован.");
                return;
            }

            string email = EmailTextBox.Text?.Trim() ?? "";
            string password = PasswordTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await ShowError("Пожалуйста, заполните все поля");
                return;
            }

            var user = await TryLoginEnhancedAsync(email, password);

            if (user == null)
            {
                _loginAttempts++;
                int remaining = MaxAttempts - _loginAttempts;
                
                if (remaining > 0)
                {
                    await ShowError($"Неверный email или пароль. Осталось попыток: {remaining}");
                }
                else
                {
                    LoginButton.IsEnabled = false;
                    await ShowError("Попытки исчерпаны. Доступ заблокирован.");
                }
                return;
            }

            _loginAttempts = 0;

            var client = _context.GetClientByEmail(email);

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
                case "Manager":
                    var managerWindow = new Manager.Menu_manager(user);
                    managerWindow.Show();
                    break;
                default:
                    await ShowError("Неизвестная роль пользователя");
                    return;
            }

            this.Close();
        }

        private async Task<User> TryLoginEnhancedAsync(string email, string password)
        {
            var userSimple = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Email == email && u.Password == password);
            
            if (userSimple != null)
                return userSimple;
            
            try
            {
                var connectionString = "Host=localhost;Port=5432;Database=FitClub;Username=postgres;Password=1234";
                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();
                    
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
                                
                                if (dbPassword == password)
                                {
                                    return _context.Users
                                        .Include(u => u.Role)
                                        .FirstOrDefault(u => u.UsersId == userId);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
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
                    
                    using (var command = new NpgsqlCommand(
                        "ALTER TABLE users DISABLE TRIGGER trigger_hash_user_password", 
                        connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                    
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
            catch (Exception)
            {
            }
        }

        private async Task<User> TryLoginAllMethodsAsync(string email, string password)
        {
            var userSimple = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Email == email && u.Password == password);
            
            if (userSimple != null)
                return userSimple;
            
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
            var user = _context.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return null;
            }

            if (user.Password == password)
            {
                return user;
            }

            try
            {
                bool isPasswordValid = CheckPasswordWithPgcrypto(email, password);
                if (isPasswordValid)
                {
                    return user;
                }
            }
            catch (Exception)
            {
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
                catch (Exception)
                {
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
            catch (Exception)
            {
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