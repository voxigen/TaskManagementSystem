using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskManagementSystem
{
    public partial class CoursesView : Page
    {
        private TaskManagementSystemEntities3 _context;
        private Users _currentUser;

        public CoursesView(Users user)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            _currentUser = user;
            LoadCourses();
            SearchBox.TextChanged += SearchBox_TextChanged;
        }

        private void LoadCourses()
        {
            var courses = new List<CourseModel>();

            var student = _context.Students.FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student != null)
            {
                var studentId = student.UserId;

                var studentCourseIds = _context.Database.SqlQuery<int>(
                    "SELECT CourseId FROM CourseStudents WHERE StudentUserId = {0}",
                    studentId).ToList();

                var studentCourses = _context.Courses
                    .Where(c => studentCourseIds.Contains(c.Id))
                    .ToList();

                foreach (var course in studentCourses)
                {
                    var courseTasks = _context.Tasks.Where(t => t.CourseId == course.Id).ToList();
                    var submittedTasks = courseTasks.Count(t => _context.Submissions.Any(s => s.TaskId == t.Id && s.StudentUserId == studentId));
                    var totalTasks = courseTasks.Count;
                    var progress = totalTasks > 0 ? (int)((double)submittedTasks / totalTasks * 100) : 0;

                    var nextAssignment = courseTasks
                        .Where(t => t.Deadline > DateTime.Now && !_context.Submissions.Any(s => s.TaskId == t.Id && s.StudentUserId == studentId))
                        .OrderBy(t => t.Deadline)
                        .FirstOrDefault();

                    var teacher = _context.Database.SqlQuery<Users>(
                        "SELECT u.* FROM Users u INNER JOIN Teachers t ON u.Id = t.UserId INNER JOIN CourseTeachers ct ON t.UserId = ct.TeacherUserId WHERE ct.CourseId = {0}",
                        course.Id).FirstOrDefault();

                    courses.Add(new CourseModel
                    {
                        CourseId = course.Id,
                        CourseName = course.Title,
                        Instructor = teacher != null ? $"Преподаватель: {teacher.FullName}" : "Преподаватель не назначен",
                        NextAssignment = nextAssignment != null ? $"Следующее задание: {nextAssignment.Deadline:dd.MM.yyyy}" : "Нет активных заданий",
                        Progress = progress,
                        TotalAssignments = totalTasks,
                        CompletedAssignments = submittedTasks,
                        CardBackground = Brushes.White
                    });
                }
            }

            CoursesItemsControl.ItemsSource = courses;
            UpdateCoursesVisibility();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterCourses();
        }

        private void FilterCourses()
        {
            string searchText = SearchBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText) || searchText == "поиск...")
            {
                CoursesItemsControl.ItemsSource = GetCoursesFromDatabase();
            }
            else
            {
                var filteredCourses = GetCoursesFromDatabase().FindAll(c =>
                    c.CourseName.ToLower().Contains(searchText) ||
                    c.Instructor.ToLower().Contains(searchText));
                CoursesItemsControl.ItemsSource = filteredCourses;
            }

            UpdateCoursesVisibility();
        }

        private List<CourseModel> GetCoursesFromDatabase()
        {
            var courses = new List<CourseModel>();

            var student = _context.Students.FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student != null)
            {
                var studentId = student.UserId;

                var studentCourseIds = _context.Database.SqlQuery<int>(
                    "SELECT CourseId FROM CourseStudents WHERE StudentUserId = {0}",
                    studentId).ToList();

                var studentCourses = _context.Courses
                    .Where(c => studentCourseIds.Contains(c.Id))
                    .ToList();

                foreach (var course in studentCourses)
                {
                    var courseTasks = _context.Tasks.Where(t => t.CourseId == course.Id).ToList();
                    var submittedTasks = courseTasks.Count(t => _context.Submissions.Any(s => s.TaskId == t.Id && s.StudentUserId == studentId));
                    var totalTasks = courseTasks.Count;
                    var progress = totalTasks > 0 ? (int)((double)submittedTasks / totalTasks * 100) : 0;

                    var nextAssignment = courseTasks
                        .Where(t => t.Deadline > DateTime.Now && !_context.Submissions.Any(s => s.TaskId == t.Id && s.StudentUserId == studentId))
                        .OrderBy(t => t.Deadline)
                        .FirstOrDefault();

                    var teacher = _context.Database.SqlQuery<Users>(
                        "SELECT u.* FROM Users u INNER JOIN Teachers t ON u.Id = t.UserId INNER JOIN CourseTeachers ct ON t.UserId = ct.TeacherUserId WHERE ct.CourseId = {0}",
                        course.Id).FirstOrDefault();

                    courses.Add(new CourseModel
                    {
                        CourseId = course.Id,
                        CourseName = course.Title,
                        Instructor = teacher != null ? $"Преподаватель: {teacher.FullName}" : "Преподаватель не назначен",
                        NextAssignment = nextAssignment != null ? $"Следующее задание: {nextAssignment.Deadline:dd.MM.yyyy}" : "Нет активных заданий",
                        Progress = progress,
                        TotalAssignments = totalTasks,
                        CompletedAssignments = submittedTasks,
                        CardBackground = Brushes.White
                    });
                }
            }

            return courses;
        }

        private void UpdateCoursesVisibility()
        {
            var items = CoursesItemsControl.ItemsSource as List<CourseModel>;
            if (items == null || items.Count == 0)
            {
                NoCoursesMessage.Visibility = Visibility.Visible;
                CoursesItemsControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoCoursesMessage.Visibility = Visibility.Collapsed;
                CoursesItemsControl.Visibility = Visibility.Visible;
            }
        }

        private void GoToCourse_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var course = button?.DataContext as CourseModel;
            if (course != null)
            {
                MessageBox.Show($"Переход к курсу: {course.CourseName}", "Курс");
            }
        }

        private void CourseCard_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var course = border?.DataContext as CourseModel;
            if (course != null)
            {
                MessageBox.Show($"Детали курса: {course.CourseName}\nПреподаватель: {course.Instructor}\nПрогресс: {course.Progress}%", "Детали курса");
            }
        }
    }

    public class CourseModel
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public string Instructor { get; set; }
        public string NextAssignment { get; set; }
        public int Progress { get; set; }
        public int TotalAssignments { get; set; }
        public int CompletedAssignments { get; set; }

        public string ProgressText => $"{Progress}% ({CompletedAssignments}/{TotalAssignments})";
        public double ProgressWidth => Progress * 3;
        public Brush ProgressColor => Progress >= 80 ? Brushes.Lavender :
                                     Progress >= 60 ? Brushes.DarkBlue :
                                     Progress >= 40 ? Brushes.Firebrick : Brushes.ForestGreen;
        public Brush CardBackground { get; set; } = Brushes.White;
    }
}