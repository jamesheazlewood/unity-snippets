# Setup for VS Code as of 5/11/2023

This is what I figured out eventually over hours of trial and error.

What you get:

* Double click scripts in Unity to open them in VS Code
* Attach Debugger (probably - I don't use it)
* Intellisense for Unity symbols like `Vector2`, `Transform`,
  and namespaces like `using UnityEngine;`
* Format on save, using a config file of defined rules
* Intellisense and code suggestions from your project

Tested using:

* Unity `2023.1.9f1`
* VS Code `1.84.0`
* C# Extension `v2.9.20`
* Unity Extension for VS Code `v0.9.2`
* Node `21.1.0`
* EditorConfig for VS Code `v0.16.4`
* Windows 10 Pro `10.0.19045 Build 19045`

Project (`csproj`) files are targeting ".NET Standard 2.1":

`<TargetFramework>netstandard2.1</TargetFramework>`

This means only .NET SDK versions that adhere to the ".NET Standard" version 2.1 will work

Note that ".NET Standard" is different to ".NET SDK", the standard is which APIs work.

Microsoft have these diagrams showing which "standard" is held by which version of .net,
including Unity:

* <https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-1>
* <https://dotnet.microsoft.com/en-us/platform/dotnet-standard#versions>

## In Unity

Open Package Manager

Install `"Visual Studio Editor"`, NOT `"Visual Studio Code Editor"` as that is also "legacy",
and you should remove it.

<https://www.youtube.com/watch?v=X8Qr78Vs0Ss>

Once `"Visual Studio Code Editor"` is removed from the packages, it will be gone forever.

Next, go to "Project Settings", and under "Player", "Other Settings",
ensure "API Compatibility Level" is set to ".NET Standard 2.1"
and "Editor Assemblies Compatibility Level" is set to "Default (.NET Framework)".

Next, go to "Preferences" and under "External Tools", choose (or browse for)
"Visual Studio Code [1.84.0]" for "External Script Editor"

Then click "Regenerate project files". This will set the `<TargetFramework>`
in `csproj` files to `netstandard2.1` if it wasn't already.

## In VS Code

* Install extension: C# Dev Kit by Microsoft
* Install extension: Unity by Microsoft

You will aslo automatically get dependent extensions "C#" and ".NET Install Tool".
You might also get "IntelliCode for C# Dev Kit `v0.1.26`". If not, it won't hurt to get it too.

Note that these extensions will automatically check and update .NET SDK/C# related libraries
every time you open VS Code, so if you have any custom settings paths set, they will break.

We then want the code formatter to run on save

This *used* to be done with config saved in `omnisharp.json`, but that is now "legacy"

<https://github.com/dotnet/vscode-csharp/issues/5446#issuecomment-1308655891>

Go to your workspace settings and ensure you have these set:

```json
{
  "editor.formatOnSave": true,
  "omnisharp.useModernNet": false,
  "omnisharp.enableEditorConfigSupport": true,
}
```

And, **DO NOT** have any of these set in your Workspace or User settings, delete them completely:

```json
{
  "omnisharp.path": "C/Users/.../OmniSharp.exe",
  "omnisharp.dotnetPath": "C/Program Files/dotnet",
  "omnisharp.sdkPath": "C:\\Program Files\\dotnet\\sdk\\2.1.818",
  "dotnet.server.useOmnisharp": true,
  "[csharp]": {
    "editor.defaultFormatter": "ms-dotnettools.csharp"
  },
}
```

There is even a tiny note buried in the UI style VS Code settings under
"OmniSharp: Use Modern Net" saying:
"This version *does not* support non-SDK-style .NET Framework projects, including Unity"

## In the terminal

The terminal I tested this on is the "git bash" one. Or the "bash" terminal loaded into the in-built
VS Code terminal

Check which version of .NET SDK is being used:

`dotnet --v`

It should be using the latest one by default, to check run:

`dotnet --list-sdks`

The version this works in is `7.0.403`, but older versions may still work. But not `2.2.207` as it
uses the old ".NET Standard"

You can force the .NET SDK version that VS Code uses by creating a
`global.json` file with the contents:

```json
{
  "sdk": {
    "version": "7.0.403"
  }
}
```

Next, we want to install the npm package for `editorconfig`.

<https://stackoverflow.com/a/48523398/2740286>

But first, double check your version of node:

`node -v`

And if it's lower than `16`, you want to install the latest version.

To check, run:

`nvm ls available`

Then, install latest node, at the time of writing, this was `21.1.0`:

`nvm install 21.1.0`

Without a version of node `>=16` you'll get a "Unsupported engine" error
when installing `editorconfig` globally.

Switch to the new version of node:

`nvm use 21.1.0`

Now, actually install `editorconfig`:

`npm install -g editorconfig`

Finally, install the editorconfig extension "EditorConfig for VS Code" - it should have a blue tick:

<https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig>

Edit any formatting config in the file `.editorconfig` which is an "ini" style file.

Note that any changes to this will need VS Code to **restart** before they take effect,
and sometimes it seems that this doesn't always take affect every time.

## Links

Config options for your `.editorconfig` file:

* <https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/code-style-rule-options?view=vs-2022>
* <https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options#csharp_indent_case_contents_when_block>

Other useful VS Code extensions:

* GitLens
* Window Colors
* Reload
* Reopen closed tab
