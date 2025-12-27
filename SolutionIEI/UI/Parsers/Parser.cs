using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text;

namespace UI.Parsers
{
    public abstract class Parser<T>
    {
        protected Stream? file;

        public void Load(string path)
        {
            file = new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        public void Unload()
        {
            file?.Dispose();
            file = null;
        }

        public virtual void LoadFromString(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                Debug.WriteLine("[LoadFromString] JSON vacío recibido");
                return;
            }

            Debug.WriteLine($"[LoadFromString] Cargando {jsonContent.Length} caracteres en MemoryStream");

            byte[] bytes = Encoding.UTF8.GetBytes(jsonContent);
            file = new MemoryStream(bytes);
        }

        public List<T> ParseList()
        {
            if (file is null) { return new List<T>(); }
            return ExecuteParse();
        }

        protected abstract List<T> ExecuteParse();


    }
}
