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
	uplevel #0 "source \[file join .. .. Scripting $f\]"
}

proc cmdQs {sender args} {
	if {[llength $args] == 0} { return }
	qs [lindex $args 0]
	tell $sender "[getColorCode privatemsg][lindex $args 0] quicksourced"
}

createChatCommand "qsrc" Admin "Quicksource the given script file" cmdQs

# lremove

proc lremove args {
	array set opts {-all 0 pattern -exact}
	while {[string match -* [lindex $args 0]]} {
	switch -glob -- [lindex $args 0] {
		-a*	{ set opts(-all) 1 }
		-g*	{ set opts(pattern) -glob }
		-r*	{ set opts(pattern) -regexp }
		--	{ set args [lreplace $args 0 0]; break }
		default {return -code error "unknown option \"[lindex $args 0]\""}
	}
	set args [lreplace $args 0 0]
	}
	set l [lindex $args 0]
	foreach i [join [lreplace $args 0 0]] {
	if {[set ix [lsearch $opts(pattern) $l $i]] == -1} continue
	set l [lreplace $l $ix $ix]
	if {$opts(-all)} {
		while {[set ix [lsearch $opts(pattern) $l $i]] != -1} {
		set l [lreplace $l $ix $ix]
		}
	}
	}
	return $l
}

# add Tcl event loop update hook
# useful for the "after" command

onWorldTick update
qs levelgen.tcl
qs spelunker.tcl