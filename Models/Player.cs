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

        public double Speed { get; set; } = 3.0;
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
            Size = 24; // Уменьшили размер для лучшего прохождения
            ObjectColor = Colors.Yellow;
            Lives = 3;
            Score = 0;
            IsMoving = false;
            IsInvulnerable = false;
            Velocity = new Vector2(0, 0);
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

        // Основной метод движения с проверкой стены
        public void Move(GameField gameField)
        {
            if (!IsActive || gameField == null) return;

            // Проверяем, можем ли повернуть в желаемом направлении
            if (NextDirection != Direction.None && NextDirection != Direction)
            {
                if (gameField.CanMoveTo(Position, NextDirection, Speed))
                {
                    Direction = NextDirection;
                    NextDirection = Direction.None;
                }
            }

            // Двигаемся в текущем направлении, если это возможно
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

                // Обновляем Velocity в соответствии с направлением
                UpdateVelocityFromDirection();

                IsMoving = true;

                // Проверяем телепортацию через туннели
                gameField.CheckTeleport(this);
            }
            else
            {
                // Если не можем двигаться, останавливаемся
                IsMoving = false;
                Velocity = new Vector2(0, 0);
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
            ObjectColor = Color.FromRgb(255, 255, 200); // Более светлый желтый
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
            Velocity = new Vector2(0, 0);
            IsMoving = false;
            BecomeInvulnerable(TimeSpan.FromSeconds(1.5));
        }

        public void Reset()
        {
            Lives = 3;
            Score = 0;
            Direction = Direction.None;
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

            // Рисуем Pac-Man как сектор круга (открытый рот)
            double mouthAngle = 45; // Угол открытия рта в градусах
            double startAngle = GetMouthStartAngle(Direction);

            // Если стоит на месте - рисуем полный круг
            if (Direction == Direction.None || !IsMoving)
            {
                drawingContext.DrawEllipse(
                    brush,
                    pen,
                    new System.Windows.Point(Position.X, Position.Y),
                    halfSize,
                    halfSize
                );
            }
            else
            {
                // Рисуем Pac-Man с открытым ртом
                drawingContext.DrawGeometry(
                    brush,
                    pen,
                    CreatePacManGeometry(Position.X, Position.Y, halfSize, startAngle, mouthAngle)
                );
            }
        }

        private double GetMouthStartAngle(Direction direction)
        {
            return direction switch
            {
                Direction.Right => 35,
                Direction.Up => 125,
                Direction.Left => 215,
                Direction.Down => 305,
                _ => 35 // По умолчанию смотрит вправо
            };
        }

        private System.Windows.Media.Geometry CreatePacManGeometry(double centerX, double centerY, double radius, double startAngle, double mouthAngle)
        {
            var pathGeometry = new System.Windows.Media.PathGeometry();
            var figure = new System.Windows.Media.PathFigure();

            // Конвертируем углы в радианы
            double startRad = (startAngle - mouthAngle / 2) * Math.PI / 180;
            double endRad = (startAngle + mouthAngle / 2) * Math.PI / 180;

            // Начальная точка (центр)
            figure.StartPoint = new System.Windows.Point(centerX, centerY);

            // Линия к началу дуги
            figure.Segments.Add(new System.Windows.Media.LineSegment(
                new System.Windows.Point(centerX + radius * Math.Cos(startRad),
                                        centerY + radius * Math.Sin(startRad)),
                true));

            // Дуга
            figure.Segments.Add(new System.Windows.Media.ArcSegment(
                new System.Windows.Point(centerX + radius * Math.Cos(endRad),
                                        centerY + radius * Math.Sin(endRad)),
                new System.Windows.Size(radius, radius),
                0,
                mouthAngle > 180,
                System.Windows.Media.SweepDirection.Clockwise,
                true));

            // Замыкаем фигуру
            figure.IsClosed = true;
            pathGeometry.Figures.Add(figure);

            return pathGeometry;
        }

        public override void OnCollision(Interfaces.ICollidable other)
        {
            base.OnCollision(other);

            if (other is Models.Items.GamePoint gamePoint)
            {
                CollectPoint(gamePoint.PointValue);
            }
        }
    }
}