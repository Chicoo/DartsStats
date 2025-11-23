using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aspire.Hosting.ApplicationModel;
using DevProxy.Hosting;

namespace DartsStats.AppHost;

/// <summary>
/// Extension methods for Aspire AppHost resources.
/// </summary>
public static class AppHostExtensions
{
    extension<T>(IResourceBuilder<T> builder) where T : IResource, IResourceWithEndpoints
    {
        /// <summary>
        /// Configures the proxy environment variables and restart the target resource.
        /// </summary>
        /// <typeparam name="TApi">The type of the API resource</typeparam>
        /// <param name="apiResource">The API resource builder to proxy</param>
        /// <param name="proxyUrl">The proxy URL. If null, uses the Dev Proxy endpoint from the builder resource.</param>
        /// <returns>The Dev Proxy resource builder for chaining</returns>
        public IResourceBuilder<T> WithProxy<TApi>(
            IResourceBuilder<TApi> apiResource,
            string? proxyUrl = null) where TApi : IResourceWithEnvironment
        {
            

            builder.OnResourceReady(async (resource, evt, cancellationToken) =>
            {
                var url = proxyUrl ?? builder.GetEndpoint(DevProxyResource.ProxyEndpointName).Url;
                apiResource
                    .WithEnvironment("HTTP_PROXY", url)
                    .WithEnvironment("HTTPS_PROXY", url);
                
                var logger = evt.Services.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Dev Proxy is ready. Restarting API to apply proxy settings...");

                // Use ResourceCommandService to execute the restart command on the API resource
                var commandService = evt.Services.GetRequiredService<ResourceCommandService>();

                var result = await commandService.ExecuteCommandAsync(
                    apiResource.Resource, 
                    KnownResourceCommands.RestartCommand, 
                    cancellationToken);

                if (result.Success)
                {
                    logger.LogInformation("API resource restarted successfully.");
                }
                else
                {
                    logger.LogWarning("Failed to restart API resource: {ErrorMessage}", result.ErrorMessage);
                }
            });
            
            builder.OnResourceStopped(async (resource, evt, cancellationToken) =>
            {
                apiResource
                    .WithEnvironment("HTTP_PROXY", string.Empty)
                    .WithEnvironment("HTTPS_PROXY", string.Empty);

                var logger = evt.Services.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Dev Proxy is stopped. Restarting API to restart proxy settings...");

                // Use ResourceCommandService to execute the restart command on the API resource
                var commandService = evt.Services.GetRequiredService<ResourceCommandService>();

                var result = await commandService.ExecuteCommandAsync(
                    apiResource.Resource,
                    KnownResourceCommands.RestartCommand,
                    cancellationToken);

                if (result.Success)
                {
                    logger.LogInformation("API resource restarted successfully.");
                }
                else
                {
                    logger.LogWarning("Failed to restart API resource: {ErrorMessage}", result.ErrorMessage);
                }
            });

            return builder;
        }
        

    }
}