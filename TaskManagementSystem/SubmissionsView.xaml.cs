using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskManagementSystem
{
    public partial class SubmissionsView : Page
    {
        private TaskManagementSystemEntities3 _context;
        private Users _currentUser;
        private List<SubmissionModel> allSubmissions;
        private bool isInitialized = false;

        public SubmissionsView(Users user)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            _currentUser = user;

          
            this.Loaded += (s, e) =>
            {
                isInitialized = true;
                LoadSubmissions();
                LoadFilters();
            };
        }

        private void LoadSubmissions()
        {
            if (!isInitialized) return;

            allSubmissions = new List<SubmissionModel>();

            switch (_currentUser.Role)
            {
                case "Student":
                    LoadStudentSubmissions();
                    break;
                case "Teacher":
                    LoadTeacherSubmissions();
                    break;
                case "Administrator":
                    LoadAllSubmissions();
                    break;
            }

            GenerateSubmissionGroups();
        }

        private void LoadStudentSubmissions()
        {
            var student = _context.Students.FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student == null) return;

            var studentId = student.UserId;

            allSubmissions = (from s in _context.Submissions
                              join t in _context.Tasks on s.TaskId equals t.Id
                              join c in _context.Courses on t.CourseId equals c.Id
                              where s.StudentUserId == studentId
                              select new SubmissionModel
                              {
                                  Id = s.Id,
                                  TaskTitle = t.Title,
                                  CourseName = c.Title,
                                  CourseId = c.Id,
                                  SubmittedDate = s.SubmittedAt ?? DateTime.MinValue,
                                  Status = s.Status,
                                  Score = s.Score,
                                  TeacherComment = s.TeacherComment,
                                  TaskDescription = t.Description,
                                  Deadline = t.Deadline,
                                  StudentName = _currentUser.FullName
                              }).ToList();
        }

        private void LoadTeacherSubmissions()
        {
            var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == _currentUser.Id);
            if (teacher == null) return;

            var teacherId = teacher.UserId;

    
            var teacherCourseIds = _context.Database.SqlQuery<int>(
                "SELECT CourseId FROM CourseTeachers WHERE TeacherUserId = {0}",
                teacherId).ToList();


            allSubmissions = (from s in _context.Submissions
                              join t in _context.Tasks on s.TaskId equals t.Id
                              join c in _context.Courses on t.CourseId equals c.Id
                              join u in _context.Users on s.StudentUserId equals u.Id
                              where teacherCourseIds.Contains(c.Id) && t.TeacherUserId == teacherId
                              select new SubmissionModel
                              {
                                  Id = s.Id,
                                  TaskTitle = t.Title,
                                  CourseName = c.Title,
                                  CourseId = c.Id,
                                  SubmittedDate = s.SubmittedAt ?? DateTime.MinValue,
                                  Status = s.Status,
                                  Score = s.Score,
                                  TeacherComment = s.TeacherComment,
                                  TaskDescription = t.Description,
                                  Deadline = t.Deadline,
                                  StudentName = u.FullName,
                                  StudentId = u.Id
                              }).ToList();
        }

        private void LoadAllSubmissions()
        {
       
            allSubmissions = (from s in _context.Submissions
                              join t in _context.Tasks on s.TaskId equals t.Id
                              join c in _context.Courses on t.CourseId equals c.Id
                              join u in _context.Users on s.StudentUserId equals u.Id
                              select new SubmissionModel
                              {
                                  Id = s.Id,
                                  TaskTitle = t.Title,
                                  CourseName = c.Title,
                                  CourseId = c.Id,
                                  SubmittedDate = s.SubmittedAt ?? DateTime.MinValue,
                                  Status = s.Status,
                                  Score = s.Score,
                                  TeacherComment = s.TeacherComment,
                                  TaskDescription = t.Description,
                                  Deadline = t.Deadline,
                                  StudentName = u.FullName,
                                  StudentId = u.Id
                              }).ToList();
        }

        private void LoadFilters()
        {
            if (!isInitialized) return;

            CourseFilter.Items.Clear();
            CourseFilter.Items.Add(new ComboBoxItem { Content = "Все курсы" });

            switch (_currentUser.Role)
            {
                case "Student":
                    LoadStudentFilters();
                    break;
                case "Teacher":
                    LoadTeacherFilters();
                    break;
                case "Administrator":
                    LoadAdminFilters();
                    break;
            }
        }

        private void LoadStudentFilters()
        {
            var student = _context.Students.FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student == null) return;

            var studentId = student.UserId;

            var studentCourses = _context.Database.SqlQuery<string>(
                "SELECT DISTINCT c.Title FROM Courses c INNER JOIN Tasks t ON c.Id = t.CourseId INNER JOIN Submissions s ON t.Id = s.TaskId WHERE s.StudentUserId = {0}",
                studentId).ToList();

            foreach (var course in studentCourses)
            {
                CourseFilter.Items.Add(new ComboBoxItem { Content = course });
            }
        }

        private void LoadTeacherFilters()
        {
            var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == _currentUser.Id);
            if (teacher == null) return;

            var teacherId = teacher.UserId;

            var teacherCourses = _context.Database.SqlQuery<string>(
                "SELECT DISTINCT c.Title FROM Courses c INNER JOIN CourseTeachers ct ON c.Id = ct.CourseId WHERE ct.TeacherUserId = {0}",
                teacherId).ToList();

            foreach (var course in teacherCourses)
            {
                CourseFilter.Items.Add(new ComboBoxItem { Content = course });
            }
        }

        private void LoadAdminFilters()
        {
            var allCourses = _context.Courses.Select(c => c.Title).Distinct().ToList();
            foreach (var course in allCourses)
            {
                CourseFilter.Items.Add(new ComboBoxItem { Content = course });
            }
        }

        private void GenerateSubmissionGroups()
        {
            if (!isInitialized || SubmissionsContainer == null) return;

            try
            {
                SubmissionsContainer.Children.Clear();

                var filteredSubmissions = FilterSubmissions();

                var underReview = filteredSubmissions.Where(s => s.Status == "Submitted" || s.Status == "Under Review").ToList();
                var completed = filteredSubmissions.Where(s => s.Status == "Completed").ToList();
                var returned = filteredSubmissions.Where(s => s.Status == "Returned" || s.Status == "Rejected").ToList();

                if (underReview.Any())
                    AddSubmissionGroup("🟡 НА ПРОВЕРКЕ", underReview, "#fff8e1");

                if (completed.Any())
                    AddSubmissionGroup("✅ ПРОВЕРЕННЫЕ", completed, "#e8f5e8");

                if (returned.Any())
                    AddSubmissionGroup("🔴 ВОЗВРАЩЕННЫЕ", returned, "#ffebee");

                UpdateSubmissionsVisibility();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке отправленных работ: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddSubmissionGroup(string groupTitle, List<SubmissionModel> submissions, string backgroundColor)
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
                Text = $"{groupTitle} ({submissions.Count})",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50"),
                Margin = new Thickness(0, 0, 0, 12)
            };
            stackPanel.Children.Add(headerText);

            foreach (var submission in submissions)
            {
                var submissionCard = CreateSubmissionCard(submission);
                stackPanel.Children.Add(submissionCard);
            }

            groupBorder.Child = stackPanel;
            SubmissionsContainer.Children.Add(groupBorder);
        }

        private Border CreateSubmissionCard(SubmissionModel submission)
        {
            var card = new Border
            {
                Background = Brushes.White,
                BorderBrush = (Brush)new BrushConverter().ConvertFrom("#bdc3c7"),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(4),
                Tag = submission
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
                Text = submission.TaskTitle,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50"),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(titleText, 0);

            var courseText = new TextBlock
            {
                Text = submission.CourseName,
                FontSize = 12,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#7f8c8d")
            };
            Grid.SetColumn(courseText, 1);

            titleGrid.Children.Add(titleText);
            titleGrid.Children.Add(courseText);
            leftPanel.Children.Add(titleGrid);

        
            if (_currentUser.Role != "Student" && !string.IsNullOrEmpty(submission.StudentName))
            {
                leftPanel.Children.Add(new TextBlock
                {
                    Text = $"👤 Студент: {submission.StudentName}",
                    FontSize = 12,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#3498db"),
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            var dateText = new TextBlock
            {
                Text = $"📅 Отправлено: {submission.SubmittedDate:dd.MM.yyyy HH:mm}",
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };
            leftPanel.Children.Add(dateText);

            var statusText = new TextBlock
            {
                Text = GetStatusText(submission),
                FontSize = 12,
                Foreground = GetStatusColor(submission.Status),
                Margin = new Thickness(0, 4, 0, 0),
                FontWeight = FontWeights.SemiBold
            };
            leftPanel.Children.Add(statusText);

            if (!string.IsNullOrEmpty(submission.TeacherComment))
            {
                var commentText = new TextBlock
                {
                    Text = $"💬 Комментарий: {submission.TeacherComment}",
                    FontSize = 11,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#7f8c8d"),
                    Margin = new Thickness(0, 4, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                leftPanel.Children.Add(commentText);
            }

            Grid.SetColumn(leftPanel, 0);
            grid.Children.Add(leftPanel);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var detailsButton = new Button
            {
                Content = "Подробнее",
                Background = (Brush)new BrushConverter().ConvertFrom("#3498db"),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(8, 0, 0, 0),
                Tag = submission,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            detailsButton.Click += DetailsButton_Click;
            buttonPanel.Children.Add(detailsButton);

          
            if (_currentUser.Role == "Teacher" && (submission.Status == "Submitted" || submission.Status == "Under Review"))
            {
                var gradeButton = new Button
                {
                    Content = "Оценить",
                    Background = (Brush)new BrushConverter().ConvertFrom("#27ae60"),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(8, 0, 0, 0),
                    Tag = submission,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                gradeButton.Click += GradeButton_Click;
                buttonPanel.Children.Add(gradeButton);
            }

            Grid.SetColumn(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            card.Child = grid;
            return card;
        }

        private string GetStatusText(SubmissionModel submission)
        {
            if (submission.Status == "Submitted")
                return "📤 Отправлено на проверку";
            else if (submission.Status == "Under Review")
                return "👨‍🏫 Проверяется";
            else if (submission.Status == "Completed")
            {
                if (submission.Score.HasValue)
                    return $"✅ Проверено | Оценка: {submission.Score}";
                else
                    return "✅ Проверено";
            }
            else if (submission.Status == "Rejected")
                return "❌ Отклонено";
            else if (submission.Status == "Returned")
                return "🔴 Возвращено на доработку";
            else
                return submission.Status;
        }

        private Brush GetStatusColor(string status)
        {
            var converter = new BrushConverter();
            if (status == "Submitted" || status == "Under Review")
                return (Brush)converter.ConvertFromString("#f39c12");
            else if (status == "Completed")
                return (Brush)converter.ConvertFromString("#27ae60");
            else if (status == "Rejected" || status == "Returned")
                return (Brush)converter.ConvertFromString("#e74c3c");
            else
                return (Brush)converter.ConvertFromString("#7f8c8d");
        }

        private List<SubmissionModel> FilterSubmissions()
        {
            var filtered = allSubmissions.AsEnumerable();

            var selectedCourse = (CourseFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedCourse != "Все курсы" && !string.IsNullOrEmpty(selectedCourse))
            {
                filtered = filtered.Where(s => s.CourseName == selectedCourse);
            }

            var selectedStatus = (StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedStatus != "Все статусы")
            {
                if (selectedStatus == "На проверке")
                    filtered = filtered.Where(s => s.Status == "Submitted" || s.Status == "Under Review");
                else if (selectedStatus == "Проверено")
                    filtered = filtered.Where(s => s.Status == "Completed");
                else if (selectedStatus == "Возвращено")
                    filtered = filtered.Where(s => s.Status == "Returned" || s.Status == "Rejected");
            }

            var selectedSort = (SortFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (selectedSort == "По дате (новые)")
                filtered = filtered.OrderByDescending(s => s.SubmittedDate);
            else if (selectedSort == "По дате (старые)")
                filtered = filtered.OrderBy(s => s.SubmittedDate);
            else if (selectedSort == "По курсу")
                filtered = filtered.OrderBy(s => s.CourseName);
            else if (selectedSort == "По оценке")
                filtered = filtered.OrderByDescending(s => s.Score ?? 0);

            return filtered.ToList();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (isInitialized)
                GenerateSubmissionGroups();
        }

        private void UpdateSubmissionsVisibility()
        {
            bool hasSubmissions = SubmissionsContainer.Children.Count > 0;
            NoSubmissionsMessage.Visibility = hasSubmissions ? Visibility.Collapsed : Visibility.Visible;
            SubmissionsContainer.Visibility = hasSubmissions ? Visibility.Visible : Visibility.Collapsed;
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var submission = button?.Tag as SubmissionModel;
            if (submission != null)
            {
                ShowSubmissionDetails(submission);
            }
        }

        private void GradeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var submission = button?.Tag as SubmissionModel;
            if (submission != null)
            {
                ShowGradeDialog(submission);
            }
        }

        private void ShowSubmissionDetails(SubmissionModel submission)
        {
            var files = _context.Files.Where(f => f.SubmissionId == submission.Id).ToList();

            string filesText = files.Any()
                ? string.Join("\n", files.Select(f => $"📎 {f.FileName}"))
                : "Файлы не прикреплены";

            string message = $"Задание: {submission.TaskTitle}\n" +
                           $"Курс: {submission.CourseName}\n";

            if (_currentUser.Role != "Student")
            {
                message += $"Студент: {submission.StudentName}\n";
            }

            message += $"Отправлено: {submission.SubmittedDate:dd.MM.yyyy HH:mm}\n" +
                       $"Статус: {GetStatusText(submission)}\n" +
                       $"Комментарий преподавателя: {submission.TeacherComment ?? "Отсутствует"}\n\n" +
                       $"Описание задания: {submission.TaskDescription ?? "Отсутствует"}\n\n" +
                       $"Прикрепленные файлы:\n{filesText}";

            MessageBox.Show(message, "Детали отправленной работы", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowGradeDialog(SubmissionModel submission)
        {
            var dbSubmission = _context.Submissions.FirstOrDefault(s => s.Id == submission.Id);
            if (dbSubmission != null)
            {
                var task = _context.Tasks.FirstOrDefault(t => t.Id == dbSubmission.TaskId);
                if (task != null)
                {
                    var reviewWindow = new TeacherReviewWindow(_currentUser, task);
                    reviewWindow.Owner = Window.GetWindow(this);
                    reviewWindow.ShowDialog();

                    LoadSubmissions();
                    GenerateSubmissionGroups();
                }
            }
        }
    }

    public class SubmissionModel
    {
        public int Id { get; set; }
        public string TaskTitle { get; set; }
        public string CourseName { get; set; }
        public int CourseId { get; set; }
        public DateTime SubmittedDate { get; set; }
        public string Status { get; set; }
        public decimal? Score { get; set; }
        public string TeacherComment { get; set; }
        public string TaskDescription { get; set; }
        public DateTime Deadline { get; set; }
        public string StudentName { get; set; }
        public int? StudentId { get; set; }
    }
}