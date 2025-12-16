using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pacman3.Utils
{
    /// <summary>
    /// Класс для отслеживания игрового времени
    /// </summary>
    public class GameTime
    {
        public TimeSpan TotalTime { get; private set; }
        public TimeSpan ElapsedTime { get; private set; }

        public void Update(TimeSpan elapsedTime)
        {
            ElapsedTime = elapsedTime;
            TotalTime += elapsedTime;
        }

        public void Reset()
        {
            TotalTime = TimeSpan.Zero;
            ElapsedTime = TimeSpan.Zero;
        }
    }
}