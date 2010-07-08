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

set uploadMode 0
set genAll 1
set gen [list]
set canGen [list cmd conf tcl]

foreach arg $argv {
	if {[lsearch $canGen [string range $arg 1 end]] >= 0} {
		set genAll 0
		lappend gen [string range $arg 1 end]
	} else if {$arg == "-upload"} {
		set uploadMode 1
	} else {
		puts "Unknown flag $arg, must be one of -upload [join $canGen " -"]"
	}
}

if {$genAll} {
	set gen $canGen
} elseif {$uploadMode} {
	set gen [list]
}

# grab file list for whatever needs it

set files [list]

proc nodotslash {filename} {
	if {[string match "./*" $filename]} {
		return [string range $filename 2 end]
	}
	return $filename
}

proc examine {directory {pattern (.cs|.tcl)$}} {
	global files
	foreach filename [glob -nocomplain -directory $directory *] {
		if {[file isdirectory $filename]} {
			examine $filename $pattern
		} elseif {[regexp -nocase $pattern $filename]} {
			lappend files [nodotslash $filename]
		}
	}
}

puts "Getting file list..."
examine . {.cs$}

# gen_cmd procedure ---------------
# generate Commands.textile (-cmd)

proc gen_cmd {} {
	puts "Generating Commands.textile..."
	global outputFolder
	
	# read in
	set infile [open "ChatCommands/ChatCommandHandling.cs"]
	set input1 [read $infile]
	close $infile
	
	set input2 ""
	set files [glob -nocomplain "ChatCommands/*.cs"]
	foreach file $files {
		set infile [open $file]
		append input2 "[read $infile]\n"
		close $infile
	}

	# get command list (input1)
	
	array set commands {}

	set i [string first "Commands.Add(" $input1]
	while {$i != -1} {
		set i2 [string first "())" $input1 $i]
		set line [string range $input1 [expr {$i + [string length "Commands.Add("]}] [expr {$i2 - 1}]]

		set i3 [string first "\"" $line]
		set i4 [string first "\"" $line [expr {$i3 + 1}]]
		set name [string range $line [expr {$i3 + 1}] [expr {$i4 - 1}]]
		
		set i5 [string first "new ChatCommands." $line]
		set class [string range $line [expr {$i5 + [string length "new ChatCommands."]}] end]
		
		set commands($name) $class
		
		set i [string first "Commands.Add(" $input1 [expr {$i + 1}]]
	}

	# get help and level for each command

	array set commandList {
		Guest {}
		Builder {}
		Mod {}
		Admin {}
	}

	array set commandHelp {}

	foreach cmd [array names commands] {
		set i [string first "public class $commands($cmd)" $input2]
		set i2 [string first "return Rank." $input2 $i]
		set i3 [string first ";" $input2 $i2]
		set rank [string range $input2 [expr {$i2 + [string length "return Rank."]}] [expr {$i3 - 1}]]
		lappend commandList($rank) $cmd
		
		set i2 [string first "return \"" $input2 $i]
		set i3 [string first "\"" $input2 [expr {$i2 + [string length "return \""]}]]
		set help [string range $input2 [expr {$i2 + [string length "return \""]}] [expr {$i3 - 1}]]
		
		if {[string match "/$cmd: *" $help]} {
			set help [string range $help [string length "/$cmd: "] end]
		}
		
		set commandHelp($cmd) $help
	}

	# write out

	set outfile [open [file join $outputFolder Commands.textile] w]
	puts $outfile "Spacecraft supports several commands for building and management. See \[\[User Ranks]] for more information on ranks."
	puts $outfile ""

	foreach level {Guest Builder Mod Admin} {
		puts $outfile "h1. $level Commands"
		foreach cmd [lsort $commandList($level)]  {
			puts $outfile "* /$cmd - $commandHelp($cmd)"
		}
		puts $outfile ""
	}

	puts $outfile ""
	puts $outfile "This documentation was auto-generated based on Spacecraft's source."
	close $outfile
}

# gen_conf procedure ---------------
# generate Configuration.textile (-conf)

proc gen_conf {} {
	puts "Generating Configuration.textile..."
	global outputFolder files
	
	# sets configInfo
	source [file join $outputFolder configInfo.dat]
	
	# known types
	array set configTypes {
		"" "String"
		"Int" "Integer"
		"Bool" "Boolean"
	}
	
	array set configOptsFound {}
	
	foreach filename $files {
		if {$filename == [file join Utils Config.cs]} {
			continue
		}
		
		set f [open $filename]
		set contents [read $f]
		close $f; unset f
		
		set i [string first "Config.Get" $contents]
		while {$i != -1} {
			set i2 [string first "(" $contents $i]
			set type [string range $contents [expr {$i + 10}] [expr {$i2 - 1}]]
			set type $configTypes($type)
			
			set i3 [string first ")" $contents $i]
			set params [split [string range $contents [expr {$i2 + 1}] [expr {$i3 - 1}]] ","]
			
			set name [string trim [lindex $params 0]]
			set i2 [string first "\"" $name]
			set i3 [string last "\"" $name]
			set name [string range $name [expr {$i2 + 1}] [expr {$i3 - 1}]]
			
			if {$name == ""} {
				break
			}
			
			set default [string trim [lindex $params 1]]
			
			if {[info exists configInfo($name)]} {
				set info $configInfo($name)
			} else {
				puts "  Warning: no information on configuration key $name"
				set info ""
			}
			
			set configOptsFound($name) [list $type $default $info]
			
			incr i
			set i [string first "Config.Get" $contents $i]
		}
	}
	
	set infile [open [file join $outputFolder Configuration.textile.proto]]
	set prototype [read $infile]
	close $infile
	
	set outfile [open [file join $outputFolder Configuration.textile] w]
	puts $outfile $prototype
	puts $outfile "|*Name*|*Type*|*Information*|*Default*|"
	
	foreach name [lsort [array names configOptsFound]] {
		set list $configOptsFound($name)
		
		set type [lindex $list 0]
		set default [lindex $list 1]
		set info [lindex $list 2]
		
		puts $outfile "|*$name*|$type|$info|$default|"
	}
	puts $outfile ""
	puts $outfile "For booleans, Spacecraft will accept \"1\", \"yes\", \"true\", and \"on\" as true. Everything else is considered false."
	puts $outfile "This documentation was auto-generated based on Spacecraft's source."
	close $outfile
}

# gen_tcl procedure ---------------
# generate Scripting.textile (-tcl)

proc gen_tcl {} {
	puts "Generating Scripting.textile..."
	global outputFolder files
	
	set infile [open "Scripting/ScriptHandler.cs"]
	set contents [read $infile]
	close $infile
	
	set numSections 2
	
	for {set i 1} {$i <= $numSections} {incr i} {
		set ind1 [string first "// $i." $contents]
		set sections(start,$i) $ind1
		set ind2 [string first "\n" $contents $ind1]
		set sections(title,$i) [string range $contents [expr {$ind1 + 6}] [expr {$ind2 - 1}]]
		set sections(commands,$i) [list]
	}
	
	array set commands {}
	
	set i [string first "Interpreter.CreateCommand(" $contents]
	while {$i != -1} {
		set i2 [string first "\"" $contents $i]
		set i3 [string first "\"" $contents [expr {$i2 + 1}]]
		set name [string range $contents [expr {$i2 + 1}] [expr {$i3 - 1}]]
		
		set i4 [string first "(" $contents $i3]
		set i5 [string first ")" $contents $i4]
		set cmd [string range $contents [expr {$i4 + 1}] [expr {$i5 - 1}]]
		
		set sec 0
		for {set s 1} {$s <= $numSections} {incr s} {
			if {$i >= $sections(start,$s)} {
				set sec $s
			}
		}
		if {$sec > 0} {
			lappend allcommands $name
			lappend sections(commands,$sec) $name
			set commands($name) $cmd
		}
		
		set i [string first "Interpreter.CreateCommand(" $contents [expr {$i + 1}]]
	}
	
	array set commandSyntax {}
	array set commandHelp {}
	
	foreach {cmdName csCmd} [array get commands] {
		set i [string first "static int $csCmd" $contents]
		set i2 [string first "// syntax:" $contents $i]
		set i3 [string first "string" $contents $i]
		if {$i2 != -1 && $i2 < $i3} {
			set i4 [string first "\n" $contents $i2]
			set syntax [string range $contents [expr {$i2 + 11}] [expr {$i4 - 1}]]
			set commandSyntax($cmdName) $syntax
		}
		set i2 [string first "// help:" $contents $i]
		if {$i2 != -1 && $i2 < $i3} {
			set i4 [string first "\n" $contents $i2]
			set help [string range $contents [expr {$i2 + 9}] [expr {$i4 - 1}]]
			set commandHelp($cmdName) $help
		}
	}
	
	set infile [open [file join $outputFolder Scripting.textile.proto]]
	set prototype [read $infile]
	close $infile
	
	set outfile [open [file join $outputFolder Scripting.textile] w]
	puts $outfile $prototype
	
	for {set sec 1} {$sec <= $numSections} {incr sec} {
		puts $outfile "h2. $sections(title,$sec)"
		puts $outfile ""
		puts $outfile "table."
		puts $outfile "|*Name*|*Syntax*|*Information*|"
		foreach {commandName} $sections(commands,$sec) {
			set syntax "_No syntax information available._"
			set help "_No help available._"
			if {[info exists commandSyntax($commandName)]} {
				set syntax $commandSyntax($commandName)
			}
			if {[info exists commandHelp($commandName)]} {
				set help $commandHelp($commandName)
			}
			puts $outfile "|*$commandName*|$syntax|$help|"
		}
		puts $outfile ""
	}
	
	puts $outfile ""
	puts $outfile "This documentation was auto-generated based on Spacecraft's source."
	close $outfile
}

# gen_tcl procedure ---------------
# generate Scripting.textile (-tcl)

proc upload {} {
	puts "Upload mode coming soon"
}

# perform the generation ---------------

if {$uploadMode} {
	upload
} else {
	foreach item $gen {
		gen_$item
	}
}