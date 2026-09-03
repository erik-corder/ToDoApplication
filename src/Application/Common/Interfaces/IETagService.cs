using System;

namespace Application.Common.Interfaces;

public interface IETagService
{
    string Encode(byte[] rowVersion);
    byte[] Decode(string etag);
}
