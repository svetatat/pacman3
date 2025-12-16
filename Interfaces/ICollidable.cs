using System.Windows;

namespace pacman3.Interfaces
{
    public interface ICollidable
    {
        Rect Bounds { get; } // Границы объекта для проверки столкновений
        void OnCollision(ICollidable other); // Вызывается при столкновении с другим объектом
    }
}
