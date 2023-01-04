using System;
using System.Collections.Generic;
using System.Text;

namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class GetCurrentFileTransfer : Response
{
    [QuerySerialize("clid")]
    public int ClientId;

    [QuerySerialize("path")]
    public string DirPath;

    [QuerySerialize("name")]
    public string FileName;

    [QuerySerialize("size")]
    public long Size;

    [QuerySerialize("sizedone")]
    public long SizeDone;

    [QuerySerialize("clientftfid")]
    public int ClientFileTransferId;

    [QuerySerialize("serverftfid")]
    public int ServerFileTransferId;

    [QuerySerialize("sender")]
    public bool ServerIsSender;

    [QuerySerialize("current_speed")]
    public double CurrentSpeed;

    [QuerySerialize("average_speed")]
    public double AverageSpeed;

    [QuerySerialize("runtime")]
    public int Runtime;
}
