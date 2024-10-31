using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Xml;

namespace LogicDiagramEditor
{
    public partial class MainWindow : Window
    {
        private List<LogicBlock> blocks = new List<LogicBlock>();
        private int currentStep = 0;
        private bool isDragging = false;
        private Point clickPosition;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void AddAndButton_Click(object sender, RoutedEventArgs e) => AddLogicBlock("AND");
        private void AddOrButton_Click(object sender, RoutedEventArgs e) => AddLogicBlock("OR");
        private void AddNotButton_Click(object sender, RoutedEventArgs e) => AddLogicBlock("NOT");
        private void AddXorButton_Click(object sender, RoutedEventArgs e) => AddLogicBlock("XOR");
        private void AddNandButton_Click(object sender, RoutedEventArgs e) => AddLogicBlock("NAND");
        private void AddNorButton_Click(object sender, RoutedEventArgs e) => AddLogicBlock("NOR");

        private void AddLogicBlock(string type)
        {
            var block = new LogicBlock(type);
            blocks.Add(block);

            // Создаем прямоугольник для блока
            Rectangle rectangle = new Rectangle
            {
                Width = 100,
                Height = 50,
                Fill = Brushes.LightGray,
                Stroke = Brushes.Black
            };

            // Создаем текстовый блок
            TextBlock textBlock = new TextBlock
            {
                Text = type,
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Создание контейнера для блока
            var grid = new Grid();
            grid.Children.Add(rectangle);
            grid.Children.Add(textBlock);
            Canvas.SetLeft(grid, 50);
            Canvas.SetTop(grid, 50 + (blocks.Count - 1) * 60);

            // Добавление блока на диаграмму
            DiagramCanvas.Children.Add(grid);
            block.VisualElement = grid;

            // Обработчики событий для перемещения
            rectangle.MouseDown += Rectangle_MouseDown;
            rectangle.MouseMove += Rectangle_MouseMove;
            rectangle.MouseUp += Rectangle_MouseUp;
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
            clickPosition = e.GetPosition(DiagramCanvas);
            ((Rectangle)sender).CaptureMouse();
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && sender is Rectangle rectangle)
            {
                var currentPosition = e.GetPosition(DiagramCanvas);
                var offset = currentPosition - clickPosition;

                var parentGrid = (Grid)rectangle.Parent;
                double left = Canvas.GetLeft(parentGrid) + offset.X;
                double top = Canvas.GetTop(parentGrid) + offset.Y;

                Canvas.SetLeft(parentGrid, left);
                Canvas.SetTop(parentGrid, top);

                clickPosition = currentPosition; // Обновляем позицию клика
            }
        }

        private void Rectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            ((Rectangle)sender).ReleaseMouseCapture();
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            // Установка начальных условий
            foreach (var block in blocks)
            {
                block.SetInputValues(1); // Устанавливаем начальное значение, пример
            }
            ExecuteDiagram();
        }

        private void ExecuteDiagram()
        {
            foreach (var block in blocks)
            {
                block.Execute();
                var rectangle = (Rectangle)((Grid)block.VisualElement).Children[0];
                rectangle.Fill = block.OutputValue == 1 ? Brushes.Green : Brushes.Red;
            }
        }

        private void StepButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep < blocks.Count)
            {
                var block = blocks[currentStep];
                block.Execute();
                var rectangle = (Rectangle)((Grid)block.VisualElement).Children[0];
                rectangle.Fill = block.OutputValue == 1 ? Brushes.Green : Brushes.Red;
                currentStep++;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveDiagramAsJson();
        }

        private void SaveDiagramAsJson()
        {
            var diagram = new DiagramModel
            {
                Blocks = new List<DiagramBlock>()
            };

            foreach (var block in blocks)
            {
                var grid = (Grid)block.VisualElement;
                double left = Canvas.GetLeft(grid);
                double top = Canvas.GetTop(grid);

                diagram.Blocks.Add(new DiagramBlock
                {
                    Type = block.Type,
                    Left = left,
                    Top = top,
                    OutputValue = block.OutputValue
                });
            }

            string json = JsonConvert.SerializeObject(diagram, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("diagram.json", json);
            MessageBox.Show("Диаграмма сохранена в diagram.json");
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadDiagramFromJson();
        }

        private void LoadDiagramFromJson()
        {
            if (!File.Exists("diagram.json"))
            {
                MessageBox.Show("Файл diagram.json не найден.");
                return;
            }

            string json = File.ReadAllText("diagram.json");
            var diagram = JsonConvert.DeserializeObject<DiagramModel>(json);

            blocks.Clear();
            DiagramCanvas.Children.Clear();

            foreach (var block in diagram.Blocks)
            {
                var logicBlock = new LogicBlock(block.Type);
                blocks.Add(logicBlock);

                // Создаем прямоугольник для блока
                Rectangle rectangle = new Rectangle
                {
                    Width = 100,
                    Height = 50,
                    Fill = Brushes.LightGray,
                    Stroke = Brushes.Black
                };

                // Создаем текстовый блок
                TextBlock textBlock = new TextBlock
                {
                    Text = block.Type,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                // Создание контейнера для блока
                var grid = new Grid();
                grid.Children.Add(rectangle);
                grid.Children.Add(textBlock);
                Canvas.SetLeft(grid, block.Left);
                Canvas.SetTop(grid, block.Top);

                // Добавление блока на диаграмму
                DiagramCanvas.Children.Add(grid);
                logicBlock.VisualElement = grid;
            }

            MessageBox.Show("Диаграмма загружена из diagram.json");
        }
    }

    public class LogicBlock
    {
        public string Type { get; }
        public int OutputValue { get; private set; }
        public UIElement VisualElement { get; set; } // Ссылка на визуальный элемент

        public LogicBlock(string type)
        {
            Type = type;
        }

        public void SetInputValues(int inputValue)
        {
            OutputValue = inputValue;
        }

        public void Execute()
        {
            // Выполнение логической операции
            switch (Type)
            {
                case "AND":
                    OutputValue = 1; // Пример
                    break;
                case "OR":
                    OutputValue = 1; // Пример
                    break;
                case "NOT":
                    OutputValue = OutputValue == 1 ? 0 : 1; // Пример
                    break;
                case "XOR":
                    OutputValue = OutputValue == 1 ? 0 : 1; // Пример
                    break;
                case "NAND":
                    OutputValue = OutputValue == 1 ? 0 : 1; // Пример
                    break;
                case "NOR":
                    OutputValue = OutputValue == 1 ? 0 : 1; // Пример
                    break;
            }
        }
    }

    [Serializable]
    public class DiagramModel
    {
        public List<DiagramBlock> Blocks { get; set; }
    }

    [Serializable]
    public class DiagramBlock
    {
        public string Type { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public int OutputValue { get; set; }
    }
}
