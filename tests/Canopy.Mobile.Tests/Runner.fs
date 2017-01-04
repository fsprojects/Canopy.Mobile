module Canopy.Mobile.Runner

open SampleTests
open Canopy.Mobile.ExpectoHelper
open System
open System.IO
open System.Net
open canopy.mobile
open Expecto

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


[<EntryPoint>]
let main args =
    try
        try
            let app = downloadDemoApp()
            
            let settings = 
                { DefaultAndroidSettings with 
                    AVDName = "AVD_for_Nexus_6_by_Google"
                    Silent = args |> Array.contains "debug" |> not }

            start settings app
            runTestsWithArgs { defaultConfig with ``parallel`` = false } args tests
        with e ->
            printfn "Error: %s" e.Message
            -1
    finally
        quit()
