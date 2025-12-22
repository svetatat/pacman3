using System;
using System.Windows.Input;
using System.Windows.Media;
using pacman3.Interfaces;
using pacman3.Models.Game;
using pacman3.Utils;

namespace pacman3.Models
{
    public class Player : GameObject, IMovable
    {
        private int _lives;
        private int _score;
        private Direction _nextDirection;

        public Vector2 Velocity { get; set; }

        public int Lives
        {
            get => _lives;
            private set
            {
                _lives = Math.Max(0, value);
                LivesChanged?.Invoke(this, _lives);
            }
        }

        public int Score
        {
            get => _score;
            private set
            {
                _score = value;
                ScoreChanged?.Invoke(this, _score);
            }
        }

        public double Speed { get; set; } = 4.0;
        public Direction Direction { get; set; } = Direction.None;

        public Direction NextDirection
        {
            get => _nextDirection;
            set => _nextDirection = value;
        }

        public event EventHandler<int> ScoreChanged;
        public event EventHandler<int> LivesChanged;
        public event EventHandler PlayerDied;

        public bool IsMoving { get; private set; }
        public bool IsInvulnerable { get; private set; }
        private DateTime _invulnerabilityEndTime;

        public Player() : base()
        {
            InitializePlayer();
        }

        public Player(double x, double y) : base(x, y)
        {
            InitializePlayer();
        }

        private void InitializePlayer()
        {
            Size = 24;
            ObjectColor = Colors.Yellow;
            Lives = 3;
            Score = 0;
            IsMoving = false;
            IsInvulnerable = false;
            Velocity = new Vector2(0, 0);
            Direction = Direction.Right; // Начальное направление
        }

        public void HandleInput(Key key)
        {
            if (!IsActive) return;

            Direction newDirection = Direction.None;

            switch (key)
            {
                case Key.Up:
                case Key.W:
                    newDirection = Direction.Up;
                    break;
                case Key.Down:
                case Key.S:
                    newDirection = Direction.Down;
                    break;
                case Key.Left:
                case Key.A:
                    newDirection = Direction.Left;
                    break;
                case Key.Right:
                case Key.D:
                    newDirection = Direction.Right;
                    break;
            }

            if (newDirection != Direction.None)
            {
                NextDirection = newDirection;
            }
        }

        public void Move(GameField gameField)
        {
            if (!IsActive || gameField == null) return;

            // Проверяем, можем ли повернуть
            if (NextDirection != Direction.None && NextDirection != Direction)
            {
                if (gameField.CanMoveTo(Position, NextDirection, Speed))
                {
                    Direction = NextDirection;
                    NextDirection = Direction.None;
                }
            }

            // Двигаемся в текущем направлении
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
                IsMoving = true;

                // Телепортация
                gameField.CheckTeleport(this);
            }
            else
            {
                IsMoving = false;
            }
        }

        public override void Update(TimeSpan gameTime)
        {
            base.Update(gameTime);

            if (IsInvulnerable && DateTime.Now > _invulnerabilityEndTime)
            {
                IsInvulnerable = false;
                ObjectColor = Colors.Yellow;
            }
        }

        public void AddScore(int points)
        {
            Score += points;
            ScoreChanged?.Invoke(this, Score);
        }

        public void CollectPoint(int pointValue)
        {
            AddScore(pointValue);
        }

        public void TakeDamage()
        {
            if (IsInvulnerable) return;

            Lives--;

            if (Lives <= 0)
            {
                Die();
            }
            else
            {
                BecomeInvulnerable(TimeSpan.FromSeconds(2));
                PlayerDied?.Invoke(this, EventArgs.Empty);
            }
        }

        public void BecomeInvulnerable(TimeSpan duration)
        {
            IsInvulnerable = true;
            _invulnerabilityEndTime = DateTime.Now.Add(duration);
            ObjectColor = Color.FromRgb(255, 255, 150); // Светло-желтый
        }

        public void Die()
        {
            IsActive = false;
            PlayerDied?.Invoke(this, EventArgs.Empty);
        }

        public void Respawn(Vector2 spawnPoint)
        {
            Position = spawnPoint;
            IsActive = true;
            Direction = Direction.Right; // Важно: явно устанавливаем направление
            NextDirection = Direction.None;
            Velocity = new Vector2(0, 0);
            IsMoving = false;
            BecomeInvulnerable(TimeSpan.FromSeconds(1.5));
        }

        public void Reset()
        {
            Lives = 3;
            Score = 0;
            Direction = Direction.Right; // Явно устанавливаем направление
            NextDirection = Direction.None;
            Velocity = new Vector2(0, 0);
            IsMoving = false;
            IsInvulnerable = false;
            IsActive = true;
            ObjectColor = Colors.Yellow;
        }

        public override void Draw(System.Windows.Media.DrawingContext drawingContext)
        {
            if (!IsActive) return;

            var brush = new SolidColorBrush(ObjectColor);
            var pen = new Pen(Brushes.Black, 1);

            double halfSize = Size / 2;

            // Просто рисуем круг - это нормально для Pac-Man
            drawingContext.DrawEllipse(
                brush,
                pen,
                new System.Windows.Point(Position.X, Position.Y),
                halfSize,
                halfSize
            );

            // Рисуем глаз
            DrawEye(drawingContext);
        }

        private void DrawEye(System.Windows.Media.DrawingContext drawingContext)
        {
            var eyeBrush = new SolidColorBrush(Colors.Black);
            double eyeSize = Size / 6;

            System.Windows.Point eyePosition = Direction switch
            {
                Direction.Right => new System.Windows.Point(Position.X + Size / 4, Position.Y - Size / 6),
                Direction.Left => new System.Windows.Point(Position.X - Size / 4, Position.Y - Size / 6),
                Direction.Up => new System.Windows.Point(Position.X, Position.Y - Size / 4),
                Direction.Down => new System.Windows.Point(Position.X, Position.Y + Size / 4),
                _ => new System.Windows.Point(Position.X + Size / 4, Position.Y - Size / 6) // По умолчанию
            };

            drawingContext.DrawEllipse(
                eyeBrush,
                null,
                eyePosition,
                eyeSize,
                eyeSize
            );
        }

        public override void OnCollision(Interfaces.ICollidable other)
        {
            base.OnCollision(other);

            if (other is Models.Items.GamePoint gamePoint)
            {
                CollectPoint(gamePoint.PointValue);
            }
        }

        // Реализация интерфейса IMovable
        public void Move(Vector2 direction)
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
            IsMoving = true;
        }
    }
}