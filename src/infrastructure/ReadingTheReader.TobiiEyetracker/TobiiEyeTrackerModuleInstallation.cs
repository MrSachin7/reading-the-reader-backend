using Microsoft.Extensions.DependencyInjection;
using ReadingTheReader.core.Application.ApplicationContracts.EyeTracker;

namespace ReadingTheReader.TobiiEyetracker;

public static class TobiiEyeTrackerModuleInstallation {

    public static IServiceCollection InstallTobiiEyeTrackerModule(this IServiceCollection collection) {
        collection.AddScoped<IEyeTrackerManager, TobiiEyeTrackerManager>();
        return collection;
    }
}