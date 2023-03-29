using Antlr4.Runtime.Misc;
using DFunc.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFunc {
    internal class SymbolConstructor : dFuncBaseListener {
        public List<Scope> ScopeStack { get; set; }     // simulates a stack. ScopeStack[0] is the global scope.
        public Scope GlobalScope => ScopeStack[0];
        public Scope CurrentScope => ScopeStack[^1];

        public SymbolConstructor(Scope globalScope) {
            ScopeStack = new() { globalScope };
        }

        override public void EnterFunctionDecl([NotNull] dFuncParser.FunctionDeclContext context) {
            var id = context.Identifier().GetText();
            GlobalScope.SymbolTable.TryGetValue(id, out var symbol);
            if (symbol != null) {
                throw new IdentifierAlreadyDefinedException(context.Start);
            }

            symbol = new Symbol(id, new NoneType(), SymbolKind.Function, context: context);

            GlobalScope.SymbolTable[id] = symbol;
        }
    }
}
