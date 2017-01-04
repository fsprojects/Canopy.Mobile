/// Contains canopy specific exception
module canopy.mobile.Exceptions

open System

type CanopyException(message) = inherit Exception(message)
type CanopyElementNotFoundException(message) = inherit CanopyException(message)