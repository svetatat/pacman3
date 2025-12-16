using System;
using System.Windows.Media;
using pacman3.Interfaces;
using pacman3.Models.Game;
using pacman3.Utils;

namespace pacman3.Models.Ghosts
{
    public abstract class Ghost : GameObject, IMovable
    {
        private GhostState _state = GhostState.Normal;
        private Random _random = new Random();

        public double Speed { get; set; } = 2.0; // Уменьшили скорость привидений
        public Direction Direction { get; set; } = Direction.None;

        // Реализация интерфейса IMovable
        public Vector2 Velocity { get; set; }

        public GhostState State
        {
            get => _state;
            set
            {
                _state = value;
                UpdateColor();
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
            Size = 26; // Уменьшили размер
            Velocity = new Vector2(0, 0);
            UpdateColor();
        }

        private void UpdateColor()
        {
            switch (State)
            {
                case GhostState.Normal:
                    ObjectColor = OriginalColor;
                    break;
                case GhostState.Vulnerable:
                    ObjectColor = Colors.Blue;
                    break;
                case GhostState.Dead:
                    ObjectColor = Colors.White;
                    break;
            }
        }

        protected virtual Color OriginalColor => Colors.Red;

        public abstract Vector2 CalculateTargetPosition(Vector2 playerPosition, Direction playerDirection);

        public void Move(Vector2 direction)
        {
            // Реализация интерфейса IMovable
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

        public void Move(GameField gameField)
        {
            if (!IsActive) return;

            // Проверяем телепортацию через туннели
            gameField.CheckTeleport(this);

            // Если привидение мертво, возвращаемся домой
            if (State == GhostState.Dead)
            {
                MoveToSpawn(gameField);
                return;
            }

            // Выбираем новое направление при необходимости
            if (_random.Next(100) < 20 || // 20% шанс сменить направление
                Direction == Direction.None ||
                !gameField.CanMoveTo(Position, Direction, Speed))
            {
                ChooseNewDirection(gameField);
            }

            if (Direction != Direction.None && gameField.CanMoveTo(Position, Direction, Speed))
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
                UpdateVelocityFromDirection();
            }
        }

        private void MoveToSpawn(GameField gameField)
        {
            // Призрак возвращается в точку спавна
            Vector2 spawnPoint = new Vector2(gameField.Width * gameField.TileSize / 2,
                                            gameField.Height * gameField.TileSize / 2);

            if (Math.Abs(Position.X - spawnPoint.X) > 1)
            {
                Direction = Position.X < spawnPoint.X ? Direction.Right : Direction.Left;
            }
            else if (Math.Abs(Position.Y - spawnPoint.Y) > 1)
            {
                Direction = Position.Y < spawnPoint.Y ? Direction.Down : Direction.Up;
            }
            else
            {
                // Достигли точки спавна
                State = GhostState.Normal;
                Direction = Direction.None;
            }

            Move(gameField);
        }

        private void ChooseNewDirection(GameField gameField)
        {
            var possibleDirections = new List<Direction>
            {
                Direction.Up, Direction.Down, Direction.Left, Direction.Right
            };

            // Убираем противоположное направление (чтобы не ходить туда-сюда)
            possibleDirections.Remove(GetOppositeDirection(Direction));

            // Фильтруем направления, в которые можно пойти
            var validDirections = new List<Direction>();
            foreach (var dir in possibleDirections)
            {
                if (gameField.CanMoveTo(Position, dir, Speed))
                {
                    validDirections.Add(dir);
                }
            }

            if (validDirections.Count > 0)
            {
                // Выбираем направление к цели
                if (State != GhostState.Vulnerable)
                {
                    // В нормальном состоянии преследуем цель
                    Vector2 difference = TargetPosition - Position;
                    Direction bestDirection = Direction;

                    if (Math.Abs(difference.X) > Math.Abs(difference.Y))
                    {
                        bestDirection = difference.X > 0 ? Direction.Right : Direction.Left;
                    }
                    else
                    {
                        bestDirection = difference.Y > 0 ? Direction.Down : Direction.Up;
                    }

                    if (validDirections.Contains(bestDirection))
                    {
                        Direction = bestDirection;
                        return;
                    }
                }

                // Если не можем идти к цели или мы уязвимы - выбираем случайное
                int index = _random.Next(validDirections.Count);
                Direction = validDirections[index];
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

        private void UpdateVelocityFromDirection()
        {
            switch (Direction)
            {
                case Direction.Up:
                    Velocity = new Vector2(0, -(float)Speed);
                    break;
                case Direction.Down:
                    Velocity = new Vector2(0, (float)Speed);
                    break;
                case Direction.Left:
                    Velocity = new Vector2(-(float)Speed, 0);
                    break;
                case Direction.Right:
                    Velocity = new Vector2((float)Speed, 0);
                    break;
                case Direction.None:
                    Velocity = new Vector2(0, 0);
                    break;
            }
        }

        public override void Update(TimeSpan gameTime)
        {
            base.Update(gameTime);
        }

        public void MakeVulnerable(TimeSpan duration)
        {
            if (State == GhostState.Dead) return;

            State = GhostState.Vulnerable;
            Speed = 1.5; // Замедляем уязвимых привидений

            // Таймер возврата в нормальное состояние
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = duration;
            timer.Tick += (s, e) =>
            {
                if (State == GhostState.Vulnerable)
                {
                    State = GhostState.Normal;
                    Speed = 2.0; // Восстанавливаем скорость
                }
                timer.Stop();
            };
            timer.Start();
        }

        public void Die()
        {
            State = GhostState.Dead;
            Speed = 4.0; // Увеличиваем скорость для возвращения домой
        }

        public void Respawn(Vector2 spawnPoint)
        {
            Position = spawnPoint;
            State = GhostState.Normal;
            Direction = Direction.None;
            Velocity = new Vector2(0, 0);
            IsActive = true;
            Speed = 2.0;
        }
    }
}