#!/bin/sh
# Start tclsh if run as a shell script \
exec tclsh "$0" ${1+"$@"}

# generate-commands-list.tcl
# This script will read from ChatCommands.cs and generate a command list in
# Textile, for use on the Spacecraft wiki.
# See http://wiki.github.com/SpaceManiac/Spacecraft/commands

# configuration

set inputFilename "ChatCommands.cs"
set outputFilename "commands-list.markdown"

# read in

set infile [open $inputFilename]
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

set outfile [open $outputFilename w]
puts $outfile {
Spacecraft supports several commands. See [[User Ranks]] for more information on ranks.

(Note: this is not always up-to-date, but is generated directly from the in-game help)
}

foreach level {Guest Builder Mod Admin} {
	puts $outfile "h1. $level Commands"
	foreach cmd [lsort $commandList($level)]  {
		puts $outfile "* /$cmd - $commandHelp($cmd)"
	}
	puts $outfile ""
}

close $outfile