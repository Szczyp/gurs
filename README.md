# Gurs #

Simple web service written in F# using ASP.NET Core 3.1 providing stats of Github user repositories.

### API definition ###

    GET /repositories/{owner} 

    {
      "owner": "...",
      "letters": [ “a”: 0, “b”:0, (...) ],
      "avgStargazers": 0,
      "avgWatchers": 0,
      "avgForks": 0,
      “avgSize”: 0
    }

## Things to improve ##
  * HttpClient resilience policies (Polly?)
  * HttpClient throttling depending on Github API's rate limits (X-RateLimit-*)
  * Service rate limits
  * Response caching (ETag)
