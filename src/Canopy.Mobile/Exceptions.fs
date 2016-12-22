module Exceptions

open System

type CanopyException(message) = inherit Exception(message)