using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PrintMate.Terminal.Services
{
    public class InputLanguageService 
    {
        public event Action<bool> LanguageChanged;

        public void NotifyLanguageChanged()
        {
            var isRussian = InputLanguageManager.Current.CurrentInputLanguage.Name.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
            LanguageChanged?.Invoke(isRussian);
        }
    }
}
