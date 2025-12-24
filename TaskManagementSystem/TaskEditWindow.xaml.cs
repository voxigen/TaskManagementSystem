using System;
using System.Linq;
using System.Windows;

namespace TaskManagementSystem
{
    public partial class TaskEditWindow : Window
    {
        private TaskManagementSystemEntities3 _context;
        private Tasks _task;

        public TaskEditWindow(Tasks task)
        {
            InitializeComponent();
            _context = new TaskManagementSystemEntities3();
            _task = task;

            LoadTaskData();
        }

        private void LoadTaskData()
        {
            TitleTextBox.Text = _task.Title;
            DescriptionTextBox.Text = _task.Description;
            DeadlineDatePicker.SelectedDate = _task.Deadline;
            MaxScoreTextBox.Text = _task.MaxScore.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                var taskToUpdate = _context.Tasks.FirstOrDefault(t => t.Id == _task.Id);
                if (taskToUpdate != null)
                {
                    taskToUpdate.Title = TitleTextBox.Text.Trim();
                    taskToUpdate.Description = DescriptionTextBox.Text.Trim();
                    taskToUpdate.Deadline = DeadlineDatePicker.SelectedDate ?? DateTime.Now.AddDays(7);

                    if (int.TryParse(MaxScoreTextBox.Text, out int maxScore))
                    {
                        taskToUpdate.MaxScore = maxScore;
                    }

                    _context.SaveChanges();

                    MessageBox.Show($"Задание '{taskToUpdate.Title}' успешно обновлено!");
                    DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении задания: {ex.Message}");
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("Введите название задания");
                TitleTextBox.Focus();
                return false;
            }

            if (DeadlineDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите срок выполнения");
                DeadlineDatePicker.Focus();
                return false;
            }

            if (DeadlineDatePicker.SelectedDate < DateTime.Now.Date)
            {
                MessageBox.Show("Срок выполнения не может быть в прошлом");
                DeadlineDatePicker.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(MaxScoreTextBox.Text))
            {
                if (!int.TryParse(MaxScoreTextBox.Text, out int score))
                {
                    MessageBox.Show("Введите корректное значение максимального балла");
                    MaxScoreTextBox.Focus();
                    return false;
                }

                if (score <= 0)
                {
                    MessageBox.Show("Максимальный балл должен быть положительным числом");
                    MaxScoreTextBox.Focus();
                    return false;
                }
            }

            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}