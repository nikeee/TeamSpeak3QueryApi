namespace TeamSpeak3QueryApi.Net
{
    public enum HostMessageMode
    {
        Log = 1, // 1: display message in chatlog
        Modal, // 2: display message in modal dialog
        ModalQuit // 3: display message in modal dialog and close connection
    }

    public enum HostBannerMode
    {
        NoAdjust = 0, // 0: do not adjust
        IgnoreAspect, // 1: adjust but ignore aspect ratio (like TeamSpeak 2)
        KeepAspect // 2: adjust and keep aspect ratio
    }

    public enum Codec
    {
        SpeexNarrowband = 0, // 0: speex narrowband (mono, 16bit, 8kHz)
        SpeexWideband, // 1: speex wideband (mono, 16bit, 16kHz)
        SpeexUltraWideband, // 2: speex ultra-wideband (mono, 16bit, 32kHz)
        CeltMono // 3: celt mono (mono, 16bit, 48kHz)
        /*
            Opus may missing
        */
    }

    public enum CodecEncryptionMode
    {
        Individual = 0, // 0: configure per channel 
        Disabled, // 1: globally disabled 
        Enabled // 2: globally enabled 
    }

    public enum LogLevel
    {
        Error = 1, // 1: everything that is really bad 
        Warning, // 2: everything that might be bad 
        Debug, // 3: output that might help find a problem 
        Info // 4: informational output 
    }

    public enum PermissionGroupDatabaseTypes
    {
        Template = 0, // 0: template group (used for new virtual servers) 
        Regular, // 1: regular group (used for regular clients) 
        Query // 2: global query group (used for ServerQuery clients) 
    }

    public enum PermissionGroupTypes
    {
        ServerGroup = 0, // 0: server group permission 
        GlobalClient, // 1: client specific permission 
        Channel, // 2: channel specific permission 
        ChannelGroup, // 3: channel group permission 
        ChannelClient // 4: channel-client specific permission 
    }

    public enum TokenType
    {
        ServerGroup = 0, // 0: server group token (id1={groupID} id2=0) 
        ChannelGroup // 1: channel group token (id1={groupID} id2={channelID}) 
    }
}
