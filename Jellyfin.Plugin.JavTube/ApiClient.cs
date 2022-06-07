using System.Collections.Specialized;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Jellyfin.Plugin.JavTube.Models;

namespace Jellyfin.Plugin.JavTube;

public static class ApiClient
{
    private const string ActorInfoApi = "/v1/actors";
    private const string MovieInfoApi = "/v1/movies";
    private const string ActorSearchApi = "/v1/actors/search";
    private const string MovieSearchApi = "/v1/movies/search";
    private const string PrimaryImageApi = "/images/primary";
    private const string ThumbImageApi = "/images/thumb";
    private const string BackdropImageApi = "/images/backdrop";

    private static string ComposeUrl(string path, NameValueCollection nv)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (string key in nv) query.Add(key, nv.Get(key));

        // Build URL
        var uriBuilder = new UriBuilder(Plugin.Instance.Configuration.Server)
        {
            Path = path,
            Query = query.ToString() ?? string.Empty
        };
        return uriBuilder.ToString();
    }

    private static string ComposeImageApiUrl(string path, string provider, string id, string url, double position,
        bool auto)
    {
        return ComposeUrl(Path.Combine(path, provider, id), new NameValueCollection
        {
            { "url", url },
            { "pos", position.ToString("R") },
            { "auto", auto.ToString() }
        });
    }

    private static string ComposeInfoApiUrl(string path, string provider, string id, bool lazy)
    {
        return ComposeUrl(Path.Combine(path, provider, id), new NameValueCollection
        {
            { "lazy", lazy.ToString() }
        });
    }

    private static string ComposeSearchApiUrl(string path, string q, string provider, bool lazy)
    {
        return ComposeUrl(path, new NameValueCollection
        {
            { "q", q },
            { "provider", provider },
            { "lazy", lazy.ToString() }
        });
    }

    public static string GetPrimaryImageApiUrl(string provider, string id, double position = -1)
    {
        return ComposeImageApiUrl(PrimaryImageApi, provider, id, string.Empty, position, false);
    }

    public static string GetPrimaryImageApiUrl(string provider, string id, string url, double position = -1,
        bool auto = false)
    {
        return ComposeImageApiUrl(PrimaryImageApi, provider, id, url, position, auto);
    }

    public static string GetThumbImageApiUrl(string provider, string id)
    {
        return ComposeImageApiUrl(ThumbImageApi, provider, id, string.Empty, -1, false);
    }

    public static string GetThumbImageApiUrl(string provider, string id, string url, double position = -1,
        bool auto = false)
    {
        return ComposeImageApiUrl(ThumbImageApi, provider, id, url, position, auto);
    }

    public static string GetBackdropImageApiUrl(string provider, string id)
    {
        return ComposeImageApiUrl(BackdropImageApi, provider, id, string.Empty, -1, false);
    }

    public static string GetBackdropImageApiUrl(string provider, string id, string url, double position = -1,
        bool auto = false)
    {
        return ComposeImageApiUrl(BackdropImageApi, provider, id, url, position, auto);
    }

    public static async Task<ActorInfoModel> GetActorInfo(string provider, string id,
        CancellationToken cancellationToken)
    {
        return await GetActorInfo(provider, id, true, cancellationToken);
    }

    public static async Task<ActorInfoModel> GetActorInfo(string provider, string id, bool lazy,
        CancellationToken cancellationToken)
    {
        var apiUrl = ComposeInfoApiUrl(ActorInfoApi, provider, id, lazy);
        return await GetDataFromApi<ActorInfoModel>(apiUrl, cancellationToken);
    }

    public static async Task<MovieInfoModel> GetMovieInfo(string provider, string id,
        CancellationToken cancellationToken)
    {
        return await GetMovieInfo(provider, id, true, cancellationToken);
    }

    public static async Task<MovieInfoModel> GetMovieInfo(string provider, string id, bool lazy,
        CancellationToken cancellationToken)
    {
        var apiUrl = ComposeInfoApiUrl(MovieInfoApi, provider, id, lazy);
        return await GetDataFromApi<MovieInfoModel>(apiUrl, cancellationToken);
    }

    public static async Task<List<ActorSearchResultModel>> SearchActor(string q, CancellationToken cancellationToken)
    {
        return await SearchActor(q, string.Empty, false, cancellationToken);
    }

    public static async Task<List<ActorSearchResultModel>> SearchActor(string q, string provider,
        CancellationToken cancellationToken)
    {
        return await SearchActor(q, provider, false, cancellationToken);
    }

    public static async Task<List<ActorSearchResultModel>> SearchActor(string q, string provider,
        bool lazy, CancellationToken cancellationToken)
    {
        var apiUrl = ComposeSearchApiUrl(ActorSearchApi, q, provider, lazy);
        return await GetDataFromApi<List<ActorSearchResultModel>>(apiUrl, cancellationToken);
    }

    public static async Task<List<MovieSearchResultModel>> SearchMovie(string q, CancellationToken cancellationToken)
    {
        return await SearchMovie(q, string.Empty, false, cancellationToken);
    }

    public static async Task<List<MovieSearchResultModel>> SearchMovie(string q, string provider,
        CancellationToken cancellationToken)
    {
        return await SearchMovie(q, provider, false, cancellationToken);
    }

    public static async Task<List<MovieSearchResultModel>> SearchMovie(string q, string provider,
        bool lazy, CancellationToken cancellationToken)
    {
        var apiUrl = ComposeSearchApiUrl(MovieSearchApi, q, provider, lazy);
        return await GetDataFromApi<List<MovieSearchResultModel>>(apiUrl, cancellationToken);
    }

    private static async Task<T> GetDataFromApi<T>(string url, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var httpClient = new HttpClient();
        // Set default timeout: 5 minutes.
        httpClient.Timeout = TimeSpan.FromSeconds(300);
        // Set Accept JSON header.
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        // Set User-Agent header.
        httpClient.DefaultRequestHeaders.Add("User-Agent", Constant.UserAgent);
        // Set Authorization API Token.
        if (!string.IsNullOrWhiteSpace(Plugin.Instance.Configuration.Token))
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Plugin.Instance.Configuration.Token);

        var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStreamAsync(cancellationToken));
    }
}