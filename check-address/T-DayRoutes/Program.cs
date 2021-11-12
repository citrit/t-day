using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows.Forms;

namespace TDayRoutes
{
    class MainClass
    {

        public static void Main(string[] args)
        {
            //Application.();
            T_DayWindow win = new T_DayWindow();
            win.Show();
            Application.Run();
        }
    }
}
