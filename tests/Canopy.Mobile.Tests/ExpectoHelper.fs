module Canopy.Mobile.ExpectoHelper

open Expecto
open canopy.mobile
open canopy.mobile.core

let screenShotDir = "../../../../temp/screenshots"

let testCase name fn =
    testCase 
        name
        (fun () -> 
            try 
                fn ()
            with 
            | _ -> 
                screenshot screenShotDir (name + ".png")
                reraise())