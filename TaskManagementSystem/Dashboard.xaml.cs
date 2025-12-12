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

            switch (_currentUser.Role)
            {
                case "Student":
                    LoadStudentDashboard();
                    break;
                case "Teacher":
                    LoadTeacherDashboard();
                    break;
                case "Administrator":
                    LoadAdminDashboard();
                    break;
            }
        }

      
        private void LoadStudentDashboard()
        {
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

            var deadlineThreshold = DateTime.Now.AddDays(7);

            var urgentTasks = _context.Tasks
                .Where(t => studentCourseIds.Contains(t.CourseId) &&
                           t.Deadline <= deadlineThreshold &&
                           !t.Submissions.Any(s => s.StudentUserId == studentId))
                .OrderBy(t => t.Deadline)
                .Take(5)
                .ToList();

            UrgentTasksTitle.Text = $"🚨 СРОЧНЫЕ ЗАДАНИЯ ({urgentTasks.Count})";

            foreach (var task in urgentTasks)
            {
                var course = _context.Courses.FirstOrDefault(c => c.Id == task.CourseId);
                int daysLeft = (int)((DateTime)task.Deadline - DateTime.Now).TotalDays;

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

      
        private void LoadTeacherDashboard()
        {
            UrgentTasksTitle.Text = "👨‍🏫 МОИ КУРСЫ";
            LoadTeacherCourses();
            LoadTasksToReview();
            LoadTeacherNotifications();
        }

        private void LoadTeacherCourses()
        {
            var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == _currentUser.Id);
            if (teacher == null) return;

            var teacherId = teacher.UserId;

            var teacherCourses = _context.Database.SqlQuery<Courses>(
                "SELECT c.* FROM Courses c INNER JOIN CourseTeachers ct ON c.Id = ct.CourseId WHERE ct.TeacherUserId = {0}",
                teacherId).ToList();

            foreach (var course in teacherCourses)
            {
                var taskCount = _context.Tasks.Count(t => t.CourseId == course.Id && t.TeacherUserId == teacherId);
                var submissionsCount = _context.Submissions
                    .Count(s => s.Tasks.CourseId == course.Id && s.Status == "Submitted");

                var coursePanel = CreateTeacherCoursePanel(course, taskCount, submissionsCount);
                UrgentTasksPanel.Children.Add(coursePanel);
            }

            if (UrgentTasksPanel.Children.Count == 0)
            {
                var emptyPanel = new TextBlock
                {
                    Text = "У вас пока нет курсов",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                };
                UrgentTasksPanel.Children.Add(emptyPanel);
            }
        }

        private Border CreateTeacherCoursePanel(Courses course, int taskCount, int submissionsCount)
        {
            var border = new Border
            {
                Width = 280,
                Height = 140,
                Margin = new Thickness(0, 0, 16, 16),
                Background = Brushes.White,
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#bdc3c7"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = course
            };

            border.MouseLeftButtonUp += (s, e) => ShowCourseDetails(course);

            var stackPanel = new StackPanel();

          
            stackPanel.Children.Add(new TextBlock
            {
                Text = course.Title.Length > 30 ? course.Title.Substring(0, 30) + "..." : course.Title,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50"),
                TextWrapping = TextWrapping.Wrap
            });

          
            if (!string.IsNullOrEmpty(course.Description))
            {
                var desc = course.Description.Length > 60 ? course.Description.Substring(0, 60) + "..." : course.Description;
                stackPanel.Children.Add(new TextBlock
                {
                    Text = desc,
                    FontSize = 12,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#7f8c8d"),
                    Margin = new Thickness(0, 4, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
            }

           
            var statsPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
            statsPanel.Children.Add(new TextBlock
            {
                Text = $"📝 Заданий: {taskCount}",
                FontSize = 12,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#3498db")
            });
            statsPanel.Children.Add(new TextBlock
            {
                Text = $"📨 На проверку: {submissionsCount}",
                FontSize = 12,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#e74c3c"),
                Margin = new Thickness(0, 2, 0, 0)
            });

            stackPanel.Children.Add(statsPanel);
            border.Child = stackPanel;
            return border;
        }

        private void LoadTasksToReview()
        {
            var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == _currentUser.Id);
            if (teacher == null) return;

            var teacherId = teacher.UserId;

            var submissionsToReview = _context.Submissions
                .Where(s => s.Tasks.TeacherUserId == teacherId &&
                           (s.Status == "Submitted" || s.Status == "Under Review"))
                .OrderBy(s => s.SubmittedAt)
                .Take(5)
                .ToList();

            if (submissionsToReview.Any())
            {
                var title = new TextBlock
                {
                    Text = $"📝 ЗАДАНИЯ НА ПРОВЕРКУ ({submissionsToReview.Count})",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50"),
                    Margin = new Thickness(0, 16, 0, 8)
                };
                ProgressPanel.Children.Add(title);

                foreach (var submission in submissionsToReview)
                {
                    var task = _context.Tasks.FirstOrDefault(t => t.Id == submission.TaskId);
                    var student = _context.Users.FirstOrDefault(u => u.Id == submission.StudentUserId);
                    int daysAgo = (int)(DateTime.Now - (DateTime)submission.SubmittedAt).TotalDays;

                    var submissionPanel = CreateSubmissionReviewPanel(submission, task, student, daysAgo);
                    ProgressPanel.Children.Add(submissionPanel);
                }
            }
            else
            {
                ProgressPanel.Children.Add(new TextBlock
                {
                    Text = "Нет заданий на проверку",
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                });
            }
        }

        private Border CreateSubmissionReviewPanel(Submissions submission, Tasks task, Users student, int daysAgo)
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#bdc3c7"),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12),
                CornerRadius = new CornerRadius(4),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = submission
            };

            border.MouseLeftButtonUp += (s, e) => ShowSubmissionForReview(submission);

            var stackPanel = new StackPanel();

            stackPanel.Children.Add(new TextBlock
            {
                Text = task?.Title ?? "Неизвестное задание",
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50")
            });

            stackPanel.Children.Add(new TextBlock
            {
                Text = $"👤 {student?.FullName ?? "Неизвестный студент"}",
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            });

            string timeText = daysAgo == 0 ? "Сегодня" : $"{daysAgo} дн. назад";
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"📅 Отправлено: {timeText}",
                FontSize = 11,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#95a5a6"),
                Margin = new Thickness(0, 4, 0, 0)
            });

            border.Child = stackPanel;
            return border;
        }

     
        private void LoadAdminDashboard()
        {
            UrgentTasksTitle.Text = "📊 СТАТИСТИКА СИСТЕМЫ";
            LoadSystemStatistics();
            LoadRecentActivity();
            LoadAdminNotifications();
        }

        private void LoadSystemStatistics()
        {
            int totalUsers = _context.Users.Count();
            int totalStudents = _context.Students.Count();
            int totalTeachers = _context.Teachers.Count();
            int totalCourses = _context.Courses.Count();
            int totalTasks = _context.Tasks.Count();
            int totalSubmissions = _context.Submissions.Count();

            var statsGrid = new WrapPanel();

            AddStatCard(statsGrid, "👥 ПОЛЬЗОВАТЕЛИ", totalUsers.ToString(), "#3498db");
            AddStatCard(statsGrid, "🎓 СТУДЕНТЫ", totalStudents.ToString(), "#2ecc71");
            AddStatCard(statsGrid, "👨‍🏫 ПРЕПОДАВАТЕЛИ", totalTeachers.ToString(), "#9b59b6");
            AddStatCard(statsGrid, "📚 КУРСЫ", totalCourses.ToString(), "#e74c3c");
            AddStatCard(statsGrid, "📝 ЗАДАНИЯ", totalTasks.ToString(), "#f39c12");
            AddStatCard(statsGrid, "📨 ОТПРАВКИ", totalSubmissions.ToString(), "#1abc9c");

            UrgentTasksPanel.Children.Add(statsGrid);
        }

        private void AddStatCard(Panel panel, string title, string value, string color)
        {
            var border = new Border
            {
                Width = 180,
                Height = 100,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 16, 16)
            };

            var stackPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stackPanel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            stackPanel.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            });

            border.Child = stackPanel;
            panel.Children.Add(border);
        }

        private void LoadRecentActivity()
        {
            var recentUsers = _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(3)
                .ToList();

            var activityPanel = new StackPanel();

            if (recentUsers.Any())
            {
                activityPanel.Children.Add(new TextBlock
                {
                    Text = "🆕 НОВЫЕ ПОЛЬЗОВАТЕЛИ",
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50"),
                    Margin = new Thickness(0, 0, 0, 8)
                });

                foreach (var user in recentUsers)
                {
                    activityPanel.Children.Add(new TextBlock
                    {
                        Text = $"• {user.FullName} ({user.Role}) - {user.CreatedAt:dd.MM.yyyy}",
                        FontSize = 12,
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                }
            }

            ProgressPanel.Children.Add(activityPanel);
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

            if (notifications.Count == 0)
            {
                NotificationsPanel.Children.Add(new TextBlock
                {
                    Text = "Нет новых уведомлений",
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                });
            }
        }

        private void LoadTeacherNotifications()
        {
            var notifications = _context.Notifications
                .Where(n => (n.UserId == _currentUser.Id) ||
                           (n.Type == "NewSubmission") ||
                           (n.Type == "GradeRequest"))
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToList();

            LoadNotificationsList(notifications);
        }

        private void LoadAdminNotifications()
        {
            var notifications = _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToList();

            LoadNotificationsList(notifications);
        }

        private void LoadNotificationsList(List<Notifications> notifications)
        {
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

                string icon = GetNotificationIcon(notification.Type);
                stackPanel.Children.Add(new TextBlock { Text = icon, FontSize = 14, Margin = new Thickness(0, 0, 8, 0) });
                stackPanel.Children.Add(new TextBlock
                {
                    Text = notification.Title,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50"),
                    TextWrapping = TextWrapping.Wrap
                });

                button.Content = stackPanel;
                NotificationsPanel.Children.Add(button);
            }

            if (notifications.Count == 0)
            {
                NotificationsPanel.Children.Add(new TextBlock
                {
                    Text = "Нет уведомлений",
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                });
            }
        }

        private string GetNotificationIcon(string type)
        {
            switch (type)
            {
                case "Grade":
                    return "✅";
                case "Comment":
                    return "📝";
                case "NewTask":
                    return "🆕";
                case "NewSubmission":
                    return "📨";
                case "GradeRequest":
                    return "📊";
                case "System":
                    return "⚙️";
                default:
                    return "🔔";
            }
        }

       
        private void ShowTaskDetails(Tasks task)
        {
            var course = _context.Courses.FirstOrDefault(c => c.Id == task.CourseId);
            MessageBox.Show($"Задание: {task.Title}\nКурс: {course?.Title ?? "Неизвестный курс"}\nДедлайн: {task.Deadline:dd.MM.yyyy}\n\n{task.Description}", "Детали задания");
        }

        private void ShowCourseDetails(Courses course)
        {
            MessageBox.Show($"Курс: {course.Title}\nОписание: {course.Description}", "Информация о курсе");
        }

        private void ShowSubmissionForReview(Submissions submission)
        {
            var task = _context.Tasks.FirstOrDefault(t => t.Id == submission.TaskId);
            var student = _context.Users.FirstOrDefault(u => u.Id == submission.StudentUserId);
            var course = _context.Courses.FirstOrDefault(c => c.Id == task.CourseId);

            MessageBox.Show($"Задание: {task?.Title}\nСтудент: {student?.FullName}\nКурс: {course?.Title}\nОтправлено: {submission.SubmittedAt:dd.MM.yyyy HH:mm}\n\nТекст ответа: {submission.TextContent}", "Проверка работы");
        }

        private void ShowNotificationDetails(Notifications notification)
        {
            MessageBox.Show(notification.Message, notification.Title);

            if (notification.UserId == _currentUser.Id && notification.IsRead == false)
            {
                notification.IsRead = true;
                _context.SaveChanges();
            }
        }
    }
}