using System.Globalization;

namespace LayerAnalyzer.Lib.Services.LayerAnalyzerSystem.AnalyzeRules;

/// <summary>
/// Модель правила анализа для сериализации/десериализации
/// </summary>
[Serializable]
public class AnalyzeRuleModel
{
    /// <summary>
    /// Имя класса правила (используется как идентификатор типа правила)
    /// </summary>
    public string RuleClassName { get; }

    /// <summary>
    /// Пользовательское имя правила
    /// </summary>
    public string UserRuleName { get; set; }

    /// <summary>
    /// Параметры правила
    /// </summary>
    public List<double> Params { get; set; }

    public AnalyzeRuleModel(string ruleClassName, string userRuleName, List<double> parameters)
    {
        RuleClassName = ruleClassName;
        UserRuleName = userRuleName;
        Params = parameters;
    }
}
