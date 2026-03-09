using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReadingTheReader.core.Application.InfrastructureContracts;

namespace ReadingTheReader.Realtime.Persistence;

public static class RealtimePersistenceModuleInstaller
{
    public static IServiceCollection InstallRealtimePersistenceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ExperimentPersistenceOptions>(configuration.GetSection(ExperimentPersistenceOptions.SectionName));
        services.Configure<ReadingTextStorageOptions>(configuration.GetSection(ReadingTextStorageOptions.SectionName));

        var options = configuration.GetSection(ExperimentPersistenceOptions.SectionName).Get<ExperimentPersistenceOptions>()
            ?? new ExperimentPersistenceOptions();
        var readingTextOptions = configuration.GetSection(ReadingTextStorageOptions.SectionName).Get<ReadingTextStorageOptions>()
            ?? new ReadingTextStorageOptions();

        if (string.Equals(options.Provider, "File", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IExperimentStateStoreAdapter>(_ => new FileSnapshotExperimentStateStoreAdapter(options.SnapshotFilePath));
        }
        else
        {
            services.AddSingleton<IExperimentStateStoreAdapter, InMemoryExperimentStateStoreAdapter>();
        }

        services.AddSingleton<IReadingTextStoreAdapter>(_ => new FileReadingTextStoreAdapter(readingTextOptions.DirectoryPath));
        services.AddSingleton<IEyeTrackerLicenseStoreAdapter, FileEyeTrackerLicenseStoreAdapter>();
        services.AddHostedService<ExperimentStateCheckpointWorker>();

        return services;
    }
}
