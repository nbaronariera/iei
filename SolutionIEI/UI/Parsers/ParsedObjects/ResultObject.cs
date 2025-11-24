namespace UI.Parsers.ParsedObjects
{
    /// <summary>
    ///  Objeto representando la estructura de datos final antes de meterla en la base de datos
    /// </summary>
    public struct ResultObject
    {
        public UI.Entidades.Estacion Estacion { get; set; }
        public UI.Entidades.Localidad Localidad { get; set; }
        public UI.Entidades.Provincia Provincia { get; set; }

        public override string ToString()
        {
            return
                $"Provincia: {Provincia.nombre} | " +
                $"Localidad: {Localidad.nombre} | " +
                $"Estación: {Estacion.nombre} | " +
                $"Tipo: {Estacion.tipo} | " +
                $"Dirección: {Estacion.direccion} | " +
                $"CP: {Estacion.codigoPostal} | " +
                $"Coordenadas: {Estacion.latitud}, {Estacion.longitud} | " +
                $"Horario: {Estacion.horario} | " +
                $"Contacto: {Estacion.contacto} | " +
                $"URL: {Estacion.URL}";
        }
    }
}
