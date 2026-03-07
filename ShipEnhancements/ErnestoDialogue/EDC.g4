grammar EDC;

condition : (simple_condition | complex_condition) EOF ;
complex_condition
	: simple_condition BOP simple_condition
	| simple_condition BOP '(' complex_condition ')'
	| '(' complex_condition ')' BOP simple_condition
	| '(' complex_condition ')' BOP '(' complex_condition ')'
	| '(' complex_condition ')'
	;
simple_condition
	: value WS? cond WS? value 
	| '(' simple_condition ')'
	;
value
	: literal
	| setting_name
	| persistent_condition_name
	;
cond : LE | GE | EQ | LT | GT ;

setting_name : 'S:' WORD ;
persistent_condition_name : 'C:' WORD ;
literal : WORD | STRING ;

WORD : [a-zA-Z0-9_-]+ ;
STRING : '"' [^"]* '"' ;

BOP
	: '&&'
	| '||'
	;
LE : '<=' ;
GE : '>=' ;
EQ : '==' ;
LT : '<' ;
GT : '>' ;
WS : ' '+ -> skip ;
