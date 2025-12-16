using System.Windows;

namespace PacManGame.Models.Interfaces
{
    /// <summary>
    /// Интерфейс для перемещаемых объектов
    /// </summary>
    public interface IMovable
    {
        /// <summary>
        /// Позиция объекта
        /// </summary>
        Vector Position { get; set; }

        /// <summary>
        /// Скорость объекта
        /// </summary>
        Vector Velocity { get; set; }

        /// <summary>
        /// Перемещение объекта
        /// </summary>
        /// <param name="direction">Направление движения</param>
        void Move(Vector direction);
    }
}