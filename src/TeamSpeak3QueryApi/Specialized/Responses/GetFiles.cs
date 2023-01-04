using System;
using System.Collections.Generic;
using System.Text;

namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class GetFiles : Response
{
    [QuerySerialize("name")]
    public string Name;

    [QuerySerialize("size")]
    public long Size;

    [QuerySerialize("datetime")]
    public DateTime Modified;

    [QuerySerialize("type")]
    public bool IsFile;
}
