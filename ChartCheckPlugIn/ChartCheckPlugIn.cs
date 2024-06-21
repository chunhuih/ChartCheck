using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows;
using System.Reflection;

namespace VMS.TPS
{
    public class Script
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context)
        {
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.CreateNoWindow = false;
            string dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            process.StartInfo.FileName = Path.Combine(dllDirectory, "ChartCheck.exe");
            MessageBox.Show(process.StartInfo.FileName);
            if (File.Exists(process.StartInfo.FileName))
            {
                process.Start();
                process.WaitForExit();
            }
            else
            {
                MessageBox.Show("The executable does not exist.");
            }
        }
    }
}