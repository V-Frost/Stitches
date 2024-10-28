using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Linq;
using System;


namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private Graph _graph;
        private int size = 5;
        private Dictionary<Node, Line> _activeConnections = new Dictionary<Node, Line>();
        public MainWindow()
        {
            InitializeComponent();
            _graph = new Graph();
            GenerateValidLevel(size);
            DrawGraph();
        }

        private void GenerateValidLevel(int size)
        {
            bool isValid = false;
            while (!isValid)
            {
                _graph.GenerateLevel(size);
                isValid = ValidateLevel();
            }
        }

        private bool ValidateLevel()
        {
            foreach (var node in _graph.Nodes)
            {
                int thickBorders = 0;

                // Перевіряємо кожен сусідній блок на наявність з'єднання (товсті бордюри)
                if (!_graph.Nodes.Any(n => n.X == node.X && n.Y == node.Y - 1 && n.GroupID == node.GroupID)) thickBorders++; // Top
                if (!_graph.Nodes.Any(n => n.X == node.X && n.Y == node.Y + 1 && n.GroupID == node.GroupID)) thickBorders++; // Bottom
                if (!_graph.Nodes.Any(n => n.X == node.X - 1 && n.Y == node.Y && n.GroupID == node.GroupID)) thickBorders++; // Left
                if (!_graph.Nodes.Any(n => n.X == node.X + 1 && n.Y == node.Y && n.GroupID == node.GroupID)) thickBorders++; // Right

                // Якщо клітинка має 3 або більше товстих бордюрів, рівень не відповідає умовам
                if (thickBorders == 4)
                {
                    return false;
                }
            }

            // Перевіряємо кількість груп
            int groupCount = CountGroups();
            if (groupCount > 5)
            {
                return false;
            }

            return true;
        }

        private int CountGroups()
        {
            var visited = new HashSet<Node>();
            int groupCount = 0;

            foreach (var node in _graph.Nodes)
            {
                if (!visited.Contains(node))
                {
                    // Запускаємо пошук для нової групи
                    TraverseGroup(node, visited);
                    groupCount++;
                }
            }

            return groupCount;
        }

        private void TraverseGroup(Node node, HashSet<Node> visited)
        {
            var stack = new Stack<Node>();
            stack.Push(node);
            visited.Add(node);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                // Додаємо сусідні вузли з таким же GroupID у стек для подальшого оброблення
                foreach (var neighbor in _graph.Nodes.Where(n =>
                    !visited.Contains(n) &&
                    n.GroupID == current.GroupID &&
                    ((Math.Abs(n.X - current.X) == 1 && n.Y == current.Y) ||
                     (Math.Abs(n.Y - current.Y) == 1 && n.X == current.X))))
                {
                    visited.Add(neighbor);
                    stack.Push(neighbor);
                }
            }
        }





        private void DrawGraph()
        {
            MyCanvas.Children.Clear();
            double cellSize = 40;
            double clickableSize = 10; // Розмір клікабельної області

            foreach (var node in _graph.Nodes)
            {
                Thickness borderThickness = new Thickness(1);

                var topNeighbor = _graph.Nodes.FirstOrDefault(n => n.X == node.X && n.Y == node.Y - 1);
                var bottomNeighbor = _graph.Nodes.FirstOrDefault(n => n.X == node.X && n.Y == node.Y + 1);
                var leftNeighbor = _graph.Nodes.FirstOrDefault(n => n.X == node.X - 1 && n.Y == node.Y);
                var rightNeighbor = _graph.Nodes.FirstOrDefault(n => n.X == node.X + 1 && n.Y == node.Y);

                if (topNeighbor == null || topNeighbor.GroupID != node.GroupID) borderThickness.Top = 3;
                if (bottomNeighbor == null || bottomNeighbor.GroupID != node.GroupID) borderThickness.Bottom = 3;
                if (leftNeighbor == null || leftNeighbor.GroupID != node.GroupID) borderThickness.Left = 3;
                if (rightNeighbor == null || rightNeighbor.GroupID != node.GroupID) borderThickness.Right = 3;

                Border border = new Border
                {
                    Width = cellSize,
                    Height = cellSize,
                    BorderBrush = Brushes.Black,
                    BorderThickness = borderThickness,
                    Tag = node
                };

                // Додаємо клікабельні області тільки для сусідів з різними GroupID
                if (topNeighbor != null && topNeighbor.GroupID != node.GroupID)
                    MyCanvas.Children.Add(CreateClickableEdge(node, topNeighbor, "top", cellSize, clickableSize));

                if (bottomNeighbor != null && bottomNeighbor.GroupID != node.GroupID)
                    MyCanvas.Children.Add(CreateClickableEdge(node, bottomNeighbor, "bottom", cellSize, clickableSize));

                if (leftNeighbor != null && leftNeighbor.GroupID != node.GroupID)
                    MyCanvas.Children.Add(CreateClickableEdge(node, leftNeighbor, "left", cellSize, clickableSize));

                if (rightNeighbor != null && rightNeighbor.GroupID != node.GroupID)
                    MyCanvas.Children.Add(CreateClickableEdge(node, rightNeighbor, "right", cellSize, clickableSize));

                Canvas.SetLeft(border, node.X * cellSize);
                Canvas.SetTop(border, node.Y * cellSize);
                MyCanvas.Children.Add(border);
            }
        }

        private Border CreateClickableEdge(Node node, Node neighbor, string position, double cellSize, double clickableSize)
        {
            // Створюємо нову область для кліка
            Border clickableEdge = new Border
            {
                Width = position == "left" || position == "right" ? clickableSize : cellSize,
                Height = position == "top" || position == "bottom" ? clickableSize : cellSize,
                Background = Brushes.Transparent, // Прозорий фон для клікабельної області
                Tag = new { node, neighbor }
            };

            // Розміщення області в залежності від її позиції
            switch (position)
            {
                case "top":
                    Canvas.SetLeft(clickableEdge, node.X * cellSize + (cellSize - clickableSize) / 2);
                    Canvas.SetTop(clickableEdge, node.Y * cellSize - clickableSize / 2);
                    break;

                case "bottom":
                    Canvas.SetLeft(clickableEdge, node.X * cellSize + (cellSize - clickableSize) / 2);
                    Canvas.SetTop(clickableEdge, (node.Y + 1) * cellSize - clickableSize / 2);
                    break;

                case "left":
                    Canvas.SetLeft(clickableEdge, node.X * cellSize - clickableSize / 2);
                    Canvas.SetTop(clickableEdge, node.Y * cellSize + (cellSize - clickableSize) / 2);
                    break;

                case "right":
                    Canvas.SetLeft(clickableEdge, (node.X + 1) * cellSize - clickableSize / 2);
                    Canvas.SetTop(clickableEdge, node.Y * cellSize + (cellSize - clickableSize) / 2);
                    break;
            }

            // Додаємо обробник події для створення з'єднання при натисканні на цю область
            clickableEdge.MouseDown += (s, e) =>
            {
                var data = (dynamic)((Border)s).Tag;
                ToggleConnection(data.node, data.neighbor);
            };

            return clickableEdge;
        }

        private void ToggleConnection(Node node, Node neighbor)
        {
            if (_activeConnections.TryGetValue(node, out Line existingLineNode))
            {
                MyCanvas.Children.Remove(existingLineNode);
                _activeConnections.Remove(node);
            }

            if (_activeConnections.TryGetValue(neighbor, out Line existingLineNeighbor))
            {
                MyCanvas.Children.Remove(existingLineNeighbor);
                _activeConnections.Remove(neighbor);
            }

            Line newLine = CreateConnectionLine(node, neighbor);
            MyCanvas.Children.Add(newLine);

            _activeConnections[node] = newLine;
            _activeConnections[neighbor] = newLine;
        }

        private Line CreateConnectionLine(Node node, Node neighbor)
        {
            double cellSize = 40;
            double centerOffset = cellSize / 2;

            return new Line
            {
                X1 = node.X * cellSize + centerOffset,
                Y1 = node.Y * cellSize + centerOffset,
                X2 = neighbor.X * cellSize + centerOffset,
                Y2 = neighbor.Y * cellSize + centerOffset,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsGraphConnected())
                MessageBox.Show("Вітаємо! Всі блоки з'єднані правильно!");
            else
                MessageBox.Show("Ще не всі блоки з'єднані або з'єднані неправильно.");
        }

        private bool IsGraphConnected()
        {
            if (_graph.Nodes.Count == 0) return false;

            var visited = new HashSet<Node>();
            var queue = new Queue<Node>();
            queue.Enqueue(_graph.Nodes[0]);
            visited.Add(_graph.Nodes[0]);

            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();
                foreach (var neighbor in currentNode.Neighbors)
                {
                    if (visited.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }

            return visited.Count == _graph.Nodes.Count;
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            MyCanvas.Children.Clear();
            _graph = new Graph();
            _graph.GenerateLevel(size);
            GenerateValidLevel(size);

            DrawGraph();
            _activeConnections.Clear(); // Очищаємо всі з'єднання
        }
    }
}
