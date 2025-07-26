using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StorkStudios.DataWaste
{
    internal class MessageWrapper
    {
        internal enum MessageType
        {
            SendData,
            StatusCheck,
            GetData
        }

        private readonly MessageType type;
        private readonly IData content;
        private readonly DateTime timestamp;
        private readonly TaskCompletionSource<string> task;

        public MessageType Type => type;
        public IData Content => content;
        public DateTime Timestamp => timestamp;
        public TaskCompletionSource<string> Task => task;

        public MessageWrapper(MessageType type, IData content, TaskCompletionSource<string> task = null)
        {
            this.type = type;
            timestamp = DateTime.UtcNow;
            this.content = content;
            this.task = task;
        }

        public MessageWrapper(MessageType type, TaskCompletionSource<string> task)
        {
            this.type = type;
            timestamp = DateTime.UtcNow;
            content = null;
            this.task = task;
        }
    }
}