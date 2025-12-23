using FastExpressionCompiler.LightExpression;
using Opc2Lib;
using PrintMate.Terminal.ConfigurationSystem.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.AppConfiguration
{
    /// <summary>
    /// Настройки автоматического процесса
    /// </summary>
    public class AutomaticProcessSettings : ConfigurationModelBase
    {
        //// 1. Готовность газовой системы
        public bool ReadyGasSystemCheck = true;

        //// 2. Готовность лазерной системы
        public bool ReadyLaserSystemCheck = true;

        //// 3. Готовность нагревателя
        public bool ReadyHeatingTableCheck = true;

        //// 4. Отсутствие ошибок блокировки
        public bool NotErrorsBlockingPrintingCheck = true;

        //// 5. Ошибка реферирования рекоутера
        public bool RecoaterRefErrorCheck = true;

        //// 6. Ошибка привода рекоутера
        public bool RecoaterEngineErrorCheck = true;

        //// 7. Ошибка привода дозатора
        public bool DoserAxesErrorCheck = true;

        //// 8. Ошибка привода платформы
        public bool PlatformAxesErrorCheck = true;
    }
}
