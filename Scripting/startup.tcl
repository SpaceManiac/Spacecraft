# startup.tcl

# quick-for
proc qf {var from to code} {
	uplevel 1 [format {
		for {set %s %d} {$%s <= %d} {incr %s} {
			%s
		}
	} $var $from $var $to $var $code]
}

# quick-source
proc qs {f} {
	source [file join .. .. Scripting $f]
	puts ""
}

proc cmdQs {sender args} {
	if {[llength $args] == 0} { return }
	qs [lindex $args 0]
	tell $sender "[getColorCode privatemsg][lindex $args 0] quicksourced"
}

createChatCommand source Admin "Quicksource the given script file" cmdQs

# load other files
qs testcmd.tcl
qs bother.tcl