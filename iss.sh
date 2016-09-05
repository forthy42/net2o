#!/bin/bash

#Copyright (C) 2016 Bernd Paysan

#This file is part of net2o

#This program is free software: you can redistribute it and/or modify
#it under the terms of the GNU Affero General Public License as published by
#the Free Software Foundation, either version 3 of the License, or
#(at your option) any later version.

#This program is distributed in the hope that it will be useful,
#but WITHOUT ANY WARRANTY; without even the implied warranty of
#MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#GNU Affero General Public License for more details.

#You should have received a copy of the GNU Affero General Public License
#along with this program.  If not, see <http://www.gnu.org/licenses/>.

# use iss.sh >net2o.iss
# copy the resulting *.iss to the location of your Windows installation
# of net2o, and start the setup compiler there.

VERSION=$(gforth --version 2>&1 | cut -f2 -d' ')
N2OVER=$(n2o version | cut -f3 -d' ')
machine=$(gforth --version 2>&1 | cut -f3 -d' ')
SF=$(gforth -e 'cell 8 = [IF] ." 64" [THEN] bye')
CYGWIN=cygwin$SF

for i in lib/gforth/$VERSION/$machine/libcc-named/*.la
do
    sed "s/dependency_libs='.*'/dependency_libs=''/g" <$i >$i+
    mv $i+ $i
done

cat <<EOF
; This is the setup script for net2o on Windows
; Setup program is Inno Setup

[Setup]
AppName=net2o$SF
AppVersion=$N2OVER
AppCopyright=Copyright © 2010-2016 Bernd Paysan
DefaultDirName={pf}\net2o$SF
DefaultGroupName=net2o$SF
AllowNoIcons=1
InfoBeforeFile=COPYING
Compression=lzma
DisableStartupPrompt=yes
ChangesEnvironment=yes
OutputBaseFilename=net2o$SF-$N2OVER
AppPublisher=Bernd Paysan
AppPublisherURL=http://fossil.net2o.de/net2o

[Messages]
WizardInfoBefore=License Agreement
InfoBeforeLabel=Gforth is free software.
InfoBeforeClickLabel=You don't have to accept the GPL to run the program. You only have to accept this license if you want to modify, copy, or distribute this program.

[Components]
Name: "help"; Description: "HTML Documentation"; Types: full
Name: "info"; Description: "GNU info Documentation"; Types: full
Name: "print"; Description: "Postscript Documentation for printout"; Types: full
Name: "objects"; Description: "Compiler generated intermediate stuff"; Types: full

[Dirs]
$(make distfiles | tr ' ' '\n' | (while read i; do
  while [ ! -z "$i" ]
  do
    if [ -d $i ]; then echo $i; fi
    if [ "${i%/*}" != "$i" ]; then i="${i%/*}"; else i=""; fi
  done
done) | sort -u | sed \
  -e 's:/:\\:g' \
  -e 's,^\(..*\)$,Name: "{app}\\\1",g')
Name: "{app}\\..\\gforth$SF\\lib\\gforth\\$VERSION\\$machine\\libcc-named"
Name: "{app}\\..\\bin"
Name: "{app}\\..\\tmp"; Permissions: users-modify

[Files]
; Parameter quick reference:
;   "Source filename", "Dest. filename", Copy mode, Flags
Source: "README.txt"; DestDir: "{app}"; Flags: isreadme
EOF

cat <<EOF
$(make distfiles | tr ' ' '\n' | (while read i; do
  if [ ! -d $i ]; then echo $i; fi
done) | sed \
  -e 's:/:\\:g' \
  -e 's,^\(..*\)\\\([^\\]*\)$,Source: "\1\\\2"; DestDir: "{app}\\\1",g' \
  -e 's,^\([^\\]*\)$,Source: "\1"; DestDir: "{app}",g')
Source: "net2o.ico"; DestDir: "{app}"

[Icons]
; Parameter quick reference:
;   "Icon title", "File name", "Parameters", "Working dir (can leave blank)",
;   "Custom icon filename (leave blank to use the default icon)", Icon index
Name: "{group}\net2o"; Filename: "{app}\\..\\gforth$SF\\run.exe"; Parameters: "./env HOME='%HOMEDRIVE%%HOMEPATH%' ../gforth$SF/mintty ../gforth$SF/gforth-fast ./n2o cmd"; WorkingDir: "{app}"; IconFilename: "{app}\\net2o.ico"

[Run]

[UninstallDelete]

[Tasks]

end;

EOF

sed -e 's/$/\r/' <README >README.txt
