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

        public SubmissionsView(Users user)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            _currentUser = user;
            LoadSubmissions();
            LoadFilters();
        }

        private void LoadSubmissions()
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
                                 
                                  Status = s.Status,
                                  Score = s.Score,
                                  TeacherComment = s.TeacherComment,
                                  TaskDescription = t.Description,
                                  Deadline = t.Deadline
                              }).ToList();

            GenerateSubmissionGroups();
        }

        private void LoadFilters()
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

        private void GenerateSubmissionGroups()
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
                CornerRadius = new CornerRadius(4)
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
                Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50")
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
                Tag = submission
            };
            detailsButton.Click += DetailsButton_Click;
            buttonPanel.Children.Add(detailsButton);

            Grid.SetColumn(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            card.Child = grid;
            return card;
        }

        private string GetStatusText(SubmissionModel submission)
        {
            switch (submission.Status)
            {
                case "Submitted":
                    return "📤 Отправлено на проверку";
                case "Under Review":
                    return "👨‍🏫 Проверяется";
                case "Completed":
                    return submission.Score.HasValue ? $"✅ Проверено | Оценка: {submission.Score}" : "✅ Проверено";
                case "Rejected":
                    return "❌ Отклонено";
                case "Returned":
                    return "🔴 Возвращено на доработку";
                default:
                    return submission.Status;
            }
        }

        private Brush GetStatusColor(string status)
        {
            var converter = new BrushConverter();
            switch (status)
            {
                case "Submitted":
                case "Under Review":
                    return (Brush)converter.ConvertFromString("#f39c12");
                case "Completed":
                    return (Brush)converter.ConvertFromString("#27ae60");
                case "Rejected":
                case "Returned":
                    return (Brush)converter.ConvertFromString("#e74c3c");
                default:
                    return (Brush)converter.ConvertFromString("#7f8c8d");
            }
        }

        private List<SubmissionModel> FilterSubmissions()
        {
            var filtered = allSubmissions.AsEnumerable();

            var selectedCourse = (CourseFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selectedCourse != "Все курсы" && !string.IsNullOrEmpty(selectedCourse))
            {
                filtered = filtered.Where(s => s.CourseName == selectedCourse);
            }

            var selectedStatus = (StatusFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selectedStatus != "Все статусы")
            {
                switch (selectedStatus)
                {
                    case "На проверке":
                        filtered = filtered.Where(s => s.Status == "Submitted" || s.Status == "Under Review");
                        break;
                    case "Проверено":
                        filtered = filtered.Where(s => s.Status == "Completed");
                        break;
                    case "Возвращено":
                        filtered = filtered.Where(s => s.Status == "Returned" || s.Status == "Rejected");
                        break;
                }
            }

            var selectedSort = (SortFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            switch (selectedSort)
            {
                case "По дате (новые)":
                    filtered = filtered.OrderByDescending(s => s.SubmittedDate);
                    break;
                case "По дате (старые)":
                    filtered = filtered.OrderBy(s => s.SubmittedDate);
                    break;
                case "По курсу":
                    filtered = filtered.OrderBy(s => s.CourseName);
                    break;
                case "По оценке":
                    filtered = filtered.OrderByDescending(s => s.Score ?? 0);
                    break;
            }

            return filtered.ToList();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
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

        private void ShowSubmissionDetails(SubmissionModel submission)
        {
            var files = _context.Files.Where(f => f.SubmissionId == submission.Id).ToList();

            string filesText = files.Any()
                ? string.Join("\n", files.Select(f => $"📎 {f.FileName}"))
                : "Файлы не прикреплены";

            string message = $"Задание: {submission.TaskTitle}\n" +
                           $"Курс: {submission.CourseName}\n" +
                           $"Отправлено: {submission.SubmittedDate:dd.MM.yyyy HH:mm}\n" +
                           $"Статус: {GetStatusText(submission)}\n" +
                           $"Комментарий преподавателя: {submission.TeacherComment ?? "Отсутствует"}\n\n" +
                           $"Прикрепленные файлы:\n{filesText}";

            MessageBox.Show(message, "Детали отправленной работы", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
}