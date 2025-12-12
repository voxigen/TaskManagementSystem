using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskManagementSystem
{
    public partial class TasksView : Page
    {
        private TaskManagementSystemEntities3 _context;
        private Users _currentUser;
        private List<TaskModel> allTasks;
        private bool isInitialized = false;

        public TasksView(Users user)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            _currentUser = user;
            LoadTasks();

            this.Loaded += (s, e) =>
            {
                isInitialized = true;
                GenerateTaskGroups();
            };
        }

        private void LoadTasks()
        {
            allTasks = new List<TaskModel>();

            switch (_currentUser.Role)
            {
                case "Student":
                    LoadStudentTasks();
                    break;
                case "Teacher":
                    LoadTeacherTasks();
                    break;
                case "Administrator":
                    LoadAllTasks();
                    break;
                default:
                    LoadSampleTasks(); 
                    break;
            }
        }

        private void LoadStudentTasks()
        {
            var student = _context.Students.FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student == null) return;

            var studentId = student.UserId;

         
            var studentCourseIds = _context.Database.SqlQuery<int>(
                "SELECT CourseId FROM CourseStudents WHERE StudentUserId = {0}",
                studentId).ToList();

          
            var tasks = _context.Tasks
                .Where(t => studentCourseIds.Contains(t.CourseId))
                .ToList();

            foreach (var task in tasks)
            {
                var course = _context.Courses.FirstOrDefault(c => c.Id == task.CourseId);
                var submission = _context.Submissions
                    .FirstOrDefault(s => s.TaskId == task.Id && s.StudentUserId == studentId);

                int daysLeft = (int)(task.Deadline - DateTime.Now).TotalDays;

                var status = GetTaskStatus(task, submission, daysLeft);

                allTasks.Add(new TaskModel
                {
                    Id = task.Id,
                    Title = task.Title,
                    Course = course?.Title ?? "Неизвестный курс",
                    Description = task.Description,
                    DueDate = task.Deadline,
                    Status = status,
                    StatusText = GetStatusText(status, daysLeft, submission, task),
                    DaysLeft = daysLeft,
                    CanSubmit = status == TaskStatus.Active || status == TaskStatus.Overdue,
                    CanReview = submission != null &&
                               (submission.Status == "Completed" || submission.Status == "Returned"),
                    SubmittedDate = submission?.SubmittedAt,
                    Grade = submission?.Score,
                    MaxScore = task.MaxScore,
                    SubmissionId = submission?.Id
                });
            }
        }

        private void LoadTeacherTasks()
        {
            var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == _currentUser.Id);
            if (teacher == null) return;

            var teacherId = teacher.UserId;

           
            var tasks = _context.Tasks
                .Where(t => t.TeacherUserId == teacherId)
                .ToList();

            foreach (var task in tasks)
            {
                var course = _context.Courses.FirstOrDefault(c => c.Id == task.CourseId);
                var submissionCount = _context.Submissions.Count(s => s.TaskId == task.Id);
                var reviewedCount = _context.Submissions.Count(s => s.TaskId == task.Id &&
                    (s.Status == "Completed" || s.Status == "Returned"));

                int daysLeft = (int)(task.Deadline - DateTime.Now).TotalDays;

                var status = daysLeft < 0 ? TaskStatus.Overdue : TaskStatus.Active;

                allTasks.Add(new TaskModel
                {
                    Id = task.Id,
                    Title = task.Title,
                    Course = course?.Title ?? "Неизвестный курс",
                    Description = task.Description,
                    DueDate = task.Deadline,
                    Status = status,
                    StatusText = $"📨 {submissionCount} отправок | ✅ {reviewedCount} проверено",
                    DaysLeft = daysLeft,
                    CanSubmit = false, 
                    CanReview = submissionCount > 0,
                    SubmissionCount = submissionCount,
                    ReviewedCount = reviewedCount,
                    MaxScore = task.MaxScore,
                    IsTeacherTask = true,
                    TeacherId = teacherId
                });
            }
        }

        private void LoadAllTasks()
        {
        
            var tasks = _context.Tasks.ToList();

            foreach (var task in tasks)
            {
                var course = _context.Courses.FirstOrDefault(c => c.Id == task.CourseId);
                var teacher = _context.Users.FirstOrDefault(u => u.Id == task.TeacherUserId);
                var submissionCount = _context.Submissions.Count(s => s.TaskId == task.Id);

                int daysLeft = (int)(task.Deadline - DateTime.Now).TotalDays;

                var status = daysLeft < 0 ? TaskStatus.Overdue : TaskStatus.Active;

                allTasks.Add(new TaskModel
                {
                    Id = task.Id,
                    Title = task.Title,
                    Course = course?.Title ?? "Неизвестный курс",
                    Description = task.Description,
                    DueDate = task.Deadline,
                    Status = status,
                    StatusText = $"👨‍🏫 {teacher?.FullName ?? "Неизвестный преподаватель"} | 📨 {submissionCount} отправок",
                    DaysLeft = daysLeft,
                    CanSubmit = false,
                    CanReview = false,
                    IsAdminView = true,
                    TeacherName = teacher?.FullName,
                    SubmissionCount = submissionCount
                });
            }
        }

        private TaskStatus GetTaskStatus(Tasks task, Submissions submission, int daysLeft)
        {
            if (submission != null)
            {
                if (submission.Status == "Completed" || submission.Status == "Returned")
                    return TaskStatus.Completed;
                else
                    return TaskStatus.Active;
            }

            return daysLeft < 0 ? TaskStatus.Overdue : TaskStatus.Active;
        }

        private string GetStatusText(TaskStatus status, int daysLeft, Submissions submission, Tasks task)
        {
            switch (status)
            {
                case TaskStatus.Completed:
                    if (submission?.Score != null)
                    {
                        return $"✅ Проверено | Оценка: {submission.Score}" +
                               (task.MaxScore.HasValue ? $"/{task.MaxScore}" : "");
                    }
                    return "✅ Проверено";
                case TaskStatus.Overdue:
                    return $"🔴 Просрочено на {-daysLeft} дней";
                case TaskStatus.Active:
                    if (daysLeft == 0) return "🟡 Сегодня";
                    if (daysLeft == 1) return "🟡 Завтра";
                    return $"🟡 Осталось {daysLeft} дней";
                default:
                    return "Неизвестно";
            }
        }

        private void GenerateTaskGroups()
        {
            if (!isInitialized || TasksContainer == null)
                return;

            try
            {
                TasksContainer.Children.Clear();

                var filteredTasks = FilterTasks();

                var overdueTasks = filteredTasks.Where(t => t.Status == TaskStatus.Overdue).ToList();
                var activeTasks = filteredTasks.Where(t => t.Status == TaskStatus.Active).ToList();
                var completedTasks = filteredTasks.Where(t => t.Status == TaskStatus.Completed).ToList();

                if (overdueTasks.Any())
                    AddTaskGroup("🔴 ПРОСРОЧЕНО", overdueTasks, "#ffebee");

                if (activeTasks.Any())
                    AddTaskGroup("🟡 АКТИВНЫЕ", activeTasks, "#fff8e1");

                if (completedTasks.Any())
                    AddTaskGroup("✅ ВЫПОЛНЕННЫЕ", completedTasks, "#e8f5e8");

                UpdateTasksVisibility();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации групп заданий: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTaskGroup(string groupTitle, List<TaskModel> tasks, string backgroundColor)
        {
            var groupBorder = new Border
            {
                Style = (Style)FindResource("CardStyle"),
                Margin = new Thickness(0, 0, 0, 16),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundColor))
            };

            var stackPanel = new StackPanel();

            var headerText = new TextBlock
            {
                Text = $"{groupTitle} ({tasks.Count})",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50"),
                Margin = new Thickness(0, 0, 0, 12)
            };
            stackPanel.Children.Add(headerText);

            foreach (var task in tasks)
            {
                var taskCard = CreateTaskCard(task);
                stackPanel.Children.Add(taskCard);
            }

            groupBorder.Child = stackPanel;
            TasksContainer.Children.Add(groupBorder);
        }

        private Border CreateTaskCard(TaskModel task)
        {
            var card = new Border
            {
                Background = Brushes.White,
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#bdc3c7"),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(4),
                Tag = task
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var leftPanel = new StackPanel();

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = task.Title,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50"),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(titleText, 0);

            var courseText = new TextBlock
            {
                Text = task.Course,
                FontSize = 12,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#7f8c8d")
            };
            Grid.SetColumn(courseText, 1);

            titleGrid.Children.Add(titleText);
            titleGrid.Children.Add(courseText);
            leftPanel.Children.Add(titleGrid);

            var dueDateText = new TextBlock
            {
                Text = $"📅 Срок: {task.DueDate:dd.MM.yyyy}",
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };
            leftPanel.Children.Add(dueDateText);

            var statusText = new TextBlock
            {
                Text = task.StatusText,
                FontSize = 12,
                Foreground = GetStatusColor(task.Status),
                Margin = new Thickness(0, 4, 0, 0),
                FontWeight = FontWeights.SemiBold
            };
            leftPanel.Children.Add(statusText);

            if (task.IsTeacherTask && task.SubmissionCount > 0)
            {
                var reviewText = new TextBlock
                {
                    Text = $"📊 {task.SubmissionCount} отправок, {task.ReviewedCount} проверено",
                    FontSize = 11,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#3498db"),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                leftPanel.Children.Add(reviewText);
            }

            if (task.IsAdminView && !string.IsNullOrEmpty(task.TeacherName))
            {
                var teacherText = new TextBlock
                {
                    Text = $"👨‍🏫 {task.TeacherName}",
                    FontSize = 11,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#9b59b6"),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                leftPanel.Children.Add(teacherText);
            }

            Grid.SetColumn(leftPanel, 0);
            grid.Children.Add(leftPanel);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (task.CanSubmit && _currentUser.Role == "Student")
            {
                var submitButton = new Button
                {
                    Content = "Сдать работу",
                    Background = (Brush)new BrushConverter().ConvertFrom("#27ae60"),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(8, 0, 0, 0),
                    Tag = task,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                submitButton.Click += SubmitWork_Click;
                buttonPanel.Children.Add(submitButton);
            }

            if (task.CanReview && _currentUser.Role == "Student")
            {
                var reviewButton = new Button
                {
                    Content = "Просмотреть",
                    Background = (Brush)new BrushConverter().ConvertFrom("#3498db"),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(8, 0, 0, 0),
                    Tag = task,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                reviewButton.Click += ReviewWork_Click;
                buttonPanel.Children.Add(reviewButton);
            }

            if (task.IsTeacherTask && task.SubmissionCount > 0 && _currentUser.Role == "Teacher")
            {
                var gradeButton = new Button
                {
                    Content = "Проверить",
                    Background = (Brush)new BrushConverter().ConvertFrom("#e74c3c"),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(8, 0, 0, 0),
                    Tag = task,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                gradeButton.Click += GradeWork_Click;
                buttonPanel.Children.Add(gradeButton);
            }

            Grid.SetColumn(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            card.Child = grid;
            return card;
        }

        private Brush GetStatusColor(TaskStatus status)
        {
            var converter = new BrushConverter();
            switch (status)
            {
                case TaskStatus.Overdue:
                    return (Brush)converter.ConvertFromString("#e74c3c");
                case TaskStatus.Active:
                    return (Brush)converter.ConvertFromString("#f39c12");
                case TaskStatus.Completed:
                    return (Brush)converter.ConvertFromString("#27ae60");
                default:
                    return (Brush)converter.ConvertFromString("#7f8c8d");
            }
        }

        private List<TaskModel> FilterTasks()
        {
            if (ActiveTasksFilter == null || OverdueTasksFilter == null || CourseFilter == null)
                return allTasks;

            var filtered = allTasks.AsEnumerable();

            if (ActiveTasksFilter.IsChecked == true)
                filtered = filtered.Where(t => t.Status == TaskStatus.Active);
            else if (OverdueTasksFilter.IsChecked == true)
                filtered = filtered.Where(t => t.Status == TaskStatus.Overdue);

            var selectedCourse = (CourseFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedCourse != "Все курсы" && !string.IsNullOrEmpty(selectedCourse))
                filtered = filtered.Where(t => t.Course == selectedCourse);

            var selectedStatus = (StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedStatus != "Все статусы")
            {
                switch (selectedStatus)
                {
                    case "На проверке":
                        filtered = filtered.Where(t => t.Status == TaskStatus.Active && !t.IsTeacherTask);
                        break;
                    case "Проверено":
                        filtered = filtered.Where(t => t.Status == TaskStatus.Completed);
                        break;
                    case "Не сдано":
                        filtered = filtered.Where(t => t.Status == TaskStatus.Overdue);
                        break;
                }
            }

            var selectedSort = (SortFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            switch (selectedSort)
            {
                case "По дате (новые)":
                    filtered = filtered.OrderByDescending(t => t.DueDate);
                    break;
                case "По дате (старые)":
                    filtered = filtered.OrderBy(t => t.DueDate);
                    break;
                case "По курсу":
                    filtered = filtered.OrderBy(t => t.Course);
                    break;
                case "По статусу":
                    filtered = filtered.OrderBy(t => t.Status);
                    break;
            }

            return filtered.ToList();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (isInitialized)
                GenerateTaskGroups();
        }

        private void UpdateTasksVisibility()
        {
            bool hasTasks = TasksContainer.Children.Count > 0;
            NoTasksMessage.Visibility = hasTasks ? Visibility.Collapsed : Visibility.Visible;
            TasksContainer.Visibility = hasTasks ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SubmitWork_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.Tag as TaskModel;
            if (task != null)
            {
                MessageBox.Show($"Сдача работы: {task.Title}\nКурс: {task.Course}\n\nЭта функция в разработке.", "Сдача работы");
            }
        }

        private void ReviewWork_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.Tag as TaskModel;
            if (task != null && task.SubmissionId.HasValue)
            {
                var submission = _context.Submissions.FirstOrDefault(s => s.Id == task.SubmissionId);
                if (submission != null)
                {
                    string message = $"Работа: {task.Title}\n" +
                                   $"Курс: {task.Course}\n" +
                                   $"Оценка: {submission.Score ?? 0}" +
                                   (task.MaxScore.HasValue ? $"/{task.MaxScore}" : "") + "\n" +
                                   $"Комментарий: {submission.TeacherComment ?? "Нет комментария"}\n" +
                                   $"Статус: {submission.Status}";

                    MessageBox.Show(message, "Просмотр работы");
                }
            }
        }

        private void GradeWork_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.Tag as TaskModel;
            if (task != null)
            {
                MessageBox.Show($"Проверка работ по заданию: {task.Title}\n" +
                              $"Курс: {task.Course}\n" +
                              $"Отправок на проверку: {task.SubmissionCount}\n\n" +
                              "Эта функция в разработке.", "Проверка работ");
            }
        }

        private void LoadSampleTasks()
        {
       
            allTasks = new List<TaskModel>
            {
                new TaskModel
                {
                    Id = 1,
                    Title = "Лаб.работа #1",
                    Course = "МДК 01.03",
                    DueDate = new DateTime(2024, 5, 1),
                    Status = TaskStatus.Overdue,
                    StatusText = "Просрочено на 5 дней",
                    DaysLeft = -5,
                    CanSubmit = true
                },
                new TaskModel
                {
                    Id = 2,
                    Title = "Тест #2",
                    Course = "ОП.05 БД",
                    DueDate = new DateTime(2024, 5, 10),
                    Status = TaskStatus.Overdue,
                    StatusText = "Просрочено на 2 дня",
                    DaysLeft = -2,
                    CanSubmit = true
                },
                new TaskModel
                {
                    Id = 3,
                    Title = "Проект БД",
                    Course = "ОП.05 БД",
                    DueDate = DateTime.Now.AddDays(3),
                    Status = TaskStatus.Active,
                    StatusText = "Осталось 3 дня",
                    DaysLeft = 3,
                    CanSubmit = true
                }
            };
        }
    }

    public class TaskModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Course { get; set; }
        public string Description { get; set; }
        public DateTime DueDate { get; set; }
        public TaskStatus Status { get; set; }
        public string StatusText { get; set; }
        public int DaysLeft { get; set; }
        public bool CanSubmit { get; set; }
        public bool CanReview { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public decimal? Grade { get; set; }
        public int? MaxScore { get; set; }
        public int? SubmissionId { get; set; }
        public int SubmissionCount { get; set; }
        public int ReviewedCount { get; set; }
        public string TeacherName { get; set; }
        public bool IsTeacherTask { get; set; }
        public bool IsAdminView { get; set; }
        public int? TeacherId { get; set; }
    }

    public enum TaskStatus
    {
        Overdue,
        Active,
        Completed
    }
}