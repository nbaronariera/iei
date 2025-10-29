using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using UI.Parsers;
using UI.UI_Gestor;


namespace UI
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            JSONParser parser = new JSONParser();
            parser.Load("W:\\iei\\SolutionIEI\\UI\\estaciones.json");

            var list = parser.ParseList();

            System.Diagnostics.Debug.WriteLine(list.Count);
            foreach (var l in list)
            {
                System.Diagnostics.Debug.WriteLine(l.ToString());
            }
        }
    }
}
