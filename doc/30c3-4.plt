set terminal pngcairo transparent linewidth 4 font ",24" size 1280, 720
set style data lines
set grid lc rgbcolor "green"
set title "WLAN (+VDSL 50), 1 of 4 connections" textcolor rgbcolor "green"
set xtics textcolor rgbcolor "green"
set ytics textcolor rgbcolor "green"
set y2tics textcolor rgbcolor "green"
set xlabel textcolor rgbcolor "green"
set ylabel textcolor rgbcolor "green"
set y2label textcolor rgbcolor "green"
set yrange [0:8]
set y2range [0:40]
set key textcolor rgbcolor "green"
set border 31 lc rgbcolor "green"
#set style line 1 lc rgbcolor "red"
#set style line 2 lc rgbcolor "cyan"
#set style line 3 lc rgbcolor "blue"
set xlabel "time [s]"
set ylabel "Rate [MB/s]"
set y2label "Slack [ms]"
plot \
 "30c3/wlan-4.1.tmg" using 1:4 title "Requested" lc rgbcolor "blue",\
 "30c3/wlan-4.1.tmg" using 1:5 title "Rate" lc rgbcolor "cyan",\
 "30c3/wlan-4.1.tmg" using 1:3 axis x1y2 title "Slack" lc rgbcolor "red"

