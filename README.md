# ZoxidePredictor

This is an
experimental [PSReadLine predictor](https://learn.microsoft.com/en-us/powershell/scripting/learn/shell/using-predictors)
using zoxide results to show you what folder you are about to cd into - *before* you press enter.

> Note: This module currently has the zoxide command fixed to ``cd``.
>
> You can set the zoxide alias to ``cd`` by replacing the ``zoxide init`` line in your ``$PROFILE`` with this
>
> ```powershell
> Invoke-Expression (& { (zoxide init powershell --cmd cd | Out-String) })
> ```

## How does this work?

1. Every 60 seconds a background thread activates which will run ``zoxide query --list --all --score`` and parses the result into a dictionary.
2. Does nothing until you type ``cd`` just like you would with zoxide.
3. This module then sends prediction results using a [C# Reimplementation of the zoxide algorithm](./ZoxidePredictor/Lib/Matcher/Matcher.cs) to the shell to show up as suggestions
    ![The Predictor in action](./assets/predictor_in_action.png)

## Installation

### Dependencies

- ``zoxide``
- [PowerShell Core](https://github.com/powerShell/powerShell)
- ``dotnet`` version 9.x
- ``PsReadLine`` version 2.1.0 or later (Update using ``Update-Module``)

### Build & Install

1. Clone this repo
2. Enter the Subdirectory ``ZoxidePredictor``
3. Run (best in PowerShell):
    ```powershell
   # non-Windows
   dotnet publish -c Release -o $HOME/.local/share/powershell/Modules/ZoxidePredictor

   # Windows
   dotnet publish -c Release -o $HOME\Documents\PowerShell\Modules\ZoxidePredictor
   ```
4. Add the following to your ``$PROFILE``:
    ```powershell
    Import-Module ZoxidePredictor
    ```
5. Restart powershell and verify that the provider has been registered by running ``Get-PSSubsystem -Kind CommandPredictor``, which should have ``zoxide`` under ``Implementations``

### Bonus Tip:

Press ``<F2>`` to switch between inline and list. (I personally prefer list).

To permanently change to list by default, add the following line to your ``$PROFILE``:

```powershell
Set-PSReadLineOption -PredictionViewStyle ListView
```

(You may also need to add ``Import-Module PSReadLine`` at the top. On some systems I need it, on others not.)
