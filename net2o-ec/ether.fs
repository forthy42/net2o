
\ Ethernet driver for TM4C1294
\   needs basisdefinitions.txt

\ Erste Versuche: Wegen der Puffer erstmal nur für RAM geeignet !
\ Flashkompilationsfähigkeit wird noch nachgerüstet.

compiletoram

$400EC000 constant Ethernet-Base
$400FE000 constant USB-Base \ for unique device ID

USB-Base $F20 + Constant UNIQUEID0
USB-Base $F24 + Constant UNIQUEID1
USB-Base $F28 + Constant UNIQUEID2
USB-Base $F2C + Constant UNIQUEID3

\ Ethernet MAC (Ethernet Offset)
Ethernet-Base $000 + constant EMACCFG       \ RW $0000.8000 Ethernet MAC Configuration
Ethernet-Base $004 + constant EMACFRAMEFLTR \ RW $0000.0000 Ethernet MAC Frame Filter
Ethernet-Base $008 + constant EMACHASHTBLH  \ RW $0000.0000 Ethernet MAC Hash Table High
Ethernet-Base $00C + constant EMACHASHTBLL  \ RW $0000.0000 Ethernet MAC Hash Table Low
Ethernet-Base $010 + constant EMACMIIADDR   \ RW $0000.0000 Ethernet MAC MII Address
Ethernet-Base $014 + constant EMACMIIDATA   \ RW $0000.0000 Ethernet MAC MII Data Register
Ethernet-Base $018 + constant EMACFLOWCTL   \ RW $0000.0000 Ethernet MAC Flow Control
Ethernet-Base $01C + constant EMACVLANTG    \ RW $0000.0000 Ethernet MAC VLAN Tag
Ethernet-Base $024 + constant EMACSTATUS    \ RO $0000.0000 Ethernet MAC Status
Ethernet-Base $028 + constant EMACRWUFF     \ RW $0000.0000 Ethernet MAC Remote Wake-Up Frame Filter
Ethernet-Base $02C + constant EMACPMTCTLSTAT \ RW $0000.0000 Ethernet MAC PMT Control and Status Register

Ethernet-Base $038 + constant EMACRIS       \ RO $0000.0000 Ethernet MAC Raw Interrupt Status
Ethernet-Base $03C + constant EMACIM        \ RW $0000.0000 Ethernet MAC Interrupt Mask

Ethernet-Base $040 + constant EMACADDR0H    \ RW $8000.FFFF Ethernet MAC Address 0 High
Ethernet-Base $044 + constant EMACADDR0L    \ RW $FFFF.FFFF Ethernet MAC Address 0 Low Register
Ethernet-Base $048 + constant EMACADDR1H    \ RW $0000.FFFF Ethernet MAC Address 1 High
Ethernet-Base $04C + constant EMACADDR1L    \ RW $FFFF.FFFF Ethernet MAC Address 1 Low
Ethernet-Base $050 + constant EMACADDR2H    \ RW $0000.FFFF Ethernet MAC Address 2 High
Ethernet-Base $054 + constant EMACADDR2L    \ RW $FFFF.FFFF Ethernet MAC Address 2 Low
Ethernet-Base $058 + constant EMACADDR3H    \ RW $0000.FFFF Ethernet MAC Address 3 High
Ethernet-Base $05C + constant EMACADDR3L    \ RW $FFFF.FFFF Ethernet MAC Address 3 Low

Ethernet-Base $0DC + constant EMACWDOGTO    \ RW $0000.0000 Ethernet MAC Watchdog Timeout

Ethernet-Base $100 + constant EMACMMCCTRL   \ RW $0000.0000 Ethernet MAC MMC Control
Ethernet-Base $104 + constant EMACMMCRXRIS  \ RO $0000.0000 Ethernet MAC MMC Receive Raw Interrupt Status
Ethernet-Base $108 + constant EMACMMCTXRIS  \ R  $0000.0000 Ethernet MAC MMC Transmit Raw Interrupt Status
Ethernet-Base $10C + constant EMACMMCRXIM   \ RW $0000.0000 Ethernet MAC MMC Receive Interrupt Mask
Ethernet-Base $110 + constant EMACMMCTXIM   \ RW $0000.0000 Ethernet MAC MMC Transmit Interrupt Mask

Ethernet-Base $118 + constant EMACTXCNTGB   \ RO $0000.0000 Ethernet MAC Transmit Frame Count for Good and Bad Frames
Ethernet-Base $14C + constant EMACTXCNTSCOL \ RO $0000.0000 Ethernet MAC Transmit Frame Count for Frames Transmitted after Single Collision
Ethernet-Base $150 + constant EMACTXCNTMCOL \ RO $0000.0000 Ethernet MAC Transmit Frame Count for Frames Transmitted after Multiple Collisions
Ethernet-Base $164 + constant EMACTXOCTCNTG \ RO $0000.0000 Ethernet MAC Transmit Octet Count Good
                                    
Ethernet-Base $180 + constant EMACRXCNTGB      \ RO $0000.0000 Ethernet MAC Receive Frame Count for Good and Bad Frames
Ethernet-Base $194 + constant EMACRXCNTCRCERR  \ RO $0000.0000 Ethernet MAC Receive Frame Count for CRC Error Frames
Ethernet-Base $198 + constant EMACRXCNTALGNERR \ RO $0000.0000 Ethernet MAC Receive Frame Count for Alignment Error Frames
Ethernet-Base $1C4 + constant EMACRXCNTGUNI    \ RO $0000.0000 Ethernet MAC Receive Frame Count for Good Unicast Frames

Ethernet-Base $584 + constant EMACVLNINCREP   \ RW $0000.0000 Ethernet MAC VLAN Tag Inclusion or Replacement
Ethernet-Base $588 + constant EMACVLANHASH    \ RW $0000.0000 Ethernet MAC VLAN Hash Table
Ethernet-Base $700 + constant EMACTIMSTCTRL   \ RW $0000.2000 Ethernet MAC Timestamp Control
Ethernet-Base $704 + constant EMACSUBSECINC   \ RW $0000.0000 Ethernet MAC Sub-Second Increment

Ethernet-Base $708 + constant EMACTIMSEC      \ RO $0000.0000 Ethernet MAC System Time - Seconds
Ethernet-Base $70C + constant EMACTIMNANO     \ RO $0000.0000 Ethernet MAC System Time - Nanoseconds
Ethernet-Base $710 + constant EMACTIMSECU     \ RW $0000.0000 Ethernet MAC System Time - Seconds Update
Ethernet-Base $714 + constant EMACTIMNANOU    \ RW $0000.0000 Ethernet MAC System Time - Nanoseconds Update
Ethernet-Base $718 + constant EMACTIMADD      \ RW $0000.0000 Ethernet MAC Timestamp Addend
Ethernet-Base $71C + constant EMACTARGSEC     \ RW $0000.0000 Ethernet MAC Target Time Seconds
Ethernet-Base $720 + constant EMACTARGNANO    \ RW $0000.0000 Ethernet MAC Target Time Nanoseconds
Ethernet-Base $724 + constant EMACHWORDSEC    \ RW $0000.0000 Ethernet MAC System Time-Higher Word Seconds
Ethernet-Base $728 + constant EMACTIMSTAT     \ RO $0000.0000 Ethernet MAC Timestamp Status

Ethernet-Base $72C + constant EMACPPSCTRL     \ RW $0000.0000 Ethernet MAC PPS Control
Ethernet-Base $760 + constant EMACPPS0INTVL   \ RW $0000.0000 Ethernet MAC PPS0 Interval
Ethernet-Base $764 + constant EMACPPS0WIDTH   \ RW $0000.0000 Ethernet MAC PPS0 Width

Ethernet-Base $C00 + constant EMACDMABUSMOD   \ RW $0002.0101 Ethernet MAC DMA Bus Mode
Ethernet-Base $C04 + constant EMACTXPOLLD     \ WO $0000.0000 Ethernet MAC Transmit Poll Demand
Ethernet-Base $C08 + constant EMACRXPOLLD     \ WO $0000.0000 Ethernet MAC Receive Poll Demand
Ethernet-Base $C0C + constant EMACRXDLADDR    \ RW $0000.0000 Ethernet MAC Receive Descriptor List Address
Ethernet-Base $C10 + constant EMACTXDLADDR    \ RW $0000.0000 Ethernet MAC Transmit Descriptor List Address
Ethernet-Base $C14 + constant EMACDMARIS      \ RW $0000.0000 Ethernet MAC DMA Interrupt Status
Ethernet-Base $C18 + constant EMACDMAOPMODE   \ RW $0000.0000 Ethernet MAC DMA Operation Mode
Ethernet-Base $C1C + constant EMACDMAIM       \ RW $0000.0000 Ethernet MAC DMA Interrupt Mask Register
                                      
Ethernet-Base $C20 + constant EMACMFBOC     \ RO  $0000.0000 Ethernet MAC Missed Frame and Buffer Overflow Counter
Ethernet-Base $C24 + constant EMACRXINTWDT  \ RW  $0000.0000 Ethernet MAC Receive Interrupt Watchdog Timer
Ethernet-Base $C48 + constant EMACHOSTXDESC \  R  $0000.0000 Ethernet MAC Current Host Transmit Descriptor
Ethernet-Base $C4C + constant EMACHOSRXDESC \ RO  $0000.0000 Ethernet MAC Current Host Receive Descriptor
Ethernet-Base $C50 + constant EMACHOSTXBA   \  R  $0000.0000 Ethernet MAC Current Host Transmit Buffer Address
Ethernet-Base $C54 + constant EMACHOSRXBA   \  R  $0000.0000 Ethernet MAC Current Host Receive Buffer Address
Ethernet-Base $FC0 + constant EMACPP        \ RO  $0000.0103 Ethernet MAC Peripheral Property Register
Ethernet-Base $FC4 + constant EMACPC        \ RW  $0080.040E Ethernet MAC Peripheral Configuration Register
Ethernet-Base $FC8 + constant EMACCC        \ RO  $0000.0000 Ethernet MAC Clock Configuration Register
Ethernet-Base $FD0 + constant EPHYRIS       \ RO  $0000.0000 Ethernet PHY Raw Interrupt Status
Ethernet-Base $FD4 + constant EPHYIM        \ RW  $0000.0000 Ethernet PHY Interrupt Mask
Ethernet-Base $FD8 + constant EPHYMISC      \ RW  $0000.0000 Ethernet PHY Masked Interrupt Status and Clear

 
\ Constants for EMACDMAIM
1 16 lshift constant NIE
1  6 lshift constant RIE


\ Dies geht natürlich nur, während ins RAM kompiliert wird. Später Pufferinitialisationen für Flash entwickeln.

: even4 ( u -- u ) dup 1 and + dup 2 and + ;
: align4, ( -- ) here 1 and if 0 c, then here 2 and if 0 h, then ;
: create4> <builds ( -- ) align4,  does> ( -- addr ) even4  ;

1 31 lshift constant own

create4> RX-Puffer-1
  2048 allot

create4> RX-Puffer-2
  2048 allot

create4> TX-Puffer
  2048 allot

create4> TX-Puffer-2
  2048 allot

1 15 lshift constant RER

create4> RX-Descriptor
  own ,                       \ RDES0
  RER
  2047 16 lshift or 
  2047 or ,                   \ RDES1  RER: Receive end of ring, Buffer sizes: Both 2047 Bytes
  RX-Puffer-1 ,               \ RDES2
  RX-Puffer-2 ,               \ RDES3
  0 ,                         \ RDES4: extended status
  0 ,                         \ RDES5: reserved
  0 , 0 ,                     \ RDES6+7: Timestamp low+high

1 30 lshift constant IC
1 29 lshift constant LS  \ Last Segment of Frame
1 28 lshift constant FS  \ First Segment of Frame
1 25 lshift constant TTSE \ time stamp enable
1 21 lshift constant TER \ Transmit End of Ring
1 17 lshift constant TTSS \ time stamp status

LS FS or TTSE or TER or TTSS or Constant TDES0
TDES0 own or Constant TDES0:own

2 29 lshift constant SAIC:RS \ replace source

create4> TX-Descriptor \ must have more than two correct descriptors!
  TDES0 ,                     \ TDES0
  SAIC:RS ,                   \ TDES1
  TX-Puffer ,                 \ TDES2
  TX-Puffer-2 ,               \ TDES3
  0 ,                         \ TDES4: extended status
  0 ,                         \ TDES5: reserved
  0 , 0 ,                     \ TDES6+7: Timestamp low+high
\ we won't use these descriptors, they are just dummy descriptors

: byte. ( u -- )
  base @ hex swap
  0 <# # # #> type
  base !
;

: word. ( u -- )
  base @ hex swap
  0 <# # # # # #> type
  base !
;


: mac. ( addr -- )
  dup     c@ byte. ." :"
  dup 1 + c@ byte. ." :"
  dup 2 + c@ byte. ." :"
  dup 3 + c@ byte. ." :"
  dup 4 + c@ byte. ." :"
      5 + c@ byte. space
;

: packetdump ( length addr )
  swap
  0 ?do ( addr )
      i $7 and 0= if space then \ Alle 8 Zeichen ein zusätzliches Leerzeichen
      i $F and 0= if cr then  \ Alle 16 Zeichen einen Zeilenumbruch
      dup i + c@ byte. space
    loop
  drop
  cr
;


: printpacket ( length addr -- )
  over ." Länge " u.
  dup      ." Ziel-MAC "  mac.
  dup  6 + ." Quell-MAC " mac.
  ." Ethertype "
  dup 12 + c@ byte.
  dup 13 + c@ byte.
  cr

  ( length addr )
  packetdump
;



1 5 lshift constant UNF \ Transmit Underflow
1 2 lshift constant TU \ Transmit Unavailabl

create4> mymac  $00 c, $1A c, $B6 c,
uniqueid0 @ dup c, 8 rshift dup c,
uniqueid3 3 + c@ c, \ use unique ID
$00 c, $80 c,

: tc, ( addr char -- addr' )  over c! 1+ ;
: tw, ( addr word -- addr' )  >r r@ 8 rshift tc, r> tc, ;
: tl, ( addr word -- addr' )  >r r@ $10 rshift tw, r> tw, ;
: t$, ( addr addr1 u1 -- addr' ) rot 2dup + >r swap move r> ;

: ffmac, ( addr -- addr' )   6 0 DO  $FF tc,  LOOP ;

: fill-arp ( -- )
    0 TX-Descriptor ! \ TDES0: Zum Füllen von der DMA übernehmen
    TX-Puffer
    \ Ziel-MAC-Adresse, Broadcast
    ffmac,
  \ Quell-MAC-Adresse, wird ersetzt
    ffmac,

    \ Ethertype: ARP
    $0806 tw,

    \ Rest im Puffer: ARP-Request, damit wir auch eine Antwort kriegen
    1 tw, $800 tw,
    6 tc, 4 tc,  0 tc, 1 tc,
    \ my mac
    mymac 6 t$,
    \ my ip
    10 tc, 0 tc, 0 tc, 2 tc,
    \ dest mac
    ffmac,
    \ dest ip
    10 tc, 0 tc, 0 tc, 1 tc,
    drop
;

: set-tdesc ( -- )
    60 SAIC:RS or
    TX-Descriptor 4 + ! \ TDES1: Puffergröße und ein paar Flags
    TDES0:own TX-Descriptor ! \ TDES0: Zum Abschicken an den DMA übergeben
;

: do-send ( -- )
    
    dint
    -1 EMACTXPOLLD !    \ Sendelogik anstuppsen
    ." Losschicken: " cr
    ." EMACDMARIS: "  emacdmaris @ hex. cr
    TX-Descriptor 4 + @ $3FFF and
    TX-Puffer printpacket
    
    unf tu or emacdmaris !    \ Transmit Buffer Underflow löschen
    eint
;

: sp ( -- ) \ Send Packet
    
    \ Warte, bis der Sendepufferdescriptor frei ist...
    \   begin TX-Descriptor @ own and 0= until
    
    
    \ Puffer mit Quatsch füllen
    fill-arp
    \ Abschicken
    set-tdesc
    do-send
;

: ethernet-handler ( -- )

  EMACDMARIS @ 
  \   1 16 lshift   1  6 lshift or EMACDMARIS !  \ Flags löschen
  -1 EMACDMARIS !

  ." Ethernet-IRQ " hex. cr


  RX-Descriptor     @ hex. space 
  RX-Descriptor 4 + @ hex. space 
  RX-Descriptor 8 + @ hex. space 
  RX-Descriptor 12 + @ hex. cr

  RX-Descriptor @ 16 rshift $3FFF and \ u.
  RX-Puffer-1 printpacket

 \ RX-Puffer-1 dump
 \ RX-Puffer-2 dump
  cr

  own RX-Descriptor !
 -1 EMACRXPOLLD !
;


$400FE000 constant Sysctl-Base

Sysctl-Base $630 + constant RCGCEPHY
Sysctl-Base $69C + constant RCGCEMAC
Sysctl-Base $0B0 + constant RSCLKCFG
Sysctl-Base $07C + constant MOSCCTL
Sysctl-Base $930 + constant PCEPHY
Sysctl-Base $99C + constant PCEMAC

$E000E100 constant en0 ( Interrupt Set Enable )
$E000E104 constant en1 ( Interrupt Set Enable )
$E000E108 constant en2 ( Interrupt Set Enable )
$E000E10C constant en3 ( Interrupt Set Enable )

\ Constants for EMACDMABUSMOD

1 7 lshift constant ATDS

\ Constants for EMACCFG

1 14 lshift constant FES
1 11 lshift constant DUPM
1  3 lshift constant TE
1  2 lshift constant RE

\ Constants for MOSCCTL
1 4 lshift constant OSCRNG

PORTF_BASE $420 + constant PORTF_AFSEL  ( Alternate Function Select )
PORTF_BASE $52C + constant PORTF_PCTL   ( Pin Control )

: init-ethernet ( -- )
  dint
  \ Enable MOSC and use this as system clock:

  OSCRNG MOSCCTL ! \ High range for MOSC

  50 0 do loop \ Wait for clocks to be stable

  3 20 lshift RSCLKCFG ! \ MOSC as oscillator

  \ Enable clock for Ethernet:

  1 RCGCEMAC !
  1 RCGCEPHY !

  50 0 do loop \ Wait for clocks to be stable

  1 PCEMAC ! \ Enable EMAC
  1 PCEPHY ! \ Enable EPHY

  \ Write to the Ethernet MAC DMA Bus Mode (EMACDMABUSMOD) register to set Host bus parameters.

  ." Reset Ethernet" cr
  1 EMACDMABUSMOD ! \ Reset MAC
  begin EMACDMABUSMOD @ 1 and not until \ Wait for Reset to complete
  ." Reset complete" cr
  EMACDMABUSMOD @ ATDS or EMACDMABUSMOD !

  \ Set Ethernet LEDs on Port F:
    $11  PORTF_AFSEL !
  $50005 PORTF_PCTL !

  \ Write to the Ethernet MAC DMA Interrupt Mask Register (EMACDMAIM) register to mask unnecessary interrupt causes.

  \ Radikal erstmal alle Interrupts global aktivieren
  -1 en0 !
  -1 en1 !
  -1 en2 !
  -1 en3 !

  ['] ethernet-handler irq-ethernet !
  RIE NIE or EMACDMAIM ! \ Interrupts: Receive and normal interrupt summary

  mymac 4 + @ EMACADDR0H !
  mymac     @ EMACADDR0L !
  
  \ Create the transmit and receive descriptor lists and then write to the Ethernet MAC Receive
  \ Descriptor List Address (EMACRXDLADDR) register and the Ethernet MAC Transmit
  \ Descriptor List Address (EMACTXDLADDR) register providing the DMA with the starting
  \ address of each list.
  RX-Descriptor EMACRXDLADDR !
  TX-Descriptor EMACTXDLADDR !


  \ Write to the Ethernet MAC Frame Filter (EMACFRAMEFLTR) register, the Ethernet MAC
  \ Hash Table High (EMACHASHTBLH) and the Ethernet MAC Hash Table Low
  \ (EMACHASHTBLL) for desired filtering options.

  0 EMACFRAMEFLTR ! \ no filtering, normal mode

  \ Write to the Ethernet MAC Configuration Register (EMACCFG) to configure the operating
  \ mode and enable the transmit operation.

  3 28 lshift \ saddr=replace with addr0
  FES or  \ 100 Mbps
  DUPM or \ Full Duplex
  TE or   \ Transmitter Enable
  RE or   \ Receiver Enable
  EMACCFG !

  \ Program Bit 15 (PS) and Bit 11 (DM) of the EMACCFG register based on the line status received
  \ or read from the PHY status register after auto-negotiation.

  \ Hardwired to Full-Duplex 100 Mbps here.


  1 13 lshift 2 or EMACDMAOPMODE !

  \ dint
  \ begin emacdmaris @ hex. ?key until 

  \ Write to the EMACCFG register to enable the receive operation.
  \ The Transmit and Receive engines enter the Running state and attempt to acquire descriptors
  \ from the respective descriptor lists. The Receive and Transmit engines then begin processing
  \ Receive and Transmit operations. The Transmit and Receive processes are independent of
  \ each other and can be started or stopped separately.

  \ RE EMACCFG bis!  \ Receiver Enable
  eint
;

\ Die Link-OK LED (D3) leuchtet jetzt, und die TX/RX-Aktivitätsled (D4) blinkert bei Paketen auf der Leitung.
\ Jetzt gibt es allerdings immer noch keine Interrupts für einkommende Pakete.
\ Irgendetwas stimmt noch nicht...

: pi ( -- ) \  Ethernet MAC Receive Frame Count for Good and Bad Frames.
  EMACRXCNTGB @ u.
;

: po ( -- ) EMACTXCNTGB @ u. ;


\ Im normalen Netzwerk mitten im Gewusel ausprobiert:
\ Dieser Zähler wächst brav mit ankommenden Paketen. 

 init-ethernet

