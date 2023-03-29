using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using DFunc.Base;
using DFunc.Exts;

namespace DFunc {
    internal class Program {
        static void Main(string[] args) {
            // loading in standard library
            var stdLibPath = @".\Examples\stdlib.df";
            var fileContent = File.ReadAllText(stdLibPath);

            var filePath = @".\Examples\example1.df";
            fileContent += File.ReadAllText(filePath);

            var lexer = new dFuncLexer(CharStreams.fromString(fileContent));
            var tokens = new CommonTokenStream(lexer);
            var parser = new dFuncParser(tokens);

            var globalScope = new Scope();

            var symbolConstructor = new SymbolConstructor(globalScope);
            var tree = parser.parse();

            // initial walk (populating global symbol table)
            var walker = new ParseTreeWalker();
            walker.Walk(symbolConstructor, tree);

            Console.WriteLine("Global symbol table created");

            // subsequent visit (context checking)
            var contextVisitor = new ContextVisitor(globalScope);
            contextVisitor.Visit(tree);

            Console.WriteLine("Semantic Checking done.");

            // final visit (runner)
            var runnerVisitor = new RunnerVisitor(globalScope);

            // get the main function
            var mainSym = globalScope.SymbolTable["main"];
            if (mainSym == null || mainSym.Context == null) {
                throw new NullReferenceException("No function named main in the program.");
            }
            var mainFunc = (dFuncParser.FunctionDeclContext)mainSym.Context;

            var ret = runnerVisitor.Visit(mainFunc);

            Console.WriteLine(ret.PrettyToString());
        }
    }
}