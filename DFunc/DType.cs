using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFunc {
    internal abstract class DType { }

    internal class BoolType : DType { }

    internal class RealType : DType { }

    internal class IntType : DType { }

    internal class StringType : DType { }

    internal class ListType : DType {
        public DType InternalType { get; set; }

        public ListType(DType InternalType) {
            this.InternalType = InternalType;
        }
    }

    internal class FunctionType : DType {

        public class Parameter {
            public string Id { get; set; }
            public DType Type { get; set; }
        }

        // note: a functiontype doesn't necessarily mean a function declaration. Params can take on the kind of a function. e.g. n: (int) -> bool, etc.
        //       hence, the 'kind' attribute of the Symbol class.
        public List<Parameter> Inputs { get; set; }
        
        public DType OutputType { get; set; }

        public FunctionType(List<Parameter> inputTypes, DType outputType) {
            Inputs = inputTypes;
            OutputType = outputType;
        }
    }

    internal class NoneType : DType {
        // assigned to things like FunctionDeclarations, which do not return a type.
    }
}
