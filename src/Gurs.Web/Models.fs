namespace Gurs.Web.Models
open System.Collections.Generic
open FSharpPlus

[<CLIMutable>]
type Repository =
    {
        Name : string
        StargazersCount : int
        WatchersCount : int
        ForksCount : int
        Size : int
    }
    with
    static member inline get_Zero () =
        {
            Name = getZero ()
            StargazersCount = getZero ()
            WatchersCount = getZero ()
            ForksCount = getZero ()
            Size = getZero ()
        }
    static member inline (+) (a, b) =
        {
            Name = a.Name + b.Name
            StargazersCount = a.StargazersCount + b.StargazersCount
            WatchersCount = a.WatchersCount + b.WatchersCount
            ForksCount = a.ForksCount + b.ForksCount
            Size = a.Size + b.Size
        }

type Stats =
    {
        Owner : string
        Letters : IDictionary<char, int>
        AvgStargazers : decimal
        AvgWatchers : decimal
        AvgForks : decimal
        AvgSize : decimal
    }

module Stats =
    let lowerCaseASCII = [|'a' .. 'z'|]
    let upperCaseASCII = [|'A' .. 'Z'|]
    let lowerCaseASCIIKVs = lowerCaseASCII |> map (fanout id <| konst 0)

    let lettersF =
        String.toLower
        >> filter (Set.ofArray lowerCaseASCII).Contains
        >> groupBy id
        >> map (mapItem2 length)
        >> dict
        >> Dict.unionWith (konst id) (lowerCaseASCIIKVs |> dict)

    let lettersD str =
        let ls = Dictionary<char, int> ()
        lowerCaseASCIIKVs |> iter ls.Add
        let isUpper = Set.ofArray(upperCaseASCII).Contains
        let cr = ref 0
        let rec update ch =
            if ls.TryGetValue (ch, cr)
            then ls.[ch] <- !cr + 1
            elif isUpper ch
            then update <| ch + char 32
        String.iter update str
        ls :> IDictionary<char, int>

    let lettersA str =
        let l = length lowerCaseASCII
        let ls = Array.init l (konst 0)
        for ch in str do
            let i = int ch - int 'a'
            if (i >= 0 && i < l) then ls.[i] <- ls.[i] + 1
            else
                let i = i + 32
                if (i >= 0 && i < l) then ls.[i] <- ls.[i] + 1
        Array.zip lowerCaseASCII ls |> dict

    let calculate (owner : string) (repositories : Repository IEnumerable) =
        let count, total : int * Repository =
            repositories
            |> map (fanout (konst 1) id)
            |> sum 
        let avg filed = decimal filed / decimal count
        {
            Owner = owner
            Letters = lettersA total.Name
            AvgStargazers = avg total.StargazersCount
            AvgWatchers = avg total.WatchersCount
            AvgForks = avg total.ForksCount
            AvgSize = avg total.Size
        }
