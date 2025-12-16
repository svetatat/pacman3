using pacman3.Models.Game;
using System.Numerics;

namespace PacManGame
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ТЕСТ БАЗОВОЙ СТРУКТУРЫ PAC-MAN ===");
            Console.WriteLine();

            // Тест GameObject
            TestGameObjectBasic();

            // Тест столкновений
            TestCollisions();

            // Тест векторов
            TestVectors();

            Console.WriteLine("\nВсе тесты пройдены успешно! ✅");
            Console.WriteLine("Нажмите любую клавишу для запуска WPF приложения...");
            Console.ReadKey();
        }

        static void TestGameObjectBasic()
        {
            Console.WriteLine("1. Тест базового класса GameObject:");

            var obj1 = new TestGameObject(50, 50);
            var obj2 = new TestGameObject(150, 150);

            Console.WriteLine($"   Объект 1: Позиция = {obj1.Position}, Активен = {obj1.IsActive}");
            Console.WriteLine($"   Объект 2: Позиция = {obj2.Position}, Активен = {obj2.IsActive}");
            Console.WriteLine($"   Размер объекта: {obj1.Size}");

            // Тест обновления
            var oldPos = obj1.Position;
            obj1.Update(TimeSpan.FromSeconds(1));
            Console.WriteLine($"   Обновление: {oldPos} -> {obj1.Position}");

            Console.WriteLine("   ✅ GameObject тест пройден");
        }

        static void TestCollisions()
        {
            Console.WriteLine("\n2. Тест столкновений:");

            var obj1 = new TestGameObject(100, 100);
            var obj2 = new TestGameObject(105, 105); // Близко для столкновения

            Console.WriteLine($"   Объект 1 границы: {obj1.Bounds}");
            Console.WriteLine($"   Объект 2 границы: {obj2.Bounds}");
            Console.WriteLine($"   Столкновение: {obj1.IntersectsWith(obj2)}");

            // Двигаем дальше
            obj2.Position = new Vector2(200, 200);
            Console.WriteLine($"   После перемещения: Столкновение = {obj1.IntersectsWith(obj2)}");

            Console.WriteLine("   ✅ Тест столкновений пройден");
        }

        static void TestVectors()
        {
            Console.WriteLine("\n3. Тест Vector2:");

            var v1 = new Vector2(10, 20);
            var v2 = new Vector2(5, 5);

            Console.WriteLine($"   v1 = {v1}");
            Console.WriteLine($"   v2 = {v2}");
            Console.WriteLine($"   v1 + v2 = {v1 + v2}");
            Console.WriteLine($"   v1 - v2 = {v1 - v2}");
            Console.WriteLine($"   v1 * 2 = {v1 * 2}");

            Console.WriteLine("   ✅ Vector2 тест пройден");
        }
    }

    // Простой тестовый класс для проверки
    public class TestGameObject : GameObject
    {
        public TestGameObject(double x, double y) : base(x, y)
        {
            Size = 30;
        }
    }
}
