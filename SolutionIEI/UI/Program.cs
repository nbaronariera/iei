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

            JSONParser Jsonparser = new JSONParser();
            CSVParser Csvparser = new CSVParser();
            GALParser Galparser = new GALParser();
            Csvparser.Load("W:\\iei\\SolutionIEI\\UI\\Fuentes\\Estacions_ITV.csv");

            var list = Csvparser.ParseList();
            var json = Jsonparser.toJSON(list);

            File.WriteAllText("W:\\iei\\SolutionIEI\\UI\\obj\\test.json", json, System.Text.Encoding.UTF8);
            Galparser.Load("W:\\iei\\SolutionIEI\\UI\\obj\\test.json");

            var lists = Galparser.ParseList();

            foreach (var e in lists){
                System.Console.WriteLine(e.ToString());
            }
        }
    }
}
