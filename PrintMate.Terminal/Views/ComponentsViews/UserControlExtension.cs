using System;
using System.Windows.Controls;

namespace PrintMate.Terminal.Views.ComponentsViews;

public static class UserControlExtension {
    public static void LoadModel(this UserControl control, object cd)
    {
        if (control.DataContext != null)
        {
            var method = control.DataContext.GetType().GetMethod("OnLoaded");


            if (method != null && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(object))
            {
                method.Invoke(control.DataContext, new object[] {cd});
            }
        }
        else
        {
        }
    }
}