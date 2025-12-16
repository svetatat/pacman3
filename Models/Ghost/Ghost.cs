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

        public double Speed { get; set; } = 3.0;
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
            Size = 30;
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
        }

        public override void Update(TimeSpan gameTime)
        {
            base.Update(gameTime);

            if (Direction != Direction.None)
            {
                Move();
            }
        }

        public void MakeVulnerable(TimeSpan duration)
        {
            if (State == GhostState.Dead) return;

            State = GhostState.Vulnerable;

            // Таймер возврата в нормальное состояние
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = duration;
            timer.Tick += (s, e) =>
            {
                if (State == GhostState.Vulnerable)
                {
                    State = GhostState.Normal;
                }
                timer.Stop();
            };
            timer.Start();
        }

        public void Die()
        {
            State = GhostState.Dead;
        }

        public void Respawn(Vector2 spawnPoint)
        {
            Position = spawnPoint;
            State = GhostState.Normal;
            Direction = Direction.None;
            Velocity = new Vector2(0, 0);
            IsActive = true;
        }
    }
}