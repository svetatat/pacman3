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
            private set => _isCollected = value;
        }

        public int PointValue
        {
            get => _pointValue;
            protected set => _pointValue = value;
        }

        public PointType Type { get; protected set; }

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
        }

        public virtual void Collect()
        {
            if (IsCollected) return;

            IsCollected = true;
            IsActive = false;
            PointCollected?.Invoke(this, System.EventArgs.Empty);
        }

        public override void OnCollision(Interfaces.ICollidable other)
        {
            base.OnCollision(other);

            if (other is Models.Player.Player)
            {
                Collect();
            }
        }

        public override void Draw(System.Windows.Media.DrawingContext drawingContext)
        {
            if (IsCollected) return;

            var brush = new SolidColorBrush(ObjectColor);
            var pen = new Pen(Brushes.Transparent, 0);

            drawingContext.DrawEllipse(
                brush,
                pen,
                new System.Windows.Point(Position.X, Position.Y),
                Size / 2,
                Size / 2
            );
        }
    }
}