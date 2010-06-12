#!/bin/sh
# Start tclsh if run as a shell script \
exec tclsh -f "$0" ${1+"$@"}

# cleanup-code.tcl
# This script will walk through each .cs and each .tcl file in the working
# directory and any subdirectories and do some basic cleanup:
#  a. convert quadruple-spaces used for indentation into tab stops
#  b. remove any whitespace at the ends of non-blank lines
#  c. convert multiple blank lines in a row to just one
# If the -l flag is used, this script will only list files requiring cleanup
# and not perform the cleanup itself. If the -t flag is used, this script will
# list TODO lines in code files.


# command-line arguments

set listmode 0
set todomode 0

foreach arg $argv {
	if {$arg == "-l"} {
		set listmode 1
	} elseif {$arg == "-t"} {
		set todomode 1
	} else {
		puts "Unknown flag $arg"
	}
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

proc nodotslash {filename} {
	if {[string match "./*" $filename]} {
		return [string range $filename 2 end]
	}
	return $filename
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

proc todo {filename contents} {
	global todos
	
	set linenum 1
	foreach line [split $contents \n] {
		if {[regexp {(//|/*|#)\s*TODO:?\s+(.+)$} $line match comment info]} {
			lappend todos "$filename\($linenum): $info"
		}
		incr linenum
	}
}

set cleanedfiles [list]
set todos [list]

foreach filename $files {
	set filename [nodotslash $filename]
	
	set contents [grabfile $filename]
	set cleaned [cleanup $contents]
	if {$cleaned != $contents} {
		lappend cleanedfiles $filename
		if {!$listmode} {
			putfile $filename $cleaned
		}
	}
	if {$todomode} {
		todo $filename $contents
	}
}

puts "Total files: [llength $files]"

if {[llength $cleanedfiles] > 0} {
	if {$listmode} {
		puts "Files requiring cleanup:"
	} else {
		puts "Files cleaned:"
	}
	
	foreach file $cleanedfiles {
		puts "  $file"
	}
} else {
	puts "No files required cleanup"
}

if {$todomode} {
	if {[llength $todos] > 0} {
		puts "Found [llength $todos] TODOs:"
		foreach todo $todos {
			puts "  $todo"
		}
	} else {
		puts "Found no TODOs"
	}
}