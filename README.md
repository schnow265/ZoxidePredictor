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

1. Every 60 seconds a background thread activates which will run ``zoxide query --list --all --score`` and parses the result into an in-memory dictionary.
2. Does nothing until you type ``cd `` (note the space at the end) just like you would with zoxide.
3. This module then sorts prediction results using a [C# Reimplementation of the zoxide algorithm](./ZoxidePredictor/Lib/Matcher.cs) to the shell to show up as suggestions
    ![The Predictor in action](./assets/predictor_in_action.png)

## Installation

### Dependencies

- [PowerShell Core](https://github.com/powerShell/powerShell)
- ``PsReadLine`` version 2.1.0 or later (Update using ``Update-Module``)
- [``dotnet``](https://dot.net) version 9.x
- [``zoxide``](https://github.com/ajeetdsouza/zoxide)

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

## TODO

These are things still to do:

- Add build workflow
- Add Tests (where possible)
- *if any of my other projects require it*: Extract [`Matcher.cs`](./ZoxidePredictor/Lib/Matcher.cs) into a seperate project and maintain the implementation properly there
- Find a way to do ci/cd releases on tags to PSGallery for easier installation

## Troubleshooting

If you have any issues which aren't printed to the terminal, rebuild the module - but replace `-c Release` with `-c Debug`. This will allow logging code to be included in the build.

The log file is called `debug.log` and is placed in the same location as the built assembly (So the output path from the build command). 

Also, it is called `debug.log` for a reason.
