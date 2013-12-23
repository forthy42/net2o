set terminal qt font ",8" size 1280, 640
set style data lines
set grid #lc rgbcolor "green"
set title "Transfer" #textcolor rgbcolor "green"
#set xtics textcolor rgbcolor "green"
#set ytics textcolor rgbcolor "green"
set y2tics #textcolor rgbcolor "green"
#set xlabel textcolor rgbcolor "green"
#set ylabel textcolor rgbcolor "green"
#set y2label textcolor rgbcolor "green"
#set key textcolor rgbcolor "green"
#set border 31 lc rgbcolor "green"
set style line 1 lc rgbcolor "red" lw 4
set style line 2 lc rgbcolor "#00ffff" lw 4
set style line 3 lc rgbcolor "blue" lw 4
set xlabel "time [s]"
set ylabel "Rate [MB/s]"
set y2label "Slack [ms]"
set multiplot layout 2, 2 rowsfirst
#set term wxt 0
plot "timing4" using 1:4 title "Requested",\
 "timing4" using 1:5 title "Rate",\
 "timing4" using 1:3 axis x1y2 title "Slack", \
 "timing4" using 1:6 axis x1y2 title "Grow"
#set term wxt 1
plot "timing1" using 1:4 title "Requested",\
 "timing1" using 1:5 title "Rate",\
 "timing1" using 1:3 axis x1y2 title "Slack", \
 "timing1" using 1:6 axis x1y2 title "Grow"
#set term wxt 2
plot "timing2" using 1:4 title "Requested",\
 "timing2" using 1:5 title "Rate",\
 "timing2" using 1:3 axis x1y2 title "Slack", \
 "timing2" using 1:6 axis x1y2 title "Grow"
#set term wxt 3
plot "timing3" using 1:4 title "Requested",\
 "timing3" using 1:5 title "Rate",\
 "timing3" using 1:3 axis x1y2 title "Slack", \
 "timing3" using 1:6 axis x1y2 title "Grow"
#unset multiplot