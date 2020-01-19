module Gurs.Web.App

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Gurs.Web.HttpHandlers
open Gurs.Web.Client.Github
open System.Net.Http
open Giraffe.Serialization
open Utf8Json.Resolvers
open Microsoft.Extensions.Configuration

// ---------------------------------
// Web app
// ---------------------------------

let webApp =
    choose [
        GET >=> routef "/repositories/%s" handleGetHello
        setStatusCode 404 >=> text "Not Found"
    ]

// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------
let configureAppConfiguration (ctx : WebHostBuilderContext) (config : IConfigurationBuilder) =
    config.AddEnvironmentVariables "GURS_" |> ignore

let configureApp (ctx : WebHostBuilderContext) (app : IApplicationBuilder) =
    app.UseGiraffeErrorHandler(errorHandler)
       .UseHttpsRedirection()
       .UseGiraffe(webApp)


let withConfig (ctx : WebHostBuilderContext) key action =
    let value = ctx.Configuration.GetValue key
    match isNotNull value with
    | true -> action value
    | false -> ()


let configureServices (ctx : WebHostBuilderContext) (services : IServiceCollection) =
    let configureGithubClient (client : HttpClient) =
        client.BaseAddress <- Uri "https://api.github.com/"
        client.DefaultRequestHeaders.Add ("Accept", "application/vnd.github.v3+json") |> ignore
        client.DefaultRequestHeaders.Add ("User-Agent", "gurs") |> ignore
        withConfig ctx "GITHUB_TOKEN" (fun token ->
            client.DefaultRequestHeaders.Add ("Authorization", sprintf "token %s" token))

    services.AddHttpClient<IGithubUserRepositories, GithubUserRepositories>(configureGithubClient) |> ignore
    services.AddGiraffe() |> ignore
    services.AddSingleton<IJsonSerializer>(Utf8JsonSerializer StandardResolver.CamelCase) |> ignore
    services.AddSingleton<IGithubJsonDeserializer>(GithubJsonDeserializer <| Utf8JsonSerializer StandardResolver.SnakeCase) |> ignore


let configureLogging (builder : ILoggingBuilder) =
    builder.AddFilter(fun l -> l.Equals LogLevel.Error)
           .AddConsole()
           .AddDebug() |> ignore


[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .ConfigureAppConfiguration(configureAppConfiguration)
        .ConfigureServices(configureServices)
        .ConfigureLogging(configureLogging)
        .Configure(configureApp)
        .Build()
        .Run()
    0