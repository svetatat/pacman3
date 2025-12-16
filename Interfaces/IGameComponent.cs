using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pacman3.Interfaces
{
    internal interface IGameComponent
    {
        void Initialize(); // Инициализация компонента
        void Update(TimeSpan gameTime); // Обновление состояния компонента
    }
}
