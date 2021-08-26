using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Database.Models
{
    public class ClanDTO
    {

        public int ClanId { get; set; }
        public string ClanName { get; set; }
        public AccountDTO ClanLeaderAccount { get; set; }
        public List<AccountDTO> ClanMemberAccounts { get; set; }
        public int AppId { get; set; }
        public bool IsDisbanded { get; set; }
        public string ClanMediusStats { get; set; }

        /// <summary>
        /// Collection of ladder stats.
        /// </summary>
        public int[] ClanWideStats { get; set; }
    }

    public class CreateClanDTO
    {
        public string ClanName { get; set; }
        public int AccountId { get; set; }
        public int AppId { get; set; }
    }

    public class ClanInvitationDTO
    {
        public int ClanId { get; set; }
        public string ClanName { get; set; }
        public int TargetAccountId { get; set; }
        public int AppId { get; set; }
    }
}
