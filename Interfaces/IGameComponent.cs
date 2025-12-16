namespace pacman3.Interfaces
{
    internal interface IGameComponent
    {
        void Initialize(); // Инициализация компонента
        void Update(TimeSpan gameTime); // Обновление состояния компонента
    }
}
