using Deadlocked.Server.Stream;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deadlocked.Server.Messages.Lobby
{
    [MediusApp(MediusAppPacketIds.PolicyResponse)]
    public class MediusGetPolicyResponse : BaseLobbyMessage
    {

        public override MediusAppPacketIds Id => MediusAppPacketIds.PolicyResponse;

        public MediusCallbackStatus StatusCode;
        public string Policy; // POLICY_MAXLEN
        public bool EndOfText;

        public override void Deserialize(BinaryReader reader)
        {
            // 
            base.Deserialize(reader);

            // 
            reader.ReadBytes(3);
            StatusCode = reader.Read<MediusCallbackStatus>();
            Policy = reader.ReadString(MediusConstants.POLICY_MAXLEN);
            EndOfText = reader.ReadBoolean();
        }

        public override void Serialize(BinaryWriter writer)
        {
            // 
            base.Serialize(writer);

            // 
            writer.Write(new byte[3]);
            writer.Write(StatusCode);
            writer.Write(Policy, MediusConstants.POLICY_MAXLEN);
            writer.Write(EndOfText);
        }


        public override string ToString()
        {
            return base.ToString() + " " +
             $"StatusCode:{StatusCode}" + " " +
$"Policy:{Policy}" + " " +
$"EndOfText:{EndOfText}";
        }

        public static List<MediusGetPolicyResponse> FromText(string policy)
        {
            List<MediusGetPolicyResponse> policies = new List<MediusGetPolicyResponse>();
            int i = 0;

            while (i < policy.Length)
            {
                // Determine length of string
                int len = policy.Length - i;
                if (len > MediusConstants.POLICY_MAXLEN)
                    len = MediusConstants.POLICY_MAXLEN;

                // Add policy subtext
                policies.Add(new MediusGetPolicyResponse()
                {
                    StatusCode = MediusCallbackStatus.MediusSuccess,
                    Policy = policy.Substring(i, len)
                });

                // Increment i
                i += len;
            }

            // Set end of text
            policies[policies.Count - 1].EndOfText = true;

            //
            return policies;
        }
    }
}