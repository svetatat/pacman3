using pacman3.Utils;
using System.Windows.Media;

namespace pacman3.Models.Ghosts
{
    public class BlinkyGhost : Ghost // преследует напрямую
    {
        protected override Color OriginalColor => Colors.Red;

        public override Vector2 CalculateTargetPosition(Vector2 playerPosition, Direction playerDirection)
        {
            // Прямое преследование игрока
            return playerPosition;
        }
    }
}