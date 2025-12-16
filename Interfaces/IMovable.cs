using pacman3.Utils;

namespace pacman3.Interfaces
{
    public interface IMovable
    {
        Vector2 Position { get; set; }
        Vector2 Velocity { get; set; }
        void Move(Vector2 direction);
    }
}