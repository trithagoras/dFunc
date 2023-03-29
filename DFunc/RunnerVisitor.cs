using Antlr4.Runtime.Misc;
using DFunc.Base;
using DFunc.Exts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DFunc {
    internal class RunnerVisitor : dFuncBaseVisitor<object> {
        public List<Scope> ScopeStack;
        public Scope GlobalScope => ScopeStack[0];
        public Scope CurrentScope => ScopeStack[^1];

        public RunnerVisitor(Scope globalScope) {
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

        //public override object VisitParse([NotNull] dFuncParser.ParseContext context) { return VisitChildren(context); }
        //public override object VisitImportBlock([NotNull] dFuncParser.ImportBlockContext context) { return VisitChildren(context); }
        //public override object VisitImportStatement([NotNull] dFuncParser.ImportStatementContext context) { return VisitChildren(context); }

        // ######################## Functions ########################

        public override object VisitIdentifierFunctionCall([NotNull] dFuncParser.IdentifierFunctionCallContext context) {
            // get function from symbol table
            var id = context.Identifier().GetText();
            var sym = Identify(id);
            var f = (FunctionType)sym.Type;
            if (sym.Context == null) {
                // function-typed parameter. e.g. f: (real) -> real; f(5);
                throw new NullReferenceException();
            }
            var ctx = (dFuncParser.FunctionDeclContext)sym.Context;

            var args = new List<object>();
            if (context.exprList() != null) {
                foreach (var expr in context.exprList().expression()) {
                    var arg = Visit(expr);

                    if (arg == null) {
                        // function identifier?
                        if (expr is dFuncParser.IdentifierExpressionContext ct) {
                            var iid = ct.Identifier().GetText();
                            arg = Identify(iid).Type;

                        }
                    }

                    args.Add(arg);

                }
            }

            // create new scope
            ScopeStack.Add(new());

            // visit params to populate scope
            if (ctx.paramList() != null) {
                Visit(ctx.paramList());
            }

            // set param values to arg values
            
            var syms = (from s in CurrentScope.SymbolTable.Values
                       select s).ToList();
            for (var i = 0; i < args.Count; i++) {
                var s = syms[i];
                var arg = args[i];

                if (arg is dFuncParser.FunctionDeclContext a) {
                    // function identifier arg
                    //s.Context = a;
                    //s.Value = Visit(s.Context); // definitely makes no sense. No args are supplied?
                } else {
                    s.Value = arg;
                }
                
            }

            object retVal = null;

            // visit function expressionBlock
            if (id == "head") {
                // handling stdlib functions    todo: better way of doing this
                retVal = ((List<object>)args[0])[0];
            } else if (id == "tail") {
                // handling stdlib functions    todo: better way of doing this
                retVal = (List<object>)args[0];
                if (((List<object>)retVal).Count > 0) {
                    var l = new List<object>((List<object>)retVal);
                    l.RemoveAt(0);
                    retVal = l;
                }
            } else {
                retVal = Visit(ctx.functionBlock());
            }
            

            // pop scope
            ScopeStack.Remove(ScopeStack[^1]);

            return retVal;
        }
        public override object VisitFunctionDecl([NotNull] dFuncParser.FunctionDeclContext context) {
            // this MUST be the main function. No other functionDecl is visited in this runner.
            // meaning, no args and no scope.
            return Visit(context.functionBlock());
        }
        public override object VisitParamList([NotNull] dFuncParser.ParamListContext context) {
            foreach (var param in context.param()) {
                Visit(param);
            }
            return null;
        }
        public override object VisitParam([NotNull] dFuncParser.ParamContext context) {
            var id = context.Identifier().GetText();
            var t = (DType)Visit(context.type());

            CurrentScope.SymbolTable[id] = new(id, t, SymbolKind.Parameter);
            return null;
        }
        public override object VisitFunctionBlock([NotNull] dFuncParser.FunctionBlockContext context) {
            if (context.inlineFunctionBlock() != null) {
                return Visit(context.inlineFunctionBlock());
            }
            return Visit(context.piecewiseFunctionBlock());
        }
        public override object VisitInlineFunctionBlock([NotNull] dFuncParser.InlineFunctionBlockContext context) {
            return Visit(context.expression());
        }
        public override object VisitPiecewiseFunctionBlock([NotNull] dFuncParser.PiecewiseFunctionBlockContext context) {
            foreach (var branch in context.piecewiseBranch()) {
                var res = Visit(branch);
                if (res != null) {
                    return res;
                }
            }

            return Visit(context.elsePiecewiseBranch());
        }
        public override object VisitPiecewiseBranch([NotNull] dFuncParser.PiecewiseBranchContext context) {
            if ((bool)Visit(context.expression(0))) {
                return Visit(context.expression(1));
            }
            return null;
        }
        public override object VisitElsePiecewiseBranch([NotNull] dFuncParser.ElsePiecewiseBranchContext context) {
            return Visit(context.expression());
        }

        // ######################## Types ########################

        public override object VisitType([NotNull] dFuncParser.TypeContext context) {
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
        public override object VisitListType([NotNull] dFuncParser.ListTypeContext context) {
            var t = (DType)Visit(context.type());
            return new ListType(t);
        }
        public override object VisitFunctionType([NotNull] dFuncParser.FunctionTypeContext context) {
            var outputType = (DType)Visit(context.type());

            var inputTypes = new List<DType>();
            if (context.typeList() != null) {
                foreach (var type in context.typeList().type()) {
                    inputTypes.Add((DType)Visit(type));
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

        // ######################## Expressions ########################

        //public override object VisitExprList([NotNull] dFuncParser.ExprListContext context) { return VisitChildren(context); }
        public override object VisitBoolExpression([NotNull] dFuncParser.BoolExpressionContext context) {
            return context.Bool().GetText() == "true";
        }
        public override object VisitNumberExpression([NotNull] dFuncParser.NumberExpressionContext context) {
            var s = context.Number().GetText();
            return double.Parse(s);
        }
        public override object VisitIdentifierExpression([NotNull] dFuncParser.IdentifierExpressionContext context) {
            var id = context.Identifier().GetText();
            var sym = Identify(id);
            
            if (sym.Value != null) {
                return sym.Value;
            }

            // if here, the symbol is probably a function kind? Return this symbol's context?
            return sym.Context;
        }
        public override object VisitNotExpression([NotNull] dFuncParser.NotExpressionContext context) {
            var expr = (bool)Visit(context.expression());
            return !expr;
        }
        public override object VisitOrExpression([NotNull] dFuncParser.OrExpressionContext context) {
            var left = (bool)Visit(context.expression(0));
            var right = (bool)Visit(context.expression(1));
            return left || right;
        }
        public override object VisitUnaryMinusExpression([NotNull] dFuncParser.UnaryMinusExpressionContext context) {
            return -(double)Visit(context.expression());
        }
        public override object VisitPowerExpression([NotNull] dFuncParser.PowerExpressionContext context) {
            var x1 = (double)Visit(context.expression()[0]);
            var x2 = (double)Visit(context.expression()[1]);

            return Math.Pow(x1, x2);
        }
        public override object VisitEqExpression([NotNull] dFuncParser.EqExpressionContext context) {
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));

            if (left.GetType() != right.GetType()) {
                return false;
            }

            if (left.GetType() == typeof(bool)) {
                return (bool)left == (bool)right;
            } else if (left.GetType() == typeof(double)) {
                return (double)left == (double)right;
            } else if (left.GetType() == typeof(string)) {
                return (string)left == (string)right;
            }

            if (left.GetType() == typeof(List<object>)) {
                return ((List<object>)left).EqualLists((List<object>)right);
            }

            throw new NotImplementedException();
        }
        public override object VisitAndExpression([NotNull] dFuncParser.AndExpressionContext context) {
            var left = (bool)Visit(context.expression(0));
            var right = (bool)Visit(context.expression(1));
            return left && right;
        }
        public override object VisitStringExpression([NotNull] dFuncParser.StringExpressionContext context) {
            return context.String().GetText();
        }
        public override object VisitExpressionExpression([NotNull] dFuncParser.ExpressionExpressionContext context) {
            return Visit(context.expression());
        }
        public override object VisitAddExpression([NotNull] dFuncParser.AddExpressionContext context) {
            var left = (double)Visit(context.expression(0));
            var right = (double)Visit(context.expression(1));

            if (context.Add() != null) {
                return left + right;
            } else {
                return left - right;
            }
        }
        public override object VisitCompExpression([NotNull] dFuncParser.CompExpressionContext context) {
            var left = (double)Visit(context.expression(0));
            var right = (double)Visit(context.expression(1));

            switch (context.op.Text) {
                case ">":
                    return left > right;
                case "<":
                    return left < right;
                case ">=":
                    return left >= right;
                case "<=":
                    return left <= right;
            }

            throw new Exception();
        }
        public override object VisitFunctionCallExpression([NotNull] dFuncParser.FunctionCallExpressionContext context) {
            return Visit(context.functionCall());
        }
        public override object VisitMultExpression([NotNull] dFuncParser.MultExpressionContext context) {
            var left = (double)Visit(context.expression(0));
            var right = (double)Visit(context.expression(1));
            
            if (context.Multiply() != null) {
                return left * right;
            } else if (context.Divide() != null) {
                return left / right;
            } else {
                return left % right;
            }
        }
        public override object VisitConcatExpression([NotNull] dFuncParser.ConcatExpressionContext context) {
            var left = (List<object>)Visit(context.expression()[0]);
            var right = (List<object>)Visit(context.expression()[1]);

            return left.Concat(right).ToList();
        }
        public override object VisitListExpression([NotNull] dFuncParser.ListExpressionContext context) {
            return Visit(context.list());
        }
        public override object VisitTernaryExpression([NotNull] dFuncParser.TernaryExpressionContext context) {
            var cond = (bool)Visit(context.expression()[0]);
            var x1 = Visit(context.expression()[1]);
            if (cond) {
                return x1;
            }

            var x2 = Visit(context.expression()[2]);
            return x2;
        }

        // ######################## Else? ########################

        public override object VisitList([NotNull] dFuncParser.ListContext context) {
            // all exprs are the same type here
            var list = new List<object>();
            if (context.exprList() == null) {
                return list;
            }

            foreach (var e in context.exprList().expression()) {
                list.Add(Visit(e));
            }
            return list;
        }
    }
}
