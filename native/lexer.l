%option noyywrap
%option yylineno
%option outfile="lexer.c"

%{
#define YY_NO_UNISTD_H
#ifdef _MSC_VER
  #include <io.h>
  #define isatty _isatty
  #define fileno _fileno
#endif
#include <stdio.h>

static int yycol = 1;

static void print_csv(const char* type, const char* txt, int line, int col) {
    fputs(type, stdout);
    fputc(',', stdout);
    fputc('"', stdout);
    for (const char* p = txt; *p; ++p) {
        if (*p == '"') fputc('"', stdout);
        fputc(*p, stdout);
    }
    fputc('"', stdout);
    fprintf(stdout, ",%d,%d\n", line, col);
}
#define EMIT(t) do { print_csv(t, yytext, yylineno, yycol); yycol += yyleng; } while(0)
%}

WS      [ \t\r]+
NL      \n+
ID      [A-Za-z_][A-Za-z0-9_]*
NUM     [0-9]+(\.[0-9]+)?
STR     \"([^\"\\]|\\.)*\"
%x CMT

%%
{WS}            { yycol += yyleng; }
{NL}            { yycol = 1; ++yylineno; }

"//".*          { EMIT("COMMENT"); }
"/*"            { BEGIN(CMT); }
<CMT>[^*\n]+    ;
<CMT>\n         { ++yylineno; yycol = 1; }
<CMT>\*+[^*/]   ;
<CMT>"*/"       { BEGIN(INITIAL); }

"if"            { EMIT("KW_IF"); }
"else"          { EMIT("KW_ELSE"); }
"while"         { EMIT("KW_WHILE"); }
"return"        { EMIT("KW_RETURN"); }
"int"           { EMIT("KW_INT"); }
"float"         { EMIT("KW_FLOAT"); }
"string"        { EMIT("KW_STRING"); }

"=="            { EMIT("EQ"); }
"!="            { EMIT("NEQ"); }
"<="            { EMIT("LE"); }
">="            { EMIT("GE"); }
"+"             { EMIT("PLUS"); }
"-"             { EMIT("MINUS"); }
"*"             { EMIT("STAR"); }
"/"             { EMIT("SLASH"); }
"="             { EMIT("ASSIGN"); }
"<"             { EMIT("LT"); }
">"             { EMIT("GT"); }
"("             { EMIT("LPAREN"); }
")"             { EMIT("RPAREN"); }
"{"             { EMIT("LBRACE"); }
"}"             { EMIT("RBRACE"); }
";"             { EMIT("SEMI"); }
","             { EMIT("COMMA"); }

{STR}           { EMIT("STRING"); }
{NUM}           { EMIT("NUMBER"); }
{ID}            { EMIT("IDENTIFIER"); }
.               { EMIT("UNKNOWN"); }
%%

int main(void){
    printf("Tipo,Lexema,Linea,Columna\n");
    yylineno = 1; yycol = 1;
    yylex();
    return 0;
}
