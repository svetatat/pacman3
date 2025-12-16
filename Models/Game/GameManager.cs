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
        private GameState _currentState = GameState.MainMenu;
        private Player _player;
        private GameField _gameField;
        private List<Ghost> _ghosts;
        private bool _isEnergizerActive;
        private DateTime _energizerEndTime;
        private int _currentLevel = 1;
        private Vector2 _playerSpawnPoint;

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
            _gameField = new GameField(19, 21);
            _gameField.Initialize();

            // Точка спавна Pac-Man (посередине внизу)
            _playerSpawnPoint = new Vector2(9 * 32 + 16, 15 * 32 + 16);
            _player = new Player(_playerSpawnPoint.X, _playerSpawnPoint.Y);
            _player.ScoreChanged += (s, score) => ScoreChanged?.Invoke(this, score);
            _player.LivesChanged += (s, lives) => LivesChanged?.Invoke(this, lives);
            _player.PlayerDied += OnPlayerDied;

            InitializeGhosts();

            _gameField.AllPointsCollected += (s, e) =>
            {
                CurrentState = GameState.Victory;
                Victory?.Invoke(this, EventArgs.Empty);
            };

            CurrentState = GameState.Playing; // Начинаем сразу играть
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

            // Установите позиции привидений в центре карты
            _ghosts[0].Position = new Vector2(9 * 32 + 16, 9 * 32 + 16); // Blinky
            _ghosts[1].Position = new Vector2(8 * 32 + 16, 9 * 32 + 16); // Pinky
            _ghosts[2].Position = new Vector2(10 * 32 + 16, 9 * 32 + 16); // Inky
            _ghosts[3].Position = new Vector2(9 * 32 + 16, 8 * 32 + 16); // Clyde
        }

        public void Update(TimeSpan gameTime)
        {
            if (CurrentState != GameState.Playing) return;

            // Обновление поля
            _gameField.Update(gameTime);

            // Проверка сбора точек игроком
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
            if (point != null && !point.IsCollected)
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
            // Проверка столкновений с привидениями
            foreach (var ghost in _ghosts)
            {
                if (!ghost.IsActive) continue;

                if (IsColliding(_player, ghost))
                {
                    if (ghost.State == GhostState.Vulnerable)
                    {
                        // Игрок съедает привидение
                        ghost.Die();
                        _player.AddScore(200);
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
                            // Перезапуск уровня после потери жизни
                            ResetLevel();
                        }
                    }
                }
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
                        ghost.Speed = 2.0; // Восстанавливаем скорость
                    }
                }
            }
        }

        private void OnPlayerDied(object sender, EventArgs e)
        {
            // Перезапуск уровня при смерти
            ResetLevel();
        }

        public void ActivateEnergizer(TimeSpan duration)
        {
            _isEnergizerActive = true;
            _energizerEndTime = DateTime.Now.Add(duration);

            foreach (var ghost in _ghosts)
            {
                if (ghost.State != GhostState.Dead)
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
            Initialize();
            CurrentState = GameState.Playing;
        }

        private void ResetLevel()
        {
            // Сброс позиций
            _player.Respawn(_playerSpawnPoint);

            foreach (var ghost in _ghosts)
            {
                ghost.Respawn(ghost.Position);
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