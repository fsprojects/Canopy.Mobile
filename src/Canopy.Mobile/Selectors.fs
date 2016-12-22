[<AutoOpen>]
module canopy.mobile.selectors

[<RequireQualifiedAccess>]
type Selector =
| XPath of xpath:string
| Name of name:string


/// Selects an android element with the given text
let elementWithText text = Selector.XPath (sprintf "//*[@text='%s']" text) 

/// Selects an android.widget.TextView with the given text
let textView text = Selector.XPath (sprintf "//android.widget.TextView[@text='%s']" text) 