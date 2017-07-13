[<AutoOpen>]
/// Contains all canopy core functions.
module canopy.mobile.core

open System
open System.IO
open OpenQA.Selenium
open OpenQA.Selenium.Remote
open OpenQA.Selenium.Appium.Android
open OpenQA.Selenium.Appium.Android.Enums
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

let mutable emulatorStarted = false

/// Starts the webdriver with the given app.
let start settings appName =
    printfn "Starting appium in emulator"
    let capabilities = getAndroidCapabilities settings appName
    appium.start()

    let testServerAddress = Uri "http://127.0.0.1:4723/wd/hub"
    driver <- new AndroidDriver<IWebElement>(testServerAddress, capabilities, TimeSpan.FromSeconds(120.0))    
    emulatorStarted <- true
    printfn "Done starting"

let getDeviceCapabilities appName =
    let capabilities = DesiredCapabilities()
    capabilities.SetCapability("platformName", "Android")
    capabilities.SetCapability("platform", "Android")
    capabilities.SetCapability("automationName", "Appium")
    capabilities.SetCapability("deviceName", "Android")
    capabilities.SetCapability("deviceReadyTimeout", 1000)
    capabilities.SetCapability("androidDeviceReadyTimeout", 1000)
    capabilities.SetCapability("app", appName)
    capabilities

/// Starts the given app on a device
let startOnDevice appName =
    printfn "Starting appium on device"
    let capabilities = getDeviceCapabilities appName
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
    if emulatorStarted then
        stopEmulator()
    emulatorStarted <- false

/// Finds elements on the current page for a given By.
let rec private findElementsBy (by : By option) (selector : string option) reliable timeout =
    let by' =
        match by with
        | Some by -> by
        | None ->
            match selector with
            | Some selector -> toBy selector
            | None -> failwith "You must provide a By or a selector"
    try
        if reliable then
            let results = ref []
            wait timeout (fun _ ->
                results := driver.FindElements by' |> List.ofSeq
                not <| List.isEmpty !results)
            !results
        else
            waitResults timeout (fun _ -> driver.FindElements by' |> List.ofSeq)
    with
    | :? WebDriverTimeoutException -> 
        let selector = 
            match selector with
            | Some selector -> selector
            | None -> 
                let bySelector = by.Value.ToString()
                bySelector.Substring(bySelector.IndexOf(": ") + 2)

        let suggestions = GetSuggestions selector
        CanopyElementNotFoundException(sprintf "can't find elements with %A%sDid you mean?:%s%A" selector Environment.NewLine Environment.NewLine suggestions)
        |> raise    

/// Returns all elements with text on the current page.
and getAllTexts() = 
    findAllBy (toBy "//*")
    |> List.filter (fun (x:IWebElement) -> String.IsNullOrWhiteSpace x.Text |> not)

/// Returns all elements for a given By - without timeout
and findAllBy by = 
    try
        findElementsBy (Some by) None true configuration.elementTimeout
    with
    | _ -> []

and findAllByNow by =
    try
        findElementsBy (Some by) None true 0.
    with
    | _ -> []

/// Returns all elements for a given selector.
and findAll selector = findAllBy (toBy selector)

/// Returns all elements on the current page.
and getAllElements() = findAllBy (toBy "//*")

/// Returns all selectors for all elements the current page.
and GetAllElementSelectors() =
    let generateSuggestions text (element : string) (resourceId : string) = 
        let pseudoSelectorSuggestion =
            match element with
            | null -> ""
            | "android.widget.TextView" -> sprintf """tv:%s""" text
            | "android.widget.EditText" -> sprintf """edit:%s""" text
            | _ -> ""
            
        let xpathAndTextSuggestion =
            match element with
            | null -> ""
            | "android.widget.TextView" -> sprintf """//android.widget.TextView[@text="%s"]""" text
            | "android.widget.EditText" -> sprintf """//android.widget.EditText[@text="%s"]""" text
            | _ -> ""

        let resourceIdSuggestion = 
            match resourceId with
            | null -> ""
            | _ when resourceId.Contains(":id/") -> sprintf """#%s""" (resourceId.Substring(resourceId.IndexOf(":id/") + 4)) 
            | _ -> ""        
            
        [ text; element; pseudoSelectorSuggestion; xpathAndTextSuggestion; resourceId; resourceIdSuggestion ]

    getAllElements ()
    |> List.map (fun element -> generateSuggestions element.Text element.TagName (element.GetAttribute("resourceId")))
    |> List.concat
    |> List.distinct
    |> List.filter (fun suggestion -> suggestion <> null && suggestion <> "")

/// Prints a description of the current view on the console
and DescribeCurrentView() =
    printfn "Available selectors:"
    for selector in GetAllElementSelectors() do
        printfn "%s" selector

/// Returns similar elements for a given selector
and GetSuggestions selector =
    GetAllElementSelectors()
    |> List.map (fun suggestion -> canopy.mobile.EditDistance.editdistance selector suggestion)
    |> List.sortByDescending (fun a -> a.similarity)
    |> List.map (fun result -> result.selector)
    |> fun results -> if results.Length >= 5 then List.take 5 results else results    

    
/// Returns the first element for a given By.
let findBy by = findElementsBy (Some by) None true configuration.elementTimeout |> List.head

/// Returns the first element that matches the given selector.
let find selector = findElementsBy None (Some selector) true configuration.elementTimeout |> List.head

/// Returns the first element for a given By or None if no such element exists right now.
let tryFindBy by = 
    try 
        findAllByNow by |> List.tryHead
    with
    | _ -> None

/// Returns the first element that matches the given selector or None if no such element exists right now.
let tryFind selector = tryFindBy (toBy selector)

/// Returns true when the selector matches an element on the current page or otherwise false.
let exists selector = 
    try
        findElementsBy None (Some selector) true 1.0 |> List.isEmpty |> not
    with
    | _ -> false

/// Waits until the given selector returns an element or throws an exception when the timeout is reached.
let waitFor selector =
    printfn "Waiting for %A" selector
    find selector |> ignore

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

/// Clicks the first element that is found with the selector.
/// If a text input is focused, this function clicks the button twice in order to get the focus.
let click selector =
    try
        printfn "Click %A" selector
        wait configuration.interactionTimeout (fun _ ->
            try
                let rec click retries =
                    let element = find selector
                    match tryFind "//*[@focused='true']" with
                    | Some focused when retries > 0 && focused.TagName = "android.widget.EditText" ->
                        element.Click()
                        System.Threading.Thread.Sleep 500
                        click (retries - 1)
                    | _ ->
                        element.Click()

                click 3

                
                true
            with 
            | :? CanopyElementNotFoundException ->
                let suggestions = GetSuggestions selector
                raise <| CanopyException(sprintf "Failed to click %A, because it could not be found.%sDid you mean?:%s%A" selector Environment.NewLine Environment.NewLine suggestions)
            | _ -> false)
    with
    | :? CanopyException -> reraise()
    | ex -> failwithf "Failed to click: %A%sInner Message: %A" selector System.Environment.NewLine ex

/// Hides the keyboard if it is open
let rec hideKeyboard () = 
    try 
        driver.HideKeyboard()
        System.Threading.Thread.Sleep(100)
        hideKeyboard()
    with
    | _ -> ()

/// Clicks the Android back button.
/// If the keyboard is still open this function hides it.
let back () =
    try
        printfn "Android back button press"
        wait configuration.interactionTimeout (fun _ ->
            try 
                hideKeyboard()
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
            | :? CanopyElementNotFoundException ->
                let suggestions = GetSuggestions selector
                raise <| CanopyException(sprintf "Equality check for %A failed because it could not be found.%sDid you mean?:%s%A" selector Environment.NewLine Environment.NewLine suggestions)
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
            | :? CanopyElementNotFoundException -> 
                let suggestions = GetSuggestions selector
                raise <| CanopyException(sprintf "Not Equal check for %A failed because it could not be found.%sDid you mean?:%s%A" selector Environment.NewLine Environment.NewLine suggestions)
            | _ -> false)
    with
    | :? CanopyException -> reraise()
    | :? WebDriverTimeoutException -> failwithf "Not Equal check failed.%sExpected NOT: %s Got: %s" System.Environment.NewLine value (find selector).Text
    | ex -> failwithf "Not Equal check failed for unknown reasons.%sInner Message: %s" System.Environment.NewLine ex.Message


/// Gets the nth element with the given selector.
/// Index starts at 1.
let nth n selector = findAll selector |> List.item (n + 1)

/// Gets the first element with the given selector.
/// Index starts at 1.
let first selector = findAll selector |> List.head

/// Gets the last element with the given selector.
/// Index starts at 1.
let last selector = findAll selector |> List.last

/// Set Orientation
let orientation orientation = driver.Orientation <- orientation

/// Set Orientation to Landscape
let landscape() = orientation ScreenOrientation.Landscape

/// Set Orientation to Portrait
let portrait() = orientation ScreenOrientation.Portrait

/// Reads the text from the given selector
let read selector = (find selector).Text

/// Sleeps for x seconds.
let sleep seconds = System.Threading.Thread.Sleep(TimeSpan.FromSeconds seconds)

let mutable private hasWritten = false

/// Writes the given text into the element that was found by the given selector and waits until the text was completely entered.
let writeIntoElement closeKeyboard selector text =
    click selector
    driver.Keyboard.SendKeys text
    if closeKeyboard then
        hideKeyboard()

    if selector.StartsWith "edit:" then
        waitFor ("edit:" + text)
    else
        selector == text
    hasWritten <- true

/// Writes the given text into the element that was found by the given selector and waits until the text was completely entered.
/// After running this function the keyboard will be closed.
let write selector text = writeIntoElement true selector text

/// Writes the given text into the element that was found by the given selector and waits until the text was completely entered.
/// After running this function the keyboard will be closed.
let ( << ) selector text = write selector text

/// Check that an element exists and is displayed.
let displayed selector = 
    try
        wait configuration.assertionTimeout (fun _ ->
            try 
                (find selector).Displayed
            with 
            | :? CanopyElementNotFoundException -> 
                let suggestions = GetSuggestions selector
                raise <| CanopyException(sprintf "Displayed check for %A failed because it could not be found.%sDid you mean?:%s%A" selector Environment.NewLine Environment.NewLine suggestions)
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
                not (find selector).Displayed
            with 
            | :? CanopyElementNotFoundException ->
                let suggestions = GetSuggestions selector
                raise <| CanopyException(sprintf "Not displayed check for %A failed because it could not be found.%sDid you mean?:%s%A" selector Environment.NewLine Environment.NewLine suggestions)
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
    if hasWritten then
        click clickSelector
    waitFor waitSelector
    hasWritten <- false

/// Clicks the Android back button and waits for the waitSelector to appear.
let backAndWait waitSelector =
    if exists waitSelector then
        failwithf "The selector %s already matched before the click of the Android back button. This makes it impossible to detect page transistions." waitSelector
    back()
    waitFor waitSelector
    hasWritten <- false