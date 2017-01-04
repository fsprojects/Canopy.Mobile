/// Contains canopy specifc wait helpers.
module canopy.mobile.Wait

open OpenQA.Selenium
open canopy.mobile.Exceptions

let mutable waitSleep = 0.5

//This is similar to how Selenium Wait works which is very useful
//Provide a function that returns a bool or object and if its true or not null then stop waiting
let waitResults timeout (f : unit -> 'a) =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let rec innerwait timeout f =
        let sleepAndWait () = System.Threading.Thread.Sleep(int (waitSleep * 1000.0)); innerwait timeout f
        if sw.Elapsed.TotalSeconds >= timeout then raise <| WebDriverTimeoutException("Timed out!")

        try
            let result = f()
            match box result with
              | :? bool as b ->
                   if b then result
                   else sleepAndWait ()
              | _ as o ->
                    if o <> null then result
                    else sleepAndWait ()
        with
          | :? WebDriverTimeoutException -> reraise()
          | :? CanopyException as ce -> raise(ce)
          | _ -> sleepAndWait ()

    innerwait timeout f

//The most common form of waiting, wait for something to be true but ignore results
let wait timeout (f : unit -> bool) = waitResults timeout f |> ignore