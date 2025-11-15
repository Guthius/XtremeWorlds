using Client.Net;
using Core.Globals;
using Eto.Drawing;
using Eto.Forms;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace Client;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        General.Client.Run();
    }
}