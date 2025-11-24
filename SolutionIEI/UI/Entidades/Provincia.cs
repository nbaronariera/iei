namespace UI.Entidades
{
    public class Provincia
    {
        public int codigo { get; set; }

        public String nombre { get; set; } = "";

        public Provincia(String nombre)
        {

            this.nombre = nombre;

        }

        public Provincia() { }

        public ICollection<Localidad> Localidades { get; set; } = new List<Localidad>();
    }
}
