using System.Windows;
using System.Windows.Media;
using pacman3.Utils;

namespace pacman3.Models.Game
{
    public class Wall : GameObject
    {
        public double Width { get; set; }
        public double Height { get; set; }

        public Wall(double x, double y, double width, double height) : base(x, y)
        {
            Width = width;
            Height = height;
            ObjectColor = Colors.Blue;
            IsActive = true;
        }

        public override Rect Bounds
        {
            get
            {
                return new Rect(
                    Position.X - Width / 2,
                    Position.Y - Height / 2,
                    Width,
                    Height
                );
            }
        }

        public override void Draw(DrawingContext drawingContext)
        {
            var brush = new SolidColorBrush(ObjectColor);
            brush.Freeze();

            var pen = new Pen(Brushes.DarkBlue, 2);
            pen.Freeze();

            // Рисуем стену с закругленными углами
            var rect = new Rect(
                Position.X - Width / 2 + 2,
                Position.Y - Height / 2 + 2,
                Width - 4,
                Height - 4
            );

            drawingContext.DrawRoundedRectangle(
                brush,
                pen,
                rect,
                5, // Радиус скругления
                5
            );
        }
    }
}