using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows.Media;

namespace TaskManagementSystem
{
    public class DashboardFacade
    {
        private TaskManagementSystemEntities3 _context;
        private Users _currentUser;

        public DashboardFacade(TaskManagementSystemEntities3 context, Users user)
        {
            _context = context;
            _currentUser = user;
        }

        public DashboardData GetDashboardData()
        {
            var data = new DashboardData
            {
                WelcomeMessage = $"Добро пожаловать, {_currentUser.FullName}!",
                DateMessage = $"Сегодня: {DateTime.Now:dddd, dd MMMM yyyy года}"
            };

            switch (_currentUser.Role)
            {
                case "Student":
                    LoadStudentData(data);
                    break;
                case "Teacher":
                    LoadTeacherData(data);
                    break;
                case "Administrator":
                    LoadAdminData(data);
                    break;
            }

            return data;
        }

        private void LoadStudentData(DashboardData data)
        {
            var student = _context.Students.FirstOrDefault(s => s.UserId == _currentUser.Id);
            if (student == null) return;

            var studentId = student.UserId;

            data.UrgentTasks = GetStudentUrgentTasks(studentId);
            data.CourseProgresses = GetStudentCourseProgress(studentId);
            data.Notifications = GetNotifications();
        }

        private void LoadTeacherData(DashboardData data)
        {
            var teacher = _context.Teachers.FirstOrDefault(t => t.UserId == _currentUser.Id);
            if (teacher == null) return;

            var teacherId = teacher.UserId;

            data.TeacherCourses = GetTeacherCourses(teacherId);
            data.SubmissionsToReview = GetSubmissionsToReview(teacherId);
            data.TeacherNotifications = GetTeacherNotifications();
        }

        private void LoadAdminData(DashboardData data)
        {
            data.SystemStatistics = GetSystemStatistics();
            data.RecentActivities = GetRecentActivities();
            data.AdminNotifications = GetAdminNotifications();
        }

        private List<UrgentTask> GetStudentUrgentTasks(int studentId)
        {
            var studentCourseIds = _context.Database.SqlQuery<int>(
                "SELECT CourseId FROM CourseStudents WHERE StudentUserId = {0}",
                studentId).ToList();

            var deadlineThreshold = DateTime.Now.AddDays(7);

            return _context.Tasks
                .Where(t => studentCourseIds.Contains(t.CourseId) &&
                           t.Deadline <= deadlineThreshold &&
                           !t.Submissions.Any(s => s.StudentUserId == studentId))
                .OrderBy(t => t.Deadline)
                .Take(5)
                .Select(t => new UrgentTask
                {
                    Task = t,
                    Course = _context.Courses.FirstOrDefault(c => c.Id == t.CourseId),
                    DaysLeft = (int)((DateTime)t.Deadline - DateTime.Now).TotalDays
                }).ToList();
        }

        private List<CourseProgress> GetStudentCourseProgress(int studentId)
        {
            var studentCourses = _context.Database.SqlQuery<Courses>(
                "SELECT c.* FROM Courses c INNER JOIN CourseStudents cs ON c.Id = cs.CourseId WHERE cs.StudentUserId = {0}",
                studentId).ToList();

            var progressList = new List<CourseProgress>();

            foreach (var course in studentCourses)
            {
                var courseTasks = _context.Tasks.Where(t => t.CourseId == course.Id).ToList();
                var submittedTasks = courseTasks.Count(t => _context.Submissions.Any(s => s.TaskId == t.Id && s.StudentUserId == studentId));
                var totalTasks = courseTasks.Count;
                var progress = totalTasks > 0 ? (int)((double)submittedTasks / totalTasks * 100) : 0;

                progressList.Add(new CourseProgress
                {
                    CourseTitle = course.Title,
                    Progress = progress,
                    SubmittedTasks = submittedTasks,
                    TotalTasks = totalTasks
                });
            }

            return progressList;
        }

        private List<NotificationItem> GetNotifications()
        {
            return _context.Notifications
                .Where(n => n.UserId == _currentUser.Id && n.IsRead == false)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new NotificationItem
                {
                    Notification = n,
                    Icon = GetNotificationIcon(n.Type)
                }).ToList();
        }

        private List<TeacherCourse> GetTeacherCourses(int teacherId)
        {
            var teacherCourses = _context.Database.SqlQuery<Courses>(
                "SELECT c.* FROM Courses c INNER JOIN CourseTeachers ct ON c.Id = ct.CourseId WHERE ct.TeacherUserId = {0}",
                teacherId).ToList();

            return teacherCourses.Select(course => new TeacherCourse
            {
                Course = course,
                TaskCount = _context.Tasks.Count(t => t.CourseId == course.Id && t.TeacherUserId == teacherId),
                SubmissionsCount = _context.Submissions
                    .Count(s => s.Tasks.CourseId == course.Id && s.Status == "Submitted")
            }).ToList();
        }

        private List<SubmissionReview> GetSubmissionsToReview(int teacherId)
        {
            return _context.Submissions
                .Where(s => s.Tasks.TeacherUserId == teacherId &&
                           (s.Status == "Submitted" || s.Status == "Under Review"))
                .OrderBy(s => s.SubmittedAt)
                .Take(5)
                .Select(s => new SubmissionReview
                {
                    Submission = s,
                    Task = _context.Tasks.FirstOrDefault(t => t.Id == s.TaskId),
                    Student = _context.Users.FirstOrDefault(u => u.Id == s.StudentUserId),
                    DaysAgo = (int)(DateTime.Now - (DateTime)s.SubmittedAt).TotalDays
                }).ToList();
        }

        private List<NotificationItem> GetTeacherNotifications()
        {
            var notifications = _context.Notifications
                .Where(n => (n.UserId == _currentUser.Id) ||
                           (n.Type == "NewSubmission") ||
                           (n.Type == "GradeRequest"))
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToList();

            return notifications.Select(n => new NotificationItem
            {
                Notification = n,
                Icon = GetNotificationIcon(n.Type)
            }).ToList();
        }

        private List<StatCard> GetSystemStatistics()
        {
            return new List<StatCard>
            {
                new StatCard { Title = "👥 ПОЛЬЗОВАТЕЛИ", Value = _context.Users.Count().ToString(), Color = "#3498db" },
                new StatCard { Title = "🎓 СТУДЕНТЫ", Value = _context.Students.Count().ToString(), Color = "#2ecc71" },
                new StatCard { Title = "👨‍🏫 ПРЕПОДАВАТЕЛИ", Value = _context.Teachers.Count().ToString(), Color = "#9b59b6" },
                new StatCard { Title = "📚 КУРСЫ", Value = _context.Courses.Count().ToString(), Color = "#e74c3c" },
                new StatCard { Title = "📝 ЗАДАНИЯ", Value = _context.Tasks.Count().ToString(), Color = "#f39c12" },
                new StatCard { Title = "📨 ОТПРАВКИ", Value = _context.Submissions.Count().ToString(), Color = "#1abc9c" }
            };
        }

        private List<RecentActivity> GetRecentActivities()
        {
            return _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(3)
                .Select(u => new RecentActivity
                {
                    Description = $"{u.FullName} ({u.Role}) - {u.CreatedAt:dd.MM.yyyy}"
                }).ToList();
        }

        private List<NotificationItem> GetAdminNotifications()
        {
            return _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new NotificationItem
                {
                    Notification = n,
                    Icon = GetNotificationIcon(n.Type)
                }).ToList();
        }

        private string GetNotificationIcon(string type)
        {
            switch (type)
            {
                case "Grade": return "✅";
                case "Comment": return "📝";
                case "NewTask": return "🆕";
                case "NewSubmission": return "📨";
                case "GradeRequest": return "📊";
                case "System": return "⚙️";
                default: return "🔔";
            }
        }
    }

    public class DashboardData
    {
        public string WelcomeMessage { get; set; }
        public string DateMessage { get; set; }
        public List<UrgentTask> UrgentTasks { get; set; }
        public List<CourseProgress> CourseProgresses { get; set; }
        public List<NotificationItem> Notifications { get; set; }
        public List<TeacherCourse> TeacherCourses { get; set; }
        public List<SubmissionReview> SubmissionsToReview { get; set; }
        public List<NotificationItem> TeacherNotifications { get; set; }
        public List<StatCard> SystemStatistics { get; set; }
        public List<RecentActivity> RecentActivities { get; set; }
        public List<NotificationItem> AdminNotifications { get; set; }
    }

    public class UrgentTask
    {
        public Tasks Task { get; set; }
        public Courses Course { get; set; }
        public int DaysLeft { get; set; }
    }

    public class CourseProgress
    {
        public string CourseTitle { get; set; }
        public int Progress { get; set; }
        public int SubmittedTasks { get; set; }
        public int TotalTasks { get; set; }
    }

    public class NotificationItem
    {
        public Notifications Notification { get; set; }
        public string Icon { get; set; }
    }

    public class TeacherCourse
    {
        public Courses Course { get; set; }
        public int TaskCount { get; set; }
        public int SubmissionsCount { get; set; }
    }

    public class SubmissionReview
    {
        public Submissions Submission { get; set; }
        public Tasks Task { get; set; }
        public Users Student { get; set; }
        public int DaysAgo { get; set; }
    }

    public class StatCard
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Color { get; set; }
    }

    public class RecentActivity
    {
        public string Description { get; set; }
    }
}