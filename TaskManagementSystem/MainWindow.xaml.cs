using System.Windows;
using System.Windows.Controls;

namespace TaskManagementSystem
{
    public partial class MainWindow : Window
    {
        private string currentUserRole;
        private Users _currentUser;

        public MainWindow(Users user)
        {
            InitializeComponent();
            _currentUser = user;
            InitializeUserData(user);
            MainContainer.Navigate(new Dashboard(user));
        }

        private void InitializeUserData(Users user)
        {
            UserNameText.Text = user.FullName;
            UserRoleText.Text = user.Role;

            switch (user.Role)
            {
                case "Student":
                    currentUserRole = "student";
                    CreateTaskButton.Visibility = Visibility.Collapsed;
                    UsersNav.Visibility = Visibility.Collapsed;
                    break;
                case "Teacher":
                    currentUserRole = "teacher";
                    CreateTaskButton.Visibility = Visibility.Visible;
                    UsersNav.Visibility = Visibility.Collapsed;
                    break;
                case "Administrator":
                    currentUserRole = "admin";
                    CreateTaskButton.Visibility = Visibility.Visible;
                    UsersNav.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void Dashboard_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContainer != null)
                MainContainer.Navigate(new Dashboard(_currentUser));

            if (PageTitle != null)
                PageTitle.Text = "Дашборд";

            if (PageSubtitle != null)
                PageSubtitle.Text = "Обзор системы";
        }

        private void Courses_Checked(object sender, RoutedEventArgs e)
        {
            var coursesView = new CoursesView(_currentUser);
            MainContainer.Navigate(coursesView);
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
            var submissionsView = new SubmissionsView(_currentUser);
            MainContainer.Navigate(submissionsView);
            PageTitle.Text = "Отправленные работы";
            PageSubtitle.Text = "История сданных работ";
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