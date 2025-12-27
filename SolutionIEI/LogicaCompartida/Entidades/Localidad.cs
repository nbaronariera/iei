namespace UI.Entidades
{
    public class Localidad
    {
        public int codigo { get; set; }

        public String nombre { get; set; } = "";

        public virtual Provincia Provincia { get; set; } = null!;
        public int codigoProvincia { get; set; }

        public Localidad(String nombre)
        {

            this.nombre = nombre;

        }

        public Localidad() { }

        public virtual ICollection<Estacion> Estaciones { get; set; } = new List<Estacion>();
    }
}
