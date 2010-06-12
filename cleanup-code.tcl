#!/bin/sh
# Start tclsh if run as a shell script \
exec tclsh -f "$0" ${1+"$@"}

# cleanup-code.tcl
# This script will walk through each .cs and each .tcl file in the working
# directory and any subdirectories and do some basic cleanup:
#  a. convert quadruple-spaces used for indentation into tab stops
#  b. remove any whitespace at the ends of non-blank lines
#  c. convert multiple blank lines in a row to just one

# command-line argument?

set listmode 0
if {[lindex $argv 0] == "-l"} {
	puts "Listing files only..."
	set listmode 1
}

# get file list

set files [list]

proc examine {directory} {
	global files
	foreach filename [glob -nocomplain -directory $directory *] {
		if {[file isdirectory $filename]} {
			examine $filename
		} elseif {[regexp -nocase {(.cs|.tcl)$} $filename]} {
			lappend files $filename
		}
	}
}

examine .

# procedures for later

proc grabfile {filename} {
	set f [open $filename]
	set r [read $f]
	close $f
	return $r
}

proc putfile {filename contents {mode w}} {
	set f [open $filename $mode]
	puts -nonewline $f $contents
	close $f
}

# cleanup time

proc cleanup {contents} {
	set cleaned ""
	
	foreach line [split $contents \n] {
		# convert quadruple-spaces at the line's start
		regsub -all {^\s*    } $line \t line
		
		if {![regexp {^\t*$} $line]} {
			# if not whitespace-only, remove whitespace at the end
			regsub -all {\t+$} $line line
		}
		
		append cleaned "$line\n"
	}
	
	# remove extra newline added by loop
	set cleaned [string range $cleaned 0 end-1]
	
	# shrink multiple blank lines in a row
	regsub -all {\n\n+} $cleaned \n\n cleaned
	
	return $cleaned
}

set cleanedfiles [list]

foreach filename $files {
	set contents [grabfile $filename]
	set cleaned [cleanup $contents]
	if {$cleaned != $contents} {
		lappend cleanedfiles $filename
		if {$listmode} {
			puts $filename
		} else {
			putfile $filename $cleaned
		}
	}
}

if {$listmode} {
	puts "Files:    [llength $files]"
	puts "To clean: [llength $cleanedfiles]"
} else {
	puts "Files:   [llength $files]"
	puts "Cleaned: [llength $cleanedfiles]"
}
