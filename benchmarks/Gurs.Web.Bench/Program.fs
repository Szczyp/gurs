open System.IO
open Gurs.Web.Models.Stats
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running


type Letters () =
    let text =
        File.ReadAllText("data/New-Introductory-Lectures-On-Psycho-Analysis.txt")
            .Replace(" ", "")
            .Replace("\n", "")

    [<Benchmark>]
    member this.F () = lettersF text
    [<Benchmark>]
    member this.D () = lettersD text
    [<Benchmark>]
    member this.A () = lettersA text

[<EntryPoint>]
let main argv =
    BenchmarkRunner.Run<Letters>() |> ignore
    0
