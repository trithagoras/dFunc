using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFunc {
    internal class Symbol {
        public string Name { get; set; }
        public DType Type { get; set; }
        public SymbolKind SymbolKind { get; set; }
        public ParserRuleContext? Context { get; set; }
        public object? Value { get; set; }

        public Symbol(string name, DType type, SymbolKind symbolKind, ParserRuleContext? context = null) {
            Name = name;
            Type = type;
            SymbolKind = symbolKind;
            Context = context;
        }
    }

    internal enum SymbolKind {
        Parameter,
        Function
    }
}
