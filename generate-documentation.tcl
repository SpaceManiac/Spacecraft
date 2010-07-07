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
	if {[lsearch $canGen [string range $arg 1 end]] >= 0} {
		set genAll 0
		lappend gen [string range $arg 1 end]
	} else {
		puts "Unknown flag $arg, must be one of -[join $canGen " -"]"
	}
}

if {$genAll} {
	set gen $canGen
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

puts -nonewline "Getting file list... "
examine . {.cs$}
puts "Done"

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

	close $outfile
}

# gen_conf procedure ---------------
# generate Configuration.textile (-conf)

proc gen_conf {} {
	puts "Generating Configuration.textile..."
	global outputFolder files
	
	# known option descriptions
	array set configInfo {
		server-name {The name of the server, as shown on the server list and the loading screen (64 characters or less).}
		motd {The message to display on the loading screen to incoming players (64 characters or less).}
		port {The port on which to listen for incoming Minecraft connections.}
		public {Whether the server is publicly listed on the Minecraft "server list":http://minecraft.net/servers.jsp.}
		max-players {The number of players the server will allow to be connected at once (no effect at the moment).}
		verify-names {Whether to verify the names of incoming players with Minecraft.net and reject unverified names. Except in special cases, this should always be on.}
		heartbeat {Whether to announce the server's presence to Minecraft.net.}
		width {The default X size of flatgrass maps to be generated.}
		height {The default Y (vertical) size of flatgrass maps to be generated.}
		depth {The default Z size of flatgrass maps to be generated.}
		random-seed {The random seed for anything random Spacecraft does. If this is set to -1, a seed will be auto-generated by the OS.}
		
		http-port {The port on which to listen for HTTP connections (to grab server stats and do remote control). Set to 0 to disable.}
		http-username {The username for HTTP authentication. CHANGE THIS!}
		http-password {The password for HTTP authentication. CHANGE THIS!}
	}
		
	
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

	set outfile [open [file join $outputFolder Configuration.textile] w]
	puts $outfile "When Spacecraft starts up, it looks for the the file @properties.txt@ in the local directory. This file contains user-configurable settings for Spacecraft, specified in the format @name = value@. Lines beginning with the hash symbol @#@ are treated as comments and ignored, as are any invalid names. If there isn't a @properties.txt@, Spacecraft will generate a default one. The full list of valid configuration names follows."
	puts $outfile ""
	puts $outfile "table."
	puts $outfile "|*Name*|*Type*|*Information*|*Default*|"
	
	foreach name [lsort [array names configOptsFound]] {
		set list $configOptsFound($name)
		
		set type [lindex $list 0]
		set default [lindex $list 1]
		set info [lindex $list 2]
		
		puts $outfile "|$name|$type|$info|$default|"
	}
	puts $outfile ""
	puts $outfile "For booleans, Spacecraft will accept \"1\", \"yes\", \"true\", and \"on\" as true. Everything else is considered false."
	puts $outfile ""
	close $outfile
}

# perform the generation ---------------

foreach item $gen {
	gen_$item
}