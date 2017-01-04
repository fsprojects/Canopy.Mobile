[<AutoOpen>]
module canopy.mobile.core

open System
open System.IO
open OpenQA.Selenium
open OpenQA.Selenium.Remote
open OpenQA.Selenium.Appium
open OpenQA.Selenium.Appium.Android
open OpenQA.Selenium.Appium.Interfaces
open OpenQA.Selenium.Appium.Android.Enums
open System.Threading
open System.Diagnostics
open System.Text
open Wait
open Exceptions

let mutable driver : AndroidDriver<IWebElement> = null

let mutable private emulatorProcess : Process = null

type AndroidSettings = {
    AVDName : string
    Silent : bool
}

let DefaultAndroidSettings = {
    AVDName = null
    Silent = true
}

let androidHome = lazy (
    let androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME")
    if String.IsNullOrEmpty androidHome then
        failwithf "Environment variable ANDROID_HOME is not set."

    if not (Directory.Exists androidHome) then
        failwithf "Environment variable ANDROID_HOME is set to %s. But this directory does not exist." androidHome

    let sdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT")
    if String.IsNullOrEmpty sdkRoot then
        Environment.SetEnvironmentVariable("ANDROID_SDK_ROOT",androidHome,EnvironmentVariableTarget.Process)

    androidHome)

/// Checks whether an Android emulator is already running
let isAndroidEmulatorRunning() =
    if not (isNull emulatorProcess) && not emulatorProcess.HasExited then true else
    let emulatorProcesses = 
        Process.GetProcesses()
        |> Array.filter (fun p -> p.ProcessName.StartsWith "qemu-system")

    Array.isEmpty emulatorProcesses |> not

/// Start an emulator process
let startEmulator settings = 
    if isAndroidEmulatorRunning() then printfn "Android emulator is already running" else
    let emulatorToolPath = Path.Combine(androidHome.Force(), "tools", "emulator.exe")
    
    let argsSB = StringBuilder()

    argsSB.AppendFormat(" -avd {0}",settings.AVDName) |> ignore

    if settings.Silent then
        argsSB.Append(" -no-window -no-boot-anim") |> ignore

    let args = argsSB.ToString()

    let pi = ProcessStartInfo(emulatorToolPath,args)
    pi.UseShellExecute <- false
    emulatorProcess <- Process.Start pi

    let adbToolPath = Path.Combine(androidHome.Force(), "platform-tools", "adb.exe")

    let pi = ProcessStartInfo(adbToolPath,"wait-for-device")
    pi.UseShellExecute <- false
    let adbProcess = Process.Start pi
    adbProcess.WaitForExit()
    
    
let getAndroidCapabilities (settings:AndroidSettings) appName =
    startEmulator settings

    let capabilities = DesiredCapabilities()
    capabilities.SetCapability("platformName", "Android")
    capabilities.SetCapability("platformVersion", "6.0")
    capabilities.SetCapability("platform", "Android")
    capabilities.SetCapability("automationName", "Appium")
    capabilities.SetCapability("deviceName", "Android Emulator")
    capabilities.SetCapability("autoLaunch", "true")
    capabilities.SetCapability("deviceReadyTimeout", 1000)
    capabilities.SetCapability("androidDeviceReadyTimeout", 1000)
    capabilities.SetCapability("app", appName)
    capabilities

/// Starts the webdriver with the given app.
let start settings appName =
    let capabilities = getAndroidCapabilities settings appName
    appium.start()

    let testServerAddress = Uri "http://127.0.0.1:4723/wd/hub"
    driver <- new AndroidDriver<IWebElement>(testServerAddress, capabilities, TimeSpan.FromSeconds(120.0))    
        
    printfn "Done starting"

/// Stops the emulator process that was started with canopy mobile
let stopEmulator() =
    if isNull emulatorProcess then () else
    if emulatorProcess.HasExited then () else
    
    printfn "Closing emulator"

    emulatorProcess.Kill()

    let adbToolPath = Path.Combine(androidHome.Force(), "platform-tools", "adb.exe")
    let pi = ProcessStartInfo(adbToolPath,"shell reboot -p")
    pi.UseShellExecute <- false
    let proc = Process.Start pi
    proc.WaitForExit()


/// Quits the web driver and appium
let quit () = 
    if not (isNull driver) then
        driver.Quit()

    appium.stop()
    stopEmulator()

/// Finds elements on the current page for a given By.
let private findElementsBy by reliable timeout =
    try
        if reliable then
            let results = ref []
            wait timeout (fun _ ->
                results := driver.FindElements by |> List.ofSeq
                not <| List.isEmpty !results)
            !results
        else
            waitResults timeout (fun _ -> driver.FindElements by |> List.ofSeq)
    with 
    | :? WebDriverTimeoutException -> raise <| CanopyElementNotFoundException(sprintf "can't find elements with By: %A" by)

/// Returns all elements for a given By.
let findAllBy by = 
    try
        findElementsBy by true configuration.elementTimeout
    with
    | _ -> []

/// Returns all elements for a given selector.
let findAll selector = findAllBy (toBy selector)

/// Returns all elements on the current page.
let getAllElements() = findAllBy (toBy "//*")

/// Returns the first element for a given By.
let findBy by = findElementsBy by true configuration.elementTimeout |> List.head

/// Returns the first element that matches the given selector.
let find selector = findBy (toBy selector)

/// Returns the first element for a given By or None if no such element exists.
let tryFindBy by = 
    try 
        findAllBy by |> List.tryHead
    with
    | _ -> None

/// Returns the first element that matches the given selector or None if no such element exists.
let tryFind selector = findAllBy (toBy selector)

/// Returns true when the selector matches an element on the current page or otherwise false.
let exists selector = 
    try
        findElementsBy (toBy selector) true 1.0 |> List.isEmpty |> not
    with
    | _ -> false

/// Waits until the given selector returns an element or throws an exception when the timeout is reached.
let waitFor selector = find selector |> ignore

//keys
let home = AndroidKeyCode.Home
let space = AndroidKeyCode.Space

/// Press a key
let press key = driver.PressKeyCode(key)

/// Press a key with meta state
let pressMeta key = driver.PressKeyCode(key, AndroidKeyMetastate.Meta_Shift_On)

/// Long press a key
let longPress key = driver.LongPressKeyCode(key)

/// Long press a key with meta state
let longPressMeta key = driver.LongPressKeyCode(key, AndroidKeyMetastate.Meta_Shift_On)

/// Clicks the first element that is found with the selector
let click selector =
    try
        wait configuration.interactionTimeout (fun _ ->
            try 
                (find selector).Click()
                true
            with 
            | :? CanopyElementNotFoundException -> raise <| CanopyException(sprintf "Failed to click: %A because it could not be found" selector)
            | _ -> false)
    with
    | :? CanopyException -> reraise()
    | ex -> failwithf "Failed to click: %A%sInner Message: %A" selector System.Environment.NewLine ex


/// Clicks the Android back button
let back () =
    try
        wait configuration.interactionTimeout (fun _ ->
            try 
                driver.PressKeyCode(AndroidKeyCode.Back)
                true
            with 
            | :? CanopyElementNotFoundException -> raise <| CanopyException(sprintf "Failed to click back button.")
            | _ -> false)
    with
    | ex -> failwithf "Failed to go back%sInner Message: %s" System.Environment.NewLine ex.Message

// Assertions

/// Check that an element has a specific value
let ( == ) selector value = 
    try
        wait configuration.assertionTimeout (fun _ ->
            try 
                (find selector).Text = value
            with 
            | :? CanopyElementNotFoundException -> raise <| CanopyException(sprintf "Equality check for : %A failed because it could not be found" selector)
            | _ -> false)
    with
    | :? CanopyException -> reraise()
    | :? WebDriverTimeoutException -> failwithf "Equality check failed.%sExpected: %s Got: %s" System.Environment.NewLine value (find selector).Text
    | ex -> failwithf "Equality check failed for unknown reasons.%sInner Message: %s" System.Environment.NewLine ex.Message

/// Check that an element does not have a specific value
let ( != ) selector value = 
    try
        wait configuration.assertionTimeout (fun _ ->
            try 
                (find selector).Text <> value
            with 
            | :? CanopyElementNotFoundException -> raise <| CanopyException(sprintf "Not Equal check for : %A failed because it could not be found" selector)
            | _ -> false)
    with
    | :? CanopyException -> reraise()
    | :? WebDriverTimeoutException -> failwithf "Not Equal check failed.%sExpected NOT: %s Got: %s" System.Environment.NewLine value (find selector).Text
    | ex -> failwithf "Not Equal check failed for unknown reasons.%sInner Message: %s" System.Environment.NewLine ex.Message

/// Writes the given text into the element that was found by the given selector and waits until the text was completely entered.
let ( << ) selector text =
    click selector
    driver.Keyboard.SendKeys text
    driver.HideKeyboard()
    selector == text

/// Check that an element exists and is displayed.
let displayed selector = 
    try
        wait configuration.assertionTimeout (fun _ ->
            try 
                (find selector).Displayed
            with 
            | :? CanopyElementNotFoundException -> raise <| CanopyException(sprintf "Displayed check for : %A failed because it could not be found" selector)
            | _ -> false)
    with
    | :? CanopyException -> reraise()
    | :? WebDriverTimeoutException -> failwith "Displayed check failed."
    | ex -> failwithf "Displayed check failed for unknown reasons.%sInner Message: %s" System.Environment.NewLine ex.Message

/// Check that an element exists but is not displayed.
let notDisplayed selector = 
    try
        wait configuration.assertionTimeout (fun _ ->
            try 
                (find selector).Displayed = false
            with 
            | :? CanopyElementNotFoundException -> raise <| CanopyException(sprintf "Not displayed check for : %A failed because it could not be found" selector)
            | _ -> false)
    with
    | :? CanopyException -> reraise()
    | :? WebDriverTimeoutException -> failwith "Not displayed check failed."
    | ex -> failwithf "Not displayed check failed for unknown reasons.%sInner Message: %s" System.Environment.NewLine ex.Message

/// Takes a screenshot of the emulator and saves it as png.
let screenshot path fileName = 
    let screenShot = driver.GetScreenshot()
    if not (Directory.Exists path) then
        Directory.CreateDirectory path |> ignore
    
    let extension = Path.GetExtension fileName 
    if not (String.IsNullOrEmpty extension) && extension <> ".png" then
        failwithf "Only png files are allowed for Screenshots. %s is not allowed." extension

    let fileName = Path.ChangeExtension(Path.Combine(path,fileName),".png")
    if File.Exists fileName then
        File.Delete fileName
        
    screenShot.SaveAsFile(fileName, Drawing.Imaging.ImageFormat.Png)

/// Clicks an element and waits for the waitSelector to appear.
let clickAndWait clickSelector waitSelector =
    if exists waitSelector then
        failwithf "The selector %s already matched before the click. This makes it impossible to detect page transistions." waitSelector
    click clickSelector
    waitFor waitSelector

/// Clicks the Android back button and waits for the waitSelector to appear.
let backAndWait waitSelector =
    if exists waitSelector then
        failwithf "The selector %s already matched before the click of the Android back button. This makes it impossible to detect page transistions." waitSelector
    back()
    waitFor waitSelector
