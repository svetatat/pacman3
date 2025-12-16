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
            var testLevel = new int[,]
            {
                {1,1,1,1,1,1,1,1,1,1},
                {1,2,2,2,2,1,2,2,2,1},
                {1,3,1,1,2,1,2,1,3,1},
                {1,2,1,2,2,2,2,1,2,1},
                {1,2,2,2,1,1,2,2,2,1},
                {1,1,2,1,1,1,1,2,1,1},
                {1,2,2,2,1,1,2,2,2,1},
                {1,2,1,2,2,2,2,1,2,1},
                {1,3,1,1,2,1,2,1,3,1},
                {1,1,1,1,1,1,1,1,1,1}
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

        public bool IsWallAt(Vector2 position)
        {
            var testRect = new Rect(position.X - TileSize / 4, position.Y - TileSize / 4, TileSize / 2, TileSize / 2);

            foreach (var wall in _walls)
            {
                if (wall.Bounds.IntersectsWith(testRect))
                    return true;
            }

            return false;
        }

        public bool CanMoveTo(Vector2 position, Direction direction, double speed)
        {
            Vector2 testPosition = position;

            switch (direction)
            {
                case Direction.Up:
                    testPosition.Y -= speed;
                    break;
                case Direction.Down:
                    testPosition.Y += speed;
                    break;
                case Direction.Left:
                    testPosition.X -= speed;
                    break;
                case Direction.Right:
                    testPosition.X += speed;
                    break;
                case Direction.None:
                    return true;
            }

            return !IsWallAt(testPosition);
        }

        public GamePoint GetPointAt(Vector2 position)
        {
            var tolerance = TileSize / 3;

            foreach (var point in _points)
            {
                if (!point.IsCollected &&
                    Math.Abs(point.Position.X - position.X) < tolerance &&
                    Math.Abs(point.Position.Y - position.Y) < tolerance)
                {
                    return point;
                }
            }

            return null;
        }

        public void Initialize()
        {
            LoadTestLevel();
        }

        public void Update(TimeSpan gameTime)
        {
            foreach (var point in _points)
            {
                point.Update(gameTime);
            }
        }

        public void Draw(System.Windows.Media.DrawingContext drawingContext)
        {
            foreach (var wall in _walls)
            {
                wall.Draw(drawingContext);
            }

            foreach (var point in _points)
            {
                point.Draw(drawingContext);
            }
        }
    }
}