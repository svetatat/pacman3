using pacman3.Interfaces;
using pacman3.Models.Game;
using pacman3.Models.Game;
using pacman3.Utils;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace pacman3
{
    public partial class MainWindow : Window
    {
        private GameTime _gameTime;
        private DispatcherTimer _gameTimer;
        private GameObject _testObject1;
        private GameObject _testObject2;

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Создаем игровое время
            _gameTime = new GameTime();

            // Создаем тестовые объекты для проверки GameObject
            _testObject1 = new TestGameObject(100, 100) { ObjectColor = Colors.Yellow };
            _testObject2 = new TestGameObject(200, 200) { ObjectColor = Colors.Red };

            // Настраиваем таймер для обновления
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _gameTimer.Tick += GameLoop;
            _gameTimer.Start();

            StatusText.Text = "Тест запущен: 2 объекта созданы";
        }

        private void GameLoop(object sender, EventArgs e)
        {
            // Обновляем игровое время
            _gameTime.Update(_gameTimer.Interval);

            // Обновляем объекты
            _testObject1.Update(_gameTime.ElapsedTime);
            _testObject2.Update(_gameTime.ElapsedTime);

            // Проверяем столкновение
            if (_testObject1.IntersectsWith(_testObject2))
            {
                _testObject1.OnCollision(_testObject2);
                _testObject2.OnCollision(_testObject1);
            }

            // Отрисовываем
            TestCanvas.Children.Clear();
            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                _testObject1.Draw(drawingContext);
                _testObject2.Draw(drawingContext);
            }

            TestCanvas.Children.Add(new DrawingVisualHost(drawingVisual));
        }

        // Вспомогательный класс для отображения DrawingVisual
        private class DrawingVisualHost : FrameworkElement
        {
            private readonly DrawingVisual _visual;

            public DrawingVisualHost(DrawingVisual visual)
            {
                _visual = visual;
            }

            protected override Visual GetVisualChild(int index) => _visual;
            protected override int VisualChildrenCount => 1;
        }

        protected override void OnClosed(EventArgs e)
        {
            _gameTimer?.Stop();
            base.OnClosed(e);
        }
    }

    /// <summary>
    /// Тестовый класс для проверки GameObject
    /// </summary>
    public class TestGameObject : GameObject
    {
        public TestGameObject(double x, double y) : base(x, y)
        {
            Size = 40;
        }

        public override void Update(TimeSpan gameTime)
        {
            // Простое движение для теста
            Position = new Vector2(
                Position.X + 0.5,
                Position.Y + 0.3
            );

            // Если вышли за границы - возвращаем
            if (Position.X > 700) Position = new Vector2(100, Position.Y);
            if (Position.Y > 500) Position = new Vector2(Position.X, 100);
        }

        public override void OnCollision(ICollidable other)
        {
            base.OnCollision(other);
            ObjectColor = Colors.Green; // Меняем цвет при столкновении
        }
    }
}