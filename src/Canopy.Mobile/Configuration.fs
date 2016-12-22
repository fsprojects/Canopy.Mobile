module canopy.mobile.configuration

open System.IO

/// TimeOut for find operations
let mutable elementTimeout = 20.0

/// TimeOut for interacation operations
let mutable interactionTimeout = 30.0

/// Android Emulator tool name
let mutable androidEmulatorToolName = "emulator.exe"