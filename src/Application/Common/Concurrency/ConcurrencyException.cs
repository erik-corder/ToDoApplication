using Application.Tasks.Dtos;

namespace Application.Common.Concurrency;

public class ConcurrencyException : Exception
{
    public TaskDto Latest { get; }
    public byte[] LatestRowVersion { get; }

    public ConcurrencyException(string message, TaskDto latest, byte[] latestRowVersion)
        : base(message)
    {
        Latest = latest;
        LatestRowVersion = latestRowVersion;
    }
}
