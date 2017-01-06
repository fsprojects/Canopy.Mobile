(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/Canopy.Mobile"
#r "canopy.mobile.dll"
open canopy.mobile

(**
Actions
========================
*)

(**
start
----
Starts a new emulator session with the given apk.
*)
start DefaultAndroidSettings "myApp.apk"

(**
quit
----
Quit the current emulator session.
*)
quit ()

(**
<< (write)
----------
Writes text to element.
*)
"#firstName" << "Alex"
//if you dont like the << syntax you can use the write function:
write "#firstName" "Alex"

(**
read
----
Reads the text of an element.
*)
let firstName = read "#firstName"
let linkText = read "#someLink"

(**
click
-----
Clicks an element via selector.
*)
click "#login"
click "menu-tv:search"

(**
clickAndWait
------------
Clicks an element via selector and waits for another selector to appear.
*)
clickAndWait "#login" "tv:password"

(**
back
----
Clicks the android back button.
*)
back ()

(**
backAndWait
-----------
Clicks the android back button and waits for a selector to appear.
*)
backAndWait "#login"

(**
landscape
---------
Sets the orientation to landscape
*)
landscape()

(**
portrait
--------
Sets the orientation to portrait
*)
portrait()