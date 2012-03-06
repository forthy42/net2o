set terminal pdfcairo
set style data lines
set title "WLAN transfer"
plot "slack-wlan" using ($1 / 1000000) title "slack (ms)",\
"rate-wlan" using (32768000 / $1) title "rate (MB/s)",\
"rate-send-wlan" using (32768000 / $1) title "req. rate (MB/s)"
quit