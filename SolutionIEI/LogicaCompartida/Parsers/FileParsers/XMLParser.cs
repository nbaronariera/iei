using System.Xml.Serialization;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    /// <summary>
    /// Implementación específica del parser para manejar formatos XML.
    /// Hereda de la base <see cref="Parser{XMLData}"/>.
    /// </summary>
    internal class XMLParser : Parser<XMLData>
    {
        /// <summary>
        /// Realiza la deserialización del archivo XML y extrae la lista de datos.
        /// </summary>
        /// <returns>Una lista de objetos <see cref="XMLData"/> extraídos del nodo Wrapper.</returns>
        /// <exception cref="InvalidCastException">Se lanza si el objeto deserializado no coincide con XMLResponse.</exception>
        protected override List<XMLData> ExecuteParse()
        {
            // Inicializa el serializador basado en la estructura de respuesta esperada
            XmlSerializer serializer = new(typeof(XMLResponse));

            // Deserializa el flujo de datos (file heredado de la clase base)
            var v = serializer.Deserialize(file!);

            // Casting y navegación por el árbol de objetos: XMLResponse -> Wrapper -> Rows
            // Nota: Se asume que 'v' no es nulo y el cast es seguro.
            return (v as XMLResponse).Wrapper.Rows;
        }
    }
}
