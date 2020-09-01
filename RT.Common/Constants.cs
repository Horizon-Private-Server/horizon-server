using System;
using System.Collections.Generic;
using System.Text;

namespace RT.Common
{
    public static class Constants
    {
        public const int MESSAGEID_MAXLEN = 21;
        public const int SESSIONKEY_MAXLEN = 17;
        public const int ACCOUNTNAME_MAXLEN = 32;
        public const int ACCOUNTSTATS_MAXLEN = 256;
        public const int CLANNAME_MAXLEN = 32;
        public const int CLANSTATS_MAXLEN = 256;
        public const int CLANMSG_MAXLEN = 200;
        public const int PASSWORD_MAXLEN = 32;
        public const int WORLDNAME_MAXLEN = 64;
        public const int WORLDPASSWORD_MAXLEN = 32;
        public const int LOBBYNAME_MAXLEN = 64;
        public const int LOBBYPASSWORD_MAXLEN = WORLDPASSWORD_MAXLEN;
        public const int GAMENAME_MAXLEN = 64;
        public const int GAMEPASSWORD_MAXLEN = 32;
        public const int GAMESTATS_MAXLEN = 256;
        public const int WINNINGTEAM_MAXLEN = 64;
        public const int DNASSIGNATURE_MAXLEN = 32;
        public const int ANNOUNCEMENT_MAXLEN = 1000;
        public const int MEDIUS_GENERIC_CHAT_FILTER_BYTES_LEN = 16;
        public const int MEDIUS_MESSAGE_MAXLEN = 512;
        public const int POLICY_MAXLEN = 256;
        public const int PLAYERNAME_MAXLEN = 32;
        public const int APPNAME_MAXLEN = 32;
        public const int CHATMESSAGE_MAXLEN = 64;
        public const int BINARYMESSAGE_MAXLEN = 400;
        public const int IP_MAXLEN = 20;

        public const int UNIVERSENAME_MAXLEN = 128;
        public const int UNIVERSEDNS_MAXLEN = 128;
        public const int UNIVERSEDESCRIPTION_MAXLEN = 256;
        public const int UNIVERSE_BSP_MAXLEN = 8;
        public const int UNIVERSE_BSP_NAME_MAXLEN = 128;
        public const int UNIVERSE_EXTENDED_INFO_MAXLEN = 128;
        public const int UNIVERSE_SVO_URL_MAXLEN = 128;

        public const int LADDERSTATSWIDE_MAXLEN = 100;

        public const int NET_SESSION_KEY_LEN = 17;
        public const int NET_ACCESS_KEY_LEN = 17;
        public const int NET_MAX_NETADDRESS_LENGTH = 16;
        public const int NET_ADDRESS_LIST_COUNT = 2;

        public const int RSA_SIZE_DWORD = 16;

        public const int MGCL_MESSAGEID_MAXLEN = 21;
        public const int MGCL_SERVERVERSION_MAXLEN = 16;
        public const int MGCL_GAMENAME_MAXLEN = 64;
        public const int MGCL_GAMESTATS_MAXLEN = 256;
        public const int MGCL_GAMEPASSWORD_MAXLEN = 32;
        public const int MGCL_SERVERIP_MAXLEN = 20;
        public const int MGCL_ACCESSKEY_MAXLEN = 17;
        public const int MGCL_SESSIONKEY_MAXLEN = 17;

        public const int MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE = 464;
        public const int MEDIUS_FILE_MAX_FILENAME_LENGTH = 128;
        public const int MEDIUS_FILE_CHECKSUM_NUMBYTES = 16;
        public const int MEDIUS_FILE_MAX_DESCRIPTION_LENGTH = 256;

        public const int DME_FRAGMENT_MAX_PAYLOAD_SIZE = MEDIUS_FILE_MAX_DOWNLOAD_DATA_SIZE + 24;
        public const int DME_VERSION_LENGTH = 16;

        public const int BUFFER_SIZE = 1500;
    }
}
