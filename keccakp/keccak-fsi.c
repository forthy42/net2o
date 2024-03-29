/* ----------------------------------------------------------------------------
 * This file was automatically generated by SWIG (http://www.swig.org).
 * Version 2.0.1
 * 
 * This file is not intended to be easily readable and contains a number of 
 * coding conventions designed to improve portability and efficiency. Do not make
 * changes to this file unless you know what you are doing--modify the SWIG 
 * interface file instead. 
 * ----------------------------------------------------------------------------- */
#include <stdio.h>
#include <string.h>
#include <stddef.h>



#include <KeccakF-1600.h>

typedef enum{ NONE = -1, GFORTH = 0, SWIFTFORTH, VFX } SwigTargetSystem;
SwigTargetSystem swigTargetSystem = NONE;

unsigned char swigPrintStackComments = 1;

/* comments */
void swigNewline()
{
	printf( "\n" );
}

void swigComment( char *comment )
{
	printf( "\\ %s", comment );
}

/* constants */
void swigIntConstant( long constant, char *name )
{
	printf( "%ld\tconstant %s\n", constant, name );
}

void swigFloatConstant( double constant, char *name )
{
	char buffer[128];
	sprintf( buffer, "%f", constant );

	/* if the constant contains no exponent, add one */
	char *s;
	for( s = buffer; *s != 0; s++ )
		if( *s == 'e' || *s == 'E' )
			break;

	/* exponent found */
	if( *s != 0 )
		printf( "%s\tfconstant %s\n", buffer, name );
	/* not found */
	else
		printf( "%se0\tfconstant %s\n", buffer, name );
}

void swigStringConstant( char* constant, char *name )
{
	char c;
	printf( ": %s s\\\" ", name );
	while( c = *constant++ )
	{
		switch(c)
		{
			case '\b': printf( "\\b" ); break;
			case '\n': printf( "\\n" ); break;
			case '\f': printf( "\\f" ); break;
			case '\r': printf( "\\r" ); break;
			case '\t': printf( "\\t" ); break;
			case '"' : printf( "\\q" ); break;
			default:
				if(c < 0x20)
					printf("\\x%02x", c);
				else
					printf("%c", c); break;
		}
	}
	printf(	"\" ;\n" );
}

/* structs */
void swigStructField( char *name, size_t offset, size_t size )
{
	printf( "\tdrop %d %d +field %s\n", offset, size, name );
}

/* functions */
void swigFunction( char* gforth, char *swiftForth, char *vfx, char *stackComment )
{
	if( swigTargetSystem == GFORTH )
		printf( gforth );
	else if( swigTargetSystem == SWIFTFORTH )
		printf( swiftForth );
	else if( swigTargetSystem == VFX )
		printf( vfx );

	if( swigPrintStackComments )
		printf( stackComment );

	printf( "\n" );
}

/* function pointers */
void swigFunctionPointer( char* gforth, char *swiftForth, char *vfx, char *stackComment )
{
	swigFunction( gforth, swiftForth, vfx, stackComment );
}

/* callbacks */
void swigCallback( char* gforth, char *swiftForth, char *vfx, char *stackComment )
{
	swigFunction( gforth, swiftForth, vfx, stackComment );
}

void swigUsage( char **argv )
{
	fprintf( stderr, "Usage: %s [-gforth|-swiftforth|-vfx]\n", argv[0] );
}

int main( int argc, char **argv )
{
	int i;

	/* check arguments */
	for( i = 1; i < argc; i++ )
		if( strcmp( argv[i], "-gforth" ) == 0 )
			swigTargetSystem = GFORTH;
		else if( strcmp( argv[i], "-swiftforth" ) == 0 )
			swigTargetSystem = SWIFTFORTH;
		else if( strcmp( argv[i], "-vfx" ) == 0 )
			swigTargetSystem = VFX;

	if( swigTargetSystem == NONE )
	{
		fprintf( stderr, "Error: no target system specified\n" );
		swigUsage( argv );
		return 1;
	}

	/* primer */
	printf( "\\ This file has been generated using SWIG and fsi,\n"
		"\\ and is already platform dependent, search for the corresponding\n"
		"\\ fsi-file to compile it where no one has compiled it before ;)\n"
		"\\ GForth has its own dynamic loader and doesn't need addional C-Code.\n"
		"\\ That's why this file contains normal Gforth-code( version 0.6.9 or higher )\n"
		"\\ and could be used directly with include or require.\n"
		"\\ As all comments are stripped during the compilation, please\n"
		"\\ insert the copyright notice of the original file here.\n"
	);



	swigNewline();

	swigComment("------===< functions >===-------\n");
	swigFunction( "c-function KeccakInitialize KeccakInitialize  -- void", "FUNCTION: KeccakInitialize", "EXTERN: void C KeccakInitialize(  );", "" );
	swigFunction( "c-function KeccakF KeccakF a -- void", "FUNCTION: KeccakF", "EXTERN: void C KeccakF( a );", "" );
	swigFunction( "c-function KeccakInitializeState KeccakInitializeState a -- void", "FUNCTION: KeccakInitializeState", "EXTERN: void C KeccakInitializeState( a );", "" );
	swigFunction( "c-function KeccakExtract KeccakExtract a a n -- void", "FUNCTION: KeccakExtract", "EXTERN: void C KeccakExtract( a a n );", "" );
	swigFunction( "c-function KeccakAbsorb KeccakAbsorb a a n -- void", "FUNCTION: KeccakAbsorb", "EXTERN: void C KeccakAbsorb( a a n );", "" );
	swigFunction( "c-function KeccakEncrypt KeccakEncrypt a a n -- void", "FUNCTION: KeccakEncrypt", "EXTERN: void C KeccakEncrypt( a a n );", "" );
	swigFunction( "c-function KeccakDecrypt KeccakDecrypt a a n -- void", "FUNCTION: KeccakDecrypt", "EXTERN: void C KeccakDecrypt( a a n );", "" );
	
	return 0;
} /* end of main */



