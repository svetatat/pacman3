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
            if (!IsActive) return;

            // Проверяем телепортацию
            gameField.CheckTeleport(this);

            // Если призрак мертв - возвращаемся в дом
            if (State == GhostState.Dead)
            {
                MoveToGhostHouse(gameField);
                return;
            }

            // Получаем доступные направления
            var availableDirections = gameField.GetAvailableDirections(Position, Direction);
            
            // Если нет доступных направлений, выбираем любое
            if (availableDirections.Count == 0)
            {
                availableDirections = new List<Direction> 
                { 
                    Direction.Up, Direction.Down, Direction.Left, Direction.Right 
                };
            }

            // Выбираем направление
            Direction chosenDirection = ChooseDirection(availableDirections, gameField);
            
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

        private Direction ChooseDirection(List<Direction> availableDirections, GameField gameField)
        {
            // Фильтруем направления, в которые можно пойти
            var validDirections = new List<Direction>();
            foreach (var dir in availableDirections)
            {
                if (gameField.CanMoveTo(Position, dir, Speed))
                {
                    validDirections.Add(dir);
                }
            }

            if (validDirections.Count == 0)
                return Direction.None;

            // Если мы в уязвимом состоянии - убегаем
            if (State == GhostState.Vulnerable)
            {
                return validDirections[_random.Next(validDirections.Count)];
            }

            // Если есть цель - пытаемся двигаться к ней
            // Исправлено: проверяем, не нулевой ли вектор цели
            if (!TargetPosition.Equals(default(Vector2)) &&
                (TargetPosition.X != 0 || TargetPosition.Y != 0)) // Дополнительная проверка
            {
                // Находим направление, которое приближает к цели
                Vector2 diff = TargetPosition - Position;

                // Предпочитаем горизонтальное или вертикальное движение
                Direction preferredDirection;
                if (Math.Abs(diff.X) > Math.Abs(diff.Y))
                {
                    preferredDirection = diff.X > 0 ? Direction.Right : Direction.Left;
                }
                else
                {
                    preferredDirection = diff.Y > 0 ? Direction.Down : Direction.Up;
                }

                // Если предпочитаемое направление доступно - используем его
                if (validDirections.Contains(preferredDirection))
                {
                    return preferredDirection;
                }
            }

            // Иначе выбираем случайное доступное направление
            return validDirections[_random.Next(validDirections.Count)];
        }

        private void MoveToGhostHouse(GameField gameField)
        {
            // Призрак возвращается в дом
            Vector2 housePos = _gameField?.GhostHouse ?? new Vector2(300, 200);
            
            // Простое движение к дому
            Vector2 diff = housePos - Position;
            
            if (Math.Abs(diff.X) > 10)
            {
                Direction = diff.X > 0 ? Direction.Right : Direction.Left;
            }
            else if (Math.Abs(diff.Y) > 10)
            {
                Direction = diff.Y > 0 ? Direction.Down : Direction.Up;
            }
            else
            {
                // Достигли дома - возрождаемся
                State = GhostState.Normal;
                Direction = Direction.Down; // Выходим из дома
            }
            
            // Двигаемся в текущем направлении
            if (Direction != Direction.None && _gameField != null)
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

        // Добавляем ссылку на GameField для метода MoveToGhostHouse
        private GameField _gameField;
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

        public void Respawn(Vector2 spawnPoint)
        {
            try
            {
                Position = spawnPoint;
                State = GhostState.Normal;
                Direction = Direction.Down;
                Velocity = new Vector2(0, 0);
                IsActive = true;
                Speed = 2.0;
                
                // Останавливаем таймеры
                _vulnerabilityTimer?.Stop();
                _vulnerabilityTimer = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в Respawn: {ex.Message}");
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