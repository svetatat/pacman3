using pacman3.Interfaces;
using pacman3.Models.Game;
using pacman3.Utils;
using System;
using System.Windows;
using System.Windows.Input;
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
            _gameTime = new GameTime();

            // Создаем объекты с помощью конструктора
            _testObject1 = new TestGameObject(100, 100);
            _testObject1.ObjectColor = Colors.Yellow;
            _testObject1.Size = 40;

            _testObject2 = new TestGameObject(200, 200);
            _testObject2.ObjectColor = Colors.Red;
            _testObject2.Size = 40;

            // Таймер игры
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            _gameTimer.Tick += GameLoop;
            _gameTimer.Start();

            StatusText.Text = "Базовые классы созданы. Объекты движутся.";
        }

        private void GameLoop(object sender, EventArgs e)
        {
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
            DrawGame();
        }

        private void DrawGame()
        {
            GameCanvas.Children.Clear();
            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                _testObject1.Draw(drawingContext);
                _testObject2.Draw(drawingContext);
            }

            GameCanvas.Children.Add(new DrawingVisualHost(drawingVisual));
        }

        // Вспомогательный класс для отрисовки
        private class DrawingVisualHost : FrameworkElement
        {
            private readonly DrawingVisual _visual;

            public DrawingVisualHost(DrawingVisual visual) => _visual = visual;
            protected override Visual GetVisualChild(int index) => _visual;
            protected override int VisualChildrenCount => 1;
        }

        protected override void OnClosed(EventArgs e)
        {
            _gameTimer?.Stop();
            base.OnClosed(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                _gameTimer.IsEnabled = !_gameTimer.IsEnabled;
                StatusText.Text = _gameTimer.IsEnabled ? "Игра запущена" : "ПАУЗА";
            }
        }
    }

    // Тестовый класс для проверки
    public class TestGameObject : GameObject
    {
        public TestGameObject(double x, double y) : base(x, y)
        {
        }

        public override void Update(TimeSpan gameTime)
        {
            // Простое движение
            Position = new Vector2(
                Position.X + 0.5,
                Position.Y + 0.3
            );

            // Возврат при выходе за границы
            if (Position.X > 700) Position = new Vector2(100, Position.Y);
            if (Position.Y > 500) Position = new Vector2(Position.X, 100);
        }

        public override void OnCollision(ICollidable other)
        {
            base.OnCollision(other);
            // Меняем цвет при столкновении
            ObjectColor = Colors.Green;
        }
    }
}