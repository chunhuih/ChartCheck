using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Configuration;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using ChartCheck.Core;

// TODO: Replace the following version attributes by creating AssemblyInfo.cs. You can do this in the properties of the Visual Studio project.
// [assembly: AssemblyInformationalVersion("1.0")]

// TODO: Uncomment the following line if the script requires write access.
// [assembly: ESAPIScript(IsWriteable = true)]
namespace ChartCheck
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //string logName = System.AppDomain.CurrentDomain.FriendlyName + ".log";
            //using (StreamWriter w = File.CreateText(logName))
            //{
            //    w.AutoFlush = true;
            //    string log = "Start of the app: " + System.AppDomain.CurrentDomain.FriendlyName;
            //    Log(log, w);
            //}
            Console.SetWindowSize(120, 60);
            Application app = null;
            try
            {
                app = Application.CreateApplication();
            }
            catch (Exception e)
            {
                //Console.Error.WriteLine($"Exception: {e}");
                Console.Error.WriteLine($"Unable to establish connection to Eclipse. Please ensure that you are running this app in an Eclipse environment.");
            }
            if (args.Length > 0)
            {
                Execute(app, args[0]);
            }
            else
            {
                Execute(app, String.Empty);
            }
            //using (StreamWriter w = File.AppendText(logName))
            //{
            //    w.AutoFlush = true;
            //    string log = "End of the app: " + System.AppDomain.CurrentDomain.FriendlyName;
            //    Log(log, w);
            //}
        }
        //public static void Log(string logMessage, TextWriter w)
        //{
        //    // The $ symbol before the quotation mark creates an interpolated string.
        //    w.Write($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
        //    w.WriteLine($": {logMessage}");
        //}
        public static void WriteInColor(string s, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.Write(s);
            Console.ResetColor();
        }
        static void Execute(Application app, string arg)
        {
            // string logName = System.AppDomain.CurrentDomain.FriendlyName + ".log";
            // Console.Clear();
            WriteInColor("=========================================================================\n", ConsoleColor.Gray);
            WriteInColor("A chart check application by: Chunhui Han.\n", ConsoleColor.Gray);
            WriteInColor("=========================================================================\n", ConsoleColor.Gray);

            string automationPredicate = ConfigurationManager.AppSettings.Get("Automation");
            string mrn = string.Empty;
            if (arg == string.Empty && automationPredicate != null && automationPredicate.ToLower() == "t")
            {
                mrn = ConfigurationManager.AppSettings.Get("MRN");
                Console.WriteLine($"MRN was read from the configuration file: {mrn}");
                CheckWorker.CheckThisPatient(mrn, app);
            }
            else
            {
                if(arg != string.Empty)
                {
                    mrn = arg;
                }
                while (true)
                {
                    if (mrn != string.Empty)
                    {
                        CheckWorker.CheckThisPatient(mrn, app);
                        app.ClosePatient();
                    }
                    WriteInColor("Please enter the patient ID (Press RETURN to exit): ", ConsoleColor.Cyan);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    mrn = Console.ReadLine();
                    Console.ResetColor();
                    if (mrn == "")
                        return;
                }
            }
        }
    }
}
