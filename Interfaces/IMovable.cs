using pacman3.Utils;
using System.Windows;

namespace pacman3.Interfaces
{
    public interface IMovable // Интерфейс для перемещаемых объектов
    {
        System.Numerics.Vector Position { get; set; }
        System.Numerics.Vector Velocity { get; set; }
        void Move(System.Numerics.Vector direction);
    }
}