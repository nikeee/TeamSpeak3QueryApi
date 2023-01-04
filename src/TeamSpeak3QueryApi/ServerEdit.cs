using System;

namespace TeamSpeak3QueryApi.Net.Specialized;

[Flags]
public enum ServerEdit
{
    virtualserver_name,
    virtualserver_welcomemessage,
    virtualserver_maxclients,
    virtualserver_password,
    virtualserver_codec_encryption_mode,
    virtualserver_hostmessage,
    virtualserver_hostmessage_mode,
    virtualserver_default_server_group,
    virtualserver_default_channel_group,
    virtualserver_flag_password,
    virtualserver_default_channel_admin_group,
    virtualserver_hostbanner_url,
    virtualserver_hostbanner_gfx_url,
    virtualserver_hostbanner_gfx_interval,
    virtualserver_complain_autoban_count,
    virtualserver_complain_autoban_time,
    virtualserver_complain_remove_time,
    virtualserver_min_clients_in_channel_before_forced_silence,
    virtualserver_antiflood_points_tick_reduce,
    virtualserver_antiflood_points_needed_command_block,
    virtualserver_antiflood_points_needed_ip_block,
    virtualserver_hostbutton_tooltip,
    virtualserver_hostbutton_url,
    virtualserver_hostbutton_gfx_url,
    virtualserver_needed_identity_security_level,
    virtualserver_min_client_version,
    virtualserver_name_phonetic,
    virtualserver_icon_id,
    virtualserver_reserved_slots,
    virtualserver_weblist_enabled,
    virtualserver_hostbanner_mode,
    virtualserver_channel_temp_delete_delay_default,
    virtualserver_min_android_version,
    virtualserver_min_ios_version,
    virtualserver_nickname,
    virtualserver_antiflood_points_needed_plugin_block,
}
