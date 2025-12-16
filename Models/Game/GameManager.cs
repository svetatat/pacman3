using System;
using System.Collections.Generic;
using System.Linq;
using pacman3.Models.Ghosts;
using pacman3.Models;
using pacman3.Utils;

namespace pacman3.Models.Game
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Victory
    }

    public class GameManager : Interfaces.IGameComponent
    {
        private GameState _currentState = GameState.MainMenu;
        private Player _player;
        private GameField _gameField;
        private List<Ghost> _ghosts;
        private bool _isEnergizerActive;
        private DateTime _energizerEndTime;

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

        // События
        public event EventHandler<GameState> GameStateChanged;
        public event EventHandler<int> ScoreChanged;
        public event EventHandler<int> LivesChanged;
        public event EventHandler GameOver;
        public event EventHandler Victory;

        public void Initialize()
        {
            _gameField = new GameField(10, 10);
            _gameField.Initialize();

            _player = new Player(5 * 32 + 16, 5 * 32 + 16);
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

            // Установите позиции отдельно
            _ghosts[0].Position = new Vector2(9 * 32 + 16, 9 * 32 + 16); // Blinky
            _ghosts[1].Position = new Vector2(1 * 32 + 16, 1 * 32 + 16); // Pinky
            _ghosts[2].Position = new Vector2(9 * 32 + 16, 1 * 32 + 16); // Inky
            _ghosts[3].Position = new Vector2(1 * 32 + 16, 9 * 32 + 16); // Clyde
        }

        public void Update(TimeSpan gameTime)
        {
            if (CurrentState != GameState.Playing) return;

            // Обновление поля
            _gameField.Update(gameTime);

            // Обновление привидений
            foreach (var ghost in _ghosts)
            {
                ghost.TargetPosition = ghost.CalculateTargetPosition(_player.Position, _player.Direction);
                UpdateGhostDirection(ghost);
                ghost.Update(gameTime);
            }

            // Обновление игрока
            bool canMove = _gameField.CanMoveTo(_player.Position, _player.Direction, _player.Speed);
            if (canMove)
            {
                bool canTurn = _gameField.CanMoveTo(_player.Position, _player.NextDirection, _player.Speed);
                _player.ApplyNextDirection(canTurn);
                _player.Update(gameTime);
            }

            // Проверка столкновений (упрощенная версия)
            CheckCollisions();

            // Проверка энергии
            CheckEnergizerStatus();
        }

        private void CheckCollisions()
        {
            // Проверка столкновений с привидениями
            foreach (var ghost in _ghosts)
            {
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
                    }
                }
            }
        }

        private bool IsColliding(GameObject obj1, GameObject obj2)
        {
            double distance = Math.Sqrt(
                Math.Pow(obj1.Position.X - obj2.Position.X, 2) +
                Math.Pow(obj1.Position.Y - obj2.Position.Y, 2)
            );
            return distance < (obj1.Size / 2 + obj2.Size / 2);
        }

        private void UpdateGhostDirection(Ghost ghost)
        {
            // Простой AI для привидений
            var possibleDirections = new List<Direction>
            {
                Direction.Up, Direction.Down, Direction.Left, Direction.Right
            };

            // Убираем противоположное направление
            possibleDirections.Remove(GetOppositeDirection(ghost.Direction));

            // Выбираем направление к цели
            Vector2 difference = ghost.TargetPosition - ghost.Position;
            Direction bestDirection = ghost.Direction;

            if (Math.Abs(difference.X) > Math.Abs(difference.Y))
            {
                bestDirection = difference.X > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                bestDirection = difference.Y > 0 ? Direction.Down : Direction.Up;
            }

            if (possibleDirections.Contains(bestDirection) &&
                _gameField.CanMoveTo(ghost.Position, bestDirection, ghost.Speed))
            {
                ghost.Direction = bestDirection;
            }
        }

        private Direction GetOppositeDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
                default: return Direction.None;
            }
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
                    }
                }
            }
        }

        private void OnPlayerDamaged(object sender, EventArgs e)
        {
            if (_player.Lives <= 0)
            {
                CurrentState = GameState.GameOver;
                GameOver?.Invoke(this, EventArgs.Empty);
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
            Initialize();
        }

        private void ResetLevel()
        {
            // Сброс позиций
            _player.Respawn(new Vector2(5 * 32 + 16, 5 * 32 + 16));

            foreach (var ghost in _ghosts)
            {
                ghost.Respawn(ghost.Position);
            }
        }

        public Player GetPlayer() => _player;
        public GameField GetGameField() => _gameField;
        public IEnumerable<Ghost> GetGhosts() => _ghosts;
    }
}