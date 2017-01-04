/// Contains configuration settings.
module canopy.mobile.configuration

/// TimeOut for find operations
let mutable elementTimeout = 20.0

/// TimeOut for interaction operations
let mutable interactionTimeout = 30.0

/// TimeOut for assertion operations
let mutable assertionTimeout = 30.0

/// Android Emulator tool name
let mutable androidEmulatorToolName = "emulator.exe"