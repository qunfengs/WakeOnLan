using System;
using System.Windows.Forms;
using WakeOnLan.Forms;

namespace WakeOnLan;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
