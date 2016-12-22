[<AutoOpen>]
module canopy.mobile.selectors

/// Using conventions convert a string representing a selector to a Selenium By
//todo there are probably many more examples to expand on
let toBy (selector : string) = 
    if selector.StartsWith("#") then
        OpenQA.Selenium.By.Id(selector.Replace("#", "android:id/"))
    else if selector.StartsWith("android:id") then
        OpenQA.Selenium.By.Id(selector)
    else if selector.StartsWith("//") then
        OpenQA.Selenium.By.XPath(selector)
    //Default to any node with a text of what you supplied
    else OpenQA.Selenium.By.XPath(sprintf """//*[@text="%s"]""" selector)

/// Selects an android.widget.TextView with the given text
let textView text = sprintf """//android.widget.TextView[@text="%s"]""" text