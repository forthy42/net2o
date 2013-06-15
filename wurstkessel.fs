\ Wurstkessel data from www.random.org                 26jan09py

require 64bit.fs
require mini-oof2.fs

8 64s Constant state#
2 2*  Constant state#32
1     Constant state#16

User wurst'

Create wurst-o 0 ,  DOES> @ o#+ [ 0 , ] wurst' @ + ;
comp: >body @ ]] wurst' @ lit+ [[ , ;

' wurst-o to var-xt

object class
    state# +field wurst-source
    state# +field wurst-state
    state# +field nextstate
    state# 8 * +field message
end-class wurst-class

current-o

: wurst-task-init ( -- )
    [ wurst-class @ ]L allocate throw wurst' ! ;

: .16 ( u[d] -- )
    [ cell 8 = ] [IF] 0 [THEN]
    base @ >r hex <<# 16 0 DO # LOOP #> type #>> r> base ! ;

: .64b ( addr -- ) 64 bounds  DO  I 64@ .16 space  1 64s +LOOP ;

: .state  ( -- ) wurst-state  .64b ;
: .source ( -- ) wurst-source .64b ;

Create source-init
$6C5F6F6CBE627172. 64, $7164C30603661C2E. 64, $CE5009401B441346. 64, $454FA335A6E63AD2. 64,
$ABE9D0D648C15F6E. 64, $B90FD4060D7935D6. 64, $F7EDA8E2E8D6CB32. 64, $6230D90DBE8E061B. 64,

Create wurst-key \ default key
$20799FEC4B2E86C7. 64, $9F5454CDBDF51F76. 64, $EE1905FFF4B24C3D. 64, $9841F78BA1E0A3B7. 64,
$B6C33E39C326A161. 64, $FD4E8C0EAA7C4362. 64, $839E0910FFD9401A. 64, $2785F5C10D610C68. 64,

Create rnd-init \ for testing only! This is salt, should not be guessable!
$FEC967C32E46440F. 64, $3F63157E14F89982. 64, $F7364A7F8083EFFA. 64, $FC62572A44559951. 64,
$9915714DB7397949. 64, $AE4180D53650E38C. 64, $C53813781DFF0C2E. 64, $A579435502F22741. 64,

Create wurst-salt \ for testing only! This is salt, should not be guessable!
$39A157A31F7D62BC. 64, $51C3BD3BA4F4F803. 64, $21D7D0ED16A5243A. 64, $3C80195D8D80874F. 64,
$6DF5EF6205D55E03. 64, $8859C59812F47028. 64, $F7795F00874ACED7. 64, $5FBE66944DBECB7F. 64,
here
$FEC967C32E46440F. 64, $3F63157E14F89982. 64, $F7364A7F8083EFFA. 64, $FC62572A44559951. 64,
$9915714DB7397949. 64, $AE4180D53650E38C. 64, $C53813781DFF0C2E. 64, $A579435502F22741. 64,
Constant state-init

Create 'rngs \ this is essentially Wurstkessel's S-box
$EA576B15A7AFBA08. 64, $BF4888DC02131EF7. 64, $5F49A40B1DAAF5FD. 64, $7798975E5233C89D. 64, 
$A70A1BD518B3FBC6. 64, $8E31D54ECB7BCDF9. 64, $949D107029F94EAA. 64, $7B40261F6B3E0763. 64, 
$E845F90477A30AC5. 64, $6BF5CDC094B7A657. 64, $B0796C9F61F990F9. 64, $C149FABA50014BFC. 64, 
$626377228BC762EF. 64, $639BFE93094E7B84. 64, $0B61140C13513E15. 64, $ADB800460D8B7A3F. 64, 
$20D3873110880D43. 64, $144B862F4755D8EF. 64, $69C127F350ECD709. 64, $4A92511FAD31D465. 64, 
$34EB0EF8ED8230B2. 64, $477BF466E332DDB8. 64, $086B6F20DC2F1B33. 64, $E020C8012E1EBC4A. 64, 
$C4A5BEF939044AFC. 64, $C5C5B03F80FAF739. 64, $AD46EFBA6E4EEFB2. 64, $EAD04EEF21AD5CCA. 64, 
$68516345F32E582E. 64, $FEDE2067A335B1F6. 64, $96611D13172BA044. 64, $1DDBC3F36257DF96. 64, 
$4BFE75A91B582D07. 64, $82DF3A7D4205D9B4. 64, $CDC7C2C769B10163. 64, $2B9EFB3A406C1C22. 64, 
$DA732F17BB5FA819. 64, $14DA2D994B88EBFB. 64, $E9E8DA371866818E. 64, $6AAFEAAB80D72758. 64, 
$E2453ACEA74719DB. 64, $62CAB78E82137E78. 64, $4B60E6778A84C82B. 64, $41BF2417B0070764. 64, 
$F3865ADEBF337A99. 64, $A1EC36C696492BE0. 64, $7C884B32080C649F. 64, $AAE99BDADC37685A. 64, 
$DCA4C59D98BEEC6C. 64, $DD8886FED8B82090. 64, $F894AA6994EFDB8A. 64, $FB954EA7107B1BF3. 64, 
$80E569581773CF5F. 64, $F418E1F97E601D94. 64, $7A9B9F9033A40820. 64, $00E06D7C4F50726B. 64, 
$19C205C7F461EB65. 64, $DB610A36DE40AE7C. 64, $0FCD201AF3E65F5F. 64, $5840910FC1902224. 64, 
$1210975240BE1829. 64, $71B97307E8E903F9. 64, $DF85C6C346DF4FF2. 64, $BB26F835FD3711F1. 64, 
$4DE8E6DC008BD249. 64, $8C11D5A647CA6231. 64, $B10D0F66EC07A251. 64, $D2D4C7BD608AACDE. 64, 
$17C7560D621E6D62. 64, $A182591BC53D7C8A. 64, $8FBF7260C16058D7. 64, $D20C1AA47AD280FD. 64, 
$4C34ABC646276D3E. 64, $DF9328222B555885. 64, $5FCEAC68B91AA75F. 64, $B662C4D84F7135C6. 64, 
$418DBD3C45D7E67C. 64, $5E07DB97A28D2A3A. 64, $D5D7B024C7E148A3. 64, $3F3023639E4ED91D. 64, 
$75590580D18BDCF7. 64, $2936C445A8CCE5D3. 64, $1C9B51352A9B38AA. 64, $1EC67B0E63EA6B9C. 64, 
$30AA42F444DD8D77. 64, $5490C75F1A50B3D4. 64, $8A62DC6866149DC6. 64, $45E71CA58A3A1A03. 64, 
$44C35A60CA62EF4C. 64, $8A8D10F67904F203. 64, $73FB47C99A789E27. 64, $F6DA264C5EC58834. 64, 
$7DE707AB941A68B1. 64, $8E5FC15AB1B82D42. 64, $169F270E31E118B9. 64, $89D77D2CA228F1A1. 64, 
$F73BFCD076EA4593. 64, $3FC2594EA868AA6B. 64, $7E712B3826BF940B. 64, $C5E47523F2ED72D3. 64, 
$B17D5E2B40D91CB7. 64, $7A46CA989B6B545C. 64, $DF53963473D8A028. 64, $1C2B05E95B6A2361. 64, 
$2A8CE6CC8AA46240. 64, $7E56673B8467B2D4. 64, $5CC08986DD1643D2. 64, $34BEC26C10A8A0F7. 64, 
$5A1065508344D9BF. 64, $964CD691C7514A54. 64, $DA6642E206D8EEC0. 64, $FE50640EACC57736. 64, 
$4FD775BEEC03E00C. 64, $2ED51322FA648470. 64, $D126396FE346FD82. 64, $321F8E62660A5358. 64, 
$B18AC0415120A970. 64, $AE66E8D0D89BDEA2. 64, $8FF3907042113713. 64, $3ED1A5AF45B9BD21. 64, 
$CD93C5A7676F9B80. 64, $B6390A3D94DAEF11. 64, $868976715C5CCA68. 64, $AD886AA064B5DDC5. 64, 
$DCD8A0CCB0EE4F42. 64, $5E825B5AF2696B48. 64, $C6AD2848B1BD2AFE. 64, $4DE5A20AD330B6E4. 64, 
$121DA3E4428AA27D. 64, $AD734AF69BB658E8. 64, $A239809834B66FEC. 64, $4E0AFF25C162024C. 64, 
$12ADAB1B8CDBAA49. 64, $7EFD205B8A2D7142. 64, $1100D36951CC6ACD. 64, $56D7D5D9087D42DC. 64, 
$19BE8F3D1D7A103F. 64, $587697A07337E076. 64, $F134983D796333BF. 64, $8A67B4F38C5624C5. 64, 
$5D8A9736AD2EEDE3. 64, $5C32F0C1D2E26BED. 64, $029AD86080A1960A. 64, $ED5F76D1DB276ED9. 64, 
$33CB581061805DFD. 64, $A5DF2522A0F691C7. 64, $A4ADEDF782FD6BD2. 64, $FE384FF0D371C964. 64, 
$F5CFEF9E4A4CD273. 64, $85CBBAC869401C81. 64, $D511B713FED7005B. 64, $A7611177D696F186. 64, 
$CB2BE1FFA608F675. 64, $25031046C85C4651. 64, $607171BC4577D270. 64, $A7BD8884299863A6. 64, 
$BB09FB728099A1E0. 64, $257145E566C8698F. 64, $656BDB6B9184535F. 64, $2682AAE2CA83AE91. 64, 
$F7A44CC4003AAEE0. 64, $888A9A9370DA460A. 64, $6DE1F7FCFF64A895. 64, $B998294B6E631726. 64, 
$DD10FD0E373DE174. 64, $A4A1C99E1EDFF788. 64, $ABF89C5C23965C8C. 64, $519FCEACDB50A42E. 64, 
$C87EE06B04A3EE27. 64, $B3B84836F52EFE4A. 64, $6771855FC5488FF2. 64, $029F27357BF79A7B. 64, 
$864E931EC02D2201. 64, $9DFA41C069A2BEEE. 64, $22A5DB4B50464091. 64, $B0D2E299A7808724. 64, 
$FFC352ACC4E06CD6. 64, $9578BEBB4DB8FC2F. 64, $DC6E349B2D6DA548. 64, $2094DAB6C646C2D7. 64, 
$3B0AF3D2FD8EF1D0. 64, $63FDE78F2E0FB634. 64, $1C99503BC604F097. 64, $1C1EF3E82C9FC053. 64, 
$6BDB8E76017C181A. 64, $26D88404B8CBAFAE. 64, $187366AF04471F8D. 64, $76A2778F66E512B8. 64, 
$E5BA2951AF211F80. 64, $86B065507B33F205. 64, $75E3B0DFDD17BE98. 64, $09EDA77B60ABFE0A. 64, 
$97BEA04E8FA350FC. 64, $BC6E641D8A5D1A28. 64, $46D6377D5FB77C8D. 64, $3F97A7C23285D9E4. 64, 
$BA50164CA926C25D. 64, $CCDB57813E220451. 64, $1C967F121B63DDDF. 64, $A2A840B2E56CA3BD. 64, 
$00787A81DB69A851. 64, $AB7BE835BFC19FE8. 64, $C35A18B6E11A9F05. 64, $F4FAD3C269CEA995. 64, 
$C52B4F9FB5F7EB87. 64, $BF066890B494DF0E. 64, $E665E54BD57BF07D. 64, $9F662650E1CAA8B3. 64, 
$B60FCBB205E1B3D4. 64, $21D47F05B16CEE46. 64, $A7706D9DA4D36B31. 64, $23028D1C88657839. 64, 
$E0F3BE98C0D8E92E. 64, $9DA5D5CDED8C4DA2. 64, $827109BFA754CEA4. 64, $435571F88E42BC1F. 64, 
$3CE06094CBB9EFCB. 64, $2C03447D95B00977. 64, $D3E63B65D96A3686. 64, $A50C72D7437BC7FE. 64, 
$5737E476389CA9FD. 64, $3C8F8495ED9FB6BB. 64, $7E66BF01BDDE8AC9. 64, $42FF650C947F1B73. 64, 
$831AD4C01A37458A. 64, $AB86296924F9D44E. 64, $D04534934527FE11. 64, $AD67B18D326BA056. 64, 
$CDC85BC218E596C3. 64, $97536CD65082A588. 64, $41838111A37C89B5. 64, $1E670AC7A5905648. 64, 
$7EB67D2636ADEDF6. 64, $0560514F780DD13E. 64, $8B78A94B6C990708. 64, $7C15977BA8EA6213. 64, 
$8C8E898D35F895FE. 64, $1A2CA8EE917F324B. 64, $2CD3067B1262A84D. 64, $169C0956D6011241. 64, 
$3213F9193BDB3C69. 64, $7BC2F0864E7C480E. 64, $539F82006AB05B2C. 64, $D684DD5C69A76F73. 64, 
$168A44E4E0FA0504. 64, $42A75FDDE3BA8C01. 64, $FB48A92AE2DAD4D1. 64, $86121899DC7429C7. 64, 
$10F72AA5B40A344A. 64, $E4926B1781F8C90C. 64, $4F4C3F28EDAD7518. 64, $744C57C4DB14A013. 64, 
$450FC24B306136AE. 64, $DBE8614B7E18115C. 64, $A4CD66811B0F87FC. 64, $31984500099D06F5. 64, 
here 'rngs -
constant rng-size

\ wurstkessel primitives

cell 8 = [IF]
    : wurst ( u1 u2 -- u3 ) >r dup 2* swap 0< - r> xor ;
    : rngs 64s 'rngs + 64@ ;
[ELSE]
    s" bigFORTH" environment? [IF] 2drop
	Code wurst ( ud1 ud2 -- ud3 )
	    DX pop  CX pop  SP ) BX xchg
	    BX BX add  CX CX adc
	    0 # BX adc   DX BX xor  CX AX xor
	    SP ) BX xchg
	    Next end-code  macro
	Code rngs  'rngs AX *8 I#) push  'rngs cell+ AX *8 I#) AX mov
	    Next end-code  macro
    [ELSE]
	: rngs 64s 'rngs + 64@ ;
	: wurst ( ud1 ud2 -- ud3 )  2>r
	    dup 0< >r d2* r> dup d- 2r>
	    64xor ;
    [THEN]
[THEN]

\ permutation generation

0 [IF]
Create permut# 0 , 1 , 2 , 3 , 4 , 5 , 6 , 7 ,
permut#
DOES> swap 7 and cells + @ ;

Constant 'permut

create state1 0 , 1 , 2 , 3 , 4 , 5 , 6 , 7 ,
create state2 8 cells allot
: permut 8 0 DO  state1 I permut# cells + @ state2 I cells + ! LOOP
    state2 state1 8 cells move ;
: permut@ 0 8 0 DO 3 lshift state1 I cells + @ or  LOOP ;

permut@ Constant init-permut

: permut-count ( -- n )
    0 BEGIN 1+ permut permut@ init-permut = UNTIL ;

: (permut-counts { n }
    n 0= IF  permut-count . 8 0 DO I permut# . LOOP  cr  EXIT  THEN
    8 0 DO
	'permut I cells + dup @ 0= IF
	    n swap ! n 1- recurse 'permut I cells + off
	ELSE  drop  THEN
    LOOP ;

: permut-counts 'permut 8 cells erase  7 (permut-counts ;
[THEN]

\ wurstkessel algorithm

: mix2bytes ( index n k -- b1 .. b8 index' n ) wurst-state + 8 0 DO
	>r over wurst-source + c@ r@ c@ xor -rot dup >r + $3F and r> r> 8 + LOOP
    drop ;

: bytes2sum ( ud b1 .. b8 -- ud' ) >r >r >r >r  >r >r >r >r
    r> rngs wurst  r> rngs wurst  r> rngs wurst  r> rngs wurst
    r> rngs wurst  r> rngs wurst  r> rngs wurst  r> rngs wurst ;

Create round# 13 , 29 , 19 , 23 , 31 , 47 , 17 , 37 ,
DOES> swap 7 and cells + @ ;
Create permut# 2 , 6 , 1 , 4 , 7 , 0 , 5 , 3 , \ permut length 15
DOES> swap 7 and cells + @ ;

: xors ( addr1 addr2 n -- ) bounds ?DO
    dup @ I @ xor I ! cell+  cell +LOOP  drop ;
: +!s ( addr1 addr2 n -- ) bounds ?DO
    dup 64@ I 64@ 64+ I 64! 64'+  1 64s +LOOP  drop ;

: update-state ( -- )
    wurst-state wurst-source state# xors
    nextstate wurst-state state# +!s ;
: round ( n -- ) dup 1- swap  8 0 DO
	wurst-state I permut# 64s + 64@ -64rot
	I mix2bytes 2>r bytes2sum 2r> 64rot nextstate I 64s + 64!
    LOOP 2drop update-state ;

\ fast mixing

[IFUNDEF] ]]
    : [[ ; \ token to end bulk-postponing
    : ]] BEGIN  >in @ ' ['] [[ <> WHILE  >in ! postpone postpone  REPEAT
	drop ; immediate
[THEN]
s" bigFORTH" environment? [IF] 2drop
    $F487 Constant [s~r]
    : S:  ( -- )  lastdes c@ dup :r =
	IF   drop [s~r] w,
	ELSE :s = 0= IF  -2 allot  THEN  THEN  :s lastdes c! ;
    : wurst-mix ( a1 a2 -- ) S: [ also assembler ]
	AX push  .b A#) AX movzx  A#) AL xor
	[ previous ] [s~r] w, ]] rngs wurst [[ ; immediate
[ELSE]
    : wurst-mix ]] Literal c@ Literal c@ xor rngs wurst [[ ; immediate
[THEN]
: mix2bytes, ( index n k -- index' n ) wurst-state + 8 0 DO
	>r over wurst-source + r@ ]] wurst-mix [[
	dup >r + $3F and r> r> 8 + LOOP
    drop ;

: round, ( n -- ) dup 1- swap  8 0 DO
	wurst-state I permut# 64s + ]] Literal 64@ [[
	I mix2bytes, nextstate I 64s + ]] Literal 64! [[
    LOOP 2drop ]] update-state [[ ;

s" gforth" environment? [IF] 2drop
    require libcc.fs
    require string.fs
    : \c, ( addr u -- ) save-c-prefix-line ;
    Variable %string  s" " %string $!
    : %> ( -- )
	BEGIN  source >in @ /string s" <%" search  0= WHILE
		postpone sliteral postpone %string postpone $+!
	    refill  0= UNTIL  EXIT  THEN
	nip source >in @ /string rot - dup 2 + >in +!
	postpone sliteral postpone %string postpone $+! ;  immediate
    : %c, ( -- )  %string $@ \c,  s" " %string $! ;
    : #$ ( n -- )  0 <<# #S #> %string $+! #>> ;
    : mix2bytes_ind, ( index n k i -- index n ) >r
	>r over r@ 64 +
	%> a<% r@ 7 and #$ %> ^=ROL(rnds[states[<% swap #$ %> ]^(0xff&(t>><%
	$7 and 8 * #$ %> ))],<% r> 7 r@ - #$ >r %> );<%	%c,
	rdrop rdrop ;
    : round_ind, ( n -- )
	%> static inline void round<% dup #$
	%> _ind(unsigned char * states, uint64_t * rnds) {<% %c,
	%>   uint64_t a0, a1, a2, a3, a4, a5, a6, a7, t;<% %c,
	round# dup 1- swap 8 0 DO
	    %> a<% I #$ %> =ROL(*((uint64_t*)(states+<%
	    I permut# 8 + 64s #$ %> )),8);<% %c,
	LOOP
	8 0 DO
	    [ 0 ] [IF]
		%> asm volatile("# line break" : : "g" (a0), "g" (a1), "g" (a2), "g" (a3), "g" (a4), "g" (a5), "g" (a6), "g" (a7));<% %c,
	    [THEN] \ seems to be a bad idea - let the compiler decide
	    %> t=*((uint64_t*)(states+<% I 8 * 64 + #$ %> ));<% %c,
	    8 0 DO  I J 8 * + J mix2bytes_ind,
		dup >r 8 * + $3F and r>
	    LOOP  dup >r + $3F and r>
	LOOP
	2drop
	8 0 DO
	    %> *((uint64_t *)(states+<% I 64s #$ %> )) ^= *((uint64_t *)(states+<% I 8 + 64s #$ %> ));<%
	    %c,
	LOOP
	8 0 DO
	    %> *((uint64_t *)(states+<% I 8 + 64s #$ %> )) += a<% I #$ %> ;<% %c,
	LOOP
	%> }<% %c, ;
	
    c-library wurstkessel
    \c #include <stdint.h>
    \c #include <string.h>
    \c #define ROL(x, n) (n==0)?x:((x << n) | (x >> (64-n)))
    \c #define COMBINE(x0,a1,a2,a3,a4,a5,a6,a7,a8) (ROL(x0,8)^ROL(a1,7)^ROL(a2,6)^ROL(a3,5)^ROL(a4,4)^ROL(a5,3)^ROL(a6,2)^ROL(a7,1)^a8)
    0 round_ind,
    1 round_ind,
    2 round_ind,
    3 round_ind,
    4 round_ind,
    5 round_ind,
    6 round_ind,
    7 round_ind,
    \c static inline void add_entropy(uint64_t * m, uint64_t * s) {
    \c    int i;
    \c    for(i=0; i<8; i++) { s[i] ^= m[i]; m[i] ^= s[i+8]; }
    \c }
    \c static inline void set_entropy(uint64_t * m, uint64_t * s) {
    \c    int i;
    \c    for(i=0; i<8; i++) { m[i] ^= s[i+8]; s[i] ^= m[i]; }
    \c }
    \c static uint64_t * rnds;
    \c void rounds_init(uint64_t * rndsi) {
    \c    rnds=rndsi;
    \c }
    \c void rounds_encrypt(unsigned char* message, unsigned int n, unsigned char * states) {
    \c if((n&15)>=1) { round0_ind(states, rnds);
    \c if(n&0x10) add_entropy((uint64_t *)(message+64*0),(uint64_t *)(states));
    \c } if((n&15)>=2) { round1_ind(states, rnds);
    \c if(n&0x10) add_entropy((uint64_t *)(message+64*1),(uint64_t *)(states));
    \c if(n&0x20) add_entropy((uint64_t *)(message+64*0),(uint64_t *)(states));
    \c } if((n&15)>=3) { round2_ind(states, rnds);
    \c if(n&0x10) add_entropy((uint64_t *)(message+64*2),(uint64_t *)(states));
    \c } if((n&15)>=4) { round3_ind(states, rnds);
    \c if(n&0x10) add_entropy((uint64_t *)(message+64*3),(uint64_t *)(states));
    \c if(n&0x20) add_entropy((uint64_t *)(message+64*1),(uint64_t *)(states));
    \c if(n&0x40) add_entropy((uint64_t *)(message+64*0),(uint64_t *)(states));
    \c } if((n&15)>=5) { round4_ind(states, rnds);
    \c if(n&0x10) add_entropy((uint64_t *)(message+64*4),(uint64_t *)(states));
    \c } if((n&15)>=6) { round5_ind(states, rnds);
    \c if(n&0x10) add_entropy((uint64_t *)(message+64*5),(uint64_t *)(states));
    \c if(n&0x20) add_entropy((uint64_t *)(message+64*2),(uint64_t *)(states));
    \c } if((n&15)>=7) { round6_ind(states, rnds);
    \c if(n&0x10) add_entropy((uint64_t *)(message+64*6),(uint64_t *)(states));
    \c } if((n&15)>=8) { round7_ind(states, rnds);
    \c if(n&0x10) add_entropy((uint64_t *)(message+64*7),(uint64_t *)(states));
    \c if(n&0x20) add_entropy((uint64_t *)(message+64*3),(uint64_t *)(states));
    \c if(n&0x40) add_entropy((uint64_t *)(message+64*1),(uint64_t *)(states));
    \c if(n&0x80) add_entropy((uint64_t *)(message+64*0),(uint64_t *)(states));
    \c } }
    \c void rounds_decrypt(unsigned char* message, unsigned int n, unsigned char * states) {
    \c if((n&15)>=1) { round0_ind(states, rnds);
    \c if(n&0x10) set_entropy((uint64_t *)(message+64*0),(uint64_t *)(states));
    \c } if((n&15)>=2) { round1_ind(states, rnds);
    \c if(n&0x10) set_entropy((uint64_t *)(message+64*1),(uint64_t *)(states));
    \c if(n&0x20) set_entropy((uint64_t *)(message+64*0),(uint64_t *)(states));
    \c } if((n&15)>=3) { round2_ind(states, rnds);
    \c if(n&0x10) set_entropy((uint64_t *)(message+64*2),(uint64_t *)(states));
    \c } if((n&15)>=4) { round3_ind(states, rnds);
    \c if(n&0x10) set_entropy((uint64_t *)(message+64*3),(uint64_t *)(states));
    \c if(n&0x20) set_entropy((uint64_t *)(message+64*1),(uint64_t *)(states));
    \c if(n&0x40) set_entropy((uint64_t *)(message+64*0),(uint64_t *)(states));
    \c } if((n&15)>=5) { round4_ind(states, rnds);
    \c if(n&0x10) set_entropy((uint64_t *)(message+64*4),(uint64_t *)(states));
    \c } if((n&15)>=6) { round5_ind(states, rnds);
    \c if(n&0x10) set_entropy((uint64_t *)(message+64*5),(uint64_t *)(states));
    \c if(n&0x20) set_entropy((uint64_t *)(message+64*2),(uint64_t *)(states));
    \c } if((n&15)>=7) { round6_ind(states, rnds);
    \c if(n&0x10) set_entropy((uint64_t *)(message+64*6),(uint64_t *)(states));
    \c } if((n&15)>=8) { round7_ind(states, rnds);
    \c if(n&0x10) set_entropy((uint64_t *)(message+64*7),(uint64_t *)(states));
    \c if(n&0x20) set_entropy((uint64_t *)(message+64*3),(uint64_t *)(states));
    \c if(n&0x40) set_entropy((uint64_t *)(message+64*1),(uint64_t *)(states));
    \c if(n&0x80) set_entropy((uint64_t *)(message+64*0),(uint64_t *)(states));
    \c } }
    \c uint64_t wurst_hash64(unsigned char * addr, unsigned int len,
    \c                       uint64_t startval, uint64_t * rnds) {
    \c   uint64_t result=startval;
    \c   unsigned int i=0;
    \c   for(i=0; i<len; i++) {
    \c     result = ROL(result, 1) + rnds[addr[i] ^ (result & 0xFF)];
    \c     /* the result feedback kills parallelism, but makes sure
    \c        the hash cannot easily be attacked */
    \c   }
    \c   return result;
    \c }
	c-function rounds-init rounds_init a -- void
	c-function (rounds-encrypt) rounds_encrypt a n a -- void
	c-function (rounds-decrypt) rounds_decrypt a n a -- void
	[IFDEF] 64bit
	    c-function wurst_hash64 wurst_hash64 a n n a -- n
	[ELSE]
	    c-function wurst_hash64 wurst_hash64 a n d a -- d
	[THEN]
    end-c-library
    wurst-source Value @state
    : rounds-setkey ( addr -- )  to @state ;
    : rounds-encrypt ( addr u -- ) @state (rounds-encrypt) ;
    : rounds-decrypt ( addr u -- ) @state (rounds-decrypt) ;
    : wurst-init ( -- )  'rngs rounds-init ;
    : hash64 ( addr n init -- hash ) 'rngs wurst_hash64 ;
[ELSE]
: round0 ( -- )  [ 0 round# round, ] ; 
: round1 ( -- )  [ 1 round# round, ] ; 
: round2 ( -- )  [ 2 round# round, ] ; 
: round3 ( -- )  [ 3 round# round, ] ; 
: round4 ( -- )  [ 4 round# round, ] ; 
: round5 ( -- )  [ 5 round# round, ] ; 
: round6 ( -- )  [ 6 round# round, ] ; 
: round7 ( -- )  [ 7 round# round, ] ;
: wurst-init ;

Create 'rounds
    ' round0 A, ' round1 A, ' round2 A, ' round3 A,
    ' round4 A, ' round5 A, ' round6 A, ' round7 A,
Create 'round-flags
    $10 , $30 , $10 , $70 , $10 , $30 , $10 , $F0 ,

: +entropy ( message -- message' )
    dup wurst-source state# xors
    wurst-state over state# xors
    state# + ;

: -entropy ( message -- message' )
    wurst-state over state# xors
    dup wurst-source state# xors
    state# + ;

: rounds-encrypt ( addr n -- )  dup $F and 8 umin 0 ?DO
	'rounds Ith execute
	dup 'round-flags Ith and IF
	    swap +entropy swap
	THEN
    LOOP 2drop ;

\ : rounds' ( n -- )  dup $F and 8 umin 0 ?DO
\ 	'rounds Ith execute .source cr .state cr cr
\ 	dup 'round-flags Ith and IF
\ 	    swap +entropy swap
\ 	THEN
\     LOOP 2drop ;

: rounds-decrypt ( n -- )  dup $F and 8 umin 0 ?DO
	'rounds Ith execute
	dup 'round-flags Ith and IF
	    swap -entropy swap
	THEN
    LOOP 2drop ;
[THEN]

\ wurstkessel file functions

0 Value wurst-in
0 Value wurst-out

: wurst-file ( addr u -- )   r/o open-file throw to wurst-in ;

: size? ( -- ud )
    wurst-in file-size throw ;

: wurst-outfile ( addr u -- )  w/o create-file throw to wurst-out ;

: wurst-close ( -- )
    wurst-in  ?dup IF  close-file 0 to wurst-in  throw  THEN
    wurst-out ?dup IF  close-file 0 to wurst-out throw  THEN ;

\ wurstkessel hash

: hash-init ( source state -- ) swap
    wurst-source state# move
    wurst-state  state# move ;

: wurst-size ( -- )
    [ cell 4 = ] [IF]
	size?      message 64!  0. message 1 64s + 64!
    [ELSE]
	size? drop message 64!  0  message 1 64s + 64!
    [THEN] ;

: >reads ( flags -- n )  dup $F and swap 4 rshift / ;

: encrypt-read ( flags -- n )  >reads >r
    message state# r> * 2dup erase  wurst-in read-file throw ;
: read-first ( flags -- n )  wurst-size  >reads >r
    message state# r> * 2 64s /string wurst-in read-file throw  2 64s + ;

: wurst-hash ( source state final-rounds rounds -- )
    2swap hash-init dup read-first
    BEGIN  0>  WHILE
	    message over rounds-encrypt
	    dup encrypt-read
    REPEAT
    drop message swap rounds-encrypt .source wurst-close ;

\ padding

: >pad ( addr u u2 -- addr u2 )
    swap >r 2dup r@ /string r> fill ;
: >unpad ( addr u' -- addr u ) over + 1- c@ ;
: ?padded ( addr u' -- flag )
    2dup + 1- c@ dup >r /string r> skip nip 0= ;

\ wurstkessel encryption

: message> ( flags -- ) >reads
    message swap state# * wurst-out write-file throw ;

: encrypt-init ( key salt -- )  swap
    wurst-state  state# move
    wurst-source state# move
    wurst-source state# wurst-out write-file throw ;

: wurst-encrypt ( key salt first-rounds rounds -- )
    >r >r encrypt-init
    message r> rounds-encrypt  r@ read-first
    BEGIN  0>  WHILE
	    message r@ rounds-encrypt  r@ message>
	    r@ encrypt-read  REPEAT
    rdrop wurst-close ;

: decrypt-init ( key -- )
    wurst-state  state# move
    wurst-source state# wurst-in read-file throw drop ;

2Variable wurst-outsize

: out- ( n -- n' )
    0 wurst-outsize 2@ dmin wurst-outsize 2@ 2over d- wurst-outsize 2! drop ;

: .xormsg-size ( flags -- )  >reads
    message 64@ [ cell 8 = ] [IF] 0 [THEN] wurst-outsize 2!
    message swap state# * 2 64s /string out-
    wurst-out write-file throw ;

: message>' ( flags -- ) >reads
    message swap state# * out- wurst-out write-file throw ;

: wurst-decrypt ( first-rounds rounds -- )
    >r >r decrypt-init
    message r> rounds-encrypt
    r@ encrypt-read message r@ rounds-decrypt r@ .xormsg-size
    BEGIN  0>  WHILE
	    r@ encrypt-read
	    message r@ rounds-decrypt  r@ message>'
    REPEAT
    rdrop  wurst-close ;

\ wurstkessel rng

4 Constant rngs#

: rng-init
    wurst-salt rounds-setkey
    message state# rngs# * erase ;

$18 Value roundsh#
$28 Value rounds#
4 Value roundse#

Variable rng-buffer state# rngs# * allot
state# rngs# * rng-buffer !

: rng-step ( -- )
    rng-init  rng-buffer cell+ rounds# rounds-encrypt
    rng-buffer off
    wurst-source rounds-setkey ;

\ buffered random numbers to output 64 bit at a time

: rng-step? ( n -- ) state# rngs# * u> IF  rng-step  THEN ;

: rng@ ( -- x )
    rng-buffer @ 64aligned 64'+ rng-step?
    rng-buffer dup @ 64aligned dup 64'+ rng-buffer ! cell+ + 64@ ;

: rng$ ( -- addr u )
    rng-buffer @ state# + rng-step?
    rng-buffer dup @ + cell+ state# dup rng-buffer +! ;

: rng32 ( -- x )
    rng-buffer @ 4 + rng-step?
    rng-buffer dup @ + cell+ l@
    4 rng-buffer +! ;

\ benchmark to evaluate quality of the compiled code

: wurst-bench ( n -- ) >r cputime d+
    r@ 0 ?DO  message rounds# rounds-encrypt  LOOP  cputime d+ d- d>f -1e-6 f*
    1/f 256e r> fm* f* 1024e fdup f* f/ f. ." MB/s" ;

: mem-rounds# ( size -- n )
    case
	state# of  $22  endof
	state# 2* of  $24  endof
	$28 swap
    endcase ;

\ OOP crypto api class

: wurst-crc ( -- xd )
    message roundse# rounds-encrypt \ another key diffusion round
    @state 128@ ;

require crypto-api.fs

crypto class
end-class wurstkessel

' wurst-task-init wurstkessel to c:init
:noname to @state ; wurstkessel to c:key!
' @state wurstkessel to c:key@
state# 2* Constant wurst-key# ' wurst-key# wurstkessel to c:key#
:noname ( addr -- )
    @state swap state# 2* move ; wurstkessel to c:key>
:noname ( addr -- )
    wurst-source state# 2* move wurst-source rounds-setkey ; wurstkessel to >c:key
:noname ( -- )
    message roundse# rounds-encrypt ; wurstkessel to c:diffuse
:noname ( addr u -- ) dup mem-rounds#
    dup >reads state# * { rnd reads }
    BEGIN  dup 0>  WHILE
	    over rnd rounds-encrypt  reads /string
    REPEAT 2drop ; wurstkessel to c:encrypt
:noname ( addr u -- ) dup mem-rounds#
    dup >reads state# * { rnd reads }
    BEGIN  dup 0>  WHILE
	    over rnd rounds-decrypt  reads /string
    REPEAT 2drop ; wurstkessel to c:decrypt
:noname ( addr u -- ) dup mem-rounds#
    dup >reads state# * { rnd reads }
    BEGIN  dup 0>  WHILE
	    over rnd rounds-encrypt  reads /string
    REPEAT drop >r wurst-crc r> 128! ; wurstkessel to c:encrypt+auth
:noname ( addr u -- ) dup mem-rounds#
    dup >reads state# * { rnd reads }
    BEGIN  dup 0>  WHILE
	    over rnd rounds-decrypt  reads /string
    REPEAT drop 128@ wurst-crc 128= ; wurstkessel to c:decrypt+auth
:noname ( addr u -- ) dup roundsh#
    dup >reads state# * { rnd reads }
    BEGIN  dup 0>  WHILE
	    over rnd rounds-encrypt  reads /string
    REPEAT drop roundse# rounds-encrypt ; wurstkessel to c:hash
:noname ( addr u -- )  2dup erase c:encrypt ; wurstkessel to c:prng
' wurst-crc wurstkessel to c:checksum
:noname ( -- x ) @state $10 + 64@ ; wurstkessel to c:cookie

static-a to allocater
wurstkessel new Constant wurstkessel-o
dynamic-a to allocater