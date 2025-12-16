using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace pacman3.Interfaces
{
    internal interface IDrawable
    {
        void Draw(DrawingContext drawingContext); // Отрисовка объекта
    }
}
