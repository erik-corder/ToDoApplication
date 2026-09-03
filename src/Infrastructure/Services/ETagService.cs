using System;
using Application.Common.Interfaces;

namespace Infrastructure.Services;

public class ETagService : IETagService
{
    public string Encode(byte[] rowVersion) => Convert.ToBase64String(rowVersion ?? Array.Empty<byte>());

    public byte[] Decode(string etag)
    {
        if (string.IsNullOrWhiteSpace(etag))
        {
            throw new ArgumentException("ETag cannot be empty.", nameof(etag));
        }

        var trimmed = etag.Trim();
        if (trimmed.StartsWith("\"") && trimmed.EndsWith("\"") && trimmed.Length >= 2)
        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2);
        }
        if (trimmed.StartsWith("W/") && trimmed.Length > 2)
        {
            trimmed = trimmed.Substring(2);
        }
        return Convert.FromBase64String(trimmed);
    }
}
