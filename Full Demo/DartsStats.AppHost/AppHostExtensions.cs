using Aspire.Hosting.ApplicationModel;
using DevProxy.Hosting;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DartsStats.AppHost;

/// <summary>
/// Extension methods for Aspire AppHost resources.
/// </summary>
public static class AppHostExtensions
{
    extension<T>(IResourceBuilder<T> builder) where T : IResource, IResourceWithEnvironment
    {
        /// <summary>
        /// Sets the environment variables for DevProxy and restarts the resource when the linked DevProxy resource is started or stopped.
        /// </summary>
        /// <typeparam name="TApi">The type of the resource</typeparam>
        /// <param name="devProxyResource">The DevProxy resource to track</param>
        /// <returns>The resource that is restarted</returns>
        public IResourceBuilder<T> WithDevProxy<TApi>(
            IResourceBuilder<TApi> devProxyResource) where TApi : IResource, IResourceWithEndpoints
        {
            devProxyResource = devProxyResource ?? throw new ArgumentNullException(nameof(devProxyResource));
            if(devProxyResource.Resource is not DevProxyContainerResource)
            {
                throw new ArgumentException("The provided resource is not a DevProxyContainerResource.", nameof(devProxyResource));
            }

            _ = devProxyResource.OnResourceReady(async (resource, evt, cancellationToken) =>
            {
                var logger = evt.Services
                    .GetRequiredService<ResourceLoggerService>()
                    .GetLogger(builder.Resource);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("{DevProxyResourceName} is ready. Restarting {BuilderResourceName} to apply settings...",
                        devProxyResource.Resource.Name, builder.Resource.Name);
                }

                var url = devProxyResource.GetEndpoint(DevProxyResource.ProxyEndpointName).Url;
                builder
                    .WithEnvironment("HTTP_PROXY", url)
                    .WithEnvironment("HTTPS_PROXY", url);

                var commandService = evt.Services.GetRequiredService<ResourceCommandService>();

                var result = await commandService.ExecuteCommandAsync(
                    builder.Resource,
                    KnownResourceCommands.RestartCommand,
                    cancellationToken);

                if (logger.IsEnabled(LogLevel.Information) && result.Success)
                {
                    logger.LogInformation("{BuilderResourceName} resource restarted successfully.", builder.Resource.Name);
                }
                else if (!result.Success && logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Failed to restart {BuilderResourceName} resource: {ErrorMessage}", builder.Resource.Name, result.ErrorMessage);
                }
            });

            devProxyResource.OnResourceStopped(async (resource, evt, cancellationToken) =>
            {
                var logger = evt.Services
                    .GetRequiredService<ResourceLoggerService>()
                    .GetLogger(builder.Resource);

                builder
                    .WithEnvironment("HTTP_PROXY", string.Empty)
                    .WithEnvironment("HTTPS_PROXY", string.Empty);

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("{DevProxyResourceName} is stopped. Restarting {BuilderResourceName} to restart proxy settings...",
                        devProxyResource.Resource.Name, builder.Resource.Name);
                }

                var commandService = evt.Services.GetRequiredService<ResourceCommandService>();

                var result = await commandService.ExecuteCommandAsync(
                    builder.Resource,
                    KnownResourceCommands.RestartCommand,
                    cancellationToken);

                if (logger.IsEnabled(LogLevel.Information) && result.Success)
                {
                    logger.LogInformation("{BuilderResourceName} resource restarted successfully.", builder.Resource.Name);
                }
                else if (!result.Success && logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Failed to restart {BuilderResourceName} resource: {ErrorMessage}", builder.Resource.Name, result.ErrorMessage);
                }
            });

            return builder;
        }
    }
}