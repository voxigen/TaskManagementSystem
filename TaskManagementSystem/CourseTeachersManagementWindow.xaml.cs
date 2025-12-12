using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TaskManagementSystem
{
    public partial class CourseTeachersManagementWindow : Window
    {
        private TaskManagementSystemEntities3 _context;
        private int _courseId;
        private List<Users> allTeachers;
        private List<Users> courseTeachers;

        public CourseTeachersManagementWindow(int courseId)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            _courseId = courseId;

            LoadTeachers();
            LoadCourseInfo();
        }

        private void LoadCourseInfo()
        {
            var course = _context.Courses.FirstOrDefault(c => c.Id == _courseId);
            if (course != null)
            {
                Title = $"Управление преподавателями курса: {course.Title}";
            }
        }

        private void LoadTeachers()
        {
            try
            {
                allTeachers = _context.Users
                    .Where(u => u.Role == "Teacher")
                    .OrderBy(u => u.FullName)
                    .ToList();

                var enrolledTeacherIds = _context.Database.SqlQuery<int>(
                    "SELECT TeacherUserId FROM CourseTeachers WHERE CourseId = {0}",
                    _courseId).ToList();

                courseTeachers = allTeachers
                    .Where(t => enrolledTeacherIds.Contains(t.Id))
                    .ToList();

                AvailableTeachersListBox.ItemsSource = allTeachers.Except(courseTeachers).ToList();
                EnrolledTeachersListBox.ItemsSource = courseTeachers;

                EnrolledCountText.Text = $"Назначено: {courseTeachers.Count}";
                AvailableCountText.Text = $"Доступно: {allTeachers.Count - courseTeachers.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке преподавателей: {ex.Message}", "Ошибка");
            }
        }

        private void AddTeacherButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = AvailableTeachersListBox.SelectedItem as Users;
            if (selected == null) return;

            try
            {
                string query = "INSERT INTO CourseTeachers (CourseId, TeacherUserId) VALUES ({0}, {1})";
                _context.Database.ExecuteSqlCommand(query, _courseId, selected.Id);

                LoadTeachers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении преподавателя: {ex.Message}", "Ошибка");
            }
        }

        private void RemoveTeacherButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = EnrolledTeachersListBox.SelectedItem as Users;
            if (selected == null) return;

            try
            {
                string query = "DELETE FROM CourseTeachers WHERE CourseId = {0} AND TeacherUserId = {1}";
                _context.Database.ExecuteSqlCommand(query, _courseId, selected.Id);

                LoadTeachers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении преподавателя: {ex.Message}", "Ошибка");
            }
        }

        private void AddAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var teacher in allTeachers.Except(courseTeachers))
                {
                    if (!_context.Database.SqlQuery<bool>(
                        "SELECT CASE WHEN EXISTS (SELECT 1 FROM CourseTeachers WHERE CourseId = {0} AND TeacherUserId = {1}) THEN 1 ELSE 0 END",
                        _courseId, teacher.Id).FirstOrDefault())
                    {
                        string query = "INSERT INTO CourseTeachers (CourseId, TeacherUserId) VALUES ({0}, {1})";
                        _context.Database.ExecuteSqlCommand(query, _courseId, teacher.Id);
                    }
                }

                LoadTeachers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении всех преподавателей: {ex.Message}", "Ошибка");
            }
        }

        private void RemoveAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string query = "DELETE FROM CourseTeachers WHERE CourseId = {0}";
                _context.Database.ExecuteSqlCommand(query, _courseId);

                LoadTeachers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении всех преподавателей: {ex.Message}", "Ошибка");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _context?.Dispose();
        }
    }
}