using pacman3.Utils;
using System.Windows;

namespace pacman3.Interfaces
{
    /// <summary>
    /// Интерфейс для перемещаемых объектов
    /// </summary>
    public interface IMovable
    {
        /// <summary>
        /// Позиция объекта
        /// </summary>
        System.Numerics.Vector Position { get; set; }

        /// <summary>
        /// Скорость объекта
        /// </summary>
        System.Numerics.Vector Velocity { get; set; }

        /// <summary>
        /// Перемещение объекта
        /// </summary>
        /// <param name="direction">Направление движения</param>
        void Move(System.Numerics.Vector direction);
    }
}