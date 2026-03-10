using Microsoft.Extensions.DependencyInjection;
using ReadingTheReader.core.Application.ApplicationContracts.Participants;
using ReadingTheReader.core.Application.ApplicationContracts.ReadingTexts;
using ReadingTheReader.core.Application.ApplicationContracts.Realtime;

namespace ReadingTheReader.core.Application;

public static class ApplicationModuleInstaller
{
    public static IServiceCollection InstallApplicationModule(this IServiceCollection collection)
    {
        collection.AddSingleton<IParticipantService, ParticipantService>();
        collection.AddSingleton<IReadingTextService, ReadingTextService>();
        collection.AddSingleton<IExperimentSessionManager, ExperimentSessionManager>();
        collection.AddSingleton<IEyeTrackerService, EyeTrackerService>();
        collection.AddSingleton<ICalibrationService, CalibrationService>();
        return collection;
    }
}
