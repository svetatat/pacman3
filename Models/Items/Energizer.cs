using System;
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
            IsActive = true;
        }

        public override void Collect()
        {
            base.Collect();
            System.Diagnostics.Debug.WriteLine("Энерджайзер собран!");
        }

        public override void Draw(System.Windows.Media.DrawingContext drawingContext)
        {
            if (IsCollected || !IsActive) return;

            // Мигающий эффект
            var time = DateTime.Now.Millisecond;
            var alpha = (byte)(128 + 127 * Math.Sin(time * 0.01));
            var color = Color.FromArgb(alpha, ObjectColor.R, ObjectColor.G, ObjectColor.B);

            var brush = new SolidColorBrush(color);
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