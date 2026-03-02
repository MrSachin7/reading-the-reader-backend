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
            services.AddSingleton<IExperimentStateStore>(_ => new FileSnapshotExperimentStateStore(options.SnapshotFilePath));
        }
        else
        {
            services.AddSingleton<IExperimentStateStore, InMemoryExperimentStateStore>();
        }

        services.AddHostedService<ExperimentStateCheckpointWorker>();

        return services;
    }
}
