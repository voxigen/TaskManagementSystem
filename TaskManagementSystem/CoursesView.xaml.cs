using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskManagementSystem
{
    public partial class CoursesView : Page
    {
        public CoursesView()
        {
            InitializeComponent();
            LoadCourses();
            SearchBox.TextChanged += SearchBox_TextChanged;
        }

        private void LoadCourses()
        {
            var courses = new List<CourseModel>
            {
                new CourseModel
                {
                    CourseName = "МДК 01.03 - Технология разработки ПО",
                    Instructor = "Преподаватель: Сидоров А.В.",
                    NextAssignment = "Следующее задание: 15.05.2024",
                    Progress = 60,
                    TotalAssignments = 10,
                    CompletedAssignments = 6,
                    CardBackground = new SolidColorBrush(Color.FromRgb(255, 255, 255))
                },
                new CourseModel
                {
                    CourseName = "ОП.05 - Основы проектирования БД",
                    Instructor = "Преподаватель: Иванова М.С.",
                    NextAssignment = "Следующее задание: 20.05.2024",
                    Progress = 80,
                    TotalAssignments = 10,
                    CompletedAssignments = 8,
                    CardBackground = new SolidColorBrush(Color.FromRgb(255, 255, 255))
                },
                new CourseModel
                {
                    CourseName = "Иностранный язык в проф. деятельности",
                    Instructor = "Преподаватель: Петрова Л.К.",
                    NextAssignment = "Следующее задание: 10.05.2024",
                    Progress = 25,
                    TotalAssignments = 8,
                    CompletedAssignments = 2,
                    CardBackground = new SolidColorBrush(Color.FromRgb(255, 255, 255))
                },
                new CourseModel
                {
                    CourseName = "МДК 02.01 - Разработка мобильных приложений",
                    Instructor = "Преподаватель: Козлов Д.С.",
                    NextAssignment = "Следующее задание: 25.05.2024",
                    Progress = 45,
                    TotalAssignments = 12,
                    CompletedAssignments = 5,
                    CardBackground = new SolidColorBrush(Color.FromRgb(255, 255, 255))
                }
            };

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
                CoursesItemsControl.ItemsSource = GetSampleCourses();
            }
            else
            {
                var filteredCourses = GetSampleCourses().FindAll(c =>
                    c.CourseName.ToLower().Contains(searchText) ||
                    c.Instructor.ToLower().Contains(searchText));
                CoursesItemsControl.ItemsSource = filteredCourses;
            }

            UpdateCoursesVisibility();
        }

        private List<CourseModel> GetSampleCourses()
        {
            return new List<CourseModel>
            {
                new CourseModel
                {
                    CourseName = "МДК 01.03 - Технология разработки ПО",
                    Instructor = "Преподаватель: Сидоров А.В.",
                    NextAssignment = "Следующее задание: 15.05.2024",
                    Progress = 60,
                    TotalAssignments = 10,
                    CompletedAssignments = 6
                },
                new CourseModel
                {
                    CourseName = "ОП.05 - Основы проектирования БД",
                    Instructor = "Преподаватель: Иванова М.С.",
                    NextAssignment = "Следующее задание: 20.05.2024",
                    Progress = 80,
                    TotalAssignments = 10,
                    CompletedAssignments = 8
                },
                new CourseModel
                {
                    CourseName = "Иностранный язык в проф. деятельности",
                    Instructor = "Преподаватель: Петрова Л.К.",
                    NextAssignment = "Следующее задание: 10.05.2024",
                    Progress = 25,
                    TotalAssignments = 8,
                    CompletedAssignments = 2
                }
            };
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