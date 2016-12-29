module Canopy.Mobile.SampleTests

open canopy.mobile
open System.IO
open Expecto
open System.Collections.Generic
open OpenQA.Selenium.Appium
open OpenQA.Selenium.Appium.Android
open OpenQA.Selenium
open System.Threading
open OpenQA.Selenium.Appium.Android.Enums
open OpenQA.Selenium.Appium.Interfaces
open System
open Canopy.Mobile.ExpectoHelper

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
            testCase "can get all elements" <| fun () ->
                let elements = getAllElements()
                Expect.equal elements.Length 42 "all elements are found"

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
            
            testCase "can click element by XPath" <| fun () ->
                waitFor "//android.widget.TextView[@text='API Demos']" // an example of a full qualified xml selector
                click "Graphics" //shortcut for text = 'Graphics'
                clickAndWait "Arcs" "tv:Graphics/Arcs"

                backAndWait "tv:BitmapDecode"

                clickAndWait "Arcs" "tv:Graphics/Arcs"

                backAndWait "tv:BitmapDecode"

                backAndWait "tv:Animation"

            testCase "can click element by canopy selector" <| fun () ->
                waitFor "tv:API Demos"
                click "tv:Graphics"
                clickAndWait "tv:Arcs" "tv:Graphics/Arcs"

                backAndWait "tv:BitmapDecode"

                clickAndWait "tv:Arcs" "tv:Graphics/Arcs"

                backAndWait "tv:BitmapDecode"

                backAndWait "tv:Animation"

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
                clickAndWait "tv:Graphics" "tv:BitmapDecode"

                let filename = DateTime.Now.ToString("MMM-d_HH-mm-ss-fff")
                screenshot screenShotDir filename
                Expect.isTrue (File.Exists(Path.Combine(screenShotDir,filename + ".png"))) "Screenshot exists"
        ]
    ]

