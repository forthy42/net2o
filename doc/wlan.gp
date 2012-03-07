set terminal pngcairo transparent linewidth 4 font ",24" size 1280, 720
set style data lines
set grid lc rgbcolor "green"
set title "WLAN transfer" textcolor rgbcolor "green"
set xtics textcolor rgbcolor "green"
set ytics textcolor rgbcolor "green"
set key textcolor rgbcolor "green"
set border 31 lc rgbcolor "green"
set style line 1 lc rgbcolor "red"
set style line 2 lc rgbcolor "#00ffff"
set style line 3 lc rgbcolor "blue"
plot "slack-wlan" using ($1 / 1000000) title "slack (ms)" linestyle 1,\
"rate-wlan" using (32768000 / $1) title "rate (MB/s)" linestyle 2,\
"rate-send-wlan" using (32768000 / $1) title "req. rate (MB/s)" linestyle 3
quit
