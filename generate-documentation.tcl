#!/bin/sh
# Start tclsh if run as a shell script \
exec tclsh "$0" ${1+"$@"}

# generate-documentation.tcl
# This script will read from Spacecraft source code and generate documentation
# in Textile for use on the wiki and inclusion in releases.
#
# A list of files and their associated command-line arguments:
#  1. Commands.textile (-cmd): Command list based on in-game help
#  2. Configuration.textile (-conf): Configuration options based on usage
# If no command-line arguments are specified, all files are generated.
#
# http://wiki.github.com/SpaceManiac/Spacecraft/

set outputFolder "Docs"

# command-line arguments ---------------

set genAll 1
set gen [list]
set canGen [list cmd conf]

foreach arg $argv {
	if {lsearch $canGen [string range $arg 1 end]} {
		set genAll 0
		lappend gen [string range $arg 1 end]
	} else {
		puts "Unknown flag $arg, must be one of -[join $canGen " -"]"
	}
}

if {$genAll} {
	set gen $canGen
}

# gen_cmd procedure ---------------
# generate Commands.textile (-cmd)

proc gen_cmd {} {
	puts -nonewline "Generating Commands.textile... "
	global outputFolder
	
	# read in
	set infile [open "ChatCommands.cs"]
	set input [read $infile]
	close $infile

	# get command list

	puts "reading command list"
	array set commands {}

	set i [string first "Commands.Add(" $input]
	while {$i != -1} {
		set i2 [string first "())" $input $i]
		set line [string range $input [expr {$i + [string length "Commands.Add("]}] [expr {$i2 - 1}]]

		set i3 [string first "\"" $line]
		set i4 [string first "\"" $line [expr {$i3 + 1}]]
		set name [string range $line [expr {$i3 + 1}] [expr {$i4 - 1}]]
		
		set i5 [string first "new ChatCommands." $line]
		set class [string range $line [expr {$i5 + [string length "new ChatCommands."]}] end]
		
		set commands($name) $class
		
		set i [string first "Commands.Add(" $input [expr {$i + 1}]]
	}

	puts "commands: [join [lsort [array names commands]] ", "]"

	# get help and level for each command

	array set commandList {
		Guest {}
		Builder {}
		Mod {}
		Admin {}
	}

	array set commandHelp {}

	foreach cmd [array names commands] {
		set i [string first "public class $commands($cmd)" $input]
		set i2 [string first "return Rank." $input $i]
		set i3 [string first ";" $input $i2]
		set rank [string range $input [expr {$i2 + [string length "return Rank."]}] [expr {$i3 - 1}]]
		lappend commandList($rank) $cmd
		
		set i2 [string first "return \"" $input $i]
		set i3 [string first "\"" $input [expr {$i2 + [string length "return \""]}]]
		set help [string range $input [expr {$i2 + [string length "return \""]}] [expr {$i3 - 1}]]
		
		if {[string match "/$cmd: *" $help]} {
			set help [string range $help [string length "/$cmd: "] end]
		}
		
		set commandHelp($cmd) $help
	}

	# write out

	set outfile [open [file join $outputFolder Commands.textile] w]
	puts $outfile "Spacecraft supports several commands for building and management. See [[User Ranks]] for more information on ranks."
	puts $outfile ""

	foreach level {Guest Builder Mod Admin} {
		puts $outfile "h1. $level Commands"
		foreach cmd [lsort $commandList($level)]  {
			puts $outfile "* /$cmd - $commandHelp($cmd)"
		}
		puts $outfile ""
	}

	close $outfile
	puts "Done"
}

# gen_conf procedure ---------------
# generate Configuration.textile (-conf)

proc gen_conf {} {
	puts -nonewline "Generating Configuration.textile... "
	global outputFolder
	
	# TODO: generate configuration info
	
	puts "Done"
}

# perform the generation ---------------

foreach item $gen {
	gen_$gen
}