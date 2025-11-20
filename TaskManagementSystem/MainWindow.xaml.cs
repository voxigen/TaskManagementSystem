using System.Windows;
using System.Windows.Controls;

namespace TaskManagementSystem
{
    public partial class MainWindow : Window
    {
        private string currentUserRole;

        public MainWindow(string username)
        {
            InitializeComponent();
            InitializeUserData(username);
            MainContainer.Navigate(new Dashboard());
        }

        private void InitializeUserData(string username)
        {
            switch (username)
            {
                case "student":
                    UserNameText.Text = "Иван Иванов";
                    UserRoleText.Text = "Студент";
                    currentUserRole = "student";
                    CreateTaskButton.Visibility = Visibility.Collapsed;
                    UsersNav.Visibility = Visibility.Collapsed;
                    break;
                case "teacher":
                    UserNameText.Text = "Петр Петрович";
                    UserRoleText.Text = "Преподаватель";
                    currentUserRole = "teacher";
                    CreateTaskButton.Visibility = Visibility.Visible;
                    UsersNav.Visibility = Visibility.Collapsed;
                    break;
                case "admin":
                    UserNameText.Text = "Администратор";
                    UserRoleText.Text = "Администратор";
                    currentUserRole = "admin";
                    CreateTaskButton.Visibility = Visibility.Visible;
                    UsersNav.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Dashboard_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContainer != null)
                MainContainer.Navigate(new Dashboard());

            if (PageTitle != null)
                PageTitle.Text = "Дашборд";

            if (PageSubtitle != null)
                PageSubtitle.Text = "Обзор системы";
        }

        private void Courses_Checked(object sender, RoutedEventArgs e)
        {
            MainContainer.Navigate(new CoursesView());
            PageTitle.Text = "Мои курсы";
            PageSubtitle.Text = "Список учебных курсов";
        }

        private void Tasks_Checked(object sender, RoutedEventArgs e)
        {
            MainContainer.Navigate(new TasksView());
            PageTitle.Text = "Задания";
            PageSubtitle.Text = "Все задания и дедлайны";
        }

        private void Submissions_Checked(object sender, RoutedEventArgs e)
        {
            ShowSimplePage("Отправленные работы", "История сданных работ");
        }

        private void Reports_Checked(object sender, RoutedEventArgs e)
        {
            ShowSimplePage("Отчеты", "Аналитика и отчетность");
        }

        private void Users_Checked(object sender, RoutedEventArgs e)
        {
            ShowSimplePage("Пользователи", "Управление пользователями");
        }

        private void Settings_Checked(object sender, RoutedEventArgs e)
        {
            ShowSimplePage("Настройки", "Настройки системы");
        }

        private void ShowSimplePage(string title, string subtitle)
        {
            var page = new Page();
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = $"Раздел '{title}' в разработке",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 20)
            });
            page.Content = stack;
            MainContainer.Navigate(page);
            PageTitle.Text = title;
            PageSubtitle.Text = subtitle;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void NotificationsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Центр уведомлений\n3 новых уведомления", "Уведомления");
        }

        private void CreateTaskButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Форма создания нового задания", "Создание задания");
        }
    }
}