namespace PrintMate.Terminal.Models;

public class RegisterInfo
{
    public string Command { get; set; }
    public string RussianLang { get; set; }
    public string EnglishLang { get; set; }
    public string ValueType { get; set; }
    public string Address { get; set; }

    public RegisterInfo(string cmd, string rg, string eg, string value, string address)
    {
            
    }

}