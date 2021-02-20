# Dnn.Resx.ResxProvider [![Nuget](https://img.shields.io/nuget/v/Dnn.Resx.ResxProvider?style=flat-square)](https://www.nuget.org/packages/Dnn.Resx.ResxProvider/)

A F# Type Provider designed for Fable. It provides typed information on DNN's Resource Files (.resx) during design time and can be loaded with localized texts on runtime with the help of [Dnn.Resx](https://github.com/DnnFable/Dnn.Resx)

## Usage
* Install [Dnn.Resx](https://github.com/DnnFable/) on your DNN server
* Install the package `Dnn.Resx.ResxProvider` from NuGet
* Add a resource file and generate your model


```fsharp
open Browser.Dom

//ViewResources is the typed representation of the resources fpr localization
type ViewResources = Dnn.Resx.ResxProvider<"/App_LocalResources/View.resx">

//it can be loaded on runtime using DNN.Resx
Dnn.Resx.loadResources "~desktopmodules/vendor/modulename/App_LocalResources/View.resx"
|> Promise.map ViewResources
|> Promise.iter
    (fun res ->
        // During design time, the items are available as properties
        console.log res.``Input.Text``)
```

## Inspiration
This type provider was inspired and originally forked from the  [Fable.JsonProvider](https://github.com/fable-compiler/Fable.JsonProvider)