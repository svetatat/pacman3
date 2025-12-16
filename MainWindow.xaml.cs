using pacman3.Models;
using pacman3.Models.Game;
using pacman3.Models.Ghosts;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace pacman3
{
    public partial class MainWindow : Window
    {
        private GameManager _gameManager;
        private DispatcherTimer _gameTimer;
        private Player _player;
        private MainMenu _mainMenu;

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            // Инициализация игрового менеджера
            _gameManager = new GameManager();
            _gameManager.Initialize();
            _gameManager.GameStateChanged += OnGameStateChanged;
            _gameManager.ScoreChanged += OnScoreChanged;
            _gameManager.LivesChanged += OnLivesChanged;
            _gameManager.GameOver += OnGameOver;
            _gameManager.Victory += OnVictory;

            // Получаем игрока
            _player = _gameManager.GetPlayer();

            // Инициализация главного меню
            _mainMenu = new MainMenu();

            // Таймер игры
            _gameTimer = new DispatcherTimer();
            _gameTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _gameTimer.Tick += GameLoop;
            _gameTimer.Start();

            // Фокус на окне для обработки клавиш
            Focus();
        }

        private void GameLoop(object sender, EventArgs e)
        {
            // Обновление игры
            _gameManager.Update(_gameTimer.Interval);

            // Отрисовка
            DrawGame();
        }

        private void DrawGame()
        {
            GameCanvas.Children.Clear();

            var drawingVisual = new DrawingVisual();
            using (var drawingContext = drawingVisual.RenderOpen())
            {
                // Рисуем в зависимости от состояния игры
                if (_gameManager.CurrentState == GameState.MainMenu)
                {
                    _mainMenu.Draw(drawingContext);
                }
                else if (_gameManager.CurrentState == GameState.Playing ||
                         _gameManager.CurrentState == GameState.Paused ||
                         _gameManager.CurrentState == GameState.GameOver ||
                         _gameManager.CurrentState == GameState.Victory)
                {
                    // Рисуем игровое поле (стены и точки)
                    var gameField = _gameManager.GetGameField();
                    if (gameField != null)
                    {
                        gameField.Draw(drawingContext);
                    }

                    // Рисуем приведений
                    foreach (var ghost in _gameManager.GetGhosts())
                    {
                        ghost.Draw(drawingContext);
                    }

                    // Рисуем игрока
                    _player.Draw(drawingContext);

                    // Рисуем UI поверх игры
                    DrawUI(drawingContext);

                    // Рисуем сообщения в зависимости от состояния
                    if (_gameManager.CurrentState == GameState.Paused)
                    {
                        DrawPauseScreen(drawingContext);
                    }
                    else if (_gameManager.CurrentState == GameState.GameOver)
                    {
                        DrawGameOverScreen(drawingContext);
                    }
                    else if (_gameManager.CurrentState == GameState.Victory)
                    {
                        DrawVictoryScreen(drawingContext);
                    }
                }
            }

            GameCanvas.Children.Add(new DrawingVisualHost(drawingVisual));

            // Обновляем UI информацию
            UpdateGameInfo();

            // Отладочная информация
            var gameFieldInfo = _gameManager.GetGameField();
            if (gameFieldInfo != null)
            {
                System.Diagnostics.Debug.WriteLine($"Точек на поле: {gameFieldInfo.Points.Count}, собрано: {gameFieldInfo.Points.Count(p => p.IsCollected)}");
            }
        }

        private void DrawUI(DrawingContext drawingContext)
        {
            // Рисуем очки
            var scoreText = new FormattedText(
                $"Очки: {_player.Score}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                20,
                Brushes.White,
                1.0
            );
            drawingContext.DrawText(scoreText, new Point(10, 10));

            // Рисуем жизни
            var livesText = new FormattedText(
                $"Жизни: {_player.Lives}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                20,
                Brushes.White,
                1.0
            );
            drawingContext.DrawText(livesText, new Point(10, 40));

            // Рисуем состояние игры
            var stateText = new FormattedText(
                $"Состояние: {GetStateText(_gameManager.CurrentState)}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                16,
                Brushes.LightGray,
                1.0
            );
            drawingContext.DrawText(stateText, new Point(10, 70));
        }

        private void DrawPauseScreen(DrawingContext drawingContext)
        {
            // Полупрозрачный темный фон
            drawingContext.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(150, 0, 0, 0)),
                null,
                new Rect(0, 0, GameCanvas.ActualWidth, GameCanvas.ActualHeight)
            );

            // Текст паузы
            var pauseText = new FormattedText(
                "ПАУЗА",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial Bold"),
                48,
                Brushes.Yellow,
                1.0
            );
            drawingContext.DrawText(pauseText, new Point(
                (GameCanvas.ActualWidth - pauseText.Width) / 2,
                GameCanvas.ActualHeight / 2 - pauseText.Height
            ));

            var instructionText = new FormattedText(
                "Нажмите ESC для продолжения",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                20,
                Brushes.White,
                1.0
            );
            drawingContext.DrawText(instructionText, new Point(
                (GameCanvas.ActualWidth - instructionText.Width) / 2,
                GameCanvas.ActualHeight / 2 + 20
            ));
        }

        private void DrawGameOverScreen(DrawingContext drawingContext)
        {
            // Полупрозрачный темный фон
            drawingContext.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                null,
                new Rect(0, 0, GameCanvas.ActualWidth, GameCanvas.ActualHeight)
            );

            // Текст Game Over
            var gameOverText = new FormattedText(
                "ИГРА ОКОНЧЕНА",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial Bold"),
                48,
                Brushes.Red,
                1.0
            );
            drawingContext.DrawText(gameOverText, new Point(
                (GameCanvas.ActualWidth - gameOverText.Width) / 2,
                GameCanvas.ActualHeight / 2 - 60
            ));

            var scoreText = new FormattedText(
                $"Ваши очки: {_player.Score}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                24,
                Brushes.White,
                1.0
            );
            drawingContext.DrawText(scoreText, new Point(
                (GameCanvas.ActualWidth - scoreText.Width) / 2,
                GameCanvas.ActualHeight / 2
            ));

            var restartText = new FormattedText(
                "Нажмите Ctrl+R для перезапуска",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                20,
                Brushes.LightBlue,
                1.0
            );
            drawingContext.DrawText(restartText, new Point(
                (GameCanvas.ActualWidth - restartText.Width) / 2,
                GameCanvas.ActualHeight / 2 + 50
            ));
        }

        private void DrawVictoryScreen(DrawingContext drawingContext)
        {
            // Полупрозрачный зеленый фон
            drawingContext.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(180, 0, 100, 0)),
                null,
                new Rect(0, 0, GameCanvas.ActualWidth, GameCanvas.ActualHeight)
            );

            // Текст победы
            var victoryText = new FormattedText(
                "ПОБЕДА!",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial Bold"),
                48,
                Brushes.Gold,
                1.0
            );
            drawingContext.DrawText(victoryText, new Point(
                (GameCanvas.ActualWidth - victoryText.Width) / 2,
                GameCanvas.ActualHeight / 2 - 60
            ));

            var scoreText = new FormattedText(
                $"Ваши очки: {_player.Score}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                24,
                Brushes.White,
                1.0
            );
            drawingContext.DrawText(scoreText, new Point(
                (GameCanvas.ActualWidth - scoreText.Width) / 2,
                GameCanvas.ActualHeight / 2
            ));

            var restartText = new FormattedText(
                "Нажмите Ctrl+R для новой игры",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                20,
                Brushes.LightBlue,
                1.0
            );
            drawingContext.DrawText(restartText, new Point(
                (GameCanvas.ActualWidth - restartText.Width) / 2,
                GameCanvas.ActualHeight / 2 + 50
            ));
        }

        private string GetStateText(GameState state)
        {
            return state switch
            {
                GameState.MainMenu => "Главное меню",
                GameState.Playing => "Игра",
                GameState.Paused => "Пауза",
                GameState.GameOver => "Игра окончена",
                GameState.Victory => "Победа!",
                _ => "Неизвестно"
            };
        }

        private void UpdateGameInfo()
        {
            StatusText.Text = $"Жизни: {_player.Lives} | Очки: {_player.Score} | Состояние: {GetStateText(_gameManager.CurrentState)}";
        }

        private void OnGameStateChanged(object sender, GameState state)
        {
            UpdateGameInfo();
        }

        private void OnScoreChanged(object sender, int score)
        {
            UpdateGameInfo();
        }

        private void OnLivesChanged(object sender, int lives)
        {
            UpdateGameInfo();
        }

        private void OnGameOver(object sender, EventArgs e)
        {
            // Автоматический переход в состояние GameOver
        }

        private void OnVictory(object sender, EventArgs e)
        {
            // Автоматический переход в состояние Victory
        }

        // Вспомогательный класс для отрисовки
        private class DrawingVisualHost : FrameworkElement
        {
            private readonly DrawingVisual _visual;

            public DrawingVisualHost(DrawingVisual visual) => _visual = visual;
            protected override Visual GetVisualChild(int index) => _visual;
            protected override int VisualChildrenCount => 1;
        }

        // Обработка ввода с клавиатуры
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.Key)
            {
                case Key.Escape:
                    // Пауза/продолжение
                    if (_gameManager.CurrentState == GameState.Playing)
                    {
                        _gameManager.PauseGame();
                    }
                    else if (_gameManager.CurrentState == GameState.Paused)
                    {
                        _gameManager.ResumeGame();
                    }
                    else if (_gameManager.CurrentState == GameState.MainMenu)
                    {
                        // Выход из главного меню
                        _gameManager.CurrentState = GameState.Playing;
                    }
                    break;

                case Key.R:
                    // Перезапуск игры
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        _gameManager.RestartGame();
                        UpdateGameInfo();
                    }
                    break;

                // Управление Pac-Man
                case Key.Up:
                case Key.W:
                    if (_gameManager.CurrentState == GameState.Playing)
                    {
                        _player.HandleInput(e.Key);
                    }
                    break;

                case Key.Down:
                case Key.S:
                    if (_gameManager.CurrentState == GameState.Playing)
                    {
                        _player.HandleInput(e.Key);
                    }
                    break;

                case Key.Left:
                case Key.A:
                    if (_gameManager.CurrentState == GameState.Playing)
                    {
                        _player.HandleInput(e.Key);
                    }
                    break;

                case Key.Right:
                case Key.D:
                    if (_gameManager.CurrentState == GameState.Playing)
                    {
                        _player.HandleInput(e.Key);
                    }
                    break;

                case Key.Enter:
                    // Старт игры с главного меню
                    if (_gameManager.CurrentState == GameState.MainMenu)
                    {
                        _gameManager.CurrentState = GameState.Playing;
                    }
                    break;

                case Key.Space:
                    // Альтернативная пауза
                    if (_gameManager.CurrentState == GameState.Playing)
                    {
                        _gameManager.PauseGame();
                    }
                    else if (_gameManager.CurrentState == GameState.Paused)
                    {
                        _gameManager.ResumeGame();
                    }
                    break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _gameTimer?.Stop();
            base.OnClosed(e);
        }
    }
}