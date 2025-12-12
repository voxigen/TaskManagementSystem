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

            switch (_currentUser.Role)
            {
                case "Student":
                    LoadStudentCourses(courses);
                    break;
                case "Teacher":
                    LoadTeacherCourses(courses);
                    break;
                case "Administrator":
                    LoadAllCourses(courses);
                    break;
            }

            CoursesItemsControl.ItemsSource = courses;
            UpdateCoursesVisibility();
        }

        private void LoadStudentCourses(List<CourseModel> courses)
        {
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

                    var nextAssignmentText = nextAssignment != null ?
                        $"Следующее задание: {((DateTime)nextAssignment.Deadline).ToString("dd.MM.yyyy")}" :
                        "Нет активных заданий";

                    courses.Add(new CourseModel
                    {
                        CourseId = course.Id,
                        CourseName = course.Title,
                        Instructor = teacher != null ? $"Преподаватель: {teacher.FullName}" : "Преподаватель не назначен",
                        NextAssignment = nextAssignmentText,
                        Progress = progress,
                        TotalAssignments = totalTasks,
                        CompletedAssignments = submittedTasks,
                        CardBackground = Brushes.White,
                        CourseDescription = course.Description,
                        StudentCount = GetStudentCount(course.Id),
                        TaskCount = totalTasks
                    });
                }
            }
        }

        private void LoadTeacherCourses(List<CourseModel> courses)
        {
            var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == _currentUser.Id);
            if (teacher != null)
            {
                var teacherId = teacher.UserId;

                var teacherCourseIds = _context.Database.SqlQuery<int>(
                    "SELECT CourseId FROM CourseTeachers WHERE TeacherUserId = {0}",
                    teacherId).ToList();

                var teacherCourses = _context.Courses
                    .Where(c => teacherCourseIds.Contains(c.Id))
                    .ToList();

                foreach (var course in teacherCourses)
                {
                    var courseTasks = _context.Tasks.Where(t => t.CourseId == course.Id).ToList();
                    var totalTasks = courseTasks.Count;

                    var tasksToReview = courseTasks.Count(t =>
                        _context.Submissions.Any(s => s.TaskId == t.Id &&
                            (s.Status == "Submitted" || s.Status == "Under Review")));

                    var totalSubmissions = courseTasks.Sum(t =>
                        _context.Submissions.Count(s => s.TaskId == t.Id));
                    var reviewedSubmissions = courseTasks.Sum(t =>
                        _context.Submissions.Count(s => s.TaskId == t.Id &&
                            (s.Status == "Completed" || s.Status == "Returned")));

                    var progress = totalSubmissions > 0 ?
                        (int)((double)reviewedSubmissions / totalSubmissions * 100) : 0;

                    var nextDeadline = courseTasks
                        .Where(t => t.Deadline > DateTime.Now)
                        .OrderBy(t => t.Deadline)
                        .FirstOrDefault();

                    var nextDeadlineText = nextDeadline != null ?
                        $"Ближайший дедлайн: {((DateTime)nextDeadline.Deadline).ToString("dd.MM.yyyy")}" :
                        "Нет активных дедлайнов";

                    courses.Add(new CourseModel
                    {
                        CourseId = course.Id,
                        CourseName = course.Title,
                        Instructor = "Вы (преподаватель)",
                        NextAssignment = nextDeadlineText,
                        Progress = progress,
                        TotalAssignments = totalTasks,
                        CompletedAssignments = reviewedSubmissions,
                        CardBackground = Brushes.White,
                        CourseDescription = course.Description,
                        StudentCount = GetStudentCount(course.Id),
                        TaskCount = totalTasks,
                        TasksToReview = tasksToReview,
                        IsTeacherCourse = true
                    });
                }
            }
        }

        private void LoadAllCourses(List<CourseModel> courses)
        {
            var allCourses = _context.Courses.ToList();

            foreach (var course in allCourses)
            {
                var courseTasks = _context.Tasks.Where(t => t.CourseId == course.Id).ToList();
                var totalTasks = courseTasks.Count;

                var studentCount = GetStudentCount(course.Id);
                var teacherCount = GetTeacherCount(course.Id);
                var submissionCount = courseTasks.Sum(t =>
                    _context.Submissions.Count(s => s.TaskId == t.Id));

                var nextDeadline = courseTasks
                    .Where(t => t.Deadline > DateTime.Now)
                    .OrderBy(t => t.Deadline)
                    .FirstOrDefault();

                var nextDeadlineText = nextDeadline != null ?
                    $"Ближайший дедлайн: {((DateTime)nextDeadline.Deadline).ToString("dd.MM.yyyy")}" :
                    "Нет активных дедлайнов";

                var teachers = _context.Database.SqlQuery<Users>(
                    "SELECT u.* FROM Users u INNER JOIN Teachers t ON u.Id = t.UserId INNER JOIN CourseTeachers ct ON t.UserId = ct.TeacherUserId WHERE ct.CourseId = {0}",
                    course.Id).ToList();

                var teachersText = teachers.Any() ?
                    string.Join(", ", teachers.Select(t => t.FullName)) :
                    "Преподаватели не назначены";

                courses.Add(new CourseModel
                {
                    CourseId = course.Id,
                    CourseName = course.Title,
                    Instructor = teachersText,
                    NextAssignment = nextDeadlineText,
                    Progress = 0,
                    TotalAssignments = totalTasks,
                    CompletedAssignments = 0,
                    CardBackground = Brushes.White,
                    CourseDescription = course.Description,
                    StudentCount = studentCount,
                    TaskCount = totalTasks,
                    TeacherCount = teacherCount,
                    IsAdminView = true
                });
            }
        }

        private int GetStudentCount(int courseId)
        {
            var count = _context.Database.SqlQuery<int?>(
                "SELECT COUNT(*) FROM CourseStudents WHERE CourseId = {0}",
                courseId).FirstOrDefault();

            return count ?? 0;
        }

        private int GetTeacherCount(int courseId)
        {
            var count = _context.Database.SqlQuery<int?>(
                "SELECT COUNT(*) FROM CourseTeachers WHERE CourseId = {0}",
                courseId).FirstOrDefault();

            return count ?? 0;
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
                LoadCourses();
            }
            else
            {
                var allCourses = GetCoursesForCurrentRole();
                var filteredCourses = allCourses.FindAll(c =>
                    c.CourseName.ToLower().Contains(searchText) ||
                    c.Instructor.ToLower().Contains(searchText) ||
                    (c.CourseDescription != null && c.CourseDescription.ToLower().Contains(searchText)));

                CoursesItemsControl.ItemsSource = filteredCourses;
                UpdateCoursesVisibility();
            }
        }

        private List<CourseModel> GetCoursesForCurrentRole()
        {
            var courses = new List<CourseModel>();

            switch (_currentUser.Role)
            {
                case "Student":
                    LoadStudentCourses(courses);
                    break;
                case "Teacher":
                    LoadTeacherCourses(courses);
                    break;
                case "Administrator":
                    LoadAllCourses(courses);
                    break;
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
                string message = $"Курс: {course.CourseName}\n";

                if (_currentUser.Role == "Student")
                {
                    message += $"Преподаватель: {course.Instructor}\n";
                    message += $"Прогресс: {course.Progress}%\n";
                    message += $"Заданий выполнено: {course.CompletedAssignments}/{course.TotalAssignments}";
                }
                else if (_currentUser.Role == "Teacher")
                {
                    message += $"Студентов: {course.StudentCount}\n";
                    message += $"Заданий: {course.TaskCount}\n";
                    message += $"На проверку: {course.TasksToReview}";
                }
                else if (_currentUser.Role == "Administrator")
                {
                    message += $"Преподавателей: {course.TeacherCount}\n";
                    message += $"Студентов: {course.StudentCount}\n";
                    message += $"Заданий: {course.TaskCount}";
                }

                MessageBox.Show(message, "Информация о курсе");
            }
        }

        private void CourseCard_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var course = border?.DataContext as CourseModel;
            if (course != null)
            {
                string description = course.CourseDescription;
                if (string.IsNullOrEmpty(description))
                    description = "Описание отсутствует";

                MessageBox.Show($"Курс: {course.CourseName}\n\nОписание:\n{description}", "Описание курса");
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
        public string CourseDescription { get; set; }
        public int StudentCount { get; set; }
        public int TeacherCount { get; set; }
        public int TaskCount { get; set; }
        public int TasksToReview { get; set; }
        public bool IsTeacherCourse { get; set; }
        public bool IsAdminView { get; set; }

        public string ProgressText
        {
            get
            {
                if (IsAdminView)
                    return $"Заданий: {TotalAssignments}";

                if (IsTeacherCourse)
                    return $"{Progress}% проверено ({CompletedAssignments} работ)";

                return $"{Progress}% ({CompletedAssignments}/{TotalAssignments})";
            }
        }

        public double ProgressWidth => Progress * 3;

        public Brush ProgressColor
        {
            get
            {
                if (IsAdminView)
                    return Brushes.Transparent;

                if (Progress >= 80)
                    return Brushes.Lavender;
                else if (Progress >= 60)
                    return Brushes.DarkBlue;
                else if (Progress >= 40)
                    return Brushes.Firebrick;
                else
                    return Brushes.ForestGreen;
            }
        }

        public Brush CardBackground { get; set; } = Brushes.White;

        public string AdditionalInfo
        {
            get
            {
                if (IsAdminView)
                    return $"👥 {StudentCount} студентов | 👨‍🏫 {TeacherCount} преподавателей";

                if (IsTeacherCourse)
                    return $"👥 {StudentCount} студентов | 📝 {TasksToReview} на проверку";

                return $"👥 {StudentCount} студентов в курсе";
            }
        }
    }
}