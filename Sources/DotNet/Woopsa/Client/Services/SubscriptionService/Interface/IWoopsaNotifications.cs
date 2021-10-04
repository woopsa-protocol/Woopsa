using System.Collections.Generic;

namespace Woopsa
{
    public interface IWoopsaNotifications
    {
        IEnumerable<IWoopsaNotification> Notifications { get; }
    }
}

