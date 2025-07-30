# ZoxidePredictor

This is an experimental [PSReadLine predictor](https://learn.microsoft.com/en-us/powershell/scripting/learn/shell/using-predictors) using zoxide results to show you what folder you are about to cd into - *before* you press enter.

## Speed & Limitations

As [the docs](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-cmdline-predictor) specify: 

> [...] the ICommandPredictor interface has a 20ms time out for responses from the Predictors. [...]

So all logic in the method ``GetSuggestion`` has to return within 20ms.

## Matching improvements

