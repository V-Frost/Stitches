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
        int cellSize = 40; // Розмір однієї клітинки в пікселях

        private Dictionary<Node, Line> _activeConnections = new Dictionary<Node, Line>();
        public MainWindow()
        {
            InitializeComponent();
            MyCanvas.Width = size * cellSize;
            MyCanvas.Height = size * cellSize;
            _graph = new Graph();
            _graph.GenerateLevel(size);
            GenerateValidLevel(size);
            DrawGraph();
            DrawConnections();
            DisplayHints(cellSize);
            //ClearStitchesAndDots();
            StartTimer(); // Запуск таймера при инициализации окна
        }

        private List<(Node, Node)> _initialConnections = new List<(Node, Node)>(); // Список для начальных красных стежков

        private bool _showCoordinates = false;
        private bool _hideTimer = false;
        private bool _nightMode = false;

        private DispatcherTimer _timer;
        private int _elapsedSeconds;

        private Image _pauseOverlay;
        private bool _isPaused = false;

        private Node _startNode;
        private bool _isDragging;

        private int _correctStitchCount;

        private string currentMode = "Common"; // По умолчанию - режим рисования стежков
        private SolidColorBrush selectedColor = Brushes.Yellow; // Цвет по умолчанию

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
                // Знімаємо паузу: показуємо накладання та підказки, перезапускаємо таймер
                _pauseOverlay.Visibility = Visibility.Hidden;

                // Відновлюємо видимість всіх підказок
                foreach (var hint in MyCanvas.Children.OfType<TextBlock>())
                {
                    hint.Visibility = Visibility.Visible;
                }

                // Відновлюємо видимість всіх з'єднань (ліній)
                foreach (var connection in MyCanvas.Children.OfType<Line>())
                {
                    connection.Visibility = Visibility.Visible;
                }

                // Перезапускаємо таймер
                _timer.Start();
            }
            else
            {
                // Ставимо гру на паузу: приховуємо накладання і підказки, зупиняємо таймер
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

                // Приховуємо всі підказки
                foreach (var hint in MyCanvas.Children.OfType<TextBlock>())
                {
                    hint.Visibility = Visibility.Hidden;
                }

                // Приховуємо всі з'єднання (лінії)
                foreach (var connection in MyCanvas.Children.OfType<Line>())
                {
                    connection.Visibility = Visibility.Hidden;
                }

                // Зупиняємо таймер
                _timer.Stop();
            }

            // Перемикаємо стан паузи
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
            ThemeManager.SetNightMode(isNightMode);

            // Set main window and canvas colors
            this.Background = ThemeManager.BackgroundColor;
            MyCanvas.Background = ThemeManager.CanvasBackgroundColor;

            // Set the timer color
            TimerText.Foreground = ThemeManager.TimerColor;

            // Update colors for other elements inside the canvas
            foreach (var child in MyCanvas.Children.OfType<UIElement>())
            {
                if (child is Border border)
                    border.BorderBrush = ThemeManager.BorderColor;
                if (child is TextBlock textBlock)
                    textBlock.Foreground = ThemeManager.ForegroundColor;
            }

            // Set slider label colors based on night mode
            SliderLabel5.Foreground = ThemeManager.HintColor;
            SliderLabel7.Foreground = ThemeManager.HintColor;
            SliderLabel10.Foreground = ThemeManager.HintColor;
            SliderLabel15.Foreground = ThemeManager.HintColor;
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
            SetNightMode(enable); // Calls the refactored SetNightMode method
        }



        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsMenu = new ContextMenu();

            var showCoordsMenuItem = new MenuItem { Header = "Показувати координати на пол", IsCheckable = true, IsChecked = _showCoordinates };
            showCoordsMenuItem.Click += (s, ev) =>
            {
                _showCoordinates = !_showCoordinates;
                ShowCoordinates(_showCoordinates);
                showCoordsMenuItem.IsChecked = _showCoordinates;
            };

            var hideTimerMenuItem = new MenuItem { Header = "Приховати таймер", IsCheckable = true, IsChecked = _hideTimer };
            hideTimerMenuItem.Click += (s, ev) =>
            {
                _hideTimer = !_hideTimer;
                TimerText.Visibility = _hideTimer ? Visibility.Hidden : Visibility.Visible;
                hideTimerMenuItem.IsChecked = _hideTimer;
            };

            var nightModeMenuItem = new MenuItem { Header = "Нічний режим", IsCheckable = true, IsChecked = _nightMode };
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
            // Remove old coordinates
            var existingCoordinates = MyCanvas.Children.OfType<TextBlock>().Where(tb => tb.Tag != null && tb.Tag.ToString() == "Coordinate");
            foreach (var coord in existingCoordinates.ToList())
            {
                MyCanvas.Children.Remove(coord);
            }

            if (!show) return;

            // Get the current color from the ThemeManager based on night mode
            var coordinateColor = ThemeManager.HintColor; // Assume this is the correct color, based on night mode settings

            // Display column coordinates (letters) at the top
            for (int x = 0; x < size; x++)
            {
                var letter = (char)('A' + x);
                var colCoord = new TextBlock
                {
                    Text = letter.ToString(),
                    FontSize = 14,
                    Foreground = coordinateColor, // Dynamic color based on theme
                    Tag = "Coordinate"
                };

                // Adjust position based on cellSize with a slight upward adjustment
                Canvas.SetLeft(colCoord, x * cellSize + (cellSize / 2) - 7); // Centered in the cell horizontally
                Canvas.SetTop(colCoord, -cellSize / 2 - 20); // Positioned slightly above the grid
                MyCanvas.Children.Add(colCoord);
            }

            // Display row coordinates (numbers) on the left
            for (int y = 0; y < size; y++)
            {
                var rowCoord = new TextBlock
                {
                    Text = (y + 1).ToString(),
                    FontSize = 14,
                    Foreground = coordinateColor, // Dynamic color based on theme
                    Tag = "Coordinate"
                };

                // Adjust position based on cellSize with a slight leftward adjustment
                Canvas.SetLeft(rowCoord, -cellSize / 2 - 25); // Positioned slightly closer to the grid on the left
                Canvas.SetTop(rowCoord, y * cellSize + (cellSize / 2) - 7); // Centered in the cell vertically
                MyCanvas.Children.Add(rowCoord);
            }
        }


        private void DisplayHints(double cellSize)
        {
            // Clear previous hints
            MyCanvas.Children.OfType<TextBlock>().ToList().ForEach(tb => MyCanvas.Children.Remove(tb));

            // Display row hints
            for (int row = 0; row < size; row++)
            {
                int rowCount = _graph.Nodes.Count(n => n.Y == row && n.HasConnection);

                TextBlock rowHint = new TextBlock
                {
                    Text = rowCount.ToString(),
                    FontSize = 14,
                    Foreground = ThemeManager.HintColor // Use ThemeManager for hint color
                };

                Canvas.SetLeft(rowHint, -cellSize / 2);
                Canvas.SetTop(rowHint, row * cellSize + (cellSize / 2) - 7);
                MyCanvas.Children.Add(rowHint);
            }

            // Display column hints
            for (int col = 0; col < size; col++)
            {
                int colCount = _graph.Nodes.Count(n => n.X == col && n.HasConnection);

                TextBlock colHint = new TextBlock
                {
                    Text = colCount.ToString(),
                    FontSize = 14,
                    Foreground = ThemeManager.HintColor // Use ThemeManager for hint color
                };

                Canvas.SetLeft(colHint, col * cellSize + (cellSize / 2) - 7);
                Canvas.SetTop(colHint, -cellSize / 2);
                MyCanvas.Children.Add(colHint);
            }
        }


        private void DrawThickerConnectionLine(Node node1, Node node2)
        {
            double cellSize = 40;
            double lineThickness = 4;
            double dotSize = 10;
            double centerOffset = cellSize / 2;

            // Добавляем соединение в список начальных красных стежков
            _initialConnections.Add((node1, node2));

            //response
            // Отрисовка линии и точек, как в предыдущем примере
            Line line = new Line
            {
                X1 = node1.X * cellSize + centerOffset,
                Y1 = node1.Y * cellSize + centerOffset,
                X2 = node2.X * cellSize + centerOffset,
                Y2 = node2.Y * cellSize + centerOffset,
                Stroke = Brushes.Red,
                StrokeThickness = lineThickness
            };
            MyCanvas.Children.Add(line);

            Ellipse startDot = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = Brushes.Red
            };
            Canvas.SetLeft(startDot, node1.X * cellSize + centerOffset - dotSize / 2);
            Canvas.SetTop(startDot, node1.Y * cellSize + centerOffset - dotSize / 2);
            MyCanvas.Children.Add(startDot);

            Ellipse endDot = new Ellipse
            {
                Width = dotSize,
                Height = dotSize,
                Fill = Brushes.Red
            };
            Canvas.SetLeft(endDot, node2.X * cellSize + centerOffset - dotSize / 2);
            Canvas.SetTop(endDot, node2.Y * cellSize + centerOffset - dotSize / 2);
            MyCanvas.Children.Add(endDot);
        }


        private bool AreConnectionsMatching()
        {
            // Проверка, что количество стежков совпадает
            if (_activeConnections.Count > _correctStitchCount)
            {
                MessageBox.Show("Стежков слишком много");
                return false; // Если стежков больше, возвращаем ложь
            }
            else if (_activeConnections.Count < _correctStitchCount)
            {
                MessageBox.Show("Стежков недостаточно");
                return false; // Если стежков меньше, возвращаем ложь
            }

            // Проверка, что каждый стежок пользователя совпадает с выигрышной конфигурацией
            foreach (var connection in _initialConnections)
            {
                if (!_activeConnections.ContainsKey(connection.Item1) ||
                    !_activeConnections[connection.Item1].Tag.Equals(_activeConnections[connection.Item2].Tag))
                {
                    MessageBox.Show("Стежки не соответствуют выигрышному варианту");
                    return false; // Если хотя бы один стежок не совпадает, возвращаем ложь
                }
            }

            return true; // Все стежки совпадают
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
                        DrawThickerConnectionLine(node, neighbor); // Отрисовка линии стежка

                        // Добавляем стежок в список начальных стежков
                        _initialConnections.Add((node, neighbor));

                        visitedConnections.Add(connection);
                        node.HasConnection = true;
                        neighbor.HasConnection = true;
                        break;
                    }
                }
            }

            //// Обновляем необходимое количество стежков после их генерации
            _correctStitchCount = _initialConnections.Count;
        }

        // Обработчик для основной кнопки, чтобы показывать или скрывать панель с кнопками
        private void ChangesButton_Click(object sender, RoutedEventArgs e)
        {
            SubButtonsPanel.Visibility =
                SubButtonsPanel.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        // Обработчик для кнопки "Крестик"
        private void CrossButton_Click(object sender, RoutedEventArgs e)
        {
            currentMode = "Cross";
        }

        // Обработчик для кнопки "Обычный режим"
        private void CommonButton_Click(object sender, RoutedEventArgs e)
        {
            currentMode = "Common";
        }

        // Обработчик для кнопки "Ластик"
        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            currentMode = "Eraser";
        }

        // Обработчик для кнопки "Цвет"
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            currentMode = "Color";
            ShowColorSelectionMenu(); // Открываем меню выбора цвета
        }

        private void ShowColorSelectionMenu()
        {
            var colorMenu = new ContextMenu();

            // Определение цветовых опций
            var colors = new Dictionary<string, SolidColorBrush>
            {
                { "Yellow", Brushes.Yellow },
                { "Green", Brushes.Green },
                { "Blue", Brushes.LightBlue },
                { "Red", Brushes.Red },
                { "Gray", Brushes.Gray }
            };

            foreach (var color in colors)
            {
                var menuItem = new MenuItem { Header = color.Key, Background = color.Value };
                menuItem.Click += (s, e) =>
                {
                    selectedColor = color.Value; // Устанавливаем выбранный цвет
                };
                colorMenu.Items.Add(menuItem);
            }

            // Открываем меню под кнопкой "Цвет"
            colorMenu.PlacementTarget = ColorButton;
            colorMenu.IsOpen = true;
        }


        private void Cell_Click(object sender, MouseButtonEventArgs e)
        {
            var cell = sender as Border;

            switch (currentMode)
            {
                case "Cross":
                    AddCrossToCell(cell);
                    break;
                case "Common":
                    StartDrawingStitch(cell);
                    break;
                case "Eraser":
                    ClearCell(cell);
                    break;
                case "Color":
                    ApplySelectedColorToCell(cell);
                    break;
            }
        }

        private void AddCrossToCell(Border cell)
        {
            // Логика для добавления крестика
        }

        private void StartDrawingStitch(Border cell)
        {
            // Логика для рисования стежка
        }

        private void ClearCell(Border cell)
        {
            // Логика для очистки клетки
        }

        private void ApplySelectedColorToCell(Border cell)
        {
            // Логика для применения выбранного цвета
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

            // Устанавливаем правильное количество стежков и обновляем текст
            _correctStitchCount = _initialConnections.Count;
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
                    BorderBrush = ThemeManager.BorderColor, // Apply theme color
                    BorderThickness = borderThickness,      // Apply group boundary thickness
                    Background = Brushes.Transparent,
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
            // Удаляем линию (стежок) с холста
            MyCanvas.Children.Remove(line);

            // Удаляем связанные точки, если они есть
            if (line.Tag is List<Ellipse> dots)
            {
                foreach (var dot in dots)
                {
                    MyCanvas.Children.Remove(dot);
                }
            }

            // Находим узлы, между которыми находится удаляемый стежок
            var nodes = _activeConnections.Where(pair => pair.Value == line).Select(pair => pair.Key).ToList();

            // Удаляем запись о стежке из _activeConnections для обоих узлов
            foreach (var node in nodes)
            {
                _activeConnections.Remove(node);
            }

            // Обновляем количество текущих стежков
            UpdateCurrentStitchCount();
        }



        private void RemoveDotAt(int x, int y)
        {
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

        private void UpdateCurrentStitchCount()
        {
            //int currentStitchCount = _activeConnections.Count;
            //CurrentStitchCountText.Text = $"Текущее количество стежков: {currentStitchCount}";
        }

        private void ToggleConnection(Node node, Node neighbor)
        {
            double lineThickness = 4;
            double dotSize = 10;
            double centerOffset = cellSize / 2;

            // Перевіряємо, чи є з'єднання від node до іншого вузла
            if (_activeConnections.TryGetValue(node, out Line existingLine))
            {
                // Видаляємо існуюче з'єднання з будь-яким іншим вузлом для node
                RemoveConnection(existingLine);
            }

            // Перевіряємо, чи є з'єднання від neighbor до іншого вузла
            if (_activeConnections.TryGetValue(neighbor, out Line neighborExistingLine))
            {
                // Видаляємо існуюче з'єднання з будь-яким іншим вузлом для neighbor
                RemoveConnection(neighborExistingLine);
            }

            // Видаляємо точки на обраних вузлах, якщо вони є
            RemoveDotAt(node.X, node.Y);
            RemoveDotAt(neighbor.X, neighbor.Y);

            // Створюємо новий стежок
            Line newLine = new Line
            {
                X1 = node.X * cellSize + centerOffset,
                Y1 = node.Y * cellSize + centerOffset,
                X2 = neighbor.X * cellSize + centerOffset,
                Y2 = neighbor.Y * cellSize + centerOffset,
                Stroke = Brushes.Black,
                StrokeThickness = lineThickness
            };

            // Додаємо новий стежок на полотно
            MyCanvas.Children.Add(newLine);

            // Створюємо точки на початку і кінці стежка і прив'язуємо їх до лінії
            Ellipse startDot = CreateDot(node, dotSize, centerOffset, newLine);
            Ellipse endDot = CreateDot(neighbor, dotSize, centerOffset, newLine);

            // Прив'язуємо точки до лінії для зручного видалення
            newLine.Tag = new List<Ellipse> { startDot, endDot };
            _activeConnections[node] = newLine;
            _activeConnections[neighbor] = newLine;

            // Оновлюємо кількість поточних стежків
            UpdateCurrentStitchCount();
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

            // Устанавливаем событие для удаления точки
            dot.MouseLeftButtonDown += (s, e) =>
            {
                if (dot.Tag is Line line) // Проверяем, связана ли точка со стежком
                {
                    RemoveDotWithStitch(line);
                }
                else
                {
                    RemoveDotIfNoConnection(dot);
                }
                e.Handled = true; // Останавливаем распространение события
            };

            return dot;
        }

        // Новый метод для удаления точки вместе со стежком и обновления счетчика
        private void RemoveDotWithStitch(Line line)
        {
            int currentStitchCount = _activeConnections.Count - 2;
            RemoveConnection(line); // Удаляем стежок вместе с привязанными точками
            UpdateCurrentStitchCount(); // Обновляем количество текущих стежков
        }


        private void RemoveDotIfNoConnection(Ellipse dot)
        {
            // Проверяем, является ли точка частью стежка
            if (dot.Tag is Line line)
            {
                // Если точка привязана к стежку, удаляем весь стежок
                RemoveConnection(line);

                // Обновляем количество текущих стежков
                UpdateCurrentStitchCount();
            }
            else
            {
                // Если точка не привязана к стежку, удаляем только точку
                MyCanvas.Children.Remove(dot);
            }
        }

        private Line CreateConnectionLine(Node node, Node neighbor)
        {
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
            if (AreConnectionsMatching())
            {
                // Остановим таймер
                _timer.Stop();

                // Скрываем подсказки
                foreach (var hint in MyCanvas.Children.OfType<TextBlock>())
                {
                    hint.Visibility = Visibility.Hidden;
                }

                // Отображаем изображение победы
                Image winImage = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Image/win.png")),
                    Stretch = Stretch.Fill,
                    Width = MyCanvas.Width,
                    Height = MyCanvas.Height,
                    Visibility = Visibility.Visible
                };
                Canvas.SetLeft(winImage, 0);
                Canvas.SetTop(winImage, 0);
                MyCanvas.Children.Add(winImage);

                MessageBox.Show("Вітаємо! Усі стібки збігаються, ви виграли!");
            }
            else
            {
                MessageBox.Show("Не готове для перемоги.");
            }
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

        private double CenterCanvas()
        {
            // Розраховуємо відступ зліва для центрування
            double leftMargin = (this.ActualWidth - MyCanvas.ActualWidth) / 2;
            return leftMargin > 0 ? leftMargin : 0; // Запобігаємо негативним значенням
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
           

            switch ((int)e.NewValue)
            {
                case 0:
                    this.Height = 650;
                    this.Width = 550;
                    TimerText.Margin = new Thickness(0, 70, 0, 0);
                    cellSize = 40;
                    size = 5;
                    break;
                case 1:
                    this.Height = 650;
                    this.Width = 550;
                    TimerText.Margin = new Thickness(0, 50, 0, 0);
                    cellSize = 40;
                    size = 7;
                    break;
                case 2:
                    this.Height = 800;
                    this.Width = 770;
                    cellSize = 40;
                    size = 10;
                    break;
                case 3:
                    this.Height = 800;
                    this.Width = 770;
                    cellSize = 35;
                    size = 15;
                    TimerText.Margin = new Thickness(0, 10, 0, 0);
                    sliderSize.Margin = new Thickness(0, 0, 0, 70);
                    panelButtons.Margin = new Thickness(0, 0, 0, 10);
                    break;
            }

            // Встановлення розмірів Canvas відповідно до нового розміру поля
            MyCanvas.Width = size * cellSize;
            MyCanvas.Height = size * cellSize;

            // Очищення та оновлення вмісту Canvas
            MyCanvas.Children.Clear();
            _graph = new Graph();
            _graph.GenerateLevel(size);

            DrawGraph();
            DrawConnections();
            DisplayHints(cellSize);

            ClearStitchesAndDots();
            ResetTimer();
            PositionWindowWithinScreenBounds();
            ShowCoordinates(_showCoordinates);

        }

        private void Slider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var slider = sender as Slider;
            if (slider == null)
                return;

            var mousePosition = e.GetPosition(slider);

            double relativePosition = mousePosition.X / slider.ActualWidth;
            double newValue = relativePosition * (slider.Maximum - slider.Minimum) + slider.Minimum;

            // Округлюємо нове значення до найближчого числа для точного переходу
            slider.Value = Math.Round(newValue / slider.TickFrequency) * slider.TickFrequency;

            e.Handled = true; // Запобігаємо стандартній поведінці
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            PositionWindowWithinScreenBounds();
        }

        private void PositionWindowWithinScreenBounds()
        {
            var screenHeight = SystemParameters.WorkArea.Height;
            var screenWidth = SystemParameters.WorkArea.Width;

            if (this.Top + this.Height > screenHeight)
            {
                this.Top = screenHeight - this.Height; // Keep window within the screen vertically
            }

            if (this.Left + this.Width > screenWidth)
            {
                this.Left = screenWidth - this.Width; // Keep window within the screen horizontally
            }
        }


        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            this.Left = (SystemParameters.PrimaryScreenWidth - this.ActualWidth) / 2;
            this.Top = (SystemParameters.PrimaryScreenHeight - this.ActualHeight) / 2;

            // Центруємо основні елементи по ширині
            SettingsButton.HorizontalAlignment = HorizontalAlignment.Right;
            TimerText.HorizontalAlignment = HorizontalAlignment.Center;
            MyCanvas.HorizontalAlignment = HorizontalAlignment.Center;
            MyCanvas.VerticalAlignment = VerticalAlignment.Center;
        }



        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            // Зупиняємо та скидаємо таймер
            _timer.Stop();
            ResetTimer();

            // Очищуємо всі елементи на Canvas
            MyCanvas.Children.Clear();
            _initialConnections.Clear();  // Очищення списку початкових стежків

            // Перевіряємо, чи гра була на паузі, та знімаємо паузу
            if (_isPaused)
            {
                _pauseOverlay.Visibility = Visibility.Hidden;
                _isPaused = false;
            }

            // Генеруємо новий рівень
            _graph = new Graph();
            _graph.GenerateLevel(size);

            // Малюємо графік та з'єднання
            DrawGraph();
            DrawConnections();

            // Показуємо підказки для нового рівня
            DisplayHints(cellSize);

            // Очищуємо всі точки та стежки після генерації підказок
            ClearStitchesAndDots();
            _activeConnections.Clear();

            // Перезапускаємо таймер
            StartTimer();
        }





    }
}