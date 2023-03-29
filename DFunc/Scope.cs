using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFunc {
    internal class Scope {
        public Dictionary<string, Symbol> SymbolTable { get; set; }

        public Scope() {
            SymbolTable = new();
        }
    }
}
