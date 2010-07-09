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
}

proc cmdQs {sender args} {
	if {[llength $args] == 0} { return }
	qs [lindex $args 0]
	tell $sender "[getColorCode privatemsg][lindex $args 0] quicksourced"
}

createChatCommand "qsrc" Admin "Quicksource the given script file" cmdQs

# add Tcl event loop update hook
# useful for the "after" command

onWorldTick update