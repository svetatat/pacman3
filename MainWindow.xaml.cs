using pacman3.Models;
using pacman3.Models.Game;
using pacman3.Models.Player;
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
        private GameField _gameField;
        private Player _player;
        private bool _isGameRunning = true;

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
            this.Focus();
        }

        private void InitializeGame()
        {
            _gameTime = new GameTime();

            // Создаем игровое поле
            _gameField = new GameField(10, 10);
            _gameField.Initialize();

            // Создаем игрока в центре
            double spawnX = 5 * _gameField.TileSize + _gameField.TileSize / 2;
            double spawnY = 5 * _gameField.TileSize + _gameField.TileSize / 2;
            _player = new Player(spawnX, spawnY);

            // Подписываемся на события
            _player.ScoreChanged += (s, score) =>
                Dispatcher.Invoke(() => ScoreText.Text = $"Очки: {score}");

            _player.LivesChanged += (s, lives) =>
                Dispatcher.Invoke(() => LivesText.Text = $"Жизни: {lives}");

            // Обновляем UI
            ScoreText.Text = $"Очки: {_player.Score}";
            LivesText.Text = $"Жизни: {_player.Lives}";

            // Запускаем игровой цикл
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            _gameTimer.Tick += GameLoop;
            _gameTimer.Start();

            StatusText.Text = "Игра запущена. Используйте стрелки для движения.";
        }

        private void GameLoop(object sender, EventArgs e)
        {
            if (!_isGameRunning) return;

            _gameTime.Update(_gameTimer.Interval);

            // Обновляем поле
            _gameField.Update(_gameTime.ElapsedTime);

            // Проверяем движение
            bool canMove = _gameField.CanMoveTo(_player.Position, _player.Direction, _player.Speed);

            if (canMove)
            {
                bool canTurn = _gameField.CanMoveTo(_player.Position, _player.NextDirection, _player.Speed);
                _player.ApplyNextDirection(canTurn);
                _player.Update(_gameTime.ElapsedTime);
            }

            // Проверяем точки
            var point = _gameField.GetPointAt(_player.Position);
            if (point != null && !point.IsCollected)
            {
                point.Collect();
                _player.CollectPoint(point.PointValue);
            }

            // Отрисовка
            DrawGame();
        }

        private void DrawGame()
        {
            GameCanvas.Children.Clear();
            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Отрисовываем поле
                _gameField.Draw(drawingContext);

                // Отрисовываем игрока
                _player.Draw(drawingContext);
            }

            GameCanvas.Children.Add(new DrawingVisualHost(drawingVisual));
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePause();
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            RestartGame();
        }

        private void TogglePause()
        {
            _isGameRunning = !_isGameRunning;
            PauseOverlay.Visibility = _isGameRunning ? Visibility.Collapsed : Visibility.Visible;
            PauseButton.Content = _isGameRunning ? "Пауза" : "Продолжить";
            StatusText.Text = _isGameRunning ? "Игра запущена" : "ПАУЗА";
        }

        private void RestartGame()
        {
            _gameTimer.Stop();
            InitializeGame();
            _isGameRunning = true;
            PauseOverlay.Visibility = Visibility.Collapsed;
            PauseButton.Content = "Пауза";
            StatusText.Text = "Игра перезапущена";
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!_isGameRunning && e.Key != Key.Escape && e.Key != Key.R) return;

            switch (e.Key)
            {
                case Key.Escape:
                    TogglePause();
                    break;

                case Key.R:
                    RestartGame();
                    break;

                case Key.Up:
                case Key.Down:
                case Key.Left:
                case Key.Right:
                    _player.HandleInput(e.Key);
                    break;
            }
        }

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
    }
}