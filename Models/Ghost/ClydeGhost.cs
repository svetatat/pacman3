using System.Windows.Media;
using pacman3.Utils;

namespace pacman3.Models.Ghosts
{
    public class ClydeGhost : Ghost // убегает на расстоянии
    {
        protected override Color OriginalColor => Colors.Orange;

        public override Vector2 CalculateTargetPosition(Vector2 playerPosition, Direction playerDirection)
        {
            // Если близко к игроку - убегает в угол
            double distance = System.Math.Sqrt(
                System.Math.Pow(Position.X - playerPosition.X, 2) +
                System.Math.Pow(Position.Y - playerPosition.Y, 2)
            );

            if (distance < 8 * 32) // 8 тайлов
            {
                // Убегает в левый нижний угол
                return new Vector2(32, 19 * 32);
            }
            else
            {
                // Преследует
                return playerPosition;
            }
        }
    }
}