module canopy.mobile.configuration

open System.IO

/// TimeOut for find operations
let mutable waitTimeout = 10.0

/// Android Emulator tool name
let mutable androidEmulatorToolName = "emulator.exe"