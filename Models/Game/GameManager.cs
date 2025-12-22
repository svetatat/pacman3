using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using pacman3.Models.Ghosts;
using pacman3.Models;
using pacman3.Utils;

namespace pacman3.Models.Game
{
    public class GameManager : Interfaces.IGameComponent
    {
        private GameState _currentState = GameState.MainMenu;
        private Player _player;
        private GameField _gameField;
        private List<Ghost> _ghosts;
        private bool _isEnergizerActive;
        private DateTime _energizerEndTime;
        private int _currentLevel = 1;
        private bool _isGameStarted = false;
        private Dictionary<Ghost, DispatcherTimer> _ghostRespawnTimers;

        public GameState CurrentState
        {
            get => _currentState;
            set
            {
                _currentState = value;
                GameStateChanged?.Invoke(this, value);
            }
        }

        public bool IsGameStarted => _isGameStarted;
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

        public GameManager()
        {
            _ghostRespawnTimers = new Dictionary<Ghost, DispatcherTimer>();
        }

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

            CurrentState = GameState.MainMenu;
            _isGameStarted = false;
        }

        public void StartGame()
        {
            if (!_isGameStarted || CurrentState == GameState.GameOver || CurrentState == GameState.Victory)
            {
                _isGameStarted = true;
                CurrentState = GameState.Playing;
                _player.Reset();
                _player.Position = _gameField.PlayerSpawn;
                _player.Direction = Direction.Right;
                ResetLevel();
            }
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

            var ghostHouse = _gameField.GhostHouse;
            int tileSize = _gameField.TileSize;

            // Размещаем привидений в доме
            _ghosts[0].Position = new Vector2(ghostHouse.X, ghostHouse.Y - tileSize);
            _ghosts[0].Direction = Direction.Right;
            _ghosts[0].State = GhostState.Normal;
            _ghosts[0].IsActive = true;
            _ghosts[0].SetGameField(_gameField);

            _ghosts[1].Position = new Vector2(ghostHouse.X - tileSize, ghostHouse.Y);
            _ghosts[1].Direction = Direction.Up;
            _ghosts[1].State = GhostState.Normal;
            _ghosts[1].IsActive = true;
            _ghosts[1].SetGameField(_gameField);

            _ghosts[2].Position = new Vector2(ghostHouse.X, ghostHouse.Y + tileSize);
            _ghosts[2].Direction = Direction.Left;
            _ghosts[2].State = GhostState.Normal;
            _ghosts[2].IsActive = true;
            _ghosts[2].SetGameField(_gameField);

            _ghosts[3].Position = new Vector2(ghostHouse.X + tileSize, ghostHouse.Y);
            _ghosts[3].Direction = Direction.Down;
            _ghosts[3].State = GhostState.Normal;
            _ghosts[3].IsActive = true;
            _ghosts[3].SetGameField(_gameField);
        }

        public void Update(TimeSpan gameTime)
        {
            if (CurrentState != GameState.Playing || !_isGameStarted) return;

            // Проверка сбора точек - ДО движения игрока
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
                    ghost.IsActive = false;

                    // Создаем таймер для возрождения этого конкретного призрака
                    var timer = new DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(5);
                    timer.Tag = ghost;
                    timer.Tick += (s, e) =>
                    {
                        if (timer.Tag is Ghost deadGhost)
                        {
                            RespawnGhost(deadGhost);
                            timer.Stop();
                            _ghostRespawnTimers.Remove(deadGhost);
                        }
                    };
                    timer.Start();

                    _ghostRespawnTimers[ghost] = timer;
                }
                else if (ghost.State == GhostState.Normal)
                {
                    // Привидение вредит игроку
                    _player.TakeDamage();

                    if (_player.Lives <= 0)
                    {
                        CurrentState = GameState.GameOver;
                        _isGameStarted = false;
                        GameOver?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        ResetLevel();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обработке столкновения: {ex.Message}");
            }
        }

        private void RespawnGhost(Ghost ghost)
        {
            try
            {
                var ghostHouse = _gameField.GhostHouse;
                ghost.Position = ghostHouse;
                ghost.State = GhostState.Normal;
                ghost.Direction = Direction.Down;
                ghost.IsActive = true;
                ghost.Speed = 2.0;

                // Останавливаем таймер уязвимости, если он был
                ghost.MakeVulnerable(TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при возрождении призрака: {ex.Message}");
            }
        }

        private bool IsColliding(GameObject obj1, GameObject obj2)
        {
            if (!obj1.IsActive || !obj2.IsActive) return false;

            double distance = Math.Sqrt(
                Math.Pow(obj1.Position.X - obj2.Position.X, 2) +
                Math.Pow(obj1.Position.Y - obj2.Position.Y, 2)
            );
            return distance < 20; // Увеличили радиус коллизии для лучшего определения
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
            // Останавливаем все таймеры респавна
            foreach (var timer in _ghostRespawnTimers.Values)
            {
                timer.Stop();
            }
            _ghostRespawnTimers.Clear();

            _currentLevel = 1;
            _gameField.ResetField();
            Initialize();
            StartGame();
        }

        public void GoToMainMenu()
        {
            // Останавливаем все таймеры респавна
            foreach (var timer in _ghostRespawnTimers.Values)
            {
                timer.Stop();
            }
            _ghostRespawnTimers.Clear();

            CurrentState = GameState.MainMenu;
            _isGameStarted = false;
        }

        private void ResetLevel()
        {
            _player.Respawn(_gameField.PlayerSpawn);

            var ghostHouse = _gameField.GhostHouse;
            int tileSize = _gameField.TileSize;

            // Возрождаем всех привидений в доме (включая тех, кто был съеден)
            for (int i = 0; i < _ghosts.Count; i++)
            {
                Vector2 position = i switch
                {
                    0 => new Vector2(ghostHouse.X, ghostHouse.Y - tileSize),
                    1 => new Vector2(ghostHouse.X - tileSize, ghostHouse.Y),
                    2 => new Vector2(ghostHouse.X, ghostHouse.Y + tileSize),
                    3 => new Vector2(ghostHouse.X + tileSize, ghostHouse.Y),
                    _ => ghostHouse
                };

                _ghosts[i].Position = position;
                _ghosts[i].Direction = i switch
                {
                    0 => Direction.Right,
                    1 => Direction.Up,
                    2 => Direction.Left,
                    3 => Direction.Down,
                    _ => Direction.Down
                };
                _ghosts[i].State = GhostState.Normal;
                _ghosts[i].IsActive = true;
                _ghosts[i].Speed = 2.0;

                // Останавливаем таймер уязвимости
                _ghosts[i].MakeVulnerable(TimeSpan.Zero);
            }

            // Очищаем таймеры респавна
            foreach (var timer in _ghostRespawnTimers.Values)
            {
                timer.Stop();
            }
            _ghostRespawnTimers.Clear();
        }

        public Player GetPlayer() => _player;
        public GameField GetGameField() => _gameField;
        public IEnumerable<Ghost> GetGhosts() => _ghosts;
    }
}