using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFunc {
    internal abstract class SemanticException : Exception {
        protected SemanticException(IToken token, string message) : base($"Semantic Error @ line {token.Line}:{token.Column}: {message}") {
        }
        protected SemanticException(string message) : base($"Semantic Error: {message}") {
        }
    }

    internal class IdentifierAlreadyDefinedException : SemanticException {
        public IdentifierAlreadyDefinedException(IToken token) : base(token, $"Identifier {token.Text} is already defined in this scope.") { }
    }

    internal class SymbolNotFoundException : SemanticException {
        public SymbolNotFoundException(string id) : base($"Identifier {id} is not declared in this scope.") { }
    }

    internal class TypeMismatchException : SemanticException {
        public TypeMismatchException(IToken token) : base(token, $"Types in expression are mismatched.") { }
    }

    internal class ArgumentException : SemanticException {
        public ArgumentException(IToken token) : base(token, "Argument length or types do not match parameters.") { }
    }
}
