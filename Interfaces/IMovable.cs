using pacman3.Utils;

namespace pacman3.Interfaces
{
    public interface IMovable
    {
        double Speed { get; set; }
        Direction Direction { get; set; }
        void Move();
    }
}