using RT.Common;

namespace Server.Medius.Models
{
    public class GameListFilter
    {
        public uint FieldID = 0;
        public MediusGameListFilterField FilterField;
        public int BaselineValue;
        public MediusComparisonOperator ComparisonOperator;
        public uint Mask;

        public bool IsMatch(Game game)
        {
            if (game == null)
                return false;

            #region FilterField
            switch (FilterField)
            {
                case MediusGameListFilterField.MEDIUS_FILTER_GAME_LEVEL: return ComparisonOperator.Compare(BaselineValue, game.GameLevel & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_1: return ComparisonOperator.Compare(BaselineValue, game.GenericField1 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_2: return ComparisonOperator.Compare(BaselineValue, game.GenericField2 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_3: return ComparisonOperator.Compare(BaselineValue, game.GenericField3 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_4: return ComparisonOperator.Compare(BaselineValue, game.GenericField4 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_5: return ComparisonOperator.Compare(BaselineValue, game.GenericField5 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_6: return ComparisonOperator.Compare(BaselineValue, game.GenericField6 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_7: return ComparisonOperator.Compare(BaselineValue, game.GenericField7 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_GENERIC_FIELD_8: return ComparisonOperator.Compare(BaselineValue, game.GenericField8 & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_LOBBY_WORLDID: return ComparisonOperator.Compare(BaselineValue, game.Id & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_MAX_PLAYERS: return ComparisonOperator.Compare(BaselineValue, game.MaxPlayers & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_MIN_PLAYERS: return ComparisonOperator.Compare(BaselineValue, game.MinPlayers & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_PLAYER_COUNT: return ComparisonOperator.Compare(BaselineValue, game.PlayerCount & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_PLAYER_SKILL_LEVEL: return ComparisonOperator.Compare(BaselineValue, game.PlayerSkillLevel & Mask);
                case MediusGameListFilterField.MEDIUS_FILTER_RULES_SET: return ComparisonOperator.Compare(BaselineValue, game.RulesSet & Mask);
                default: return false;
            }
            #endregion
        }
    }
}