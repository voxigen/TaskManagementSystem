using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskManagementSystem
{
    public partial class Dashboard : Page
    {
        private TaskManagementSystemEntities3 _context;
        private Users _currentUser;

        public Dashboard(Users user)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            _currentUser = user;
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            WelcomeText.Text = $"Добро пожаловать, {_currentUser.FullName}!";
            DateText.Text = $"Сегодня: {DateTime.Now:dddd, dd MMMM yyyy года}";

            LoadUrgentTasks();
            LoadCourseProgress();
            LoadNotifications();
        }

        private void LoadUrgentTasks()
        {
            var student = _context.Students.FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student == null) return;

            var studentId = student.UserId;

            var studentCourseIds = _context.Database.SqlQuery<int>(
                "SELECT CourseId FROM CourseStudents WHERE StudentUserId = {0}",
                studentId).ToList();

            var urgentTasks = _context.Tasks
                .Where(t => studentCourseIds.Contains(t.CourseId) &&
                           t.Deadline <= DateTime.Now.AddDays(7) &&
                           !t.Submissions.Any(s => s.StudentUserId == studentId))
                .OrderBy(t => t.Deadline)
                .Take(5)
                .ToList();

            UrgentTasksTitle.Text = $"🚨 СРОЧНЫЕ ЗАДАНИЯ ({urgentTasks.Count})";

            foreach (var task in urgentTasks)
            {
                var course = _context.Courses.FirstOrDefault(c => c.Id == task.CourseId);
                var daysLeft = (task.Deadline - DateTime.Now).Days;

                var button = CreateTaskButton(task, course, daysLeft);
                UrgentTasksPanel.Children.Add(button);
            }
        }

        private Button CreateTaskButton(Tasks task, Courses course, int daysLeft)
        {
            var button = new Button
            {
                Width = 280,
                Height = 120,
                Margin = new Thickness(0, 0, 16, 16),
                Background = daysLeft <= 1 ? (Brush)new BrushConverter().ConvertFrom("#fff5f5") : Brushes.White,
                BorderBrush = daysLeft <= 1 ? (Brush)new BrushConverter().ConvertFrom("#e74c3c") : (Brush)new BrushConverter().ConvertFrom("#bdc3c7"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = task
            };

            button.Click += (s, e) => ShowTaskDetails(task);

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(new TextBlock
            {
                Text = course?.Title ?? "Неизвестный курс",
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50")
            });
            stackPanel.Children.Add(new TextBlock { Text = task.Title, FontSize = 14, Margin = new Thickness(0, 4, 0, 0) });

            string dueText;
            if (daysLeft < 0)
                dueText = $"Просрочено на {-daysLeft} дней";
            else if (daysLeft == 0)
                dueText = "Сегодня";
            else if (daysLeft == 1)
                dueText = "Завтра";
            else
                dueText = $"Через {daysLeft} дней";

            var dueColor = daysLeft <= 1 ? "#e74c3c" : "#f39c12";
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"До: {dueText}",
                FontSize = 12,
                Foreground = (Brush)new BrushConverter().ConvertFrom(dueColor),
                Margin = new Thickness(0, 8, 0, 0)
            });
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"⏰ {(int)(task.Deadline - DateTime.Now).TotalHours} часов",
                FontSize = 11,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#95a5a6"),
                Margin = new Thickness(0, 4, 0, 0)
            });

            button.Content = stackPanel;
            return button;
        }

        private void LoadCourseProgress()
        {
            var student = _context.Students.FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student == null) return;

            var studentId = student.UserId;

            var studentCourses = _context.Database.SqlQuery<Courses>(
                "SELECT c.* FROM Courses c INNER JOIN CourseStudents cs ON c.Id = cs.CourseId WHERE cs.StudentUserId = {0}",
                studentId).ToList();

            foreach (var course in studentCourses)
            {
                var courseTasks = _context.Tasks.Where(t => t.CourseId == course.Id).ToList();
                var submittedTasks = courseTasks.Count(t => _context.Submissions.Any(s => s.TaskId == t.Id && s.StudentUserId == studentId));
                var totalTasks = courseTasks.Count;
                var progress = totalTasks > 0 ? (int)((double)submittedTasks / totalTasks * 100) : 0;

                var progressPanel = CreateProgressPanel(course.Title, progress, submittedTasks, totalTasks);
                ProgressPanel.Children.Add(progressPanel);
            }
        }

        private StackPanel CreateProgressPanel(string courseTitle, int progress, int submittedTasks, int totalTasks)
        {
            var stackPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };

            var grid = new Grid();
            grid.Children.Add(new TextBlock { Text = courseTitle, FontWeight = FontWeights.SemiBold });
            grid.Children.Add(new TextBlock
            {
                Text = $"{progress}% ({submittedTasks}/{totalTasks} заданий)",
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#7f8c8d")
            });

            stackPanel.Children.Add(grid);

            var progressBorder = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFrom("#ecf0f1"),
                Height = 8,
                Margin = new Thickness(0, 4, 0, 0)
            };
            progressBorder.Child = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFrom("#3498db"),
                Height = 8,
                Width = progress * 3
            };

            stackPanel.Children.Add(progressBorder);
            return stackPanel;
        }

        private void LoadNotifications()
        {
            var notifications = _context.Notifications
                .Where(n => n.UserId == _currentUser.Id && n.IsRead == false)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToList();

            foreach (var notification in notifications)
            {
                var button = new Button
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(8),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Tag = notification
                };

                button.Click += (s, e) => ShowNotificationDetails(notification);

                var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

                string icon;
                if (notification.Type == "Grade")
                    icon = "✅";
                else if (notification.Type == "Comment")
                    icon = "📝";
                else if (notification.Type == "NewTask")
                    icon = "🆕";
                else
                    icon = "🔔";

                stackPanel.Children.Add(new TextBlock { Text = icon, FontSize = 14, Margin = new Thickness(0, 0, 8, 0) });
                stackPanel.Children.Add(new TextBlock
                {
                    Text = notification.Title,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50")
                });

                button.Content = stackPanel;
                NotificationsPanel.Children.Add(button);
            }
        }

        private void ShowTaskDetails(Tasks task)
        {
            var course = _context.Courses.FirstOrDefault(c => c.Id == task.CourseId);
            MessageBox.Show($"Задание: {task.Title}\nКурс: {course?.Title ?? "Неизвестный курс"}\nДедлайн: {task.Deadline:dd.MM.yyyy}\n\n{task.Description}", "Детали задания");
        }

        private void ShowNotificationDetails(Notifications notification)
        {
            MessageBox.Show(notification.Message, notification.Title);
            notification.IsRead = true;
            _context.SaveChanges();
        }
    }
}