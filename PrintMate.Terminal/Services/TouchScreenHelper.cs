using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.Services
{
    public static class TouchScreenHelper
    {
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_DIGITIZER = 94;
        private const int NID_READY = 0x80;

        public static bool IsTouchScreenAvailable()
        {
            int digitizer = GetSystemMetrics(SM_DIGITIZER);
            return (digitizer & NID_READY) != 0;
        }
    }
}
