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

            // Получаем границы дома призраков
            var ghostHouse = _gameField.GhostHouse;
            int tileSize = _gameField.TileSize;

            // Дом призраков: 7x5 тайлов, центр в ghostHouse
            int houseCenterX = (int)ghostHouse.X;
            int houseCenterY = (int)ghostHouse.Y;
            int houseLeft = houseCenterX - (3 * tileSize);  // 7/2 = 3.5, округляем до 3
            int houseRight = houseCenterX + (3 * tileSize);
            int houseTop = houseCenterY - (2 * tileSize);   // 5/2 = 2.5, округляем до 2
            int houseBottom = houseCenterY + (2 * tileSize);

            // Размещаем привидений СРАЗУ ЗА ПРЕДЕЛАМИ ДОМА:

            // 1. Blinky - справа от дома
            _ghosts[0].Position = new Vector2(houseRight + tileSize, houseCenterY);
            _ghosts[0].Direction = Direction.Left; // Двигается влево, к дому

            // 2. Pinky - слева от дома  
            _ghosts[1].Position = new Vector2(houseLeft - tileSize, houseCenterY);
            _ghosts[1].Direction = Direction.Right; // Двигается вправо, к дому

            // 3. Inky - сверху от дома
            _ghosts[2].Position = new Vector2(houseCenterX, houseTop - tileSize);
            _ghosts[2].Direction = Direction.Down; // Двигается вниз, к дому

            // 4. Clyde - снизу от дома (но выше выхода)
            _ghosts[3].Position = new Vector2(houseCenterX, houseBottom + tileSize);
            _ghosts[3].Direction = Direction.Up; // Двигается вверх, к дому

            foreach (var ghost in _ghosts)
            {
                ghost.State = GhostState.Normal;
                ghost.IsActive = true;
                ghost.SetGameField(_gameField);
                // Направление уже установлено выше для каждого привидения
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

                    // Через 3 секунды возрождаем ЗА ПРЕДЕЛАМИ дома
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromSeconds(3);
                    timer.Tick += (s, e) =>
                    {
                        RespawnGhostOutsideHouse(ghost);
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обработке столкновения: {ex.Message}");
            }
        }

        private void RespawnGhostOutsideHouse(Ghost ghost)
        {
            var ghostHouse = _gameField.GhostHouse;
            int tileSize = _gameField.TileSize;

            // Выбираем случайную позицию вокруг дома
            var random = new Random();
            int side = random.Next(4); // 0: слева, 1: справа, 2: сверху, 3: снизу

            Vector2 spawnPosition;
            Direction spawnDirection;

            switch (side)
            {
                case 0: // Слева
                    spawnPosition = new Vector2(ghostHouse.X - (4 * tileSize), ghostHouse.Y);
                    spawnDirection = Direction.Right;
                    break;
                case 1: // Справа
                    spawnPosition = new Vector2(ghostHouse.X + (4 * tileSize), ghostHouse.Y);
                    spawnDirection = Direction.Left;
                    break;
                case 2: // Сверху
                    spawnPosition = new Vector2(ghostHouse.X, ghostHouse.Y - (3 * tileSize));
                    spawnDirection = Direction.Down;
                    break;
                default: // Снизу
                    spawnPosition = new Vector2(ghostHouse.X, ghostHouse.Y + (3 * tileSize));
                    spawnDirection = Direction.Up;
                    break;
            }

            ghost.Position = spawnPosition;
            ghost.State = GhostState.Normal;
            ghost.Direction = spawnDirection;
            ghost.IsActive = true;
            ghost.Speed = 2.0;
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

            // Возрождаем привидений за пределами дома
            var ghostHouse = _gameField.GhostHouse;
            int tileSize = _gameField.TileSize;

            _ghosts[0].Position = new Vector2(ghostHouse.X + (4 * tileSize), ghostHouse.Y);
            _ghosts[0].Direction = Direction.Left;
            _ghosts[0].State = GhostState.Normal;
            _ghosts[0].IsActive = true;
            _ghosts[0].Speed = 2.0;

            _ghosts[1].Position = new Vector2(ghostHouse.X - (4 * tileSize), ghostHouse.Y);
            _ghosts[1].Direction = Direction.Right;
            _ghosts[1].State = GhostState.Normal;
            _ghosts[1].IsActive = true;
            _ghosts[1].Speed = 2.0;

            _ghosts[2].Position = new Vector2(ghostHouse.X, ghostHouse.Y - (3 * tileSize));
            _ghosts[2].Direction = Direction.Down;
            _ghosts[2].State = GhostState.Normal;
            _ghosts[2].IsActive = true;
            _ghosts[2].Speed = 2.0;

            _ghosts[3].Position = new Vector2(ghostHouse.X, ghostHouse.Y + (3 * tileSize));
            _ghosts[3].Direction = Direction.Up;
            _ghosts[3].State = GhostState.Normal;
            _ghosts[3].IsActive = true;
            _ghosts[3].Speed = 2.0;
        }

        public Player GetPlayer() => _player;
        public GameField GetGameField() => _gameField;
        public IEnumerable<Ghost> GetGhosts() => _ghosts;
    }
}