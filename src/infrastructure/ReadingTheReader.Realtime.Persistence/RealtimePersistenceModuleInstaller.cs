using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReadingTheReader.core.Application.InfrastructureContracts;

namespace ReadingTheReader.Realtime.Persistence;

public static class RealtimePersistenceModuleInstaller
{
    public static IServiceCollection InstallRealtimePersistenceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ExperimentPersistenceOptions>(configuration.GetSection(ExperimentPersistenceOptions.SectionName));

        var options = configuration.GetSection(ExperimentPersistenceOptions.SectionName).Get<ExperimentPersistenceOptions>()
            ?? new ExperimentPersistenceOptions();

        if (string.Equals(options.Provider, "File", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IExperimentStateStoreAdapter>(_ => new FileSnapshotExperimentStateStoreAdapter(options.SnapshotFilePath));
        }
        else
        {
            services.AddSingleton<IExperimentStateStoreAdapter, InMemoryExperimentStateStoreAdapter>();
        }

        services.AddSingleton<IEyeTrackerLicenseStoreAdapter, FileEyeTrackerLicenseStoreAdapter>();
        services.AddHostedService<ExperimentStateCheckpointWorker>();

        return services;
    }
}
