module canopy.mobile.configuration

open System.IO

/// TimeOut for find operations
let mutable waitTimeout = 10.0

/// Wait time after every click.
let mutable waitAfterClick = 1000

let rec private findNodeModulesPath (di:DirectoryInfo) = 
    if Directory.Exists(Path.Combine(di.FullName,"node_modules")) then
        Path.Combine(di.FullName,"node_modules")
    elif di.Parent <> null then 
        findNodeModulesPath di.Parent 
    else
        Path.Combine(".", "node_modules")

let nodeModules = findNodeModulesPath (DirectoryInfo("."))