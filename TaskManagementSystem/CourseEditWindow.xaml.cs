using System;
using System.Linq;
using System.Windows;

namespace TaskManagementSystem
{
    public partial class CourseEditWindow : Window
    {
        private TaskManagementSystemEntities3 _context;
        private Courses _course;
        private bool _isEditMode;

        public CourseEditWindow(Courses course = null)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();

            if (course != null)
            {
                _course = course;
                _isEditMode = true;
                Title = "Редактирование курса";
                TitleTextBox.Text = course.Title;
                DescriptionTextBox.Text = course.Description;
            }
            else
            {
                _isEditMode = false;
                Title = "Создание нового курса";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Название курса не может быть пустым", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_isEditMode)
                {
                    var courseToUpdate = _context.Courses.FirstOrDefault(c => c.Id == _course.Id);
                    if (courseToUpdate != null)
                    {
                        courseToUpdate.Title = TitleTextBox.Text.Trim();
                        courseToUpdate.Description = DescriptionTextBox.Text.Trim();
                    }
                }
                else
                {
                    var newCourse = new Courses
                    {
                        Title = TitleTextBox.Text.Trim(),
                        Description = DescriptionTextBox.Text.Trim()
                    };
                    _context.Courses.Add(newCourse);
                }

                _context.SaveChanges();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении курса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}