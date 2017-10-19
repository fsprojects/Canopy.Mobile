/// Contains canopy specifc wait helpers.
module canopy.mobile.Wait

open OpenQA.Selenium
open canopy.mobile.Exceptions

let mutable waitSleep = 0.5

//This is similar to how Selenium Wait works which is very useful
//Provide a function that returns a bool or object and if its true or not null then stop waiting
let waitResults timeout (f : unit -> 'a) =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let mutable finalResult : 'a = Unchecked.defaultof<'a>
    let mutable keepGoing = true
    while keepGoing do
        try
            if sw.Elapsed.TotalSeconds >= timeout then raise <| WebDriverTimeoutException("Timed out!")

            let result = f()
            match box result with
              | :? bool as b ->
                   if b then
                      keepGoing <- false
                      finalResult <- result
                   else System.Threading.Thread.Sleep(int (waitSleep * 1000.0))
              | _ as o ->
                    if o <> null then
                      keepGoing <- false
                      finalResult <- result
                    else System.Threading.Thread.Sleep(int (waitSleep * 1000.0))
        with
          | :? WebDriverTimeoutException -> reraise()
          | :? CanopyException as ce -> raise(ce)
          | _ -> System.Threading.Thread.Sleep(int (waitSleep * 1000.0))

    finalResult
//The most common form of waiting, wait for something to be true but ignore results
let wait timeout (f : unit -> bool) = waitResults timeout f |> ignore
