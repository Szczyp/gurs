namespace Gurs.Web.Client.Github

open Gurs.Web.Models
open System.Threading.Tasks
open FSharp.Control.Tasks.V2.ContextInsensitive
open System.Collections.Generic
open System.Net.Http
open System.IO
open Giraffe.Serialization
open System.Net

type IGithubJsonDeserializer =
    abstract member DeserializeAsync : Stream -> 'T Task

type GithubJsonDeserializer (serializer : IJsonSerializer) =
    interface IGithubJsonDeserializer with
        member this.DeserializeAsync stream = serializer.DeserializeAsync stream

type IGithubUserRepositories =
    abstract member Get : string -> Task<Result<IEnumerable<Repository>, HttpStatusCode>>

type GithubUserRepositories (client : HttpClient, jsonSerializer : IGithubJsonDeserializer) =
    interface IGithubUserRepositories with
        member this.Get user =
            task {
                let! response = client.GetAsync (sprintf "users/%s/repos" user)
                printfn "%O" response.Headers
                match response.IsSuccessStatusCode with
                | true ->
                    use! stream = response.Content.ReadAsStreamAsync ()
                    let! repos = jsonSerializer.DeserializeAsync<IEnumerable<Repository>> stream
                    return Ok repos
                | false ->
                    return Error response.StatusCode
            }
