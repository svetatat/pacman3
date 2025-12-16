using System.Windows.Media;

namespace pacman3.Models.Items
{
    public class Energizer : GamePoint
    {
        public Energizer(double x, double y) : base(x, y)
        {
        }

        protected override void InitializePoint()
        {
            base.InitializePoint();
            Size = 12;
            ObjectColor = Colors.Pink;
            PointValue = 50;
            Type = PointType.Energizer;
        }

        public override void Collect()
        {
            base.Collect();
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