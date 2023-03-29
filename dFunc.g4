grammar dFunc;

parse
 : importBlock? functionDecl* EOF
 ;

importBlock
 : importStatement+
 ;

importStatement
 : KW_IMPORT String ';' ;

functionCall
 : Identifier '(' exprList? ')' #identifierFunctionCall
 ;

functionDecl
 : Identifier Assign '(' paramList? ')' '->' type functionBlock ';'
 ;

paramList
 : param ( ',' param )*
 ;

param
 : Identifier ':' type
 ;

functionBlock
 : inlineFunctionBlock
 | piecewiseFunctionBlock
 ;

inlineFunctionBlock
 : '=>' expression
 ;

piecewiseFunctionBlock
 : piecewiseBranch+ (elsePiecewiseBranch)?
 ;

piecewiseBranch
 : '|' expression '=>' expression
 ;

elsePiecewiseBranch
 : '|' Else '=>' expression
 ;

type
 : PrimitiveType
 | listType
 | functionType
 ;

listType
 : '[' type ']'
 ;

PrimitiveType
 : 'bool'
 | 'int'
 | 'real'
 | 'string'
 ;

functionType
 : '(' typeList? ')' '->' type
 ;

typeList
 : type (',' type)*
 ;

exprList
 : expression ( ',' expression )*
 ;

expression
 : '-' expression                                       #unaryMinusExpression
 | <assoc=right> expression '^' expression              #powerExpression
 | expression op=( '*' | '/' | '%' ) expression         #multExpression
 | expression op=( '+' | '-' ) expression               #addExpression
 | expression op=( '>=' | '<=' | '>' | '<' ) expression #compExpression
 | expression op=( '=' | '!=' ) expression              #eqExpression
 | '!' expression                                       #notExpression
 | expression 'and' expression                          #andExpression
 | expression 'or' expression                           #orExpression
 | expression '++' expression                           #concatExpression
 | 'if' expression 'then' expression 'else' expression  #ternaryExpression
 | Number                                               #numberExpression
 | Bool                                                 #boolExpression
 | functionCall                                         #functionCallExpression
 | list                                                 #listExpression
 | Identifier                                           #identifierExpression
 | String                                               #stringExpression
 | '(' expression ')'                                   #expressionExpression
 ;

list
 : '[' exprList? ']'
 ;

Else     : 'else';

Or       : 'or';
And      : 'and';
Equals   : '=';
NEquals  : '!=';
GTEquals : '>=';
LTEquals : '<=';
Pow      : '^';
Excl     : '!';
GT       : '>';
LT       : '<';
Add      : '+';
Subtract : '-';
Multiply : '*';
Divide   : '/';
Modulus  : '%';
OBrace   : '{';
CBrace   : '}';
OBracket : '[';
CBracket : ']';
OParen   : '(';
CParen   : ')';
SColon   : ';';
Assign   : ':=';
Comma    : ',';
QMark    : '?';
Colon    : ':';
Concat   : '++';

Bool
 : 'true'
 | 'false'
 ;

KW_IMPORT : 'import';

Number
 : Int ( '.' Digit* )?
 ;

Identifier
 : [a-zA-Z_] [a-zA-Z_0-9]*
 ;

String
 : ["] ( ~["\r\n\\] | '\\' ~[\r\n] )* ["]
 | ['] ( ~['\r\n\\] | '\\' ~[\r\n] )* [']
 ;
Comment
 : ( '#' ~[\r\n]* | '{#' .*? '#}' ) -> skip
 ;
Space
 : [ \t\r\n\u000C] -> skip
 ;
fragment Int
 : [1-9] Digit*
 | '0'
 ;

fragment Digit
 : [0-9]
 ;