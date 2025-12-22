using System;
using System.Collections.Generic;
using System.Windows.Media;
using pacman3.Interfaces;
using pacman3.Models.Game;
using pacman3.Utils;

namespace pacman3.Models.Ghosts
{
    public abstract class Ghost : GameObject, IMovable
    {
        private GhostState _state = GhostState.Normal;
        protected Random _random = new Random();
        private System.Windows.Threading.DispatcherTimer _vulnerabilityTimer;
        private GameField _gameField;
        private int _stuckCounter = 0;
        private Vector2 _lastPosition;

        public double Speed { get; set; } = 2.0;
        public Direction Direction { get; set; } = Direction.None;

        public Vector2 Velocity { get; set; }

        public GhostState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    UpdateColor();
                    StateChanged?.Invoke(this, value);
                }
            }
        }

        public Vector2 TargetPosition { get; set; }

        public event EventHandler<GhostState> StateChanged;

        protected Ghost() : base()
        {
            InitializeGhost();
        }

        protected Ghost(double x, double y) : base(x, y)
        {
            InitializeGhost();
        }

        private void InitializeGhost()
        {
            Size = 26;
            Velocity = new Vector2(0, 0);
            UpdateColor();
            _lastPosition = Position;
        }

        private void UpdateColor()
        {
            switch (State)
            {
                case GhostState.Normal:
                    ObjectColor = OriginalColor;
                    Speed = 2.0;
                    break;
                case GhostState.Vulnerable:
                    ObjectColor = Colors.Blue;
                    Speed = 1.5;
                    break;
                case GhostState.Dead:
                    ObjectColor = Colors.White;
                    Speed = 4.0;
                    break;
            }
        }

        protected virtual Color OriginalColor => Colors.Red;

        public abstract Vector2 CalculateTargetPosition(Vector2 playerPosition, Direction playerDirection);

        public void Move(GameField gameField)
        {
            if (!IsActive || gameField == null) return;

            _gameField = gameField;

            // Проверяем телепортацию
            gameField.CheckTeleport(this);

            // Если призрак мертв - возвращаемся в дом
            if (State == GhostState.Dead)
            {
                MoveToGhostHouse();
                return;
            }

            // Проверяем, не застрял ли призрак
            if (Vector2.Distance(Position, _lastPosition) < 0.5)
            {
                _stuckCounter++;
                if (_stuckCounter > 10)
                {
                    // Призрак застрял, выбираем случайное направление
                    ForceChangeDirection();
                    _stuckCounter = 0;
                }
            }
            else
            {
                _stuckCounter = 0;
            }

            _lastPosition = Position;

            // Получаем доступные направления
            var availableDirections = GetAvailableDirections();

            // Выбираем направление
            Direction chosenDirection = ChooseDirection(availableDirections);

            if (chosenDirection != Direction.None && gameField.CanMoveTo(Position, chosenDirection, Speed))
            {
                Direction = chosenDirection;

                // Двигаемся
                Vector2 newPosition = Position;
                switch (Direction)
                {
                    case Direction.Up:
                        newPosition.Y -= Speed;
                        break;
                    case Direction.Down:
                        newPosition.Y += Speed;
                        break;
                    case Direction.Left:
                        newPosition.X -= Speed;
                        break;
                    case Direction.Right:
                        newPosition.X += Speed;
                        break;
                }

                Position = newPosition;
            }
        }

        private void ForceChangeDirection()
        {
            // Принудительно меняем направление на случайное доступное
            var directions = new List<Direction>
            {
                Direction.Up, Direction.Down, Direction.Left, Direction.Right
            };

            // Убираем противоположное текущему направлению
            directions.Remove(GetOppositeDirection(Direction));

            foreach (var dir in directions)
            {
                if (_gameField != null && _gameField.CanMoveTo(Position, dir, Speed))
                {
                    Direction = dir;
                    break;
                }
            }
        }

        private List<Direction> GetAvailableDirections()
        {
            var directions = new List<Direction>();

            if (_gameField != null)
            {
                if (_gameField.CanMoveTo(Position, Direction.Up, Speed))
                    directions.Add(Direction.Up);
                if (_gameField.CanMoveTo(Position, Direction.Down, Speed))
                    directions.Add(Direction.Down);
                if (_gameField.CanMoveTo(Position, Direction.Left, Speed))
                    directions.Add(Direction.Left);
                if (_gameField.CanMoveTo(Position, Direction.Right, Speed))
                    directions.Add(Direction.Right);
            }

            // Убираем противоположное направление
            var opposite = GetOppositeDirection(Direction);
            directions.Remove(opposite);

            return directions;
        }

        private Direction GetOppositeDirection(Direction dir)
        {
            return dir switch
            {
                Direction.Up => Direction.Down,
                Direction.Down => Direction.Up,
                Direction.Left => Direction.Right,
                Direction.Right => Direction.Left,
                _ => Direction.None
            };
        }

        private Direction ChooseDirection(List<Direction> availableDirections)
        {
            if (availableDirections.Count == 0)
            {
                // Если нет доступных направлений, пробуем любое
                var allDirections = new List<Direction>
                {
                    Direction.Up, Direction.Down, Direction.Left, Direction.Right
                };
                allDirections.Remove(GetOppositeDirection(Direction));

                foreach (var dir in allDirections)
                {
                    if (_gameField != null && _gameField.CanMoveTo(Position, dir, Speed))
                    {
                        return dir;
                    }
                }
                return Direction.None;
            }

            // Если мы в уязвимом состоянии - выбираем случайное направление
            if (State == GhostState.Vulnerable)
            {
                return availableDirections[_random.Next(availableDirections.Count)];
            }

            // Если есть цель - пытаемся двигаться к ней
            if (!TargetPosition.Equals(default(Vector2)))
            {
                Direction bestDirection = Direction.None;
                double bestDistance = double.MaxValue;

                foreach (var dir in availableDirections)
                {
                    Vector2 testPos = Position;
                    switch (dir)
                    {
                        case Direction.Up:
                            testPos.Y -= Speed;
                            break;
                        case Direction.Down:
                            testPos.Y += Speed;
                            break;
                        case Direction.Left:
                            testPos.X -= Speed;
                            break;
                        case Direction.Right:
                            testPos.X += Speed;
                            break;
                    }

                    double distance = Vector2.Distance(testPos, TargetPosition);

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestDirection = dir;
                    }
                }

                if (bestDirection != Direction.None)
                    return bestDirection;
            }

            // Иначе выбираем случайное доступное направление
            return availableDirections[_random.Next(availableDirections.Count)];
        }

        private void MoveToGhostHouse()
        {
            if (_gameField == null) return;

            Vector2 housePos = _gameField.GhostHouse;

            // Двигаемся к дому
            Vector2 diff = housePos - Position;

            if (Math.Abs(diff.X) > 5)
            {
                Direction = diff.X > 0 ? Direction.Right : Direction.Left;
            }
            else if (Math.Abs(diff.Y) > 5)
            {
                Direction = diff.Y > 0 ? Direction.Down : Direction.Up;
            }
            else
            {
                // Достигли дома - возрождаемся
                State = GhostState.Normal;
                Direction = Direction.Down;
                return;
            }

            // Двигаемся в текущем направлении
            if (Direction != Direction.None)
            {
                Vector2 newPosition = Position;
                switch (Direction)
                {
                    case Direction.Up:
                        newPosition.Y -= Speed;
                        break;
                    case Direction.Down:
                        newPosition.Y += Speed;
                        break;
                    case Direction.Left:
                        newPosition.X -= Speed;
                        break;
                    case Direction.Right:
                        newPosition.X += Speed;
                        break;
                }

                Position = newPosition;
            }
        }

        public void SetGameField(GameField gameField) => _gameField = gameField;

        public override void Update(TimeSpan gameTime)
        {
            base.Update(gameTime);
        }

        public void MakeVulnerable(TimeSpan duration)
        {
            try
            {
                if (State == GhostState.Dead) return;

                // Останавливаем предыдущий таймер, если он есть
                _vulnerabilityTimer?.Stop();

                State = GhostState.Vulnerable;

                // Таймер возврата в нормальное состояние
                _vulnerabilityTimer = new System.Windows.Threading.DispatcherTimer();
                _vulnerabilityTimer.Interval = duration;
                _vulnerabilityTimer.Tick += OnVulnerabilityEnded;
                _vulnerabilityTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в MakeVulnerable: {ex.Message}");
            }
        }

        private void OnVulnerabilityEnded(object sender, EventArgs e)
        {
            try
            {
                if (State == GhostState.Vulnerable)
                {
                    State = GhostState.Normal;
                }
                _vulnerabilityTimer?.Stop();
                _vulnerabilityTimer = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в OnVulnerabilityEnded: {ex.Message}");
            }
        }

        public void Die()
        {
            try
            {
                State = GhostState.Dead;
                // Останавливаем таймер уязвимости
                _vulnerabilityTimer?.Stop();
                _vulnerabilityTimer = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в Die: {ex.Message}");
            }
        }

        // Реализация интерфейса IMovable
        public void Move(Vector2 direction)
        {
            try
            {
                if (direction.X == 0 && direction.Y == 0) return;

                Velocity = new Vector2(
                    direction.X * (float)Speed,
                    direction.Y * (float)Speed
                );

                Vector2 newPosition = new Vector2(
                    Position.X + Velocity.X,
                    Position.Y + Velocity.Y
                );

                Position = newPosition;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в Move: {ex.Message}");
            }
        }
    }
}