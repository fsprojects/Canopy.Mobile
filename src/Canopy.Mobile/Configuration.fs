module canopy.mobile.configuration

open System.IO

/// TimeOut for find operations
let mutable waitTimeout = 10.0

/// Wait time after every click.
let mutable waitAfterClick = 1000

/// Appium installation directory
let mutable appiumToolPath = Path.Combine(".", "node_modules", "appium", "bin", "appium.js")