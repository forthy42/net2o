set terminal wxt font ",12" size 640, 360
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
set term wxt 0
plot "timing1" using 2:4 title "Requested", "timing2" using 2:4 title "Requested", "timing3" using 2:4 title "Requested", "timing0" using 2:4 title "Requested"
set term wxt 1
plot "timing1" using 2:5 title "Rate", "timing2" using 2:5 title "Rate", "timing3" using 2:5 title "Rate", "timing0" using 2:5 title "Rate"
set term wxt 2
plot "timing1" using 2:3 axis x1y2 title "Slack", "timing2" using 2:3 axis x1y2 title "Slack", "timing3" using 2:3 axis x1y2 title "Slack", "timing0" using 2:3 axis x1y2 title "Slack"
set term wxt 3
plot "timing1" using 2:6 axis x1y2 title "Grow", "timing2" using 2:6 axis x1y2 title "Grow", "timing3" using 2:6 axis x1y2 title "Grow", "timing0" using 2:6 axis x1y2 title "Grow"

