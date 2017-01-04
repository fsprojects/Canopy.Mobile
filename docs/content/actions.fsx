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
<< (write)
----------
Write text to element.
*)
"#firstName" << "Alex"
//if you dont like the << syntax you can use the write function:
write "#firstName" "Alex"