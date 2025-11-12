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

- [PowerShell Core](https://github.com/powerShell/powerShell) 7.0 or later
- ``PsReadLine`` version 2.1.0 or later (Update using ``Update-Module``)
- [``zoxide``](https://github.com/ajeetdsouza/zoxide)

### Install from PowerShell Gallery (Recommended)

Coming soon! Once published to PSGallery, you'll be able to install with:

```powershell
Install-Module -Name ZoxidePredictor
```

Then add to your ``$PROFILE``:
```powershell
Import-Module ZoxidePredictor
```

### Build & Install from Source

If you want to build from source or use the latest development version:

1. Clone this repo
2. Ensure you have [``dotnet``](https://dot.net) SDK version 9.x installed
3. Enter the Subdirectory ``ZoxidePredictor``
4. Run (best in PowerShell):
    ```powershell
   # non-Windows
   dotnet publish -c Release -o $HOME/.local/share/powershell/Modules/ZoxidePredictor

   # Windows
   dotnet publish -c Release -o $HOME\Documents\PowerShell\Modules\ZoxidePredictor
   ```
5. Add the following to your ``$PROFILE``:
    ```powershell
    Import-Module ZoxidePredictor
    ```
6. Restart powershell and verify that the provider has been registered by running ``Get-PSSubsystem -Kind CommandPredictor``, which should have ``zoxide`` under ``Implementations``

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

## Deployment

This module is configured for automatic deployment to PowerShell Gallery using GitHub Actions.

### Publishing a New Version

To publish a new version to PSGallery:

1. Update the version number if needed (the workflow will use the tag version)
2. Create and push a version tag:
   ```bash
   git tag v0.1.0
   git push origin v0.1.0
   ```
3. The GitHub Actions workflow will automatically:
   - Build the module
   - Update the manifest version
   - Test the module
   - Publish to PowerShell Gallery

### Required Secrets

The repository needs the following secret configured in GitHub:
- `PSGALLERY_API_KEY`: Your PowerShell Gallery API key (get it from [PowerShell Gallery](https://www.powershellgallery.com/account/apikeys))
