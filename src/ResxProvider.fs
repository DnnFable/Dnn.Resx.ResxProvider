namespace Dnn

module Resx =

    open System
    open System.IO
    open System.Net
    open System.Text.RegularExpressions
    open FSharp.Quotations
    open FSharp.Core.CompilerServices
    open ProviderImplementation.ProvidedTypes

    open ProviderDsl
    open Fable.Core

    [<Emit("""
    (() => {
      const res =$0;
      return $.get($.ServicesFramework(0).getServiceRoot('dnn.resx') + 'service/get?strategy=0&resource=' + res);
    })()""" )>]
    let loadResources (resxUrl: string): JS.Promise<obj> = (unbox) obj ()

    [<Emit("JSON.parse($0)")>]
    let jsonParse (json: string) = obj ()

    [<Emit("$0[$1]")>]
    let getProp (o: obj) (k: string) = obj ()

    let firstToUpper (s: string) = s.[0..0].ToUpper() + s.[1..]

    let getterCode name =
        fun (args: Expr list) -> <@@ getProp %%args.Head name @@>

    let rec makeType typeName json =
        match json with
        | JsonParser.Null -> Any
        | JsonParser.Bool _ -> Bool
        | JsonParser.Number _ -> Float
        | JsonParser.String _ -> String
        | JsonParser.Array items ->
            match items with
            | [] -> Array Any
            // TODO: Check if all items have same type
            | item :: _ -> makeType typeName item |> Array
        | JsonParser.Object members ->
            let members =
                members |> List.collect (makeMember typeName)

            makeCustomType (typeName, members) |> Custom

    and makeMember ns (name, json) =
        let t = makeType (firstToUpper name) json

        let m =
            Property(name, t, false, getterCode name)

        match t with
        | Custom t -> [ ChildType t; m ]
        | _ -> [ m ]

    let parseJson asm ns typeName sample =
        let makeRootType withCons basicMembers =
            makeRootType (
                asm,
                ns,
                typeName,
                [ yield! basicMembers |> List.collect (makeMember "")
                  if withCons then
                      yield Constructor([ "json", String ], (fun args -> <@@ jsonParse %%args.Head @@>))
                      yield Constructor([ "obj", Any ], (fun args -> <@@ %%args.Head @@>)) ]
            )

        match JsonParser.parse sample with
        | Some (JsonParser.Object members) -> makeRootType true members |> Some
        | Some (JsonParser.Array ((JsonParser.Object members) :: _)) ->
            let t = makeRootType false members
            let array = t.MakeArrayType() |> Custom

            [ Method("ParseArray", [ "json", String ], array, true, (fun args -> <@@ jsonParse %%args.Head @@>)) ]
            |> addMembers t

            Some t
        | _ -> None

    [<TypeProvider>]
    type public Resx(config: TypeProviderConfig) as this =
        inherit TypeProviderForNamespaces(config)

        let asm =
            System.Reflection.Assembly.GetExecutingAssembly()

        let ns = "Dnn.Resx"

        let staticParams =
            [ ProvidedStaticParameter("sample", typeof<string>) ]

        let generator =
            ProvidedTypeDefinition(asm, ns, "ResxProvider", Some typeof<obj>, isErased = true)

        do
            generator.DefineStaticParameters(
                parameters = staticParams,
                instantiationFunction =
                    (fun typeName pVals ->
                        match pVals with
                        | [| :? string as arg |] ->
                            let arg = arg.Trim()

                            let readfile arg =
                                let filepath =
                                    if Path.IsPathRooted arg then
                                        arg
                                    else
                                        Path.GetFullPath(Path.Combine(config.ResolutionFolder, arg))

                                File.ReadAllText(filepath, System.Text.Encoding.UTF8)

                            let parseResx text =
                                let matches =
                                    Regex.Matches(text, "<!--(?:.|\n)*?-->|data\s+name=\"(.*?)\"")

                                seq { for m in matches -> m }
                                |> Seq.map (fun m -> m.Groups.[1].Value)
                                |> Seq.filter (System.String.IsNullOrWhiteSpace >> not)

                            let content =
                                // Check if the string is a JSON literal
                                if arg.StartsWith("{") || arg.StartsWith("[") then
                                    arg
                                else if arg.EndsWith(".resx") then
                                    readfile arg
                                    |> parseResx
                                    |> Seq.map (fun n -> sprintf "\"%s\":\"\"," n)
                                    |> Seq.fold (+) ""
                                    |> sprintf "{%s}"

                                else
                                    readfile arg

                            match parseJson asm ns typeName content with
                            | Some t -> t
                            | None -> failwithf "Local sample is not a valid JSON: %s" content
                        | _ -> failwith "unexpected parameter values")
            )

        do this.AddNamespace(ns, [ generator ])

    [<assembly: TypeProviderAssembly>]
    do ()
