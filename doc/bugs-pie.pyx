set palette yellow, red, blue, orange, green
set width 6
set terminal pdf color
set preamble r"\renewcommand{\familydefault}{\sfdefault}"
set output "bugs-pie.pdf"
piechart 'bugs-pie.dat' using $1 label key "%s"%($2)
