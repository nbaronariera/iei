﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Entidades
{
    internal class Localidad
    {
        public int codigo { get; set; }

        public String nombre { get; set; } = "";

        public Provincia Provincia { get; set; } = null!;
        public int codigoProvincia { get; set; }

        public Localidad(String nombre) {

            this.nombre = nombre;

        }

        public Localidad() { }

        public ICollection<Estacion> Estaciones { get; set; } = new List<Estacion>();
    }
}
