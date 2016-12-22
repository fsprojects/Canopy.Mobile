module canopy.mobile.configuration

open System.IO

/// TimeOut for find operations
let mutable waitTimeout = 20.0

/// Wait after click in seconds
let mutable waitAfterClick = 0.5

/// Android Emulator tool name
let mutable androidEmulatorToolName = "emulator.exe"