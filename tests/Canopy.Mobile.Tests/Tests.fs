module Canopy.Mobile.Tests

open canopy.mobile
open System.IO
open Expecto
open System.Net

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

[<Tests>]
let tests =
    testCase "yes" <| fun () ->
        let deviceUIID = driver.SessionDetails.["deviceUDID"].ToString()
        Expect.isNotNull deviceUIID "deviceUIID was set"

[<EntryPoint>]
let main args =
    let app = downloadDemoApp()
    start app
    let result = runTestsInAssembly defaultConfig args
    quit()
    result