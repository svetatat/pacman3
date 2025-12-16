using System;
using System.Windows.Input;
using System.Windows.Media;
using pacman3.Interfaces;
using pacman3.Models.Game;
using pacman3.Utils;

namespace pacman3.Models.Player
{
    public class Player : GameObject, IMovable
    {
        private int _lives;
        private int _score;
        private Direction _nextDirection;

        // Реализация свойства Velocity из интерфейса IMovable
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
            private set => _nextDirection = value;
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
            Size = 28;
            ObjectColor = Colors.Yellow;
            Lives = 3;
            Score = 0;
            IsMoving = false;
            IsInvulnerable = false;
            Velocity = new Vector2(0, 0); // Используем конструктор вместо Zero
        }

        public void HandleInput(Key key)
        {
            Direction newDirection = Direction;

            switch (key)
            {
                case Key.Up:
                    newDirection = Direction.Up;
                    break;
                case Key.Down:
                    newDirection = Direction.Down;
                    break;
                case Key.Left:
                    newDirection = Direction.Left;
                    break;
                case Key.Right:
                    newDirection = Direction.Right;
                    break;
            }

            if (newDirection != Direction)
            {
                NextDirection = newDirection;
            }
        }

        // Реализация метода Move из интерфейса IMovable
        public void Move(Vector2 direction)
        {
            // Проверяем, является ли direction нулевым
            if (direction.X == 0 && direction.Y == 0) return;

            // Обновляем Velocity на основе направления и скорости
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

        // Старый метод Move (для обратной совместимости)
        public void Move()
        {
            if (Direction == Direction.None) return;

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

            // Обновляем Velocity в соответствии с направлением
            UpdateVelocityFromDirection();

            IsMoving = true;
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
                    Velocity = new Vector2(0, 0); // Используем конструктор
                    break;
            }
        }

        public void ApplyNextDirection(bool canTurn)
        {
            if (canTurn && NextDirection != Direction.None)
            {
                Direction = NextDirection;
                NextDirection = Direction.None;
                UpdateVelocityFromDirection();
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

            if (Direction != Direction.None)
            {
                Move();
            }
        }

        public void AddScore(int points)
        {
            Score += points;
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
            ObjectColor = Color.FromRgb(255, 255, 200);
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
            Direction = Direction.None;
            NextDirection = Direction.None;
            Velocity = new Vector2(0, 0); // Используем конструктор
            IsMoving = false;
            BecomeInvulnerable(TimeSpan.FromSeconds(1.5));
        }

        public void Reset()
        {
            Lives = 3;
            Score = 0;
            Direction = Direction.None;
            NextDirection = Direction.None;
            Velocity = new Vector2(0, 0); // Используем конструктор
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
            drawingContext.DrawEllipse(
                brush,
                pen,
                new System.Windows.Point(Position.X, Position.Y),
                halfSize,
                halfSize
            );
        }
    }
}