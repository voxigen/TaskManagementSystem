using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace TaskManagementSystem
{
    public partial class LoginWindow : Window
    {
        private TaskManagementSystemEntities3 _context;

        public LoginWindow()
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            Loaded += (s, e) => LoginTextBox.Focus();

            PasswordBox.KeyDown += PasswordBox_KeyDown;
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && LoginButton.IsEnabled)
            {
                LoginButton_Click(sender, e);
            }
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Функция восстановления пароля временно недоступна. Обратитесь к администратору.",
                "Восстановление пароля", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(login))
            {
                MessageBox.Show("Введите логин", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                LoginTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            if (PasswordHasher.IsSimplePassword(password))
            {
                MessageBox.Show("Пароль слишком простой. Используйте более сложный пароль.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            try
            {
                var user = _context.Users.FirstOrDefault(u => u.Login == login);

                if (user != null)
                {
                    if (PasswordHasher.VerifyPassword(password, user.PasswordHash))
                    {
                        MainWindow mainWindow = new MainWindow(user);
                        mainWindow.Show();
                        this.Close();
                        return;
                    }
                }

                MessageBox.Show("Неверный логин или пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                PasswordBox.Clear();
                PasswordBox.Focus();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Произошла ошибка: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}