using System;

namespace TeamSpeak3QueryApi.Net.Specialized
{
    public class ServerEdited : Notify
    {
        [QuerySerialize("reasonid")]
        public ReasonId Reason;

        [QuerySerialize("invokerid")]
        public int InvokerId;

        [QuerySerialize("invokername")]
        public string InvokerName;

        [QuerySerialize("invokeruid")]
        public string InvokerUid;

        public string virtualserver_name;

        public int virtualserver_codec_encryption_mode;

        public int virtualserver_default_server_group;

        public int virtualserver_default_channel_group;

        public string virtualserver_hostbanner_url;

        public string virtualserver_hostbanner_gfx_url;

        public TimeSpan virtualserver_hostbanner_gfx_interval;

        public string virtualserver_priority_speaker_dimm_modificator;

        public string virtualserver_hostbutton_tooltip;

        public string virtualserver_hostbutton_url;

        public string virtualserver_hostbutton_gfx_url;

        public string virtualserver_name_phonetic;

        public long virtualserver_icon_id;

        public int virtualserver_hostbanner_mode;

        public int virtualserver_channel_temp_delete_delay_default;
    }
}
