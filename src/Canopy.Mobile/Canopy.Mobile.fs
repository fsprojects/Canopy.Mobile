module canopy.mobile

open System
open OpenQA.Selenium
open OpenQA.Selenium.Remote
open OpenQA.Selenium.Appium
open OpenQA.Selenium.Appium.Android
open OpenQA.Selenium.Appium.Interfaces

type ExecutionSource =
| Console

let getExecutionSource() = Console

let getTestServerAddress () =
    match getExecutionSource() with
    | Console  -> Uri "http://127.0.0.1:4723/wd/hub"

let mutable driver : AndroidDriver<IWebElement> = null

//configuration
let mutable waitTimeout = 10.0

[<RequireQualifiedAccess>]
type Selector =
| XPath of xpath:string
| Name of name:string

[<RequireQualifiedAccess>]
type Direction =
| Up
| Down

let getCapabilities appName =
    match getExecutionSource() with
    | Console  ->
        let capabilities = DesiredCapabilities()
        capabilities.SetCapability("platformName", "Android")
        capabilities.SetCapability("platformVersion", "6.0")
        capabilities.SetCapability("platform", "Android")
        capabilities.SetCapability("deviceName", "Android Emulator")
        capabilities.SetCapability("app", appName)
        capabilities

let start appName =
    let capabilities = getCapabilities appName

    let testServerAddress = getTestServerAddress ()
    driver <- new AndroidDriver<IWebElement>(testServerAddress, capabilities, TimeSpan.FromSeconds(120.0))

    //driver.ExecuteScript(mechanicjs.source) |> ignore
    canopy.types.browser <- driver
    printfn "Done starting"

let quit () = if not (isNull driver) then driver.Quit()