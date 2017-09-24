grammar test;
transformlist: wsp* transforms? wsp*;
transforms:
    transform
    | transform commawsp+ transforms;
transform:
    matrix
    | translate
    | scale
    | rotate
    | skewX
    | skewY;
matrix:
    'matrix' wsp* '(' wsp*
       number commawsp
       number commawsp
       number commawsp
       number commawsp
       number commawsp
       number wsp* ')';
translate:
    'translate' wsp* '(' wsp* number ( commawsp number )? wsp* ')';
scale:
    'scale' wsp* '(' wsp* number ( commawsp number )? wsp* ')';
rotate:
    'rotate' wsp* '(' wsp* number ( commawsp number commawsp number )? wsp* ')';
skewX:
    'skewX' wsp* '(' wsp* number wsp* ')';
skewY:
    'skewY' wsp* '(' wsp* number wsp* ')';
number:
    sign? integer
    | sign? float;
commawsp:
    (wsp+ comma? wsp*) | (comma wsp*);
comma:
    ',';
integer:
    digits;
float:
    fraction exponent?
    | digits exponent;
fraction:
    digits? '.' digits
    | digits '.';
exponent:
    ( 'e' | 'E' ) sign? digits?;
sign:
    '+' | '-';
digits:
    digit
    | digit digits;
digit:
    '0' | '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9';
wsp:
    ('\u0020'|'\u0009'|'\u000D'|'\u000A');