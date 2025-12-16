using pacman3.Models.Ghosts;
using pacman3.Models.Items;
using pacman3.Models.Player;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;

namespace pacman3.Models.Game
{
    public class CollisionManager
    {
        public event EventHandler<int> PointCollected;
        public event EventHandler GhostEaten;
        public event EventHandler PlayerDamaged;

        // ИСПРАВЛЕНО: Просто Player, так как он в той же сборке
        public void CheckCollisions(Player.Player player,  // Player.Player если есть конфликт
                                   IEnumerable<Ghost> ghosts,
                                   IEnumerable<GamePoint> points)
        {
            // Проверка столкновений с точками
            foreach (var point in points)
            {
                if (!point.IsCollected && player.IntersectsWith(point))
                {
                    point.Collect();
                    player.CollectPoint(point.PointValue);
                    PointCollected?.Invoke(this, point.PointValue);

                    if (point is Energizer)
                    {
                        // Активируем уязвимость всех привидений
                        foreach (var ghost in ghosts)
                        {
                            ghost.MakeVulnerable(TimeSpan.FromSeconds(10));
                        }
                    }
                }
            }

            // Проверка столкновений с привидениями
            foreach (var ghost in ghosts)
            {
                if (player.IntersectsWith(ghost))
                {
                    HandlePlayerGhostCollision(player, ghost);
                }
            }
        }

        private void HandlePlayerGhostCollision(Player.Player player, Ghost ghost)
        {
            if (ghost.State == GhostState.Vulnerable)
            {
                // Игрок съедает привидение
                ghost.Die();
                player.AddScore(200);
                GhostEaten?.Invoke(this, EventArgs.Empty);
            }
            else if (ghost.State == GhostState.Normal)
            {
                // Привидение ранит игрока
                player.TakeDamage();
                PlayerDamaged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}