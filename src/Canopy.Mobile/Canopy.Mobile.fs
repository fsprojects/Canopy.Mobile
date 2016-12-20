[<AutoOpen>]
module canopy.mobile.core

open System
open System.IO
open OpenQA.Selenium
open OpenQA.Selenium.Remote
open OpenQA.Selenium.Appium
open OpenQA.Selenium.Appium.Android
open OpenQA.Selenium.Appium.Interfaces
open System.Threading
open canopy

type ExecutionSource =
| Console

let getExecutionSource() = Console

let getTestServerAddress () =
    match getExecutionSource() with
    | Console  -> Uri "http://127.0.0.1:4723/wd/hub"

let mutable driver : AndroidDriver<IWebElement> = null

[<RequireQualifiedAccess>]
type Selector =
| XPath of xpath:string
| Name of name:string


let getCapabilities appName =
    match getExecutionSource() with
    | Console  ->
        let capabilities = DesiredCapabilities()
        capabilities.SetCapability("platformName", "Android")
        capabilities.SetCapability("platformVersion", "6.0")
        capabilities.SetCapability("platform", "Android")
        capabilities.SetCapability("avd", "Nexus_6_API_23")
        capabilities.SetCapability("deviceName", "Android Emulator")
        capabilities.SetCapability("app", appName)
        capabilities

/// Starts the webdriver with the given app.
let start appName =
    appium.start()
    let capabilities = getCapabilities appName

    let testServerAddress = getTestServerAddress ()
    driver <- new AndroidDriver<IWebElement>(testServerAddress, capabilities, TimeSpan.FromSeconds(120.0))

    //driver.ExecuteScript(mechanicjs.source) |> ignore
    canopy.types.browser <- driver
    printfn "Done starting"

/// Quits the web driver and appium
let quit () = 
    if not (isNull driver) then 
        driver.Quit()

    appium.stop()

let private findElements' selector = 
    match selector with
    | Selector.XPath xpath -> driver.FindElementsByXPath xpath |> List.ofSeq
    | Selector.Name name -> driver.FindElementsByName name |> List.ofSeq

/// Finds elements on the current page.
let findElements selector reliable timeout =
    try
        if reliable then
            let results = ref []
            wait timeout (fun _ ->
                results := findElements' selector


                not <| List.isEmpty !results)
            !results
        else
            waitResults timeout (fun _ -> findElements' selector)
    with 
    | :? WebDriverTimeoutException -> failwithf "can't find elements with selector: %A" selector

/// Returns all elements that match the given selector.
let findAll selector = findElements selector true configuration.waitTimeout

/// Returns the first element that matches the given selector.
let find selector = findAll selector |> List.head

/// Returns the first element that matches the given selector or None if no such element exists.
let tryFind selector = findAll selector |> List.tryHead

/// Returns true when the selector matches an element on the current page or otherwise false.
let exists selector = findElements selector true configuration.waitTimeout |> List.isEmpty |> not

/// Clicks the first element that's found with the selector
let click selector =
    try
        wait configuration.waitTimeout (fun _ ->
            try 
                (find selector).Click()
                Thread.Sleep configuration.waitAfterClick
                true
            with _ -> false)
    with
    | _ -> failwithf "Failed to click: %A" selector


/// Clicks the Android back button
let back () =
    try
        wait configuration.waitTimeout (fun _ ->
            try 
                driver.PressKeyCode(AndroidKeyCode.Back)
                Thread.Sleep configuration.waitAfterClick
                true
            with _ -> false)
    with
    | _ -> failwithf "Failed to click Android back button"

