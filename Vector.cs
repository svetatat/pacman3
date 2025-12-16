using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pacman3
{
    internal class Vector
    {
        public struct Vector2 // 2D вектор для позиционирования объектов
        {
            public double X { get; set; }
            public double Y { get; set; }

            public Vector2(double x, double y)
            {
                X = x;
                Y = y;
            }

            public static Vector2 operator +(Vector2 a, Vector2 b)
                => new Vector2(a.X + b.X, a.Y + b.Y);

            public static Vector2 operator -(Vector2 a, Vector2 b)
                => new Vector2(a.X - b.X, a.Y - b.Y);

            public static Vector2 operator *(Vector2 v, double scalar)
                => new Vector2(v.X * scalar, v.Y * scalar);

            public override string ToString() => $"({X}, {Y})";
        }
    }
}
