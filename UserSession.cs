namespace FitClub
{
    public static class UserSession
    {
        public static string CurrentUserEmail { get; set; }
        public static string CurrentUserRole { get; set; }
        public static bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUserEmail);
        
        // Перегруженные методы для обратной совместимости
        public static void Login(string email)
        {
            CurrentUserEmail = email;
            CurrentUserRole = "Client"; // По умолчанию
        }
        
        public static void Login(string email, string role)
        {
            CurrentUserEmail = email;
            CurrentUserRole = role;
        }
        
        public static void Logout()
        {
            CurrentUserEmail = null;
            CurrentUserRole = null;
        }
    }
}