using pacman3.Interfaces;
using pacman3.Interfaces;
using pacman3.Utils;
using System;
using System.Numerics;
using System.Windows;
using System.Windows.Media;

namespace pacman3.Models.Game
{
    /// <summary>
    /// Базовый абстрактный класс для всех игровых объектов
    /// Демонстрация: Наследование + Абстрактный класс
    /// </summary>
    public abstract class GameObject : IGameComponent, ICollidable, IDrawable
    {
        // ИНКАПСУЛЯЦИЯ: приватные поля с публичными свойствами
        private Vector2 _position;
        private bool _isActive = true;

        /// <summary>
        /// Позиция объекта на поле
        /// </summary>
        public Vector2 Position
        {
            get => _position;
            set
            {
                // Можно добавить валидацию
                _position = value;
            }
        }

        /// <summary>
        /// Активен ли объект
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            protected set => _isActive = value;
        }

        /// <summary>
        /// Размер объекта (для отрисовки и коллизий)
        /// </summary>
        public virtual double Size { get; protected set; } = 32;

        /// <summary>
        /// Цвет объекта (для базовой отрисовки)
        /// </summary>
        public virtual Color ObjectColor { get; protected set; } = Colors.White;

        // Реализация ICollidable
        /// <summary>
        /// Границы объекта для проверки столкновений
        /// </summary>
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

        // Конструктор
        protected GameObject()
        {
            Initialize();
        }

        protected GameObject(double x, double y) : this()
        {
            Position = new Vector2(x, y);
        }

        // Реализация IGameComponent
        public virtual void Initialize()
        {
            // Базовая инициализация
            IsActive = true;
        }

        public virtual void Update(TimeSpan gameTime)
        {
            // Базовая логика обновления
            // Может быть переопределена в наследниках
        }

        // Реализация ICollidable
        public virtual void OnCollision(ICollidable other)
        {
            // Базовая реакция на столкновение
            // Будет переопределена в конкретных объектах
            Console.WriteLine($"{GetType().Name} столкнулся с {other.GetType().Name}");
        }

        // Реализация IDrawable
        public virtual void Draw(DrawingContext drawingContext)
        {
            if (!IsActive) return;

            // Базовая отрисовка - круг
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

        /// <summary>
        /// Проверка столкновения с другим объектом
        /// </summary>
        public bool IntersectsWith(GameObject other)
        {
            return Bounds.IntersectsWith(other.Bounds);
        }

        /// <summary>
        /// Деактивация объекта
        /// </summary>
        public virtual void Deactivate()
        {
            IsActive = false;
        }
    }
}