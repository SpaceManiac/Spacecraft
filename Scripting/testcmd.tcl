# /testTcl command

proc testCommand {name args} {
	broadcast "Trololol, $name called the test command"
}

createChatCommand testTcl Builder "This is the help for testTcl" testCommand