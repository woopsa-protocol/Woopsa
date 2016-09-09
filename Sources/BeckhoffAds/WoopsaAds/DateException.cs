using System;

namespace WoopsaAds
{
    public class DateException
    {
        public Exception exception { get; set; }
        public DateTime arrivalTime { get; set; }

        public DateException(Exception ex, DateTime dateTime)
        {
            exception = ex;
            arrivalTime = dateTime;
        }
    }
}
