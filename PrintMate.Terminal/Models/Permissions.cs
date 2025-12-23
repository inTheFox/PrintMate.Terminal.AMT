using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.Models
{
    public static class Permissions
    {
        // Доступ к разделу Управление осями
        public const string AxesMenu = "permissions.axesMenu";

        // Доступ к кнопке включения освещения
        public const string LightControl = "permissions.lightControl";
      
    }

}
