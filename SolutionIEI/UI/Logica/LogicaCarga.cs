using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Entidades;
using UI.Parsers;
using UI.Wrappers;

namespace UI.Logica
{
    public class LogicaCarga
    {
        private readonly Persistencia.Persistencia _persistencia;

        private const string TIPO_FIJA = "Estacion_fija";
        private const string TIPO_MOVIL = "Estacion_movil";

        public LogicaCarga()
        {
            _persistencia = new Persistencia.Persistencia();
        }

        public string ObtenerCV()
        {
            string JsonCV = JSONConversor.Ejecutar();
            var cvExtractor = new CVExtractor();
            cvExtractor.Load(JsonCV);
            var resultadosCv = cvExtractor.FromParsedToUsefull(cvExtractor.ParseList());
            return resultadosCv.Item4;
        }

        public string ObtenerCat()
        {
            string JsonCAT = XMLaJSONConversor.Ejecutar();

            var catExtractor = new CATExtractor();
            catExtractor.Load(JsonCAT);
            var resultadosCat = catExtractor.FromParsedToUsefull(catExtractor.ParseList());
            return resultadosCat.Item4;
        }

        public string ObtenerGal()
        {
            string JsonGAL = CSVaJSONConversor.Ejecutar();
            var galExtractor = new GALExtractor();
            galExtractor.Load(JsonGAL);
            var resultadosGal = galExtractor.FromParsedToUsefull(galExtractor.ParseList());
            return resultadosGal.Item4;
        }

        public bool Clean()
        {
            using (var db = new AppDbContext())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }
            return true;
        }
    }
}