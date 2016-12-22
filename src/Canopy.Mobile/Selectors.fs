[<AutoOpen>]
module canopy.mobile.selectors

/// Selector Conventions that determine if a selector matches a pattern and if it does what the resulting By is
let mutable selectorConventions =
    [
        fun (selector : string) -> if selector.StartsWith("#") then Some (OpenQA.Selenium.By.Id(selector.Replace("#", "android:id/"))) else None
        fun (selector : string) -> if selector.StartsWith("tv:") then Some (OpenQA.Selenium.By.XPath(sprintf """//android.widget.TextView[@text="%s"]""" (selector.Replace("tv:", "")))) else None
        fun (selector : string) -> if selector.StartsWith("android:id") then Some (OpenQA.Selenium.By.Id(selector)) else None
        fun (selector : string) -> if selector.StartsWith("//") then Some (OpenQA.Selenium.By.XPath(selector)) else None
    ]

/// The default Selector Convetion if none of the configurable Selector Convetions are matched
let mutable defaultSelectorConvention = fun selector -> OpenQA.Selenium.By.XPath(sprintf """//*[@text="%s"]""" selector)

/// Using conventions convert a string representing a selector to a Selenium By
let toBy (selector : string) = 
    let convention = 
        selectorConventions 
        |> List.map (fun convention -> convention selector) 
        |> List.choose id 
        |> List.tryHead

    match convention with
    | Some(by) -> by
    | None -> defaultSelectorConvention selector