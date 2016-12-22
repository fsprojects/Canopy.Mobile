[<AutoOpen>]
module canopy.mobile.selectors

[<RequireQualifiedAccess>]
type Selector =
| XPath of xpath:string
| Name of name:string

/// Selects an android.widget.TextView with the given text
let textView text = Selector.XPath (sprintf "//android.widget.TextView[@text='%s']" text) 