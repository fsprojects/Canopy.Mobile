(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/Canopy.Mobile"
#r "canopy.mobile.dll"
open canopy.mobile

(**
Selectors
=========

canonpy.mobile supports all Appium selectors and adds some conventions on top that allow easier access.
*)

(**
Finding elements by id
----------------------

The # sign allows to find elements by it's resource-id.
*)
find "#MyElementID"

(**
Finding elements by text
----------------------

canopy.mobile provides conventions to find elements when the displayed text of an element is known. 
*)
find "tv:Some displayed text" // looks for textviews with the text
find "edit:Some entered text" // looks for edit fields with the text
find "some text" // looks for any element with the text

(**

Selector functions
==================
*)

(**
find
----
Returns the first element that matches the given selector.
*)
find "tv:Test"

(**
findAll
-------
Returns a list with all elements that matches the given selector.
*)
findAll "tv:Test"

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