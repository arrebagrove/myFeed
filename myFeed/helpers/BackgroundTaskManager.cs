using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace myFeed
{
    public static class BackgroundTaskManager
    {
        public static async void RegisterNotifier(uint CheckTime)
        {
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (backgroundAccessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                backgroundAccessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == "myFeedNotify") task.Value.Unregister(true);
                }

                if (CheckTime != 0)
                {
                    BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
                    builder.Name = "myFeedNotify";
                    builder.TaskEntryPoint = "myFeed.FeedUpdater.Notify";
                    builder.SetTrigger(new TimeTrigger(CheckTime, false)); /// Note: Time measures in minutes here, e.g. 30 = 30 minutes
                    builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                    builder.Register();
                }
            }
        }
    }
}
