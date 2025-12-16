using System;
using System.Collections.Generic;
using System.Linq;
using pacman3.Models.Ghosts;
using pacman3.Models;
using pacman3.Utils;

namespace pacman3.Models.Game
{
    public class GameManager : Interfaces.IGameComponent
    {
        private GameState _currentState = GameState.Playing;
        private Player _player;
        private GameField _gameField;
        private List<Ghost> _ghosts;
        private bool _isEnergizerActive;
        private DateTime _energizerEndTime;
        private int _currentLevel = 1;

        public GameState CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                GameStateChanged?.Invoke(this, value);
            }
        }

        public int Score => _player?.Score ?? 0;
        public int Lives => _player?.Lives ?? 0;
        public int Level => _currentLevel;

        // События
        public event EventHandler<GameState> GameStateChanged;
        public event EventHandler<int> ScoreChanged;
        public event EventHandler<int> LivesChanged;
        public event EventHandler GameOver;
        public event EventHandler Victory;
        public event EventHandler<int> LevelChanged;

        public void Initialize()
        {
            _gameField = new GameField();
            _gameField.Initialize();

            // Используем точку спавна из GameField
            _player = new Player(_gameField.PlayerSpawn.X, _gameField.PlayerSpawn.Y);
            _player.ScoreChanged += (s, score) => ScoreChanged?.Invoke(this, score);
            _player.LivesChanged += (s, lives) => LivesChanged?.Invoke(this, lives);
            _player.PlayerDied += OnPlayerDied;

            InitializeGhosts();

            _gameField.AllPointsCollected += (s, e) =>
            {
                CurrentState = GameState.Victory;
                Victory?.Invoke(this, EventArgs.Empty);
            };

            CurrentState = GameState.Playing;
        }

        private void InitializeGhosts()
        {
            _ghosts = new List<Ghost>
            {
                new BlinkyGhost(),
                new PinkyGhost(),
                new InkyGhost(),
                new ClydeGhost()
            };

            // Ставим привидений в дом призраков
            var ghostHouse = _gameField.GhostHouse;
            _ghosts[0].Position = new Vector2(ghostHouse.X - 32, ghostHouse.Y);
            _ghosts[1].Position = new Vector2(ghostHouse.X + 32, ghostHouse.Y);
            _ghosts[2].Position = new Vector2(ghostHouse.X, ghostHouse.Y - 32);
            _ghosts[3].Position = new Vector2(ghostHouse.X, ghostHouse.Y + 32);

            foreach (var ghost in _ghosts)
            {
                ghost.State = GhostState.Normal;
                ghost.IsActive = true;
                ghost.SetGameField(_gameField);
            }
        }

        public void Update(TimeSpan gameTime)
        {
            if (CurrentState != GameState.Playing) return;

            // Проверка сбора точек
            CheckPointCollection();

            // Обновление игрока
            _player.Move(_gameField);
            _player.Update(gameTime);

            // Обновление привидений
            foreach (var ghost in _ghosts)
            {
                if (ghost.IsActive)
                {
                    ghost.TargetPosition = ghost.CalculateTargetPosition(_player.Position, _player.Direction);
                    ghost.Move(_gameField);
                    ghost.Update(gameTime);
                }
            }

            // Проверка столкновений
            CheckCollisions();

            // Проверка энергии
            CheckEnergizerStatus();
        }

        private void CheckPointCollection()
        {
            var point = _gameField.GetPointAt(_player.Position);
            if (point != null && !point.IsCollected && point.IsActive)
            {
                point.OnCollision(_player);

                if (point is Models.Items.Energizer)
                {
                    ActivateEnergizer(TimeSpan.FromSeconds(10));
                }
            }
        }

        private void CheckCollisions()
        {
            foreach (var ghost in _ghosts)
            {
                if (!ghost.IsActive) continue;

                if (IsColliding(_player, ghost))
                {
                    HandleGhostCollision(ghost);
                }
            }
        }

        private void HandleGhostCollision(Ghost ghost)
        {
            try
            {
                if (ghost.State == GhostState.Vulnerable)
                {
                    // Игрок съедает привидение
                    ghost.Die();
                    _player.AddScore(200);

                    // Временно деактивируем привидение
                    ghost.IsActive = false;

                    // Через 3 секунды возрождаем
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(3);
                    timer.Tick += (s, e) =>
                    {
                        ghost.Respawn(_gameField.GhostHouse);
                        timer.Stop();
                    };
                    timer.Start();
                }
                else if (ghost.State == GhostState.Normal)
                {
                    // Привидение вредит игроку
                    _player.TakeDamage();

                    if (_player.Lives <= 0)
                    {
                        CurrentState = GameState.GameOver;
                        GameOver?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        ResetLevel();
                    }
                }
                // Если призрак мертв - игрок просто проходит сквозь него
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обработке столкновения: {ex.Message}");
            }
        }

        private bool IsColliding(GameObject obj1, GameObject obj2)
        {
            if (!obj1.IsActive || !obj2.IsActive) return false;

            double distance = Math.Sqrt(
                Math.Pow(obj1.Position.X - obj2.Position.X, 2) +
                Math.Pow(obj1.Position.Y - obj2.Position.Y, 2)
            );
            return distance < (obj1.Size / 2 + obj2.Size / 2);
        }

        private void CheckEnergizerStatus()
        {
            if (_isEnergizerActive && DateTime.Now > _energizerEndTime)
            {
                _isEnergizerActive = false;
                foreach (var ghost in _ghosts)
                {
                    if (ghost.State == GhostState.Vulnerable)
                    {
                        ghost.State = GhostState.Normal;
                        ghost.Speed = 2.0;
                    }
                }
            }
        }

        private void OnPlayerDied(object sender, EventArgs e)
        {
            ResetLevel();
        }

        public void ActivateEnergizer(TimeSpan duration)
        {
            _isEnergizerActive = true;
            _energizerEndTime = DateTime.Now.Add(duration);

            foreach (var ghost in _ghosts)
            {
                if (ghost.State != GhostState.Dead && ghost.IsActive)
                {
                    ghost.MakeVulnerable(duration);
                }
            }
        }

        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                CurrentState = GameState.Paused;
            }
        }

        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                CurrentState = GameState.Playing;
            }
        }

        public void RestartGame()
        {
            _currentLevel = 1;
            _gameField.ResetField();
            Initialize();
            CurrentState = GameState.Playing;
        }

        private void ResetLevel()
        {
            _player.Respawn(_gameField.PlayerSpawn);

            foreach (var ghost in _ghosts)
            {
                ghost.Respawn(_gameField.GhostHouse);
                ghost.State = GhostState.Normal;
                ghost.IsActive = true;
                ghost.Speed = 2.0;
            }
        }

        public Player GetPlayer() => _player;
        public GameField GetGameField() => _gameField;
        public IEnumerable<Ghost> GetGhosts() => _ghosts;
    }
}