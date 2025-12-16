using System.Windows;
using System.Windows.Media;

namespace pacman3.Models.Game
{
    public class MainMenu : Interfaces.IDrawable
    {
        public void Draw(DrawingContext drawingContext)
        {
            // Фон
            drawingContext.DrawRectangle(
                new SolidColorBrush(Color.FromArgb(255, 0, 0, 50)),
                null,
                new Rect(0, 0, 800, 600)
            );

            // Заголовок
            var titleText = new FormattedText(
                "PAC-MAN",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial Bold"),
                72,
                Brushes.Yellow,
                1.0
            );
            drawingContext.DrawText(titleText, new Point(250, 100));

            // Подзаголовок
            var subtitleText = new FormattedText(
                "Курсовой проект",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                24,
                Brushes.LightYellow,
                1.0
            );
            drawingContext.DrawText(subtitleText, new Point(280, 190));

            // Инструкции
            var instructionHeader = new FormattedText(
                "Управление:",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial Bold"),
                28,
                Brushes.White,
                1.0
            );
            drawingContext.DrawText(instructionHeader, new Point(100, 250));

            // Список управления
            string[] controls = {
                "Стрелки или WASD - Движение",
                "ESC или ПРОБЕЛ - Пауза",
                "Ctrl+R - Перезапуск игры",
                "ESC в меню - Выход из меню"
            };

            for (int i = 0; i < controls.Length; i++)
            {
                var controlText = new FormattedText(
                    controls[i],
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Arial"),
                    20,
                    Brushes.LightGray,
                    1.0
                );
                drawingContext.DrawText(controlText, new Point(120, 300 + i * 35));
            }

            // Цель игры
            var objectiveHeader = new FormattedText(
                "Цель игры:",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial Bold"),
                28,
                Brushes.White,
                1.0
            );
            drawingContext.DrawText(objectiveHeader, new Point(100, 450));

            var objectiveText = new FormattedText(
                "Соберите все точки на поле, избегая привидений.",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                20,
                Brushes.LightGreen,
                1.0
            );
            drawingContext.DrawText(objectiveText, new Point(120, 490));

            // Сообщение о начале игры
            var startText = new FormattedText(
                "Нажмите ENTER для начала игры",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                24,
                Brushes.Cyan,
                1.0
            );
            drawingContext.DrawText(startText, new Point(220, 550));

            // Мигающий эффект для текста начала
            var blink = (DateTime.Now.Second % 2 == 0);
            if (blink)
            {
                var blinkRect = new Rect(215, 545, startText.Width + 10, startText.Height + 10);
                drawingContext.DrawRectangle(
                    Brushes.Transparent,
                    new Pen(Brushes.Cyan, 2),
                    blinkRect
                );
            }
        }
    }
}