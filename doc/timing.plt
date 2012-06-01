set terminal wxt font ",12" size 1280, 720
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
plot "timing" using 1:3 title "Requested",\
 "timing" using 1:4 title "Rate",\
 "timing" using 1:2 axis x1y2 title "Slack"

