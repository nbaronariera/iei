using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using UI.Parsers.ParsedObjects;

public class GALDataMap : ClassMap<GALData>
{
    public GALDataMap()
    {
        Map(m => m.NombreEstacion).Name("NOME DA ESTACIÓN");
        Map(m => m.Direccion).Name("ENDEREZO");
        Map(m => m.Municipio).Name("CONCELLO");
        Map(m => m.CodigoPostal).Name("CÓDIGO POSTAL");
        Map(m => m.Provincia).Name("PROVINCIA");
        Map(m => m.Telefono).Name("TELÉFONO");
        Map(m => m.HorarioRaw).Name("HORARIO");
        Map(m => m.UrlCita).Name("SOLICITUDE DE CITA PREVIA");
        Map(m => m.Correo).Name("CORREO ELECTRÓNICO");
        Map(m => m.Coordenadas).Name("COORDENADAS GMAPS");
    }
}