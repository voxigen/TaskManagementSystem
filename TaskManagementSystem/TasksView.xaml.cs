using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskManagementSystem
{
    public partial class TasksView : Page
    {
        private List<TaskModel> allTasks;
        private bool isInitialized = false;

        public TasksView()
        {
            InitializeComponent();
            LoadSampleTasks();

         
            this.Loaded += (s, e) =>
            {
                isInitialized = true;
                GenerateTaskGroups();
            };
        }

        private void LoadSampleTasks()
        {
            allTasks = new List<TaskModel>
            {
                new TaskModel
                {
                    Title = "Лаб.работа #1",
                    Course = "МДК 01.03",
                    DueDate = new DateTime(2024, 5, 1),
                    Status = TaskStatus.Overdue,
                    StatusText = "Просрочено на 5 дней",
                    DaysLeft = -5,
                    Files = new List<string> { "ТЗ.docx" },
                    CanSubmit = true
                },
                new TaskModel
                {
                    Title = "Тест #2",
                    Course = "ОП.05 БД",
                    DueDate = new DateTime(2024, 5, 10),
                    Status = TaskStatus.Overdue,
                    StatusText = "Просрочено на 2 дня",
                    DaysLeft = -2,
                    CanSubmit = true
                },
                new TaskModel
                {
                    Title = "Проект БД",
                    Course = "ОП.05 БД",
                    DueDate = DateTime.Now.AddDays(3),
                    Status = TaskStatus.Active,
                    StatusText = "Осталось 3 дня",
                    DaysLeft = 3,
                    Files = new List<string> { "ТЗ.docx", "Примеры.zip" },
                    CanSubmit = true
                },
                new TaskModel
                {
                    Title = "Эссе",
                    Course = "Английский",
                    DueDate = DateTime.Now.AddDays(8),
                    Status = TaskStatus.Active,
                    StatusText = "Осталось 8 дней",
                    DaysLeft = 8,
                    CanSubmit = true
                },
                new TaskModel
                {
                    Title = "Лаб.работа #3",
                    Course = "МДК 01.03",
                    DueDate = DateTime.Now.AddDays(1),
                    Status = TaskStatus.Active,
                    StatusText = "Осталось 24 часа",
                    DaysLeft = 1,
                    Files = new List<string> { "Инструкция.pdf" },
                    CanSubmit = true
                },
                new TaskModel
                {
                    Title = "Тест #1",
                    Course = "МДК 01.03",
                    DueDate = new DateTime(2024, 4, 25),
                    Status = TaskStatus.Completed,
                    StatusText = "Проверено, оценка: 5",
                    DaysLeft = 0,
                    SubmittedDate = new DateTime(2024, 4, 25),
                    Grade = 5,
                    CanReview = true
                },
                new TaskModel
                {
                    Title = "Домашнее задание #2",
                    Course = "ОП.05 БД",
                    DueDate = new DateTime(2024, 4, 20),
                    Status = TaskStatus.Completed,
                    StatusText = "Проверено, оценка: 4",
                    DaysLeft = 0,
                    SubmittedDate = new DateTime(2024, 4, 20),
                    Grade = 4,
                    CanReview = true
                }
            };
        }

        private void GenerateTaskGroups()
        {
            
            if (!isInitialized || TasksContainer == null)
                return;

            try
            {
                TasksContainer.Children.Clear();

                var filteredTasks = FilterTasks();

                var overdueTasks = filteredTasks.Where(t => t.Status == TaskStatus.Overdue).ToList();
                var activeTasks = filteredTasks.Where(t => t.Status == TaskStatus.Active).ToList();
                var completedTasks = filteredTasks.Where(t => t.Status == TaskStatus.Completed).ToList();

                if (overdueTasks.Any())
                    AddTaskGroup("🔴 ПРОСРОЧЕНО", overdueTasks, "#ffebee");

                if (activeTasks.Any())
                    AddTaskGroup("🟡 АКТИВНЫЕ", activeTasks, "#fff8e1");

                if (completedTasks.Any())
                    AddTaskGroup("✅ ВЫПОЛНЕННЫЕ", completedTasks, "#e8f5e8");

                UpdateTasksVisibility();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при генерации групп заданий: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTaskGroup(string groupTitle, List<TaskModel> tasks, string backgroundColor)
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
                Text = $"{groupTitle} ({tasks.Count})",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50"),
                Margin = new Thickness(0, 0, 0, 12)
            };
            stackPanel.Children.Add(headerText);

            foreach (var task in tasks)
            {
                var taskCard = CreateTaskCard(task);
                stackPanel.Children.Add(taskCard);
            }

            groupBorder.Child = stackPanel;
            TasksContainer.Children.Add(groupBorder);
        }

        private Border CreateTaskCard(TaskModel task)
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
                Text = task.Title,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#2c3e50")
            };
            Grid.SetColumn(titleText, 0);

            var courseText = new TextBlock
            {
                Text = task.Course,
                FontSize = 12,
                Foreground = (Brush)new BrushConverter().ConvertFrom("#7f8c8d")
            };
            Grid.SetColumn(courseText, 1);

            titleGrid.Children.Add(titleText);
            titleGrid.Children.Add(courseText);
            leftPanel.Children.Add(titleGrid);

            var dueDateText = new TextBlock
            {
                Text = $"📅 {task.DueDate:dd.MM.yyyy}",
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };
            leftPanel.Children.Add(dueDateText);

            var statusText = new TextBlock
            {
                Text = task.StatusText,
                FontSize = 12,
                Foreground = GetStatusColor(task.Status),
                Margin = new Thickness(0, 4, 0, 0),
                FontWeight = FontWeights.SemiBold
            };
            leftPanel.Children.Add(statusText);

            if (task.Files != null && task.Files.Any())
            {
                var filesText = new TextBlock
                {
                    Text = $"📎 Материалы: {string.Join(", ", task.Files)}",
                    FontSize = 11,
                    Foreground = (Brush)new BrushConverter().ConvertFrom("#7f8c8d"),
                    Margin = new Thickness(0, 4, 0, 0)
                };
                leftPanel.Children.Add(filesText);
            }

            Grid.SetColumn(leftPanel, 0);
            grid.Children.Add(leftPanel);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (task.CanSubmit)
            {
                var submitButton = new Button
                {
                    Content = "Сдать работу",
                    Background = (Brush)new BrushConverter().ConvertFrom("#27ae60"),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                 
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(8, 0, 0, 0),
                    Tag = task
                };
                submitButton.Click += SubmitWork_Click;
                buttonPanel.Children.Add(submitButton);
            }

            if (task.CanReview)
            {
                var reviewButton = new Button
                {
                    Content = "Просмотреть",
                    Background = (Brush)new BrushConverter().ConvertFrom("#3498db"),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                   
                    BorderThickness = new Thickness(0),
                    Margin = new Thickness(8, 0, 0, 0),
                    Tag = task
                };
                reviewButton.Click += ReviewWork_Click;
                buttonPanel.Children.Add(reviewButton);
            }

            Grid.SetColumn(buttonPanel, 1);
            grid.Children.Add(buttonPanel);

            card.Child = grid;
            return card;
        }

        private Brush GetStatusColor(TaskStatus status)
        {
            var converter = new BrushConverter();
            switch (status)
            {
                case TaskStatus.Overdue:
                    return (Brush)converter.ConvertFromString("#e74c3c");
                case TaskStatus.Active:
                    return (Brush)converter.ConvertFromString("#f39c12");
                case TaskStatus.Completed:
                    return (Brush)converter.ConvertFromString("#27ae60");
                default:
                    return (Brush)converter.ConvertFromString("#7f8c8d");
            }
        }

        private List<TaskModel> FilterTasks()
        {
         
            if (ActiveTasksFilter == null || OverdueTasksFilter == null || CourseFilter == null)
                return allTasks;

            var filtered = allTasks.AsEnumerable();

            if (ActiveTasksFilter.IsChecked == true)
                filtered = filtered.Where(t => t.Status == TaskStatus.Active);
            else if (OverdueTasksFilter.IsChecked == true)
                filtered = filtered.Where(t => t.Status == TaskStatus.Overdue);

            var selectedCourse = (CourseFilter.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selectedCourse != "Все курсы")
                filtered = filtered.Where(t => t.Course == selectedCourse);

            return filtered.ToList();
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (isInitialized)
                GenerateTaskGroups();
        }

        private void UpdateTasksVisibility()
        {
            bool hasTasks = TasksContainer.Children.Count > 0;
            NoTasksMessage.Visibility = hasTasks ? Visibility.Collapsed : Visibility.Visible;
            TasksContainer.Visibility = hasTasks ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SubmitWork_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.Tag as TaskModel;
            if (task != null)
            {
                MessageBox.Show($"Сдача работы: {task.Title}\nКурс: {task.Course}", "Сдача работы");
            }
        }

        private void ReviewWork_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.Tag as TaskModel;
            if (task != null)
            {
                MessageBox.Show($"Просмотр работы: {task.Title}\nОценка: {task.Grade}\nКурс: {task.Course}", "Просмотр работы");
            }
        }
    }

    public class TaskModel
    {
        public string Title { get; set; }
        public string Course { get; set; }
        public DateTime DueDate { get; set; }
        public TaskStatus Status { get; set; }
        public string StatusText { get; set; }
        public int DaysLeft { get; set; }
        public List<string> Files { get; set; }
        public bool CanSubmit { get; set; }
        public bool CanReview { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public int? Grade { get; set; }
    }

    public enum TaskStatus
    {
        Overdue,
        Active,
        Completed
    }
}