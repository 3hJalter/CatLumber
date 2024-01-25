namespace _Game.Utilities.Grid
{
    public interface ICell<T>
    {
        public void SetGridPosition(int x, int y);
        public (int, int) GetGridPosition();
        public string ToString();
        public T GetCellValue();
        public void SetCellValue(T value);
    }
}
