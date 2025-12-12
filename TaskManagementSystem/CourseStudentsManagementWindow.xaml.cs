using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace TaskManagementSystem
{
    public partial class CourseStudentsManagementWindow : Window
    {
        private TaskManagementSystemEntities3 _context;
        private int _courseId;
        private List<Users> allStudents;
        private List<Users> courseStudents;

        public CourseStudentsManagementWindow(int courseId)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            _courseId = courseId;

            LoadStudents();
            LoadCourseInfo();
        }

        private void LoadCourseInfo()
        {
            var course = _context.Courses.FirstOrDefault(c => c.Id == _courseId);
            if (course != null)
            {
                Title = $"Управление студентами курса: {course.Title}";
            }
        }

        private void LoadStudents()
        {
            try
            {
                allStudents = _context.Users
                    .Where(u => u.Role == "Student")
                    .OrderBy(u => u.FullName)
                    .ToList();

                var enrolledStudentIds = _context.Database.SqlQuery<int>(
                    "SELECT StudentUserId FROM CourseStudents WHERE CourseId = {0}",
                    _courseId).ToList();

                courseStudents = allStudents
                    .Where(s => enrolledStudentIds.Contains(s.Id))
                    .ToList();

                AvailableStudentsListBox.ItemsSource = allStudents.Except(courseStudents).ToList();
                EnrolledStudentsListBox.ItemsSource = courseStudents;

                EnrolledCountText.Text = $"Зачислено: {courseStudents.Count}";
                AvailableCountText.Text = $"Доступно: {allStudents.Count - courseStudents.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке студентов: {ex.Message}", "Ошибка");
            }
        }

        private void AddStudentButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = AvailableStudentsListBox.SelectedItem as Users;
            if (selected == null) return;

            try
            {
                string query = "INSERT INTO CourseStudents (CourseId, StudentUserId) VALUES ({0}, {1})";
                _context.Database.ExecuteSqlCommand(query, _courseId, selected.Id);

                LoadStudents();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении студента: {ex.Message}", "Ошибка");
            }
        }

        private void RemoveStudentButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = EnrolledStudentsListBox.SelectedItem as Users;
            if (selected == null) return;

            try
            {
                string query = "DELETE FROM CourseStudents WHERE CourseId = {0} AND StudentUserId = {1}";
                _context.Database.ExecuteSqlCommand(query, _courseId, selected.Id);

                LoadStudents();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении студента: {ex.Message}", "Ошибка");
            }
        }

        private void AddAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var student in allStudents.Except(courseStudents))
                {
                    if (!_context.Database.SqlQuery<bool>(
                        "SELECT CASE WHEN EXISTS (SELECT 1 FROM CourseStudents WHERE CourseId = {0} AND StudentUserId = {1}) THEN 1 ELSE 0 END",
                        _courseId, student.Id).FirstOrDefault())
                    {
                        string query = "INSERT INTO CourseStudents (CourseId, StudentUserId) VALUES ({0}, {1})";
                        _context.Database.ExecuteSqlCommand(query, _courseId, student.Id);
                    }
                }

                LoadStudents();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении всех студентов: {ex.Message}", "Ошибка");
            }
        }

        private void RemoveAllButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string query = "DELETE FROM CourseStudents WHERE CourseId = {0}";
                _context.Database.ExecuteSqlCommand(query, _courseId);

                LoadStudents();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении всех студентов: {ex.Message}", "Ошибка");
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