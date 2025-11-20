using System.Windows;
using System.Windows.Controls;

namespace TaskManagementSystem
{
    public partial class Dashboard : Page
    {
        public Dashboard()
        {
            InitializeComponent();
        }

        private void Task1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Открыть задание: Лаб. работа #2", "Задание");
        }

        private void Task2_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Открыть задание: Проект БД", "Задание");
        }

        private void Task3_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Открыть задание: Эссе", "Задание");
        }

        private void Notification1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ваша работа 'Лаб.работа #1' проверена - Оценка: 5", "Уведомление");
        }

        private void Notification2_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Преподаватель оставил комментарий к работе", "Уведомление");
        }

        private void Notification3_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Новое задание: 'Проект БД' в курсе ОП.05", "Уведомление");
        }
    }
}