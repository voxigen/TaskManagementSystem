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
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && LoginButton.IsEnabled)
            {
                LoginButton_Click(sender, e);
            }
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Функция восстановления пароля временно недоступна. Обратитесь к администратору.", "Восстановление пароля", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = _context.Users.FirstOrDefault(u => u.Login == login);

            if (user != null && user.PasswordHash == password)
            {
                MainWindow mainWindow = new MainWindow(user);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}