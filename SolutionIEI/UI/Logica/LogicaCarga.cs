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
            var cvExtractor = new CVExtractor();
            return cvExtractor.LoadData();
        }

        public string ObtenerCat()
        {
            var catExtractor = new CATExtractor();
            return catExtractor.LoadData();
        }

        public string ObtenerGal()
        {
            var galExtractor = new GALExtractor();
            return galExtractor.LoadData();
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