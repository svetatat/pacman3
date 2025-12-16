using System.Windows.Media;
using pacman3.Utils;

namespace pacman3.Models.Ghosts
{
    public class InkyGhost : Ghost // непредсказуемый
    {
        private Random _random = new Random();

        protected override Color OriginalColor => Colors.Cyan;

        public override Vector2 CalculateTargetPosition(Vector2 playerPosition, Direction playerDirection)
        {
            // Случайное движение
            return new Vector2(
                _random.Next(0, 20) * 32,
                _random.Next(0, 20) * 32
            );
        }
    }
}