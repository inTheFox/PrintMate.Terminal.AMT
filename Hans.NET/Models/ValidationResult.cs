namespace Hans.NET.Models
{
    /// <summary>
    /// Результат валидации конфигурации
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public string PropertyName { get; set; }

        public static ValidationResult Success()
        {
            return new ValidationResult { IsValid = true };
        }

        public static ValidationResult Failure(string propertyName, string errorMessage)
        {
            return new ValidationResult
            {
                IsValid = false,
                PropertyName = propertyName,
                ErrorMessage = errorMessage
            };
        }

        public override string ToString()
        {
            if (IsValid)
                return "Valid";
            return $"Invalid: {PropertyName} - {ErrorMessage}";
        }
    }
}
