namespace Gurs.Web

module HttpHandlers =

    open FSharp.Control.Tasks.V2.ContextInsensitive
    open Giraffe
    open Gurs.Web.Client.Github
    open Gurs.Web.Models

    let handleGetHello user =
        handleContext
        <| fun ctx ->
            task {
                let client = ctx.GetService<IGithubUserRepositories> ()
                let! repositories = client.Get user
                return!
                    match repositories with
                    | Ok repos ->
                        ctx.WriteJsonAsync <| Stats.calculate user repos
                    | Error code ->
                        ctx.SetStatusCode <| int code
                        ctx.WriteTextAsync <| sprintf "%O" code
            }