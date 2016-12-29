module Canopy.Mobile.Tests

open canopy.mobile
open System.IO
open Expecto
open System.Collections.Generic
open System.Net
open OpenQA.Selenium.Appium
open OpenQA.Selenium.Appium.Android
open OpenQA.Selenium
open System.Threading
open OpenQA.Selenium.Appium.Android.Enums
open OpenQA.Selenium.Appium.Interfaces
open System

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

let downloadDemoApp () =
    let localFile = FileInfo("./temp/ApiDemos-debug.apk")
    if File.Exists localFile.FullName then
        printfn "app %s already exists" localFile.FullName
    else
        let appOnServer = "http://appium.github.io/appium/assets/ApiDemos-debug.apk"
        Directory.CreateDirectory localFile.Directory.FullName |> ignore
        use client = new WebClient()
        
        printfn "downloading %s to %s" appOnServer localFile.FullName
        client.DownloadFile(appOnServer, localFile.FullName)
        printfn "app downloaded"

    localFile.FullName

let tests =
    testList "android tests" [
        testList "session tests" [
            testCase "can get device UUID" <| fun () ->
                let deviceUIID = driver.SessionDetails.["deviceUDID"].ToString()
                Expect.isNotNull deviceUIID "deviceUIID was set"

            testCase "can get dictionary data" <| fun () ->
                let dictionary : Dictionary<string, obj> = driver.SessionDetails.["desired"] |> unbox
                Expect.isGreaterThan dictionary.Count 0 "desired data is set"
        ]

        
        testList "device tests" [
            testCase "can get device time" <| fun () ->
                let time = driver.DeviceTime

                Expect.equal time.Length 28 "time has correct format"
        ]
        
        testList "app strings tests" [
            testCase "can get app strings" <| fun () ->
                let appStrings = driver.GetAppStringDictionary()
                Expect.notEqual appStrings.Count 0 "app strings are not empty"

            testCase "can get app strings using lang" <| fun () ->
                let appStrings = driver.GetAppStringDictionary("en")
                Expect.notEqual appStrings.Count 0 "app strings are not empty"
        ]
        
        testList "key press tests" [
            testCase "can press key code" <| fun () ->
                press home

            testCase "can press key code with meta state" <| fun () ->
                pressMeta space

            testCase "can long press key code" <| fun () ->
                longPress space

            testCase "can long press key code with meta state" <| fun () ->
                longPressMeta space                
        ]
        
        testList "element tests" [
            testCase "can find element by Android UI Automator" <| fun () ->
                driver.StartActivity("io.appium.android.apis", ".ApiDemos")
                let byAndroidUIAutomator = new ByAndroidUIAutomator("new UiSelector().clickable(true)")
                let element = (find "#content").FindElement(byAndroidUIAutomator)

                Expect.isNotNull element.Text "text is set"
                Expect.isGreaterThanOrEqual ((find "#content").FindElements(byAndroidUIAutomator).Count) 1 "selects at least 1 element"
                
            testCase "can find element by XPath" <| fun () ->
                let element = find "//android.widget.TextView[@text='API Demos']"
                Expect.isNotNull element.Text "headline is set"


            testCase "can click back button" <| fun () ->
                waitFor "tv:Animation"

                click "tv:Graphics"
                waitFor "tv:BitmapDecode"
                
                back()
                waitFor "tv:Animation"

                click "tv:Graphics"
                waitFor "tv:BitmapDecode"

                back()
                waitFor "tv:Animation"
            
            // testCase "can click element by XPath" <| fun () ->
            //     waitFor "//android.widget.TextView[@text='API Demos']" // an example of a full qualified xml selector
            //     click "Graphics" //shortcut for text = 'Graphics'
            //     click "Arcs"

            //     back()
            //     click "Arcs"

            //     back()
            //     waitFor "API Demos"
            //     waitFor "Arcs"

            //     back()
            //     waitFor "API Demos"
            //     waitFor "Graphics"

            // testCase "can click element by canopy selector" <| fun () ->
            //     waitFor "tv:API Demos"
            //     click "tv:Graphics"
            //     click "tv:Arcs"

            //     back()
            //     waitFor "tv:API Demos"
            //     click "tv:Arcs"

            //     back()
            //     waitFor "tv:API Demos"
            //     waitFor "tv:Arcs"

            //     back()
            //     waitFor "tv:API Demos"
            //     waitFor "tv:Graphics"

            testCase "equality check for API Demos and Animation" <| fun () ->
                "tv:API Demos" == "API Demos"
                "tv:Animation" == "Animation"

            testCase "not equality check for API Demos and Animation" <| fun () ->
                "API Demos" != "Blah"
                "Animation" != "Blah Blah"
        ]

        testList "complex android tests" [
            testCase "can take screenshot" <| fun () ->
                displayed "tv:Animation"
                click "tv:Graphics"
                waitFor "tv:BitmapDecode"

                let filename = DateTime.Now.ToString("MMM-d_HH-mm-ss-fff")
                screenshot screenShotDir filename
                Expect.isTrue (File.Exists(Path.Combine(screenShotDir,filename + ".png"))) "Screenshot exists"
        ]
    ]

[<EntryPoint>]
let main args =
    try
        try
            let app = downloadDemoApp()
            
            let settings = 
                { DefaultAndroidSettings with 
                    AVDName = "AVD_for_Nexus_6_by_Google"
                    Silent = false } //args |> Array.contains "debug" |> not }

            start settings app
            runTests { defaultConfig with ``parallel`` = false } tests
        with e ->
            printfn "Error: %s" e.Message
            -1
    finally
        quit()
