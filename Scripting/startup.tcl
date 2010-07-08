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
proc qs {f} { source [file join Scripting $f] }

# load other files
qs testcmd.tcl
qs bother.tcl