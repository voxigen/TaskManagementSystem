using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskManagementSystem
{
    public partial class MainWindow : Window
    {
        private Users _currentUser;
        private TaskManagementSystemEntities3 _context;

        public MainWindow(Users user)
        {
            InitializeComponent();
            _currentUser = user;
            _context = new TaskManagementSystemEntities3();

            InitializeUserData(user);
            LoadNotifications();

           
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (PageTitle != null)
                PageTitle.Text = "Дашборд";

            if (PageSubtitle != null)
                PageSubtitle.Text = "Обзор системы";

            if (MainContainer != null)
                MainContainer.Navigate(new Dashboard(_currentUser));
        }

        private void InitializeUserData(Users user)
        {
            UserNameText.Text = user.FullName;
            UserRoleText.Text = GetRoleDisplayName(user.Role);

            switch (user.Role)
            {
                case "Student":
                    CourseManagementNav.Visibility = Visibility.Collapsed;
                    UsersNav.Visibility = Visibility.Collapsed;
                    SystemNav.Visibility = Visibility.Collapsed;
                    CreateTaskButton.Visibility = Visibility.Collapsed;
                    QuickActionsButton.Visibility = Visibility.Visible;
                    ReportsNav.Visibility = Visibility.Collapsed;
                    TaskCreateNav.Visibility = Visibility.Collapsed;
                    break;

                case "Teacher":
                    CourseManagementNav.Visibility = Visibility.Collapsed;
                    UsersNav.Visibility = Visibility.Collapsed;
                    SystemNav.Visibility = Visibility.Collapsed;
                    CreateTaskButton.Visibility = Visibility.Visible;
                    QuickActionsButton.Visibility = Visibility.Visible;
                    ReportsNav.Visibility = Visibility.Visible;
                    TaskCreateNav.Visibility = Visibility.Visible;
                    break;

                case "Administrator":
                    CourseManagementNav.Visibility = Visibility.Visible;
                    UsersNav.Visibility = Visibility.Visible;
                    SystemNav.Visibility = Visibility.Visible;
                    CreateTaskButton.Visibility = Visibility.Visible;
                    QuickActionsButton.Visibility = Visibility.Visible;
                    ReportsNav.Visibility = Visibility.Visible;
                    TaskCreateNav.Visibility = Visibility.Visible;
                    break;
            }
        }

        private string GetRoleDisplayName(string role)
        {
            switch (role)
            {
                case "Student": return "Студент";
                case "Teacher": return "Преподаватель";
                case "Administrator": return "Администратор";
                default: return role;
            }
        }

        private void LoadNotifications()
        {
            try
            {
                var unreadCount = _context.Notifications
                    .Count(n => n.UserId == _currentUser.Id && n.IsRead == false);

                NotificationBadge.Visibility = unreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
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
            if (MainContainer != null)
                MainContainer.Navigate(new CoursesView(_currentUser));

            if (PageTitle != null)
                PageTitle.Text = "Мои курсы";

            if (PageSubtitle != null)
                PageSubtitle.Text = "Список учебных курсов";
        }

        private void Tasks_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContainer != null)
                MainContainer.Navigate(new TasksView(_currentUser));

            if (PageTitle != null)
                PageTitle.Text = "Задания";

            if (PageSubtitle != null)
                PageSubtitle.Text = "Все задания и дедлайны";
        }

        private void Submissions_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContainer != null)
                MainContainer.Navigate(new SubmissionsView(_currentUser));

            if (PageTitle != null)
                PageTitle.Text = "Отправленные работы";

            if (PageSubtitle != null)
                PageSubtitle.Text = "История сданных работ";
        }

        private void Reports_Checked(object sender, RoutedEventArgs e)
        {
            if (_currentUser.Role == "Student")
            {
                MessageBox.Show("У вас нет прав для просмотра отчетов");
                return;
            }

            ShowSimplePage("Отчеты", "Аналитика и отчетность");
        }

        private void TaskCreate_Checked(object sender, RoutedEventArgs e)
        {
            if (_currentUser.Role == "Student")
            {
                MessageBox.Show("У вас нет прав для создания заданий");
                return;
            }

            var taskCreateWindow = new TaskCreateWindow(_currentUser);
            taskCreateWindow.Owner = this;
            taskCreateWindow.ShowDialog();
        }

        private void CourseManagement_Checked(object sender, RoutedEventArgs e)
        {
            if (_currentUser.Role != "Administrator")
            {
                MessageBox.Show("У вас нет прав для управления курсами");
                return;
            }

            var managementWindow = new CourseManagementWindow();
            managementWindow.Owner = this;
            managementWindow.ShowDialog();
        }

        private void Users_Checked(object sender, RoutedEventArgs e)
        {
            if (_currentUser.Role != "Administrator")
            {
                MessageBox.Show("У вас нет прав для управления пользователями");
                return;
            }

            var usersWindow = new UserManagementWindow();
            usersWindow.Owner = this;
            usersWindow.ShowDialog();
        }

        private void System_Checked(object sender, RoutedEventArgs e)
        {
            if (_currentUser.Role != "Administrator")
            {
                MessageBox.Show("У вас нет прав для настройки системы");
                return;
            }

            ShowSimplePage("Настройки системы", "Системные настройки и конфигурация");
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

            if (MainContainer != null)
                MainContainer.Navigate(page);

            if (PageTitle != null)
                PageTitle.Text = title;

            if (PageSubtitle != null)
                PageSubtitle.Text = subtitle;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void NotificationsButton_Click(object sender, RoutedEventArgs e)
        {
            var notifications = _context.Notifications
                .Where(n => n.UserId == _currentUser.Id && n.IsRead == false)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToList();

            if (notifications.Count == 0)
            {
                MessageBox.Show("Нет новых уведомлений", "Уведомления");
                return;
            }

            string message = "📢 Новые уведомления:\n\n";
            foreach (var notification in notifications)
            {
                message += $"• {notification.Title}\n";
                if (!string.IsNullOrEmpty(notification.Message))
                {
                    message += $"  {notification.Message}\n";
                }
                message += $"  {notification.CreatedAt:dd.MM.yyyy HH:mm}\n\n";
            }

            MessageBox.Show(message, "Уведомления", MessageBoxButton.OK, MessageBoxImage.Information);

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            _context.SaveChanges();
            LoadNotifications();
        }

        private void CreateTaskButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser.Role == "Student")
            {
                MessageBox.Show("У вас нет прав для создания заданий");
                return;
            }

            var taskCreateWindow = new TaskCreateWindow(_currentUser);
            taskCreateWindow.Owner = this;
            taskCreateWindow.ShowDialog();
        }

        private void QuickActionsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser.Role == "Student")
            {
                var actions = new Window
                {
                    Title = "Быстрые действия",
                    Width = 300,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                var btnSubmitWork = new Button
                {
                    Content = "📝 Сдать работу",
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 40,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27ae60")),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold
                };
                btnSubmitWork.Click += (s, args) =>
                {
                    Tasks_Checked(s, e);
                    actions.Close();
                };

                var btnViewGrades = new Button
                {
                    Content = "📊 Посмотреть оценки",
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 40,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498db")),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold
                };
                btnViewGrades.Click += (s, args) =>
                {
                    Submissions_Checked(s, e);
                    actions.Close();
                };

                stackPanel.Children.Add(btnSubmitWork);
                stackPanel.Children.Add(btnViewGrades);
                actions.Content = stackPanel;
                actions.ShowDialog();
            }
            else
            {
                var actions = new Window
                {
                    Title = "Быстрые действия",
                    Width = 300,
                    Height = 250,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                var stackPanel = new StackPanel { Margin = new Thickness(20) };

                var btnCreateTask = new Button
                {
                    Content = "➕ Создать задание",
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 40,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27ae60")),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold
                };
                btnCreateTask.Click += (s, args) =>
                {
                    CreateTaskButton_Click(s, e);
                    actions.Close();
                };

                var btnCheckWorks = new Button
                {
                    Content = "📝 Проверить работы",
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 40,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e74c3c")),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold
                };
                btnCheckWorks.Click += (s, args) =>
                {
                    Tasks_Checked(s, e);
                    actions.Close();
                };

                var btnGenerateReport = new Button
                {
                    Content = "📊 Создать отчет",
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 40,
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9b59b6")),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold
                };
                btnGenerateReport.Click += (s, args) =>
                {
                    Reports_Checked(s, e);
                    actions.Close();
                };

                stackPanel.Children.Add(btnCreateTask);
                stackPanel.Children.Add(btnCheckWorks);
                stackPanel.Children.Add(btnGenerateReport);
                actions.Content = stackPanel;
                actions.ShowDialog();
            }
        }
    }
}