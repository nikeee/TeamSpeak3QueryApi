using System;
using System.Collections.Generic;
using System.Text;

namespace TeamSpeak3QueryApi.Net.Specialized.Responses;

public class InitDownload : Response
{
    [QuerySerialize("clientftfid")]
    public int ClientFileTransferId;

    [QuerySerialize("serverftfid")]
    public int ServerFileTransferId;

    [QuerySerialize("ftkey")]
    public string FileTransferKey;

    [QuerySerialize("port")]
    public int Port;

    [QuerySerialize("size")]
    public long Size;
}
