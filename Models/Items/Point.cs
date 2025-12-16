using System.Windows.Media;
using pacman3.Models.Game;
using pacman3.Utils;

namespace pacman3.Models.Items
{
    public class GamePoint : GameObject
    {
        private bool _isCollected;
        private int _pointValue;

        public bool IsCollected
        {
            get => _isCollected;
            set // Изменено с private на public
            {
                if (_isCollected != value)
                {
                    _isCollected = value;
                    IsActive = !value; // Деактивируем объект при сборе
                }
            }
        }

        public int PointValue
        {
            get => _pointValue;
            set => _pointValue = value; // Изменено с protected на public
        }

        public PointType Type { get; set; } // Изменено с protected на public

        public event System.EventHandler PointCollected;

        public GamePoint(double x, double y) : base(x, y)
        {
            InitializePoint();
        }

        protected virtual void InitializePoint()
        {
            Size = 6;
            ObjectColor = Colors.White;
            IsCollected = false;
            PointValue = 10;
            Type = PointType.Regular;
            IsActive = true;
        }

        public virtual void Collect()
        {
            if (IsCollected) return;

            IsCollected = true;
            PointCollected?.Invoke(this, System.EventArgs.Empty);

            System.Diagnostics.Debug.WriteLine($"Точка собрана! Очки: {PointValue}");
        }

        public override void OnCollision(Interfaces.ICollidable other)
        {
            base.OnCollision(other);

            if (other is Player player)
            {
                Collect();
                player.CollectPoint(PointValue);
            }
        }

        public override void Draw(System.Windows.Media.DrawingContext drawingContext)
        {
            if (IsCollected || !IsActive) return;

            var brush = new SolidColorBrush(ObjectColor);
            brush.Freeze();

            drawingContext.DrawEllipse(
                brush,
                null,
                new System.Windows.Point(Position.X, Position.Y),
                Size / 2,
                Size / 2
            );
        }
    }
}