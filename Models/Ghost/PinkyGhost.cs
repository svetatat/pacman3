using System.Windows.Media;
using pacman3.Utils;

namespace pacman3.Models.Ghosts
{
    public class PinkyGhost : Ghost // пытается зайти спереди
    {
        protected override Color OriginalColor => Colors.Pink;

        public override Vector2 CalculateTargetPosition(Vector2 playerPosition, Direction playerDirection)
        {
            // Заход на 4 тайла вперед от игрока
            Vector2 target = playerPosition;

            switch (playerDirection)
            {
                case Direction.Up:
                    target.Y -= 4 * 32;
                    break;
                case Direction.Down:
                    target.Y += 4 * 32;
                    break;
                case Direction.Left:
                    target.X -= 4 * 32;
                    break;
                case Direction.Right:
                    target.X += 4 * 32;
                    break;
            }

            return target;
        }
    }
}