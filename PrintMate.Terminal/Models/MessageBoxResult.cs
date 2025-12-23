namespace PrintMate.Terminal.Models
{
    /// <summary>
    /// Результат работы MessageBox
    /// </summary>
    public enum MessageBoxResult
    {
        None,
        Yes,
        No,
        OK,
        Cancel
    }

    /// <summary>
    /// Тип MessageBox (определяет набор кнопок)
    /// </summary>
    public enum MessageBoxType
    {
        /// <summary>Только кнопка OK</summary>
        OK,

        /// <summary>Кнопки Да и Нет</summary>
        YesNo,

        /// <summary>Кнопки OK и Отмена</summary>
        OKCancel,

        /// <summary>Кнопки Да, Нет и Отмена</summary>
        YesNoCancel
    }

    /// <summary>
    /// Иконка MessageBox
    /// </summary>
    public enum MessageBoxIcon
    {
        None,
        Information,
        Warning,
        Error,
        Question,
        Success
    }
}
