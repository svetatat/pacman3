using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using pacman3.Models.Ghosts;
using pacman3.Models.Items;
using pacman3.Utils;

namespace pacman3.Models.Game
{
    public class GameField : Interfaces.IGameComponent
    {
        private List<GamePoint> _points;
        private List<Wall> _walls;
        private List<Energizer> _energizers;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileSize { get; private set; } = 32;

        public IReadOnlyList<GamePoint> Points => _points;
        public IReadOnlyList<Wall> Walls => _walls;
        public IReadOnlyList<Energizer> Energizers => _energizers;
        public int TotalPoints => _points.Count(p => !p.IsCollected) + _energizers.Count(e => !e.IsCollected);

        // Позиция спавна игрока
        public Vector2 PlayerSpawn { get; private set; }

        // Позиция дома призраков
        public Vector2 GhostHouse { get; private set; }

        public event EventHandler<int> PointsCollectedChanged;
        public event EventHandler AllPointsCollected;

        public GameField()
        {
            Width = 19; // Уменьшим для простоты
            Height = 21;
            _points = new List<GamePoint>();
            _walls = new List<Wall>();
            _energizers = new List<Energizer>();

            InitializeField();
        }

        private void InitializeField()
        {
            CreateSimpleMap();
        }

        private void CreateSimpleMap()
        {
            // Спавн игрока в центре
            PlayerSpawn = new Vector2(
                Width / 2 * TileSize + TileSize / 2,
                (Height - 3) * TileSize + TileSize / 2
            );

            // Дом призраков в центре
            GhostHouse = new Vector2(
                Width / 2 * TileSize + TileSize / 2,
                10 * TileSize + TileSize / 2
            );

            // Очищаем все списки
            _walls.Clear();
            _points.Clear();
            _energizers.Clear();

            // 1. Внешние стены
            CreateBorderWalls();

            // 2. Простой лабиринт внутри
            CreateSimpleMaze();

            // 3. Дом призраков (с выходом!)
            CreateGhostHouseWithExit();

            // 4. Точки везде, где можно пройти
            CreateDots();

            // 5. Энерджайзеры
            CreateEnergizers();
        }

        private void CreateBorderWalls()
        {
            // Верхняя стена
            for (int x = 0; x < Width; x++)
            {
                AddWall(x, 0);
            }

            // Нижняя стена
            for (int x = 0; x < Width; x++)
            {
                AddWall(x, Height - 1);
            }

            // Левая стена
            for (int y = 1; y < Height - 1; y++)
            {
                AddWall(0, y);
            }

            // Правая стена
            for (int y = 1; y < Height - 1; y++)
            {
                AddWall(Width - 1, y);
            }

            // Туннели - отверстия в левой и правой стенах
            RemoveWall(0, Height / 2); // Левое отверстие
            RemoveWall(0, Height / 2 + 1);
            RemoveWall(Width - 1, Height / 2); // Правое отверстие
            RemoveWall(Width - 1, Height / 2 + 1);
        }

        private void CreateSimpleMaze()
        {
            // Вертикальные перегородки
            for (int y = 3; y < 8; y++)
            {
                AddWall(4, y);
                AddWall(Width - 5, y);
            }

            // Горизонтальные перегородки
            for (int x = 6; x < Width - 6; x++)
            {
                AddWall(x, 5);
                AddWall(x, Height - 6);
            }

            // Несколько блоков
            AddWall(7, 8);
            AddWall(8, 8);
            AddWall(9, 8);

            AddWall(Width - 8, 8);
            AddWall(Width - 9, 8);
            AddWall(Width - 10, 8);

            AddWall(Width / 2, 15);
            AddWall(Width / 2 - 1, 15);
            AddWall(Width / 2 + 1, 15);
        }

        private void CreateGhostHouseWithExit()
        {
            int centerX = Width / 2;
            int houseY = 8; // Выше, чтобы было проще выйти

            // Верхняя стена дома
            for (int x = centerX - 2; x <= centerX + 2; x++)
            {
                AddWall(x, houseY - 2);
            }

            // Нижняя стена дома
            for (int x = centerX - 2; x <= centerX + 2; x++)
            {
                AddWall(x, houseY + 2);
            }

            // Левая стена дома
            AddWall(centerX - 2, houseY - 1);
            AddWall(centerX - 2, houseY);
            AddWall(centerX - 2, houseY + 1);

            // Правая стена дома
            AddWall(centerX + 2, houseY - 1);
            AddWall(centerX + 2, houseY);
            AddWall(centerX + 2, houseY + 1);

            // ВЫХОД из дома - ОЧЕНЬ ВАЖНО!
            RemoveWall(centerX, houseY + 2); // Убираем нижнюю стену посередине
            RemoveWall(centerX, houseY + 1); // И еще одну для хорошего прохода

            // Проход снизу
            for (int y = houseY + 3; y < houseY + 6; y++)
            {
                RemoveWall(centerX, y);
            }
        }

        private void CreateDots()
        {
            // Проходим по всей карте
            for (int x = 1; x < Width - 1; x++)
            {
                for (int y = 1; y < Height - 1; y++)
                {
                    // Пропускаем стены
                    if (IsWallAtGrid(x, y))
                        continue;

                    // Пропускаем дом призраков
                    if (IsInGhostHouse(x, y))
                        continue;

                    // Пропускаем места для энерджайзеров
                    if (IsEnergizerSpot(x, y))
                        continue;

                    // Добавляем точку
                    AddPoint(x, y);
                }
            }
        }

        private void CreateEnergizers()
        {
            // 4 энерджайзера в углах лабиринта
            AddEnergizer(2, 2);
            AddEnergizer(Width - 3, 2);
            AddEnergizer(2, Height - 3);
            AddEnergizer(Width - 3, Height - 3);
        }

        // Вспомогательные методы
        private void AddWall(int gridX, int gridY)
        {
            double posX = gridX * TileSize + TileSize / 2;
            double posY = gridY * TileSize + TileSize / 2;
            _walls.Add(new Wall(posX, posY, TileSize, TileSize));
        }

        private void RemoveWall(int gridX, int gridY)
        {
            double targetX = gridX * TileSize + TileSize / 2;
            double targetY = gridY * TileSize + TileSize / 2;

            _walls.RemoveAll(w =>
                Math.Abs(w.Position.X - targetX) < 1 &&
                Math.Abs(w.Position.Y - targetY) < 1);
        }

        private void AddPoint(int gridX, int gridY)
        {
            double posX = gridX * TileSize + TileSize / 2;
            double posY = gridY * TileSize + TileSize / 2;

            var point = new GamePoint(posX, posY);
            point.PointCollected += OnPointCollected;
            _points.Add(point);
        }

        private void AddEnergizer(int gridX, int gridY)
        {
            double posX = gridX * TileSize + TileSize / 2;
            double posY = gridY * TileSize + TileSize / 2;

            var energizer = new Energizer(posX, posY);
            energizer.PointCollected += OnPointCollected;
            _energizers.Add(energizer);
        }

        private bool IsWallAtGrid(int gridX, int gridY)
        {
            double posX = gridX * TileSize + TileSize / 2;
            double posY = gridY * TileSize + TileSize / 2;

            foreach (var wall in _walls)
            {
                if (Math.Abs(wall.Position.X - posX) < 1 &&
                    Math.Abs(wall.Position.Y - posY) < 1)
                    return true;
            }

            return false;
        }

        private bool IsInGhostHouse(int gridX, int gridY)
        {
            int centerX = Width / 2;
            int houseY = 8;

            return gridX >= centerX - 2 && gridX <= centerX + 2 &&
                   gridY >= houseY - 2 && gridY <= houseY + 2;
        }

        private bool IsEnergizerSpot(int gridX, int gridY)
        {
            return (gridX == 2 && gridY == 2) ||
                   (gridX == Width - 3 && gridY == 2) ||
                   (gridX == 2 && gridY == Height - 3) ||
                   (gridX == Width - 3 && gridY == Height - 3);
        }

        private void OnPointCollected(object sender, EventArgs e)
        {
            int collectedPoints = _points.Count(p => p.IsCollected) + _energizers.Count(e => e.IsCollected);
            PointsCollectedChanged?.Invoke(this, collectedPoints);

            if (_points.All(p => p.IsCollected) && _energizers.All(e => e.IsCollected))
            {
                AllPointsCollected?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsWallAt(Vector2 position, double radius = 12)
        {
            var testRect = new Rect(
                position.X - radius,
                position.Y - radius,
                radius * 2,
                radius * 2
            );

            foreach (var wall in _walls)
            {
                if (wall.Bounds.IntersectsWith(testRect))
                    return true;
            }

            return false;
        }

        public bool CanMoveTo(Vector2 position, Direction direction, double speed)
        {
            if (direction == Direction.None) return true;

            Vector2 nextPosition = position;

            switch (direction)
            {
                case Direction.Up:
                    nextPosition.Y -= speed;
                    break;
                case Direction.Down:
                    nextPosition.Y += speed;
                    break;
                case Direction.Left:
                    nextPosition.X -= speed;
                    break;
                case Direction.Right:
                    nextPosition.X += speed;
                    break;
            }

            // Телепортация через туннели
            if (nextPosition.X < -TileSize / 2 &&
                nextPosition.Y >= (Height / 2 - 1) * TileSize &&
                nextPosition.Y <= (Height / 2 + 2) * TileSize)
            {
                return true; // Разрешаем вход в туннель
            }

            if (nextPosition.X > Width * TileSize + TileSize / 2 &&
                nextPosition.Y >= (Height / 2 - 1) * TileSize &&
                nextPosition.Y <= (Height / 2 + 2) * TileSize)
            {
                return true; // Разрешаем вход в туннель
            }

            // Проверка границ
            if (nextPosition.X < 0 || nextPosition.X > Width * TileSize ||
                nextPosition.Y < 0 || nextPosition.Y > Height * TileSize)
                return false;

            return !IsWallAt(nextPosition);
        }

        public GamePoint GetPointAt(Vector2 position)
        {
            double tolerance = 12;

            // Проверяем обычные точки
            foreach (var point in _points)
            {
                if (!point.IsCollected && point.IsActive)
                {
                    double distance = Math.Sqrt(
                        Math.Pow(point.Position.X - position.X, 2) +
                        Math.Pow(point.Position.Y - position.Y, 2)
                    );

                    if (distance < tolerance)
                    {
                        return point;
                    }
                }
            }

            // Проверяем энерджайзеры
            foreach (var energizer in _energizers)
            {
                if (!energizer.IsCollected && energizer.IsActive)
                {
                    double distance = Math.Sqrt(
                        Math.Pow(energizer.Position.X - position.X, 2) +
                        Math.Pow(energizer.Position.Y - position.Y, 2)
                    );

                    if (distance < tolerance)
                    {
                        return energizer;
                    }
                }
            }

            return null;
        }

        public void Initialize()
        {
            // Уже инициализировано в конструкторе
        }

        public void Update(TimeSpan gameTime)
        {
            // Ничего не обновляем
        }

        public void Draw(System.Windows.Media.DrawingContext drawingContext)
        {
            // Черный фон
            drawingContext.DrawRectangle(
                System.Windows.Media.Brushes.Black,
                null,
                new Rect(0, 0, Width * TileSize, Height * TileSize)
            );

            // Рисуем стены
            foreach (var wall in _walls)
            {
                wall.Draw(drawingContext);
            }

            // Рисуем точки
            foreach (var point in _points)
            {
                point.Draw(drawingContext);
            }

            // Рисуем энерджайзеры
            foreach (var energizer in _energizers)
            {
                energizer.Draw(drawingContext);
            }

            // Рисуем туннели
            DrawTunnels(drawingContext);
        }

        private void DrawTunnels(System.Windows.Media.DrawingContext drawingContext)
        {
            int tunnelY = Height / 2;

            // Левый туннель
            var leftTunnel = new Rect(-TileSize, (tunnelY - 1) * TileSize, TileSize * 2, TileSize * 3);
            drawingContext.DrawRectangle(
                System.Windows.Media.Brushes.Black,
                new System.Windows.Media.Pen(System.Windows.Media.Brushes.Blue, 2),
                leftTunnel
            );

            // Правый туннель
            var rightTunnel = new Rect(Width * TileSize - TileSize, (tunnelY - 1) * TileSize, TileSize * 2, TileSize * 3);
            drawingContext.DrawRectangle(
                System.Windows.Media.Brushes.Black,
                new System.Windows.Media.Pen(System.Windows.Media.Brushes.Blue, 2),
                rightTunnel
            );
        }

        public void CheckTeleport(GameObject obj)
        {
            // Левая телепортация
            if (obj.Position.X < -TileSize / 2 &&
                obj.Position.Y >= (Height / 2 - 1) * TileSize &&
                obj.Position.Y <= (Height / 2 + 2) * TileSize)
            {
                obj.Position = new Vector2(Width * TileSize + TileSize / 2, obj.Position.Y);
            }
            // Правая телепортация
            else if (obj.Position.X > Width * TileSize + TileSize / 2 &&
                     obj.Position.Y >= (Height / 2 - 1) * TileSize &&
                     obj.Position.Y <= (Height / 2 + 2) * TileSize)
            {
                obj.Position = new Vector2(-TileSize / 2, obj.Position.Y);
            }
        }

        public void ResetField()
        {
            foreach (var point in _points)
            {
                point.IsCollected = false;
                point.IsActive = true;
            }

            foreach (var energizer in _energizers)
            {
                energizer.IsCollected = false;
                energizer.IsActive = true;
            }
        }

        public List<Wall> GetWalls()
        {
            return _walls;
        }

        // Новый метод для привидений - получить доступные направления
        public List<Direction> GetAvailableDirections(Vector2 position, Direction currentDirection)
        {
            var directions = new List<Direction>();

            // Проверяем все 4 направления
            if (CanMoveTo(position, Direction.Up, 1))
                directions.Add(Direction.Up);
            if (CanMoveTo(position, Direction.Down, 1))
                directions.Add(Direction.Down);
            if (CanMoveTo(position, Direction.Left, 1))
                directions.Add(Direction.Left);
            if (CanMoveTo(position, Direction.Right, 1))
                directions.Add(Direction.Right);

            // Убираем противоположное направление (чтобы не ходить назад)
            var opposite = GetOppositeDirection(currentDirection);
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
    }
}