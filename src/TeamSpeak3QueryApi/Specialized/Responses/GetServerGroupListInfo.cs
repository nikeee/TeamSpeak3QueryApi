namespace TeamSpeak3QueryApi.Net.Specialized.Responses
{
    public class GetServerGroupListInfo : Response
    {
        [QuerySerialize("sgid")]
        public int Id;

        [QuerySerialize("name")]
        public string Name;

        [QuerySerialize("type")]
        public ServerGroupType ServerGroupType;

        [QuerySerialize("iconid")]
        public int IconId;

        [QuerySerialize("savedb")]
        public int SaveDb;

        [QuerySerialize("sortid")]
        public int SortId;

        [QuerySerialize("namemode")]
        public int NamingMode;

        [QuerySerialize("n_modifyp")]
        public int NeededModifyPower;

        [QuerySerialize("n_member_addp")]
        public int NeededMemberAddPower;

        [QuerySerialize("n_member_remove_p")]
        public int NeededMemberRemovePower;
    }
}
