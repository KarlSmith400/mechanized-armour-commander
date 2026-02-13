using System.Collections.Generic;
using System.Linq;

namespace MechanizedArmourCommander.Core.Models
{
    public class CombatState
    {
        public HexGrid Grid { get; set; } = null!;
        public List<CombatFrame> PlayerFrames { get; set; } = new();
        public List<CombatFrame> EnemyFrames { get; set; } = new();
        public int RoundNumber { get; set; } = 1;
        public TurnPhase Phase { get; set; } = TurnPhase.RoundStart;
        public List<CombatFrame> InitiativeOrder { get; set; } = new();
        public int CurrentInitiativeIndex { get; set; } = 0;
        public CombatLog Log { get; set; } = new();
        public CombatResult Result { get; set; } = CombatResult.Ongoing;

        public CombatFrame? ActiveFrame =>
            CurrentInitiativeIndex < InitiativeOrder.Count
                ? InitiativeOrder[CurrentInitiativeIndex]
                : null;

        public bool IsPlayerTurn =>
            ActiveFrame != null && PlayerFrames.Contains(ActiveFrame);

        public List<CombatFrame> AllFrames =>
            PlayerFrames.Concat(EnemyFrames).ToList();

        public List<CombatFrame> AlivePlayerFrames =>
            PlayerFrames.Where(f => !f.IsDestroyed).ToList();

        public List<CombatFrame> AliveEnemyFrames =>
            EnemyFrames.Where(f => !f.IsDestroyed).ToList();
    }

    public class HitChanceBreakdown
    {
        public int BaseAccuracy { get; set; }
        public int GunneryBonus { get; set; }
        public int EvasionPenalty { get; set; }
        public int RangeModifier { get; set; }
        public int BraceBonus { get; set; }
        public int SensorPenalty { get; set; }
        public int ActuatorPenalty { get; set; }
        public int TerrainDefense { get; set; }
        public int LOSPenalty { get; set; }
        public int EquipmentModifier { get; set; }
        public List<(HexCoord coord, int penalty)> InterveningHexes { get; set; } = new();
        public int FinalHitChance { get; set; }
    }
}
