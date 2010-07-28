#!/bin/sh
# Start tclsh if run as a shell script \
exec tclsh "$0" ${1+"$@"}

# copy-scripts.tcl
# This script will copy all .tcl files in the Scripting subdirectory into
# the folder specified on the command line.

set dest $argv

catch {file mkdir $dest}

foreach f [glob -nocomplain Scripting/*.tcl] {
	file delete -force -- [file join $dest [file tail $f]]
	file copy $f $dest
}