using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core
{
    public class Status
    {
        #region StatusType enum

        public enum StatusType
        {
            Info,
            Warning,
            Error,
            Success,
            Progress
        }

        #endregion

        public Status(StatusType type, string message)
        {
            Type = type;
            Message = message;
        }

        public Status(float percentage)
        {
            Percentage = percentage;
            Type = StatusType.Progress;
        }

        public string Message { get; }

        public float Percentage { get; } = -1;

        public StatusType Type { get; }
    }
}