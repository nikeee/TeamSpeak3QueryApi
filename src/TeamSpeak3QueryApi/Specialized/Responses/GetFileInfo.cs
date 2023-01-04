using System;
using System.Collections.Generic;
using System.Text;

namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class GetFileInfo : Response
{
    [QuerySerialize("name")]
    public string FilePath;

    [QuerySerialize("size")]
    public long Size;

    [QuerySerialize("datetime")]
    public DateTime Modified;
}
