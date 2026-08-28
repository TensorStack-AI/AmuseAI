namespace Amuse.Common.Message
{
    public sealed class CommandRequest
    {
        public CommandRequest() { }
        public CommandRequest(CommandRequestType type)
        {
            Type = type;
        }

        public CommandRequestType Type { get; init; }
    }

    public enum CommandRequestType
    {
        Cancel = 0,
        Complete = 1
    }
}
