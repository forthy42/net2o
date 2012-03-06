set terminal pdfcairo
set style data lines
set title "VDSL transfer"
plot "slack-vdsl" using ($1 / 1000000) title "slack (ms)",\
"rate-vdsl" using (32768000 / $1) title "rate (MB/s)",\
"rate-send-vdsl" using (32768000 / $1) title "req. rate (MB/s)"
quit