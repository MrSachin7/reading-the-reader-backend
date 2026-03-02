using Microsoft.Extensions.DependencyInjection;
using ReadingTheReader.core.Application.InfrastructureContracts;

namespace ReadingTheReader.TobiiEyetracker;

public static class TobiiEyeTrackerModuleInstallation {

    public static IServiceCollection InstallTobiiEyeTrackerModule(this IServiceCollection collection) {
        collection.AddSingleton<IEyeTrackerManager, TobiiEyeTrackerManager>();
        return collection;
    }
}
