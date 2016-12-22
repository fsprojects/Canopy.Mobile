module canopy.mobile.configuration

open System.IO

/// TimeOut for find operations
let mutable waitTimeout = 10.0

/// Wait time after every click.
let mutable waitAfterClick = 1500

/// Android Emulator tool name
let mutable androidEmulatorToolName = "emulator.exe"