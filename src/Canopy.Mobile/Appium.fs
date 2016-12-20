/// Contains methods for dealing with [appium](http://appium.io).
module canopy.mobile.appium

open System.IO
open OpenQA.Selenium.Appium.Service
open System

let mutable localService : AppiumLocalService = null
let mutable logFile = "./temp/AppiumLog.txt"


/// Starts appium as local service
let start() =
    if isNull localService then
        let fi = FileInfo logFile
        if not fi.Directory.Exists then
            fi.Directory.Create()

        let builder = 
            AppiumServiceBuilder()
              .WithLogFile(fi)

        let builder = 
            let fi = FileInfo(configuration.appiumToolPath)
            if fi.Exists then 
                Environment.SetEnvironmentVariable(AppiumServiceConstants.AppiumBinaryPath, fi.Directory.FullName)
                builder.WithAppiumJS fi.Directory.FullName
            else 
                builder

        localService <- builder.Build()
    if not localService.IsRunning then
        localService.Start()

/// Starts appium as local service
let stop() =
    if not (isNull localService) then
        if localService.IsRunning then
            localService.Dispose()
        localService <- null