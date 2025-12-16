using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using pacman3.Models.Items;
using pacman3.Utils;

namespace pacman3.Models.Game
{
    public class GameField : Interfaces.IGameComponent
    {
        private int[,] _grid;
        private List<GamePoint> _points;
        private List<Wall> _walls;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int TileSize { get; private set; } = 32;

        public IReadOnlyList<GamePoint> Points => _points;
        public IReadOnlyList<Wall> Walls => _walls;
        public int TotalPoints => _points.Count(p => !p.IsCollected);

        public event EventHandler<int> PointsCollectedChanged;
        public event EventHandler AllPointsCollected;

        public GameField(int width, int height)
        {
            Width = width;
            Height = height;
            _grid = new int[width, height];
            _points = new List<GamePoint>();
            _walls = new List<Wall>();
        }

        public void LoadLevel(int[,] levelData)
        {
            if (levelData.GetLength(0) != Width || levelData.GetLength(1) != Height)
                return;

            _grid = (int[,])levelData.Clone();
            GenerateObjectsFromGrid();
        }

        public void LoadTestLevel()
        {
            // Упрощенная карта 19x21 с нормальными стенами
            var testLevel = new int[,]
            {
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
                {1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1},
                {1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1},
                {1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1},
                {1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1},
                {1,2,1,1,2,1,2,1,1,1,1,1,2,1,2,1,1,2,1},
                {1,2,2,2,2,1,2,2,2,1,2,2,2,1,2,2,2,2,1},
                {1,1,1,1,2,1,1,1,0,1,0,1,1,1,2,1,1,1,1},
                {2,2,2,1,2,1,0,0,0,0,0,0,0,1,2,1,2,2,2},
                {1,1,1,1,2,1,0,1,1,1,1,1,0,1,2,1,1,1,1},
                {2,2,2,2,2,0,0,1,2,2,2,1,0,0,2,2,2,2,2},
                {1,1,1,1,2,1,0,1,1,1,1,1,0,1,2,1,1,1,1},
                {2,2,2,1,2,1,0,0,0,0,0,0,0,1,2,1,2,2,2},
                {1,1,1,1,2,1,0,1,1,1,1,1,0,1,2,1,1,1,1},
                {1,2,2,2,2,2,2,2,2,1,2,2,2,2,2,2,2,2,1},
                {1,2,1,1,2,1,1,1,2,1,2,1,1,1,2,1,1,2,1},
                {1,3,2,1,2,2,2,2,2,2,2,2,2,2,2,1,2,3,1},
                {1,1,2,1,2,1,2,1,1,1,1,1,2,1,2,1,2,1,1},
                {1,2,2,2,2,1,2,2,2,1,2,2,2,1,2,2,2,2,1},
                {1,2,1,1,1,1,1,1,2,1,2,1,1,1,1,1,1,2,1},
                {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}
            };

            LoadLevel(testLevel);
        }

        private void GenerateObjectsFromGrid()
        {
            _points.Clear();
            _walls.Clear();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    double posX = x * TileSize + TileSize / 2;
                    double posY = y * TileSize + TileSize / 2;

                    switch (_grid[x, y])
                    {
                        case 1:
                            _walls.Add(new Wall(posX, posY, TileSize, TileSize));
                            break;
                        case 2:
                            var point = new GamePoint(posX, posY);
                            point.PointCollected += OnPointCollected;
                            _points.Add(point);
                            break;
                        case 3:
                            var energizer = new Energizer(posX, posY);
                            energizer.PointCollected += OnPointCollected;
                            _points.Add(energizer);
                            break;
                        case 0:
                            // Пустое пространство (туннели)
                            break;
                    }
                }
            }
        }

        private void OnPointCollected(object sender, EventArgs e)
        {
            PointsCollectedChanged?.Invoke(this, _points.Count(p => p.IsCollected));

            if (_points.All(p => p.IsCollected))
            {
                AllPointsCollected?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool IsWallAt(Vector2 position, double radius = 12)
        {
            var playerRect = new Rect(
                position.X - radius,
                position.Y - radius,
                radius * 2,
                radius * 2
            );

            foreach (var wall in _walls)
            {
                if (wall.Bounds.IntersectsWith(playerRect))
                    return true;
            }

            return false;
        }

        public bool CanMoveTo(Vector2 position, Direction direction, double speed)
        {
            if (direction == Direction.None) return true;

            // Проверяем следующую позицию
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

            // Проверяем телепортацию через туннели
            if (nextPosition.X < -20)
            {
                nextPosition.X = Width * TileSize + 20;
            }
            else if (nextPosition.X > Width * TileSize + 20)
            {
                nextPosition.X = -20;
            }

            // Проверяем, не выходит ли за границы
            if (nextPosition.Y < 0 || nextPosition.Y > Height * TileSize)
                return false;

            return !IsWallAt(nextPosition);
        }

        public GamePoint GetPointAt(Vector2 position)
        {
            foreach (var point in _points)
            {
                if (!point.IsCollected)
                {
                    double distance = Math.Sqrt(
                        Math.Pow(point.Position.X - position.X, 2) +
                        Math.Pow(point.Position.Y - position.Y, 2)
                    );

                    if (distance < 8) // Радиус сбора точек
                    {
                        return point;
                    }
                }
            }

            return null;
        }

        public void Initialize()
        {
            Width = 19;
            Height = 21;
            _grid = new int[Width, Height];
            _points = new List<GamePoint>();
            _walls = new List<Wall>();

            LoadTestLevel();
        }

        public void Update(TimeSpan gameTime)
        {
            // Точки не требуют обновления
        }

        public void Draw(System.Windows.Media.DrawingContext drawingContext)
        {
            // Рисуем фон
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

            // Рисуем туннели
            DrawTunnels(drawingContext);
        }

        private void DrawTunnels(System.Windows.Media.DrawingContext drawingContext)
        {
            // Левый туннель
            var leftTunnel = new Rect(-TileSize, 9 * TileSize, TileSize * 2, TileSize * 3);
            drawingContext.DrawRectangle(
                System.Windows.Media.Brushes.Black,
                new System.Windows.Media.Pen(System.Windows.Media.Brushes.Blue, 2),
                leftTunnel
            );

            // Правый туннель
            var rightTunnel = new Rect(Width * TileSize - TileSize, 9 * TileSize, TileSize * 2, TileSize * 3);
            drawingContext.DrawRectangle(
                System.Windows.Media.Brushes.Black,
                new System.Windows.Media.Pen(System.Windows.Media.Brushes.Blue, 2),
                rightTunnel
            );
        }

        // Проверка телепортации через туннели
        public void CheckTeleport(GameObject obj)
        {
            // Телепортация через левый туннель
            if (obj.Position.X < -obj.Size / 2)
            {
                obj.Position = new Vector2(Width * TileSize + obj.Size / 2, obj.Position.Y);
            }
            // Телепортация через правый туннель
            else if (obj.Position.X > Width * TileSize + obj.Size / 2)
            {
                obj.Position = new Vector2(-obj.Size / 2, obj.Position.Y);
            }
        }
    }
}