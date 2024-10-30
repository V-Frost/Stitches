using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Linq;
using System;
using System.Windows.Threading;
using System.Windows.Media.Imaging;


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
            int cellSize = 40; // Розмір однієї клітинки в пікселях
            MyCanvas.Width = size * cellSize;
            MyCanvas.Height = size * cellSize;
            _graph = new Graph();
            _graph.GenerateLevel(size);
            GenerateValidLevel(size);
            DrawGraph();
            DrawConnections();
            DisplayHints();
            ClearStitchesAndDots();
            InitializeComponent();
            StartTimer(); // Запуск таймера при инициализации окна
        }


        private bool _showCoordinates = false;
        private bool _hideTimer = false;
        private bool _nightMode = false;

        private DispatcherTimer _timer;
        private int _elapsedSeconds;

        private Image _pauseOverlay;
        private bool _isPaused = false;

        private Node _startNode;
        private bool _isDragging;


        private void ClearStitchesAndDots()
        {
            // Удаляем все линии (стежки) и точки (эллипсы) с MyCanvas
            var elementsToRemove = MyCanvas.Children.OfType<UIElement>()
                .Where(element => element is Line || element is Ellipse)
                .ToList();

            foreach (var element in elementsToRemove)
            {
                MyCanvas.Children.Remove(element);
            }
        }


        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
            {
                // Снимаем паузу: показываем изображение и цифры, перезапускаем таймер
                _pauseOverlay.Visibility = Visibility.Hidden;
                foreach (var hint in MyCanvas.Children.OfType<TextBlock>())
                {
                    hint.Visibility = Visibility.Visible;
                }
                _timer.Start();
            }
            else
            {
                // Ставим игру на паузу: скрываем изображение и цифры, останавливаем таймер
                if (_pauseOverlay == null)
                {
                    _pauseOverlay = new Image
                    {
                        Source = new BitmapImage(new Uri("pack://application:,,,/Image/wait.png")),
                        Stretch = Stretch.Fill,
                        Width = MyCanvas.Width,
                        Height = MyCanvas.Height,
                        Visibility = Visibility.Visible
                    };
                    Canvas.SetLeft(_pauseOverlay, 0);
                    Canvas.SetTop(_pauseOverlay, 0);
                    MyCanvas.Children.Add(_pauseOverlay);
                }
                _pauseOverlay.Visibility = Visibility.Visible;

                foreach (var hint in MyCanvas.Children.OfType<TextBlock>())
                {
                    hint.Visibility = Visibility.Hidden;
                }

                _timer.Stop();
            }
            _isPaused = !_isPaused;
        }


        private void StartTimer()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) =>
            {
                _elapsedSeconds++;
                TimerText.Text = TimeSpan.FromSeconds(_elapsedSeconds).ToString(@"hh\:mm\:ss");
            };
            _timer.Start();
        }

        private void ResetTimer()
        {
            _elapsedSeconds = 0;
            TimerText.Text = "00:00:00";
        }

        private void SetNightMode(bool isNightMode)
        {
            var background = isNightMode ? Brushes.Black : Brushes.White;
            var foreground = isNightMode ? Brushes.White : Brushes.Black;

            MyCanvas.Background = background;
            TimerText.Foreground = foreground;
            foreach (var child in MyCanvas.Children.OfType<UIElement>())
            {
                if (child is Border border)
                    border.BorderBrush = foreground;
                if (child is TextBlock textBlock)
                    textBlock.Foreground = foreground;
            }
        }
        private void ToggleShowCoordinates(bool show)
        {
            _showCoordinates = show;
            ShowCoordinates(_showCoordinates);
        }

        private void ToggleHideTimer(bool hide)
        {
            _hideTimer = hide;
            TimerText.Visibility = hide ? Visibility.Hidden : Visibility.Visible;
        }
        private void ToggleNightMode(bool enable)
        {
            _nightMode = enable;
            SetNightMode(enable);
        }


        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsMenu = new ContextMenu();

            var showCoordsMenuItem = new MenuItem { Header = "Показывать координаты на поле", IsCheckable = true, IsChecked = _showCoordinates };
            showCoordsMenuItem.Click += (s, ev) =>
            {
                _showCoordinates = !_showCoordinates;
                ShowCoordinates(_showCoordinates);
                showCoordsMenuItem.IsChecked = _showCoordinates;
            };

            var hideTimerMenuItem = new MenuItem { Header = "Скрыть таймер", IsCheckable = true, IsChecked = _hideTimer };
            hideTimerMenuItem.Click += (s, ev) =>
            {
                _hideTimer = !_hideTimer;
                TimerText.Visibility = _hideTimer ? Visibility.Hidden : Visibility.Visible;
                hideTimerMenuItem.IsChecked = _hideTimer;
            };

            var nightModeMenuItem = new MenuItem { Header = "Ночной режим", IsCheckable = true, IsChecked = _nightMode };
            nightModeMenuItem.Click += (s, ev) =>
            {
                _nightMode = !_nightMode;
                SetNightMode(_nightMode);
                nightModeMenuItem.IsChecked = _nightMode;
            };

            settingsMenu.Items.Add(showCoordsMenuItem);
            settingsMenu.Items.Add(hideTimerMenuItem);
            settingsMenu.Items.Add(nightModeMenuItem);

            settingsMenu.PlacementTarget = SettingsButton;
            settingsMenu.IsOpen = true;
        }


        private void ShowCoordinates(bool show)
        {
            // Удаляем старые координаты
            var existingCoordinates = MyCanvas.Children.OfType<TextBlock>().Where(tb => tb.Tag != null && tb.Tag.ToString() == "Coordinate");
            foreach (var coord in existingCoordinates.ToList())
            {
                MyCanvas.Children.Remove(coord);
            }

            if (!show) return;

            // Отображаем буквенные обозначения сверху с отступом в 30 пикселей
            for (int x = 0; x < size; x++)
            {
                var letter = (char)('A' + x);
                var colCoord = new TextBlock
                {
                    Text = letter.ToString(),
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    Tag = "Coordinate"
                };
                Canvas.SetLeft(colCoord, x * 40 + 15);
                Canvas.SetTop(colCoord, -45); // Отступ на 30 пикселей от верхней границы
                MyCanvas.Children.Add(colCoord);
            }

            // Отображаем цифровые обозначения сбоку с отступом в 30 пикселей
            for (int y = 0; y < size; y++)
            {
                var rowCoord = new TextBlock
                {
                    Text = (y + 1).ToString(),
                    FontSize = 14,
                    Foreground = Brushes.Gray,
                    Tag = "Coordinate"
                };
                Canvas.SetLeft(rowCoord, -45); // Отступ на 30 пикселей от левой границы
                Canvas.SetTop(rowCoord, y * 40 + 15);
                MyCanvas.Children.Add(rowCoord);
            }
        }


        private void DisplayHints()
        {
            // Очищаем предыдущие подсказки
            MyCanvas.Children.OfType<TextBlock>().ToList().ForEach(tb => MyCanvas.Children.Remove(tb));

            int cellSize = 40;

            // Подсчет и отображение количества точек в каждом ряду
            for (int row = 0; row < size; row++)
            {
                int rowCount = 0; // Счетчик для ряда

                for (int col = 0; col < size; col++)
                {
                    // Проверка наличия точки (1) в ячейке
                    if (_graph.GetCellValue(row, col) == 1)
                    {
                        rowCount++;
                    }
                }

                // Отображение подсказки для текущего ряда
                TextBlock rowHint = new TextBlock
                {
                    Text = rowCount.ToString(),
                    FontSize = 14,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(rowHint, -20); // Позиция слева от ряда
                Canvas.SetTop(rowHint, row * cellSize + 15); // Центровка по высоте ряда
                MyCanvas.Children.Add(rowHint);
            }

            // Подсчет и отображение количества точек в каждом столбце
            for (int col = 0; col < size; col++)
            {
                int colCount = 0; // Счетчик для столбца

                for (int row = 0; row < size; row++)
                {
                    // Проверка наличия точки (1) в ячейке
                    if (_graph.GetCellValue(row, col) == 1)
                    {
                        colCount++;
                    }
                }

                // Отображение подсказки для текущего столбца
                TextBlock colHint = new TextBlock
                {
                    Text = colCount.ToString(),
                    FontSize = 14,
                    Foreground = Brushes.Black
                };

                Canvas.SetLeft(colHint, col * cellSize + 15); // Центровка по ширине столбца
                Canvas.SetTop(colHint, -20); // Позиция над столбцом
                MyCanvas.Children.Add(colHint);
            }
        }


        private void DrawThickerConnectionLine(Node node1, Node node2)
        {
            double cellSize = 40;
            double lineThickness = 4; // Увеличенная толщина стежка
            double dotSize = 10;      // Размер точки
            double centerOffset = cellSize / 2;

            // Создаем линию соединения между узлами
            Line line = new Line
            {
                X1 = node1.X * cellSize + centerOffset,
                Y1 = node1.Y * cellSize + centerOffset,
                X2 = node2.X * cellSize + centerOffset,
                Y2 = node2.Y * cellSize + centerOffset,
                Stroke = Brushes.Black,
                StrokeThickness = lineThickness // Задаем увеличенную толщину линии
            };
            MyCanvas.Children.Add(line);

            // Добавляем точку на начале стежка
            Ellipse startDot = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = Brushes.Black
            };

            // Устанавливаем позицию точки на начале линии
            Canvas.SetLeft(startDot, node1.X * cellSize + centerOffset - dotSize / 2);
            Canvas.SetTop(startDot, node1.Y * cellSize + centerOffset - dotSize / 2);
            MyCanvas.Children.Add(startDot);

            // Добавляем точку на конце стежка
            Ellipse endDot = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = Brushes.Black
            };

            // Устанавливаем позицию точки на конце линии
            Canvas.SetLeft(endDot, node2.X * cellSize + centerOffset - dotSize / 2);
            Canvas.SetTop(endDot, node2.Y * cellSize + centerOffset - dotSize / 2);
            MyCanvas.Children.Add(endDot);
        }



        // Метод для з'єднання всіх сусідніх блоків
        private void DrawConnections()
        {
            var visitedConnections = new HashSet<(int, int)>();

            foreach (var node in _graph.Nodes)
            {
                if (node.HasConnection) continue;

                foreach (var neighbor in GetNeighboringBlocks(node))
                {
                    var connection = node.GroupID < neighbor.GroupID
                        ? (node.GroupID, neighbor.GroupID)
                        : (neighbor.GroupID, node.GroupID);

                    if (!visitedConnections.Contains(connection))
                    {
                        DrawThickerConnectionLine(node, neighbor); // Обновленный вызов метода
                        visitedConnections.Add(connection);

                        node.HasConnection = true;
                        neighbor.HasConnection = true;
                        break;
                    }
                }
            }
        }


        // Метод для отримання сусідніх блоків, які мають інший GroupID
        private List<Node> GetNeighboringBlocks(Node node)
        {
            List<Node> neighbors = new List<Node>();

            // Верхній сусід
            var topNeighbor = _graph.Nodes.FirstOrDefault(n => n.X == node.X && n.Y == node.Y - 1);
            if (topNeighbor != null && topNeighbor.GroupID != node.GroupID && !topNeighbor.HasConnection)
                neighbors.Add(topNeighbor);

            // Нижній сусід
            var bottomNeighbor = _graph.Nodes.FirstOrDefault(n => n.X == node.X && n.Y == node.Y + 1);
            if (bottomNeighbor != null && bottomNeighbor.GroupID != node.GroupID && !bottomNeighbor.HasConnection)
                neighbors.Add(bottomNeighbor);

            // Лівий сусід
            var leftNeighbor = _graph.Nodes.FirstOrDefault(n => n.X == node.X - 1 && n.Y == node.Y);
            if (leftNeighbor != null && leftNeighbor.GroupID != node.GroupID && !leftNeighbor.HasConnection)
                neighbors.Add(leftNeighbor);

            // Правий сусід
            var rightNeighbor = _graph.Nodes.FirstOrDefault(n => n.X == node.X + 1 && n.Y == node.Y);
            if (rightNeighbor != null && rightNeighbor.GroupID != node.GroupID && !rightNeighbor.HasConnection)
                neighbors.Add(rightNeighbor);

            return neighbors;
        }
        // Метод для малювання лінії з'єднання між двома вузлами
        private void DrawConnectionLine(Node node1, Node node2)
        {
            double cellSize = 40; // Размер клетки
            double centerOffset = cellSize / 2;

            // Создаем линию соединения между узлами
            Line line = new Line
            {
                X1 = node1.X * cellSize + centerOffset,
                Y1 = node1.Y * cellSize + centerOffset,
                X2 = node2.X * cellSize + centerOffset,
                Y2 = node2.Y * cellSize + centerOffset,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            MyCanvas.Children.Add(line);

            // Размер точки (увеличенный)
            double dotSize = 10;

            // Добавляем точку на начале стежка
            Ellipse startDot = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = Brushes.Black
            };

            // Устанавливаем позицию точки на начале линии
            Canvas.SetLeft(startDot, node1.X * cellSize + centerOffset - dotSize / 2);
            Canvas.SetTop(startDot, node1.Y * cellSize + centerOffset - dotSize / 2);
            MyCanvas.Children.Add(startDot);

            // Добавляем точку на конце стежка
            Ellipse endDot = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = Brushes.Black
            };

            // Устанавливаем позицию точки на конце линии
            Canvas.SetLeft(endDot, node2.X * cellSize + centerOffset - dotSize / 2);
            Canvas.SetTop(endDot, node2.Y * cellSize + centerOffset - dotSize / 2);
            MyCanvas.Children.Add(endDot);
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


        private void ToggleDot(int x, int y)
        {
            // Определяем позицию точки
            double cellSize = 40;
            double dotSize = 10;
            double centerOffset = cellSize / 2 - dotSize / 2;

            // Проверяем наличие точки в данной клетке
            var dotInCell = MyCanvas.Children.OfType<Ellipse>().FirstOrDefault(e =>
                Canvas.GetLeft(e) == x * cellSize + centerOffset && Canvas.GetTop(e) == y * cellSize + centerOffset);

            if (dotInCell != null)
            {
                // Если точка связана со стежком, удаляем весь стежок
                if (dotInCell.Tag is Line line)
                {
                    RemoveConnection(line);
                }
                else
                {
                    // Если точка не связана со стежком, удаляем только точку
                    MyCanvas.Children.Remove(dotInCell);
                }
            }
            else
            {
                // Если точки нет, добавляем новую точку без стежка
                Ellipse dot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = Brushes.Black
                };
                Canvas.SetLeft(dot, x * cellSize + centerOffset);
                Canvas.SetTop(dot, y * cellSize + centerOffset);
                MyCanvas.Children.Add(dot);
            }
        }


        private void StartDrawing(Node node)
        {
            _startNode = node;  // Сохраняем начальный узел
            _isDragging = true;

            // Добавляем точку на начальной клетке, если её нет
            ToggleDot(node.X, node.Y);
        }

        private void ContinueDrawing(Node currentNode)
        {
            if (_isDragging && _startNode != null && currentNode != _startNode)
            {
                // Проверяем, является ли текущая клетка соседней
                bool isNeighbor = Math.Abs(_startNode.X - currentNode.X) + Math.Abs(_startNode.Y - currentNode.Y) == 1;

                if (isNeighbor)
                {
                    if (_startNode.GroupID != currentNode.GroupID)
                    {
                        // Если это переход в другой блок, рисуем стежок
                        ToggleConnection(_startNode, currentNode);
                    }
                    else
                    {
                        // Если это тот же блок, добавляем вторую точку
                        ToggleDot(currentNode.X, currentNode.Y);
                    }
                    _isDragging = false; // Останавливаем рисование
                }
            }
        }

        private void EndDrawing(Node node)
        {
            _isDragging = false; // Завершаем процесс рисования
            _startNode = null;
        }



        private void DrawGraph()
        {
            MyCanvas.Children.Clear();
            double cellSize = 40;
            double clickableSize = 10; // Размер кликабельной области

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
                    Background = Brushes.Transparent, // Устанавливаем фон, чтобы Border реагировал на клики
                    Tag = node
                };

                // Обработчики для начала и конца рисования
                border.MouseLeftButtonDown += (s, e) => StartDrawing(node);
                border.MouseMove += (s, e) => ContinueDrawing(node);
                border.MouseLeftButtonUp += (s, e) => EndDrawing(node);

                // Добавляем кликабельные области только для соседей с разными GroupID
                if (topNeighbor != null && topNeighbor.GroupID != node.GroupID)
                    MyCanvas.Children.Add(CreateClickableEdge(node, topNeighbor, "top", cellSize, clickableSize));

                if (bottomNeighbor != null && bottomNeighbor.GroupID != node.GroupID)
                    MyCanvas.Children.Add(CreateClickableEdge(node, bottomNeighbor, "bottom", cellSize, clickableSize));

                if (leftNeighbor != null && leftNeighbor.GroupID != node.GroupID)
                    MyCanvas.Children.Add(CreateClickableEdge(node, leftNeighbor, "left", cellSize, clickableSize));

                if (rightNeighbor != null && rightNeighbor.GroupID != node.GroupID)
                    MyCanvas.Children.Add(CreateClickableEdge(node, rightNeighbor, "right", cellSize, clickableSize));

                // Устанавливаем позицию и добавляем ячейку на Canvas
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

        private void RemoveConnection(Line line)
        {
            MyCanvas.Children.Remove(line);
            if (line.Tag is List<Ellipse> dots)
            {
                foreach (var dot in dots)
                {
                    MyCanvas.Children.Remove(dot);
                }
            }
        }


        private void RemoveDotAt(int x, int y)
        {
            double cellSize = 40;
            double dotSize = 10;
            double centerOffset = cellSize / 2 - dotSize / 2;

            // Проверяем наличие точки на данных координатах и удаляем её
            var existingDot = MyCanvas.Children.OfType<Ellipse>()
                .FirstOrDefault(e => Canvas.GetLeft(e) == x * cellSize + centerOffset && Canvas.GetTop(e) == y * cellSize + centerOffset);

            if (existingDot != null)
            {
                MyCanvas.Children.Remove(existingDot);
            }
        }



        private void ToggleConnection(Node node, Node neighbor)
        {
            double cellSize = 40;
            double lineThickness = 4;
            double dotSize = 10;
            double centerOffset = cellSize / 2;

            // Проверяем, есть ли уже линия (стежок) между узлами
            if (_activeConnections.TryGetValue(node, out Line existingLineNode))
            {
                RemoveConnection(existingLineNode); // Удаляем стежок, если он существует
                _activeConnections.Remove(node);
                _activeConnections.Remove(neighbor);
                return;
            }

            RemoveDotAt(node.X, node.Y);
            RemoveDotAt(neighbor.X, neighbor.Y);

            // Создаем новую линию (стежок)
            Line newLine = new Line
            {
                X1 = node.X * cellSize + centerOffset,
                Y1 = node.Y * cellSize + centerOffset,
                X2 = neighbor.X * cellSize + centerOffset,
                Y2 = neighbor.Y * cellSize + centerOffset,
                Stroke = Brushes.Black,
                StrokeThickness = lineThickness
            };

            MyCanvas.Children.Add(newLine);

            // Создаем точки и связываем их со стежком
            Ellipse startDot = CreateDot(node, dotSize, centerOffset, newLine);
            Ellipse endDot = CreateDot(neighbor, dotSize, centerOffset, newLine);

            // Устанавливаем событие удаления для точек и линии
            startDot.MouseLeftButtonDown += (s, e) => { RemoveConnection(newLine); e.Handled = true; };
            endDot.MouseLeftButtonDown += (s, e) => { RemoveConnection(newLine); e.Handled = true; };
            newLine.MouseLeftButtonDown += (s, e) => { RemoveConnection(newLine); e.Handled = true; };

            // Связываем точки с линией для удобного удаления
            newLine.Tag = new List<Ellipse> { startDot, endDot };
            _activeConnections[node] = newLine;
            _activeConnections[neighbor] = newLine;
        }

        // Метод создания точки для подключения к стежку
        private Ellipse CreateDot(Node node, double dotSize, double centerOffset, Line associatedLine = null)
        {
            Ellipse dot = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = Brushes.Black,
                Tag = associatedLine // Связываем точку со стежком, если он есть
            };

            Canvas.SetLeft(dot, node.X * 40 + centerOffset - dotSize / 2);
            Canvas.SetTop(dot, node.Y * 40 + centerOffset - dotSize / 2);
            MyCanvas.Children.Add(dot);

            // Устанавливаем событие для удаления точки при нажатии на нее, если она не привязана к стежку
            dot.MouseLeftButtonDown += (s, e) =>
            {
                RemoveDotIfNoConnection(dot);
                e.Handled = true; // Останавливаем распространение события
            };

            return dot;
        }


        private void RemoveDotIfNoConnection(Ellipse dot)
        {
            // Проверяем, является ли точка частью стежка
            if (dot.Tag == null)
            {
                // Если точка не привязана к стежку, удаляем ее
                MyCanvas.Children.Remove(dot);
            }
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
            // Если игра на паузе, снимаем её с паузы перед рестартом
            if (_isPaused)
            {
                _pauseOverlay.Visibility = Visibility.Hidden;
                foreach (var hint in MyCanvas.Children.OfType<TextBlock>())
                {
                    hint.Visibility = Visibility.Visible;
                }
                _timer.Start();
                _isPaused = false;
            }

            // Удаляем старый оверлей паузы, если он есть
            if (_pauseOverlay != null)
            {
                MyCanvas.Children.Remove(_pauseOverlay);
                _pauseOverlay = null;
            }

            MyCanvas.Children.Clear();
            _graph = new Graph();
            _graph.GenerateLevel(size);
            GenerateValidLevel(size);

            DrawGraph();
            DrawConnections();

            // Генерируем подсказки для точек и стежков
            DisplayHints();

            // Очищаем все точки и стежки после генерации подсказок
            ClearStitchesAndDots();

            _activeConnections.Clear();

            ResetTimer();
        }

    }
}
