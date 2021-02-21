# Dnn.Resx.ResxProvider [![Nuget](https://img.shields.io/nuget/v/Dnn.Resx.ResxProvider?style=flat-square)](https://www.nuget.org/packages/Dnn.Resx.ResxProvider/)

A F# Type Provider designed for Fable. It provides typed information on DNN's Resource Files (.resx) during design time and can be loaded with localized texts on runtime with the help of 
[Dnn.Resx](https://github.com/DnnFable/Dnn.Resx)

## Usage
* Install [Dnn.Resx](https://github.com/DnnFable/) on your DNN server
* Install the package `Dnn.Resx.ResxProvider` from NuGet
* Add a resource file and generate your model


```fsharp
open Browser.Dom

//ViewResources is the typed representation of the resources for localization
type ViewResources = Dnn.Resx.ResxProvider<"./App_LocalResources/View.resx">

//it can be loaded on runtime using DNN.Resx.
Dnn.Resx.loadResources "~desktopmodules/vendor/modulename/App_LocalResources/View.resx"
|> Promise.iter
    (fun result ->
        let resources = ViewResources result

        // During design time, the items are already available as properties
        console.log resources.``Input.Text``)
```

### What is happening behind the scene?
Fable is going to compile the code from above to this javascript:

```javascript
import { some } from "./.fable/fable-library.3.1.5/Option.js";

(function () {
    const pr = (() => {
        const res = "~desktopmodules/vendor/modulename/App_LocalResources/View.resx";
        return $.get($.ServicesFramework(0).getServiceRoot('dnn.resx') + 'service/get?strategy=0&resource=' + res);
    })();
    pr.then(((result) => {
        console.log(some(result["Input.Text"]));
    }));
})();

```
As you see, it uses DNN's old JQuery based ServiceFramework to query the Dnn.Resx service, which returns a JObject inside a promise.

The generated type `ViewResources` is now completly erased. It already did his job during design and compile time. It was its responsibility to support the developer with intellisense. If the resource gets modified and keys were changed, it would even result into compiler errors.

## Inspiration
This type provider was inspired and originally forked from the  [Fable.JsonProvider](https://github.com/fable-compiler/Fable.JsonProvider)