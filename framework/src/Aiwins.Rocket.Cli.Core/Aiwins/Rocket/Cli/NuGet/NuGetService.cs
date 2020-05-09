using Newtonsoft.Json;
using NuGet.Versioning;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Aiwins.Rocket.Cli.Auth;
using Aiwins.Rocket.Cli.Http;
using Aiwins.Rocket.Cli.Licensing;
using Aiwins.Rocket.Cli.ProjectBuilding;
using Aiwins.Rocket.Cli.ProjectModification;
using Aiwins.Rocket.DependencyInjection;
using Aiwins.Rocket.Json;
using Aiwins.Rocket.Threading;

namespace Aiwins.Rocket.Cli.NuGet
{
    public class NuGetService : ITransientDependency
    {
        public ILogger<AiwinsNugetPackagesVersionUpdater> Logger { get; set; }
        protected IJsonSerializer JsonSerializer { get; }
        protected ICancellationTokenProvider CancellationTokenProvider { get; }
        protected IRemoteServiceExceptionHandler RemoteServiceExceptionHandler { get; }
        private readonly IApiKeyService _apiKeyService;
        private List<string> _proPackageList;
        private DeveloperApiKeyResult _apiKeyResult;

        public NuGetService(
            IJsonSerializer jsonSerializer,
            IRemoteServiceExceptionHandler remoteServiceExceptionHandler,
            ICancellationTokenProvider cancellationTokenProvider,
            IApiKeyService apiKeyService)
        {
            JsonSerializer = jsonSerializer;
            RemoteServiceExceptionHandler = remoteServiceExceptionHandler;
            CancellationTokenProvider = cancellationTokenProvider;
            _apiKeyService = apiKeyService;
            Logger = NullLogger<AiwinsNugetPackagesVersionUpdater>.Instance;
        }

        public async Task<SemanticVersion> GetLatestVersionOrNullAsync(string packageId, bool includePreviews = false, bool includeNightly = false)
        {
            if (AuthService.IsLoggedIn())
            {
                if (_proPackageList == null)
                {
                    _proPackageList = await GetProPackageListAsync();
                }
            }

            string url;
            if (includeNightly)
            {
                url = $"https://www.myget.org/F/rocket-nightly/api/v3/flatcontainer/{packageId.ToLowerInvariant()}/index.json";
            }
            else if (_proPackageList?.Contains(packageId) ?? false)
            {
                url = await GetNuGetUrlForCommercialPackage(packageId);
            }
            else
            {
                url = $"https://api.nuget.org/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json";
            }

            using (var client = new CliHttpClient(setBearerToken: false))
            {
                var responseMessage = await client.GetHttpResponseMessageWithRetryAsync(
                    url,
                    cancellationToken: CancellationTokenProvider.Token,
                    logger: Logger
                );

                await RemoteServiceExceptionHandler.EnsureSuccessfulHttpResponseAsync(responseMessage);

                var responseContent = await responseMessage.Content.ReadAsStringAsync();

                var versions = JsonSerializer
                    .Deserialize<NuGetVersionResultDto>(responseContent)
                    .Versions
                    .Select(SemanticVersion.Parse);

                if (!includePreviews && !includeNightly)
                {
                    versions = versions.Where(x => !x.IsPrerelease);
                }

                var semanticVersions = versions.ToList();
                return semanticVersions.Any() ? semanticVersions.Max() : null;
            }
        }

        private async Task<string> GetNuGetUrlForCommercialPackage(string packageId)
        {
            if (_apiKeyResult == null)
            {
                _apiKeyResult = await _apiKeyService.GetApiKeyOrNullAsync();
            }

            return CliUrls.GetNuGetPackageInfoUrl(_apiKeyResult.ApiKey, packageId);
        }

        private async Task<List<string>> GetProPackageListAsync()
        {
            using var client = new CliHttpClient();

            var url = $"{CliUrls.WwwRocketIo}api/app/nugetPackage/proPackageNames";

            var responseMessage = await client.GetHttpResponseMessageWithRetryAsync(
                url: url,
                cancellationToken: CancellationTokenProvider.Token,
                logger: Logger
            );

            if (responseMessage.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<List<string>>(await responseMessage.Content.ReadAsStringAsync());
            }

            var exceptionMessage = "Remote server returns '" + (int)responseMessage.StatusCode + "-" + responseMessage.ReasonPhrase + "'. ";
            var remoteServiceErrorMessage = await RemoteServiceExceptionHandler.GetRocketRemoteServiceErrorAsync(responseMessage);

            if (remoteServiceErrorMessage != null)
            {
                exceptionMessage += remoteServiceErrorMessage;
            }

            Logger.LogError(exceptionMessage);
            return null;
        }

        public class NuGetVersionResultDto
        {
            [JsonProperty("versions")]
            public List<string> Versions { get; set; }
        }
    }
}
