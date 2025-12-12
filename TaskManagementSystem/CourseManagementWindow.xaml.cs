using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TaskManagementSystem
{
    public partial class CourseManagementWindow : Window
    {
        private TaskManagementSystemEntities3 _context;
        private List<CourseWithStats> allCourses;
        private CourseWithStats selectedCourse;

        public CourseManagementWindow()
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();

            InitializeSearchBox();
            LoadCourses();
        }

        private void InitializeSearchBox()
        {
            SearchTextBox.GotFocus += (s, e) =>
            {
                if (SearchTextBox.Text == "Поиск курсов...")
                {
                    SearchTextBox.Text = "";
                    SearchTextBox.Foreground = System.Windows.Media.Brushes.Black;
                }
            };

            SearchTextBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    SearchTextBox.Text = "Поиск курсов...";
                    SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
                }
            };

            SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            SearchTextBox.Text = "Поиск курсов...";
        }

        private void LoadCourses()
        {
            try
            {
                var courses = _context.Courses.ToList();
                allCourses = new List<CourseWithStats>();

                foreach (var course in courses)
                {
                    var studentCount = _context.Database.SqlQuery<int?>(
                        "SELECT COUNT(*) FROM CourseStudents WHERE CourseId = {0}",
                        course.Id).FirstOrDefault() ?? 0;

                    var teacherCount = _context.Database.SqlQuery<int?>(
                        "SELECT COUNT(*) FROM CourseTeachers WHERE CourseId = {0}",
                        course.Id).FirstOrDefault() ?? 0;

                    allCourses.Add(new CourseWithStats
                    {
                        Id = course.Id,
                        Title = course.Title,
                        Description = course.Description,
                        StudentCount = studentCount,
                        TeacherCount = teacherCount
                    });
                }

                CoursesDataGrid.ItemsSource = allCourses;
                TotalCoursesText.Text = allCourses.Count.ToString();

                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке курсов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = selectedCourse != null;

            EditCourseButton.IsEnabled = hasSelection;
            DeleteCourseButton.IsEnabled = hasSelection;
            ManageStudentsButton.IsEnabled = hasSelection;
            ManageTeachersButton.IsEnabled = hasSelection;
            ViewStatisticsButton.IsEnabled = hasSelection;

            if (hasSelection)
            {
                SelectedCourseInfo.Text = $"{selectedCourse.Title}\n" +
                                        $"Студентов: {selectedCourse.StudentCount}\n" +
                                        $"Преподавателей: {selectedCourse.TeacherCount}";
            }
            else
            {
                SelectedCourseInfo.Text = "Не выбран";
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchTextBox.Text == "Поиск курсов..." || string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                CoursesDataGrid.ItemsSource = allCourses;
                return;
            }

            string searchText = SearchTextBox.Text.ToLower();
            var filtered = allCourses.Where(c =>
                (c.Title != null && c.Title.ToLower().Contains(searchText)) ||
                (c.Description != null && c.Description.ToLower().Contains(searchText))).ToList();

            CoursesDataGrid.ItemsSource = filtered;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCourses();
            SearchTextBox.Text = "Поиск курсов...";
            SearchTextBox.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void CoursesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedCourse = CoursesDataGrid.SelectedItem as CourseWithStats;
            UpdateButtonStates();
        }

        private void AddCourseButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new CourseEditWindow();
            if (editWindow.ShowDialog() == true)
            {
                LoadCourses();
            }
        }

        private void EditCourseButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCourse == null) return;

            var course = _context.Courses.FirstOrDefault(c => c.Id == selectedCourse.Id);
            if (course == null) return;

            var editWindow = new CourseEditWindow(course);
            if (editWindow.ShowDialog() == true)
            {
                LoadCourses();
            }
        }

        private void DeleteCourseButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCourse == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите удалить курс?\n\n" +
                                       $"Название: {selectedCourse.Title}\n\n" +
                                       "Это действие удалит все связанные данные!",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string deleteQuery = "DELETE FROM Courses WHERE Id = {0}";
                    _context.Database.ExecuteSqlCommand(deleteQuery, selectedCourse.Id);

                    MessageBox.Show($"Курс '{selectedCourse.Title}' успешно удален",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    selectedCourse = null;
                    LoadCourses();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении курса: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ManageStudentsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCourse != null)
            {
                var manageWindow = new CourseStudentsManagementWindow(selectedCourse.Id);
                manageWindow.Owner = this;
                manageWindow.ShowDialog();
                LoadCourses();
            }
        }

        private void ManageTeachersButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCourse != null)
            {
                var manageWindow = new CourseTeachersManagementWindow(selectedCourse.Id);
                manageWindow.Owner = this;
                manageWindow.ShowDialog();
                LoadCourses();
            }
        }

        private void ViewStatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCourse != null)
            {
                int taskCount = _context.Tasks.Count(t => t.CourseId == selectedCourse.Id);
                int submissionCount = _context.Submissions
                    .Count(s => s.Tasks.CourseId == selectedCourse.Id);
                int completedCount = _context.Submissions
                    .Count(s => s.Tasks.CourseId == selectedCourse.Id && s.Status == "Completed");

                double completionRate = taskCount > 0 ? (double)completedCount / taskCount * 100 : 0;

                string message = $"📊 Статистика курса: {selectedCourse.Title}\n\n" +
                               $"📝 Заданий: {taskCount}\n" +
                               $"📨 Отправок: {submissionCount}\n" +
                               $"✅ Выполнено: {completedCount}\n" +
                               $"📈 Процент выполнения: {completionRate:F1}%\n" +
                               $"👥 Студентов: {selectedCourse.StudentCount}\n" +
                               $"👨‍🏫 Преподавателей: {selectedCourse.TeacherCount}";

                MessageBox.Show(message, "Статистика курса");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _context?.Dispose();
        }
    }

    public class CourseWithStats
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int StudentCount { get; set; }
        public int TeacherCount { get; set; }
    }
}