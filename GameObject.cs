using System;
using System.Windows;
using System.Windows.Media;
using pacman3.Interfaces;
using pacman3.Utils;

namespace pacman3.Models.Game
{
    public abstract class GameObject : IGameComponent, ICollidable, IDrawable
    {
        private Vector2 _position;
        private bool _isActive = true;
        private Color _objectColor = Colors.White;
        private double _size = 32;

        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value; // Изменено с protected на public set
        }

        public virtual Color ObjectColor
        {
            get => _objectColor;
            set => _objectColor = value;
        }

        public virtual double Size
        {
            get => _size;
            set => _size = value;
        }

        public virtual Rect Bounds
        {
            get
            {
                double halfSize = Size / 2;
                return new Rect(
                    Position.X - halfSize,
                    Position.Y - halfSize,
                    Size,
                    Size
                );
            }
        }

        protected GameObject()
        {
            Initialize();
        }

        protected GameObject(double x, double y) : this()
        {
            Position = new Vector2(x, y);
        }

        public virtual void Initialize()
        {
            IsActive = true;
        }

        public virtual void Update(TimeSpan gameTime) { }

        public virtual void OnCollision(ICollidable other)
        {
            // Базовая реализация может быть пустой
        }

        public virtual void Draw(DrawingContext drawingContext)
        {
            if (!IsActive) return;

            var brush = new SolidColorBrush(ObjectColor);
            var pen = new Pen(Brushes.Black, 1);

            double halfSize = Size / 2;
            drawingContext.DrawEllipse(
                brush,
                pen,
                new Point(Position.X, Position.Y),
                halfSize,
                halfSize
            );
        }

        public bool IntersectsWith(GameObject other)
        {
            return Bounds.IntersectsWith(other.Bounds);
        }

        public virtual void Deactivate()
        {
            IsActive = false;
        }
    }
}