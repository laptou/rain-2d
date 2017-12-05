using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ibinimator.Core {
    public class Status
    {
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

        public float Percentage { get; } = -1;

        public StatusType Type { get; }

        public string Message { get; }

        public enum StatusType
        {
            Info,
            Warning,
            Error,
            Success,
            Progress
        }
    }
}