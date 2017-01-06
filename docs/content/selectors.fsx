(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/Canopy.Mobile"
#r "canopy.mobile.dll"
open canopy.mobile

(**
Selectors
=========
*)

(**
nth
---
Gets the nth element with the given selector. Index starts at 1.
*)
nth "tv:Test"

(**
first
-----
Gets the first element with the given selector.
*)
first "tv:Test"

(**
last
----
Gets the last element with the given selector.
*)
last "tv:Test"