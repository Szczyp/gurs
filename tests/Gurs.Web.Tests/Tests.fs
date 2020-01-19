module Tests

open System
open System.Net
open System.Net.Http
open Xunit
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open Gurs.Web.Client.Github
open System.Collections.Generic
open Gurs.Web.Models
open Giraffe.Serialization
open Utf8Json.Resolvers
open Giraffe
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.Extensions.FileProviders
open FSharpPlus

// ---------------------------------
// Helper functions (extend as you need)
// ---------------------------------

type GithubUserTestRepositories (client : HttpClient, jsonSerializer : IGithubJsonDeserializer, fileProvider : IFileProvider) =
    interface IGithubUserRepositories with
        member this.Get user =
            task {
                use stream = fileProvider.GetFileInfo("repos").CreateReadStream()
                let! repos = jsonSerializer.DeserializeAsync<IEnumerable<Repository>> stream
                return Ok repos
            }

let createHost() =
    WebHostBuilder()
        .UseSolutionRelativeContentRoot("tests/data", "gurs.sln")
        .Configure(fun app ->
            app.UseGiraffe(Gurs.Web.App.webApp)
        )
        .ConfigureServices(fun ctx services ->
            services.AddGiraffe() |> ignore
            services.AddSingleton<IFileProvider>(ctx.HostingEnvironment.ContentRootFileProvider) |> ignore
            services.AddSingleton<IJsonSerializer>(Utf8JsonSerializer StandardResolver.CamelCase) |> ignore
            services.AddSingleton<IGithubJsonDeserializer>(GithubJsonDeserializer <| Utf8JsonSerializer StandardResolver.SnakeCase) |> ignore
            services.AddHttpClient<IGithubUserRepositories, GithubUserTestRepositories>() |> ignore
        )

let runTask task =
    task
    |> Async.AwaitTask
    |> Async.RunSynchronously

let httpGet (path : string) (client : HttpClient) =
    path
    |> client.GetAsync
    |> runTask

let isStatus (code : HttpStatusCode) (response : HttpResponseMessage) =
    Assert.Equal(code, response.StatusCode)
    response

let ensureSuccess (response : HttpResponseMessage) =
    if not response.IsSuccessStatusCode
    then response.Content.ReadAsStringAsync() |> runTask |> failwithf "%A"
    else response

let readText (response : HttpResponseMessage) =
    response.Content.ReadAsStringAsync()
    |> runTask

let shouldEqual expected actual =
    Assert.Equal(expected, actual)

let shouldContain (expected : string) (actual : string) =
    Assert.True(actual.Contains expected)

let readFile (server : TestServer) file =
    let env = server.Services.GetService<IWebHostEnvironment>()
    use stream = env.ContentRootFileProvider.GetFileInfo(file).CreateReadStream()
    use reader = new IO.StreamReader (stream, Text.Encoding.UTF8)
    reader.ReadToEnd ()


// ---------------------------------
// Tests
// ---------------------------------

[<Fact>]
let ``lettersF "aA-2a"`` () =
    let str = "aA-2a"
    let d = Dict.union <| dict [('a', 3)] <| dict Stats.lowerCaseASCIIKVs
    let f = Stats.lettersF str

    Assert.Equal<IDictionary<char, int>>(d, f)


[<Fact>]
let ``letters{F,D,A} are equal`` () =
    let str = "aA-2a"
    let f = Stats.lettersF str 
    let d = Stats.lettersD str
    let a = Stats.lettersA str

    Assert.Equal<IDictionary<char, int>>(f, d)
    Assert.Equal<IDictionary<char, int>>(d, a)


[<Fact>]
let ``Route /repositories/Szczyp returns 'data/stats'`` () =
    use server = new TestServer(createHost())
    use client = server.CreateClient()

    client
    |> httpGet "/repositories/Szczyp"
    |> ensureSuccess
    |> readText
    |> shouldEqual (readFile server "stats")


[<Fact>]
let ``Route which doesn't exist returns 404 Page not found`` () =
    use server = new TestServer(createHost())
    use client = server.CreateClient()

    client
    |> httpGet "/route/which/does/not/exist"
    |> isStatus HttpStatusCode.NotFound
    |> readText
    |> shouldEqual "Not Found"