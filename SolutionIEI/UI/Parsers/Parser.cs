using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UI.Parsers.ParsedObjects;

namespace UI.Parsers
{
    public abstract class Parser<T>
    {
        protected FileStream? file;

        public void Load(string path)
        { 
            file = new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        public void Unload()
        {
            if (file is not null) { file.Close(); }
        }

        public List<T> ParseList()
        {
            if (file is null) { return new List<T>(); }
            return ExecuteParse();
        }

        protected abstract List<ResultObject> FromParsedToUsefull(List<T> parsed);

        protected abstract List<T> ExecuteParse();
    }
}
