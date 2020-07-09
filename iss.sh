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
N2OVER=$(n2o version | cut -f1 -d' ' | cut -f2- -d-)
machine=$(gforth --version 2>&1 | cut -f3 -d' ')
SF=$(gforth -e 'cell 8 = [IF] ." 64" [THEN] bye')
CYGWIN=cygwin$SF
CYGWIN64=cygwin64
CYGWIN32=cygwin
X64=$(gforth -e 'cell 8 = [IF] ." x64" [THEN] bye')

ln -fs /cygdrive/c/cygwin$(pwd)/lib/gforth/$VERSION/386 lib/gforth/$VERSION/

for m in amd64 386
do
    for i in lib/gforth/$VERSION/$m/libcc-named/*.la
    do
	sed "s/dependency_libs='.*'/dependency_libs=''/g" <$i >$i+
	mv $i+ $i
    done
done

cat <<EOF
; This is the setup script for net2o on Windows
; Setup program is Inno Setup

[Setup]
AppName=net2o
AppVersion=$N2OVER
AppCopyright=Copyright © 2010-2018 Bernd Paysan
DefaultDirName={pf}\net2o
DefaultGroupName=net2o
AllowNoIcons=1
InfoBeforeFile=COPYING
Compression=lzma
DisableStartupPrompt=yes
ChangesEnvironment=yes
OutputBaseFilename=net2o-$N2OVER
AppPublisher=Bernd Paysan
AppPublisherURL=http://fossil.net2o.de/net2o
SignTool=sha1
SignTool=sha256
; add the following sign tools:
; sha1=signtool sign /a /fd sha1 /tr http://timestamp.entrust.net/TSS/RFC3161sha2TS /td sha1 $f
; sha256=signtool sign /a /as /fd sha256 /tr http://timestamp.entrust.net/TSS/RFC3161sha2TS /td sha256 $f
SetupIconFile=net2o.ico
UninstallDisplayIcon={app}\\net2o.ico
ArchitecturesInstallIn64BitMode=$X64

[Messages]
WizardInfoBefore=License Agreement
InfoBeforeLabel=net2o is free software.
InfoBeforeClickLabel=You don't have to accept the GPL to run the program. You only have to accept this license if you want to modify, copy, or distribute this program.

[Components]

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
Name: "{app}\\lib\\gforth\\$VERSION\\amd64\\libcc-named"; Check: Is64BitInstallMode
Name: "{app}\\lib\\gforth\\$VERSION\\386\\libcc-named"; Check: not Is64BitInstallMode
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
$(for i in */.libs/*.dll; do
echo "Source: \"$i\"; DestDir: \"{app}\\..\\gforth\"; Check: Is64BitInstallMode" | tr '/' '\\'
done)
$(for i in */.libs/*.dll; do
echo "Source: \"C:\\cygwin$(pwd)\\$i\"; DestDir: \"{app}\\..\\gforth\"; Check: not Is64BitInstallMode" | tr '/' '\\'
done)
$(ls lib/gforth/$VERSION/amd64/libcc-named/*.la | sed -e 's,^\(..*\)$,Source: "\1"; DestDir: "{app}\\..\\gforth\\lib\\gforth\\'$VERSION'\\amd64\\libcc-named"; Check: Is64BitInstallMode,g' -e 's:/:\\:g')
$(ls lib/gforth/$VERSION/amd64/libcc-named/.libs/*.dll | sed -e 's,^\(..*\)$,Source: "\1"; DestDir: "{app}\\..\\gforth\\lib\\gforth\\'$VERSION'\\amd64\\libcc-named\\.libs"; Check: Is64BitInstallMode,g' -e 's:/:\\:g')
$(ls lib/gforth/$VERSION/386/libcc-named/*.la | sed -e 's,^\(..*\)$,Source: "C:\\cygwin'$(pwd)'\\\1"; DestDir: "{app}\\..\\gforth\\lib\\gforth\\'$VERSION'\\386\\libcc-named"; Check: not Is64BitInstallMode,g' -e 's:/:\\:g')
$(ls lib/gforth/$VERSION/386/libcc-named/.libs/*.dll | sed -e 's,^\(..*\)$,Source: "C:\\cygwin'$(pwd)'\\\1"; DestDir: "{app}\\..\\gforth\\lib\\gforth\\'$VERSION'\\386\\libcc-named\.libs"; Check: not Is64BitInstallMode,g' -e 's:/:\\:g')
Source: "c:\\$CYGWIN64\\bin\\cygstdc++-6.dll"; DestDir: "{app}\\..\\gforth"; Check: Is64BitInstallMode
Source: "c:\\$CYGWIN32\\bin\\cygstdc++-6.dll"; DestDir: "{app}\\..\\gforth"; Check: not Is64BitInstallMode

[Icons]
; Parameter quick reference:
;   "Icon title", "File name", "Parameters", "Working dir (can leave blank)",
;   "Custom icon filename (leave blank to use the default icon)", Icon index
Name: "{group}\net2o"; Filename: "{app}\\..\\gforth\\run.exe"; Parameters: "./env HOME='%HOMEDRIVE%%HOMEPATH%' ../gforth/mintty ../gforth/gforth-fast ./n2o cmd"; WorkingDir: "{app}"; IconFilename: "{app}\\net2o.ico"

[Run]

[UninstallDelete]

[Tasks]

EOF

sed -e 's/$/\r/' <README.md >README.txt
