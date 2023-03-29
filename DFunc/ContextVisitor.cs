using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using DFunc.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFunc {
    internal class ContextVisitor : dFuncBaseVisitor<DType> {

        // TODO: type coercion, equivalence, etc. Ints don't exist in this system.

        public List<Scope> ScopeStack;
        public Scope GlobalScope => ScopeStack[0];
        public Scope CurrentScope => ScopeStack[^1];

        public ContextVisitor(Scope globalScope) {
            ScopeStack = new() { globalScope };
        }

        public Symbol Identify(string id) {
            var idx = ScopeStack.Count - 1;
            Symbol? symbol = null;

            while (idx >= 0 && symbol == null) {
                symbol = ScopeStack[idx].SymbolTable.GetValueOrDefault(id);
                idx--;
            }

            if (symbol == null) {
                throw new SymbolNotFoundException(id);
            }

            return symbol;
        }

        // ######################## Program ########################

        //public override DType VisitParse([NotNull] dFuncParser.ParseContext context) { return VisitChildren(context); }
        //public override DType VisitImportBlock([NotNull] dFuncParser.ImportBlockContext context) { return VisitChildren(context); }
        //public override DType VisitImportStatement([NotNull] dFuncParser.ImportStatementContext context) { return VisitChildren(context); }

        // ######################## Functions ########################

        public override DType VisitIdentifierFunctionCall([NotNull] dFuncParser.IdentifierFunctionCallContext context) {
            var id = context.Identifier().GetText();

            // check if id type is already resolved
            var sym = Identify(id);
            if (sym.Type is NoneType) {
                Visit(sym.Context!);
            }

            // type is now populated, check argument types
            var args = new List<DType>();
            if (context.exprList() != null) {
                foreach (var expr in context.exprList().expression()) {
                    args.Add(Visit(expr));
                }
            }

            var paramTypes = (from p in ((FunctionType)sym.Type).Inputs
                             let type = p.Type
                             select type).ToList();

            if (paramTypes.Count != args.Count) {
                throw new ArgumentException(context.Start);
            }

            for (int i = 0; i < paramTypes.Count; i++) {
                var paramType = paramTypes[i];
                var arg = args[i];
                if (paramType.GetType() != arg.GetType()) {
                    throw new ArgumentException(context.Start);
                }
            }

            // return
            return ((FunctionType)sym.Type).OutputType;
        }
        public override DType VisitFunctionDecl([NotNull] dFuncParser.FunctionDeclContext context) {
            var id = context.Identifier().GetText();
            var sym = Identify(id);
            if (sym.Type is not NoneType) {
                // this decl has already been visited, return early
                return new NoneType();
            }

            // add new scope and add params to scope
            ScopeStack.Add(new());

            // if any params
            if (context.paramList() != null) {
                foreach (var p in (context.paramList().param())) {
                    Visit(p);
                }
            }

            // any ids in scope now were defined within the function
            var ps = from s in CurrentScope.SymbolTable.Values
                     select new FunctionType.Parameter {
                         Id = s.Name,
                         Type = s.Type
                     };

            var f = new FunctionType(ps.ToList(), Visit(context.type()));
            sym.Type = f;

            // visit block
            var res = Visit(context.functionBlock());
            if (res.GetType() != f.OutputType.GetType()) {
                throw new TypeMismatchException(context.Start);
            }

            // pop scope off stack
            ScopeStack.Remove(ScopeStack[^1]);

            return new NoneType();
        }
        //public override DType VisitParamList([NotNull] dFuncParser.ParamListContext context) { return VisitChildren(context); }
        public override DType VisitParam([NotNull] dFuncParser.ParamContext context) {
            // at this point, a new scope has been created to push params onto
            var id = context.Identifier().GetText();
            if (CurrentScope.SymbolTable.ContainsKey(id)) {
                throw new IdentifierAlreadyDefinedException(context.Start);
            }

            CurrentScope.SymbolTable[id] = new(id, Visit(context.type()), SymbolKind.Parameter);
            return new NoneType();
        }
        public override DType VisitFunctionBlock([NotNull] dFuncParser.FunctionBlockContext context) {
            if (context.inlineFunctionBlock() != null) {
                return Visit(context.inlineFunctionBlock());
            }
            return Visit(context.piecewiseFunctionBlock());
        }
        public override DType VisitInlineFunctionBlock([NotNull] dFuncParser.InlineFunctionBlockContext context) {
            return Visit(context.expression());
        }
        public override DType VisitPiecewiseFunctionBlock([NotNull] dFuncParser.PiecewiseFunctionBlockContext context) {
            var branchTypes = (from b in context.piecewiseBranch()
                              select Visit(b)).ToList();
            
            if (context.elsePiecewiseBranch() != null) {
                branchTypes.Add(Visit(context.elsePiecewiseBranch()));
            }

            var compType = branchTypes[0];

            if (branchTypes.Any(t => t.GetType() != compType.GetType())) {
                throw new TypeMismatchException(context.Start);
            }

            return compType;
        }
        public override DType VisitPiecewiseBranch([NotNull] dFuncParser.PiecewiseBranchContext context) {
            var cond = Visit(context.expression()[0]);
            if (cond is not BoolType) {
                throw new TypeMismatchException(context.Start);
            }

            return Visit(context.expression()[1]);
        }
        public override DType VisitElsePiecewiseBranch([NotNull] dFuncParser.ElsePiecewiseBranchContext context) {
            return Visit(context.expression());
        }

        // ######################## Types ########################

        public override DType VisitType([NotNull] dFuncParser.TypeContext context) {
            if (context.listType() != null) {
                return Visit(context.listType());
            }

            if (context.functionType() != null) {
                return Visit(context.functionType());
            }

            switch (context.PrimitiveType().GetText()) {
                case "bool":
                    return new BoolType();
                case "string":
                    return new StringType();
                case "int":
                    return new IntType();
                case "real":
                    return new RealType();
            }

            throw new NotImplementedException();
        }
        public override DType VisitListType([NotNull] dFuncParser.ListTypeContext context) {
            return new ListType(Visit(context.type()));
        }
        public override DType VisitFunctionType([NotNull] dFuncParser.FunctionTypeContext context) {
            // (tn1, tn2, ...) -> tm
            var outputType = Visit(context.type());
            
            var inputTypes = new List<DType>();
            if (context.typeList() != null) {
                foreach (var type in context.typeList().type()) {
                    inputTypes.Add(Visit(type));
                }
            }

            var ps = from type in inputTypes
                     select new FunctionType.Parameter {
                         Id = "",
                         Type = type
                     };

            var f = new FunctionType(ps.ToList(), outputType);
            return f;
        }
        //public override DType VisitTypeList([NotNull] dFuncParser.TypeListContext context) {
        //    return base.VisitTypeList(context);
        //}

        // ######################## Expressions ########################

        //public override DType VisitExprList([NotNull] dFuncParser.ExprListContext context) { return VisitChildren(context); }
        public override DType VisitBoolExpression([NotNull] dFuncParser.BoolExpressionContext context) {
            return new BoolType();
        }
        public override DType VisitNumberExpression([NotNull] dFuncParser.NumberExpressionContext context) {
            return new RealType();
        }
        public override DType VisitIdentifierExpression([NotNull] dFuncParser.IdentifierExpressionContext context) {
            // necessarily id is a parameter
            var id = context.Identifier().GetText();
            return Identify(id).Type;
        }
        public override DType VisitNotExpression([NotNull] dFuncParser.NotExpressionContext context) {
            var expr = context.expression();
            if (Visit(expr) is not BoolType) {
                throw new TypeMismatchException(expr.Start);
            }
            return new BoolType();
        }
        public override DType VisitOrExpression([NotNull] dFuncParser.OrExpressionContext context) {
            foreach (var expr in context.expression()) {
                if (Visit(expr) is not BoolType) {
                    throw new TypeMismatchException(expr.Start);
                }
            }
            return new BoolType();
        }
        public override DType VisitUnaryMinusExpression([NotNull] dFuncParser.UnaryMinusExpressionContext context) {
            var expr = context.expression();
            if (Visit(expr) is not RealType) {
                throw new TypeMismatchException(expr.Start);
            }
            return new RealType();
        }
        public override DType VisitPowerExpression([NotNull] dFuncParser.PowerExpressionContext context) {
            foreach (var expr in context.expression()) {
                if (Visit(expr) is not RealType) {
                    throw new TypeMismatchException(expr.Start);
                }
            }
            return new RealType();
        }
        public override DType VisitEqExpression([NotNull] dFuncParser.EqExpressionContext context) {
            var types = new List<DType>();
            foreach (var expr in context.expression()) {
                types.Add(Visit(expr));
            }

            if (types[0].GetType() != types[1].GetType()) {
                throw new TypeMismatchException(context.expression()[1].Start);
            }

            // todo: nested list values can bypass this ^^

            return new BoolType();

        }
        public override DType VisitAndExpression([NotNull] dFuncParser.AndExpressionContext context) {
            foreach (var expr in context.expression()) {
                if (Visit(expr) is not BoolType) {
                    throw new TypeMismatchException(expr.Start);
                }
            }
            return new BoolType();
        }
        public override DType VisitStringExpression([NotNull] dFuncParser.StringExpressionContext context) {
            return new StringType();
        }
        public override DType VisitExpressionExpression([NotNull] dFuncParser.ExpressionExpressionContext context) {
            return Visit(context.expression());
        }
        public override DType VisitAddExpression([NotNull] dFuncParser.AddExpressionContext context) {
            foreach (var expr in context.expression()) {
                if (Visit(expr) is not RealType) {
                    throw new TypeMismatchException(expr.Start);
                }
            }
            return new RealType();
        }
        public override DType VisitCompExpression([NotNull] dFuncParser.CompExpressionContext context) {
            foreach (var expr in context.expression()) {
                if (Visit(expr) is not RealType) {
                    throw new TypeMismatchException(expr.Start);
                }
            }
            return new BoolType();
        }
        public override DType VisitFunctionCallExpression([NotNull] dFuncParser.FunctionCallExpressionContext context) {
            return Visit(context.functionCall());
        }
        public override DType VisitMultExpression([NotNull] dFuncParser.MultExpressionContext context) {
            foreach (var expr in context.expression()) {
                if (Visit(expr) is not RealType) {
                    throw new TypeMismatchException(expr.Start);
                }
            }
            return new RealType();
        }
        public override DType VisitConcatExpression([NotNull] dFuncParser.ConcatExpressionContext context) {
            var left = Visit(context.expression()[0]);
            var right = Visit(context.expression()[1]);

            if (left is not ListType || right is not ListType) {
                throw new TypeMismatchException(context.Start);
            }

            if (((ListType)left).InternalType is NoneType) {
                if (((ListType)right).InternalType is NoneType) {
                    return new NoneType();
                }
                return new ListType(((ListType)right).InternalType);
            }
            if (((ListType)right).InternalType is NoneType) {
                return new ListType(((ListType)left).InternalType);
            }

            if (((ListType)left).InternalType.GetType() != ((ListType)right).InternalType.GetType()) {
                throw new TypeMismatchException(context.Start);
            }

            return new ListType(((ListType)left).InternalType);
        }
        public override DType VisitListExpression([NotNull] dFuncParser.ListExpressionContext context) {
            return Visit(context.list());
        }
        public override DType VisitTernaryExpression([NotNull] dFuncParser.TernaryExpressionContext context) {
            // if x then y else z; where x is condition, and type(x) == type(y)

            var cond = Visit(context.expression()[0]);
            var x1 = Visit(context.expression()[1]);
            var x2 = Visit(context.expression()[2]);

            if (cond is not BoolType) {
                throw new TypeMismatchException(context.Start);
            }

            if (x1.GetType() != x2.GetType()) {
                throw new TypeMismatchException(context.Start);
            }

            return x1;
        }

        // ######################## Else? ########################

        public override DType VisitList([NotNull] dFuncParser.ListContext context) {
            if (context.exprList() == null) {
                return new ListType(new NoneType());        // maybe change NoneType to AnyType to be less confusing...
            }

            var t1 = Visit(context.exprList().expression()[0]);
            foreach (var e in context.exprList().expression()) {
                if (Visit(e).GetType() != t1.GetType()) {
                    throw new TypeMismatchException(context.Start);
                }
            }

            return new ListType(t1);
        }
    }
}
