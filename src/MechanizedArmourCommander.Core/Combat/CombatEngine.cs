using MechanizedArmourCommander.Core.Models;

namespace MechanizedArmourCommander.Core.Combat;

/// <summary>
/// Core combat resolution engine for hex grid individual activation combat
/// </summary>
public class CombatEngine
{
    private readonly Random _random = new();
    private readonly PositioningSystem _positioning = new();
    private readonly DamageSystem _damage = new();
    private readonly ReactorSystem _reactor = new();
    private readonly ActionSystem _actions = new();
    private readonly CombatAI _ai = new();
    private const int CriticalHitChance = 5;

    /// <summary>
    /// Initialize a combat state from frames and map size
    /// </summary>
    public CombatState InitializeCombat(List<CombatFrame> playerFrames, List<CombatFrame> enemyFrames, MapSize mapSize, string landscape = "Habitable")
    {
        var (width, height) = HexGrid.GetDimensions(mapSize);
        var grid = new HexGrid(width, height, landscape);

        var state = new CombatState
        {
            Grid = grid,
            PlayerFrames = playerFrames,
            EnemyFrames = enemyFrames,
            RoundNumber = 1,
            Phase = TurnPhase.RoundStart,
            Result = CombatResult.Ongoing
        };

        _positioning.InitializeHexPositions(grid, playerFrames, enemyFrames);
        return state;
    }

    /// <summary>
    /// Initialize combat with only enemy deployment — player deploys manually
    /// </summary>
    public CombatState InitializeCombatForDeployment(List<CombatFrame> playerFrames, List<CombatFrame> enemyFrames, MapSize mapSize, string landscape = "Habitable")
    {
        var (width, height) = HexGrid.GetDimensions(mapSize);
        var grid = new HexGrid(width, height, landscape);

        var state = new CombatState
        {
            Grid = grid,
            PlayerFrames = playerFrames,
            EnemyFrames = enemyFrames,
            RoundNumber = 1,
            Phase = TurnPhase.Deployment,
            Result = CombatResult.Ongoing
        };

        _positioning.InitializeEnemyPositions(grid, enemyFrames);
        return state;
    }

    /// <summary>
    /// Start a new round: refresh energy/AP, build initiative order
    /// </summary>
    public List<CombatEvent> StartRound(CombatState state)
    {
        var events = new List<CombatEvent>();

        foreach (var frame in state.AllFrames.Where(f => !f.IsDestroyed))
        {
            _reactor.RefreshEnergy(frame);
            _actions.RefreshActionPoints(frame);

            if (frame.IsShutDown)
            {
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.ReactorShutdown,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    Message = $"{frame.CustomName} reactor recovering from shutdown..."
                });
            }
        }

        // Build initiative order
        state.InitiativeOrder = state.AllFrames
            .Where(f => !f.IsDestroyed && !f.IsShutDown)
            .OrderByDescending(f => f.Speed)
            .ThenByDescending(f => f.PilotPiloting)
            .ToList();

        state.CurrentInitiativeIndex = 0;
        state.Phase = TurnPhase.AwaitingActivation;

        return events;
    }

    /// <summary>
    /// Advance to the next unit in initiative order.
    /// Returns the active frame, or null if round is over.
    /// </summary>
    public CombatFrame? AdvanceActivation(CombatState state)
    {
        while (state.CurrentInitiativeIndex < state.InitiativeOrder.Count)
        {
            var frame = state.InitiativeOrder[state.CurrentInitiativeIndex];
            if (!frame.IsDestroyed && !frame.IsShutDown && !frame.HasActedThisRound)
            {
                state.Phase = state.IsPlayerTurn ? TurnPhase.PlayerInput : TurnPhase.AIActing;
                return frame;
            }
            state.CurrentInitiativeIndex++;
        }

        state.Phase = TurnPhase.RoundEnd;
        return null;
    }

    /// <summary>
    /// Execute a single player action on the hex grid
    /// </summary>
    public List<CombatEvent> ExecuteAction(CombatState state, CombatFrame frame,
        CombatAction action, HexCoord? targetHex = null, int? targetFrameId = null,
        int? weaponGroupId = null, HitLocation? calledShotLocation = null)
    {
        var events = new List<CombatEvent>();

        if (!_actions.CanPerformAction(frame, action))
            return events;

        switch (action)
        {
            case CombatAction.Move:
                if (targetHex.HasValue)
                    events.AddRange(ExecuteHexMove(state, frame, targetHex.Value, false));
                break;

            case CombatAction.Sprint:
                if (targetHex.HasValue)
                    events.AddRange(ExecuteHexMove(state, frame, targetHex.Value, true));
                break;

            case CombatAction.FireGroup:
                if (weaponGroupId.HasValue && targetFrameId.HasValue)
                {
                    var target = state.AllFrames.FirstOrDefault(f => f.InstanceId == targetFrameId.Value);
                    if (target != null)
                        events.AddRange(ExecuteHexFireGroup(state, frame, weaponGroupId.Value, target, null));
                }
                break;

            case CombatAction.CalledShot:
                if (weaponGroupId.HasValue && targetFrameId.HasValue && calledShotLocation.HasValue)
                {
                    var target = state.AllFrames.FirstOrDefault(f => f.InstanceId == targetFrameId.Value);
                    if (target != null)
                        events.AddRange(ExecuteHexFireGroup(state, frame, weaponGroupId.Value, target, calledShotLocation));
                }
                break;

            case CombatAction.Brace:
                frame.IsBracing = true;
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.Brace,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    Message = $"{frame.CustomName} braces for impact (+20% evasion)"
                });
                break;

            case CombatAction.Overwatch:
                frame.IsOnOverwatch = true;
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.Overwatch,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    Message = $"{frame.CustomName} sets overwatch"
                });
                break;

            case CombatAction.VentReactor:
                events.Add(_reactor.VentReactor(frame));
                break;
        }

        _actions.ConsumeActionPoints(frame, action);
        return events;
    }

    /// <summary>
    /// Execute a full AI turn for the current frame
    /// </summary>
    public List<CombatEvent> ExecuteAITurn(CombatState state, CombatFrame frame, TacticalOrders orders)
    {
        var events = new List<CombatEvent>();

        bool isPlayerFrame = state.PlayerFrames.Contains(frame);
        var enemies = isPlayerFrame ? state.AliveEnemyFrames : state.AlivePlayerFrames;

        if (!enemies.Any())
            return events;

        var frameActions = _ai.GenerateHexActions(frame, enemies, orders, _actions, state.Grid);
        var target = frameActions.FocusTargetId.HasValue
            ? enemies.FirstOrDefault(e => e.InstanceId == frameActions.FocusTargetId.Value) ?? enemies.First()
            : _ai.SelectTarget(frame, enemies, orders.TargetPriority);

        foreach (var action in frameActions.Actions)
        {
            if (frame.ActionPoints <= 0 || frame.IsDestroyed) break;

            switch (action.Action)
            {
                case CombatAction.Move:
                    if (action.TargetHex.HasValue)
                        events.AddRange(ExecuteHexMove(state, frame, action.TargetHex.Value, false));
                    break;

                case CombatAction.Sprint:
                    if (action.TargetHex.HasValue)
                        events.AddRange(ExecuteHexMove(state, frame, action.TargetHex.Value, true));
                    break;

                case CombatAction.FireGroup:
                    if (action.WeaponGroupId.HasValue)
                        events.AddRange(ExecuteHexFireGroup(state, frame, action.WeaponGroupId.Value, target, null));
                    break;

                case CombatAction.CalledShot:
                    if (action.WeaponGroupId.HasValue && action.CalledShotLocation.HasValue)
                        events.AddRange(ExecuteHexFireGroup(state, frame, action.WeaponGroupId.Value, target, action.CalledShotLocation));
                    break;

                case CombatAction.Brace:
                    frame.IsBracing = true;
                    events.Add(new CombatEvent
                    {
                        Type = CombatEventType.Brace,
                        AttackerId = frame.InstanceId,
                        AttackerName = frame.CustomName,
                        Message = $"{frame.CustomName} braces for impact (+20% evasion)"
                    });
                    break;

                case CombatAction.Overwatch:
                    frame.IsOnOverwatch = true;
                    events.Add(new CombatEvent
                    {
                        Type = CombatEventType.Overwatch,
                        AttackerId = frame.InstanceId,
                        AttackerName = frame.CustomName,
                        Message = $"{frame.CustomName} sets overwatch"
                    });
                    break;

                case CombatAction.VentReactor:
                    events.Add(_reactor.VentReactor(frame));
                    break;
            }

            _actions.ConsumeActionPoints(frame, action.Action);
        }

        frame.HasActedThisRound = true;
        return events;
    }

    /// <summary>
    /// End the current activation and move to the next unit
    /// </summary>
    public void EndActivation(CombatState state)
    {
        if (state.ActiveFrame != null)
            state.ActiveFrame.HasActedThisRound = true;
        state.CurrentInitiativeIndex++;
    }

    /// <summary>
    /// Process end-of-round: overwatch, reactor stress, check combat end
    /// </summary>
    public List<CombatEvent> EndRound(CombatState state)
    {
        var events = new List<CombatEvent>();

        events.AddRange(ResolveOverwatch(state));

        foreach (var frame in state.AllFrames.Where(f => !f.IsDestroyed))
        {
            var reactorEvents = _reactor.ProcessEndOfRound(frame);
            events.AddRange(reactorEvents);
        }

        CheckCombatEnd(state);

        if (state.Result == CombatResult.Ongoing)
        {
            state.RoundNumber++;
            if (state.RoundNumber > 30)
            {
                state.Result = CombatResult.Stalemate;
                state.Phase = TurnPhase.CombatOver;
            }
            else
            {
                state.Phase = TurnPhase.RoundStart;
            }
        }

        return events;
    }

    /// <summary>
    /// Auto-resolve entire combat (AI vs AI)
    /// </summary>
    public CombatLog ResolveCombat(CombatState state, TacticalOrders playerOrders, TacticalOrders enemyOrders)
    {
        while (state.Result == CombatResult.Ongoing)
        {
            if (_ai.ShouldWithdraw(state.PlayerFrames, playerOrders.WithdrawalThreshold))
            {
                state.Result = CombatResult.Withdrawal;
                state.Log.Rounds.Add(new CombatRound
                {
                    RoundNumber = state.RoundNumber,
                    Events = new List<CombatEvent>
                    {
                        new CombatEvent { Type = CombatEventType.Movement, Message = "Player forces withdraw from combat!" }
                    }
                });
                break;
            }

            if (_ai.ShouldWithdraw(state.EnemyFrames, enemyOrders.WithdrawalThreshold))
            {
                state.Result = CombatResult.Victory;
                state.Log.Rounds.Add(new CombatRound
                {
                    RoundNumber = state.RoundNumber,
                    Events = new List<CombatEvent>
                    {
                        new CombatEvent { Type = CombatEventType.Movement, Message = "Enemy forces withdraw from combat!" }
                    }
                });
                break;
            }

            var roundEvents = new List<CombatEvent>();
            roundEvents.AddRange(StartRound(state));

            while (true)
            {
                var frame = AdvanceActivation(state);
                if (frame == null) break;

                bool isPlayer = state.PlayerFrames.Contains(frame);
                var orders = isPlayer ? playerOrders : enemyOrders;
                roundEvents.AddRange(ExecuteAITurn(state, frame, orders));
                EndActivation(state);
            }

            roundEvents.AddRange(EndRound(state));

            state.Log.Rounds.Add(new CombatRound
            {
                RoundNumber = state.RoundNumber - 1,
                Events = roundEvents
            });
        }

        state.Log.Result = state.Result;
        state.Log.TotalRounds = state.Log.Rounds.Count;
        state.Phase = TurnPhase.CombatOver;
        return state.Log;
    }

    #region Hex Movement

    private List<CombatEvent> ExecuteHexMove(CombatState state, CombatFrame frame, HexCoord targetHex, bool isSprint)
    {
        var events = new List<CombatEvent>();

        int maxRange = isSprint
            ? PositioningSystem.GetSprintRange(frame)
            : PositioningSystem.GetEffectiveHexMovement(frame);

        if (maxRange <= 0)
        {
            events.Add(new CombatEvent
            {
                Type = CombatEventType.Movement,
                AttackerId = frame.InstanceId,
                AttackerName = frame.CustomName,
                Message = $"{frame.CustomName} cannot move (legs destroyed)"
            });
            return events;
        }

        var reachable = HexPathfinding.GetReachableHexes(state.Grid, frame.HexPosition, maxRange);
        if (!reachable.Contains(targetHex))
        {
            events.Add(new CombatEvent
            {
                Type = CombatEventType.Movement,
                AttackerId = frame.InstanceId,
                AttackerName = frame.CustomName,
                Message = $"{frame.CustomName} target hex out of range"
            });
            return events;
        }

        int energyCost = _positioning.GetMovementEnergyCost(frame);
        if (isSprint) energyCost *= 2;

        if (!_reactor.ConsumeEnergy(frame, energyCost))
        {
            events.Add(new CombatEvent
            {
                Type = CombatEventType.Movement,
                AttackerId = frame.InstanceId,
                AttackerName = frame.CustomName,
                Message = $"{frame.CustomName} insufficient energy to {(isSprint ? "sprint" : "move")} (need {energyCost})"
            });
            return events;
        }

        var oldPos = frame.HexPosition;
        state.Grid.MoveFrame(frame.InstanceId, oldPos, targetHex);
        frame.HexPosition = targetHex;

        int hexesMoved = HexCoord.Distance(oldPos, targetHex);
        string moveType = isSprint ? "sprints" : "moves";

        events.Add(new CombatEvent
        {
            Type = CombatEventType.Movement,
            AttackerId = frame.InstanceId,
            AttackerName = frame.CustomName,
            Message = $"{frame.CustomName} {moveType} {hexesMoved} hex{(hexesMoved != 1 ? "es" : "")} ({energyCost}E)"
        });

        events.AddRange(CheckOverwatchTriggers(state, frame));

        return events;
    }

    #endregion

    #region Hex Firing

    private List<CombatEvent> ExecuteHexFireGroup(CombatState state, CombatFrame frame,
        int groupId, CombatFrame target, HitLocation? calledShotLocation)
    {
        var events = new List<CombatEvent>();

        if (!frame.WeaponGroups.TryGetValue(groupId, out var weapons))
            return events;

        int hexDistance = HexCoord.Distance(frame.HexPosition, target.HexPosition);
        bool isCalledShot = calledShotLocation.HasValue;

        foreach (var weapon in weapons.Where(w => !w.IsDestroyed))
        {
            int maxRange = PositioningSystem.GetWeaponMaxRange(weapon.RangeClass);
            if (hexDistance > maxRange)
            {
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.Miss,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    Message = $"{weapon.Name} out of range ({hexDistance} hexes, max {maxRange})"
                });
                continue;
            }

            if (weapon.EnergyCost > 0 && !_reactor.ConsumeEnergy(frame, weapon.EnergyCost))
            {
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.Miss,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    Message = $"{weapon.Name} cannot fire - insufficient reactor energy"
                });
                continue;
            }

            if (weapon.AmmoPerShot > 0)
            {
                int currentAmmo = frame.AmmoByType.GetValueOrDefault(weapon.AmmoType, 0);
                if (currentAmmo < weapon.AmmoPerShot)
                {
                    events.Add(new CombatEvent
                    {
                        Type = CombatEventType.Miss,
                        AttackerId = frame.InstanceId,
                        AttackerName = frame.CustomName,
                        Message = $"{weapon.Name} cannot fire - out of {weapon.AmmoType} ammo"
                    });
                    continue;
                }
                frame.AmmoByType[weapon.AmmoType] = currentAmmo - weapon.AmmoPerShot;
            }

            bool hit = RollToHit(frame, target, weapon, hexDistance, isCalledShot, state.Grid);

            if (hit)
            {
                // Point Defense System: chance to negate missile hits
                if (weapon.WeaponType == "Missile" && target.HasEquipment("MissileDefense"))
                {
                    int missileDefenseChance = target.GetEquipmentValue("MissileDefense");
                    if (_random.Next(100) < missileDefenseChance)
                    {
                        events.Add(new CombatEvent
                        {
                            Type = CombatEventType.Miss,
                            AttackerId = frame.InstanceId,
                            AttackerName = frame.CustomName,
                            DefenderId = target.InstanceId,
                            DefenderName = target.CustomName,
                            Message = $"{weapon.Name} intercepted by {target.CustomName}'s Point Defense System!"
                        });
                        continue;
                    }
                }

                bool isCritical = _random.Next(100) < CriticalHitChance;
                int damage = weapon.Damage;
                HitLocation hitLocation = calledShotLocation ?? _damage.RollHitLocation();

                var damageEvents = _damage.ApplyDamage(target, hitLocation, damage,
                    frame.InstanceId, frame.CustomName, weapon.Name);

                events.Add(new CombatEvent
                {
                    Type = isCritical ? CombatEventType.Critical : CombatEventType.Hit,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    DefenderId = target.InstanceId,
                    DefenderName = target.CustomName,
                    Damage = damage,
                    TargetLocation = hitLocation,
                    IsCritical = isCritical,
                    Message = $"{weapon.Name} {(isCritical ? "CRITICAL HIT" : "HIT")} - {damage} damage to {target.CustomName} {DamageSystem.FormatLocation(hitLocation)}"
                });

                events.AddRange(damageEvents);

                if (target.IsDestroyed)
                    state.Grid.RemoveFrame(target.HexPosition);
            }
            else
            {
                events.Add(new CombatEvent
                {
                    Type = CombatEventType.Miss,
                    AttackerId = frame.InstanceId,
                    AttackerName = frame.CustomName,
                    DefenderId = target.InstanceId,
                    DefenderName = target.CustomName,
                    Message = $"{weapon.Name} MISS - shot at {target.CustomName} ({hexDistance} hexes)"
                });
            }
        }

        return events;
    }

    private bool RollToHit(CombatFrame attacker, CombatFrame target, EquippedWeapon weapon,
        int hexDistance, bool isCalledShot, HexGrid grid)
    {
        int baseChance = weapon.BaseAccuracy;
        int gunneryBonus = attacker.PilotGunnery * 2;
        int evasionPenalty = target.Evasion;
        int rangeModifier = _positioning.GetHexRangeAccuracyModifier(weapon, hexDistance);
        int braceBonus = target.IsBracing ? 20 : 0;
        int sensorPenalty = attacker.HasSensorHit ? 10 : 0;
        int calledShotPenalty = isCalledShot ? 15 : 0;

        // Terrain defense bonus for target's hex
        var targetCell = grid.GetCell(target.HexPosition);
        int terrainDefense = targetCell != null ? HexGrid.GetTerrainDefenseBonus(targetCell.Terrain) : 0;

        // LOS penalty from intervening terrain (forest/rocks between attacker and target)
        var (losPenalty, _) = grid.GetLOSPenalty(attacker.HexPosition, target.HexPosition);

        int actuatorPenalty = 0;
        if (weapon.MountLocation is HitLocation.LeftArm or HitLocation.RightArm)
        {
            actuatorPenalty = attacker.DamagedComponents.Count(c =>
                c.Type == ComponentDamageType.ActuatorDamaged
                && c.Location == weapon.MountLocation) * 10;
        }

        // Equipment modifiers (attacker bonuses + target defense equipment)
        int equipmentBonus = GetEquipmentAccuracyModifiers(attacker, target, weapon, hexDistance);

        int hitChance = baseChance + gunneryBonus - evasionPenalty + rangeModifier
            - braceBonus - sensorPenalty - calledShotPenalty - actuatorPenalty - terrainDefense - losPenalty
            + equipmentBonus;
        hitChance = Math.Clamp(hitChance, 5, 95);

        return _random.Next(100) < hitChance;
    }

    /// <summary>
    /// Calculate net accuracy modifier from equipment on both attacker and target.
    /// Positive = helps attacker, negative = helps defender.
    /// </summary>
    private static int GetEquipmentAccuracyModifiers(CombatFrame attacker, CombatFrame target,
        EquippedWeapon weapon, int hexDistance)
    {
        int modifier = 0;

        // Attacker: Gyro Stabilizer — reduces evasion penalty on attacks
        modifier += attacker.GetEquipmentValue("EvasionReduction");

        // Attacker: Sensor Array — bonus accuracy at Long range
        if (weapon.RangeClass == "Long")
            modifier += attacker.GetEquipmentValue("LongRangeBonus");

        // Target: Stealth Plating — defense bonus (harder to hit)
        modifier -= target.GetEquipmentValue("StealthPlating");

        // Target: Phantom Emitter — penalty to attackers beyond 5 hexes
        if (hexDistance > 5)
            modifier -= target.GetEquipmentValue("RangedECM");

        // Target: Countermeasure Suite (active) — only if activated this round
        var activeEcm = target.Equipment.FirstOrDefault(e => e.Effect == "ECM" && e.IsActive);
        if (activeEcm != null)
            modifier -= activeEcm.EffectValue;

        return modifier;
    }

    /// <summary>
    /// Calculate hit chance breakdown for UI display (does not roll dice).
    /// </summary>
    public HitChanceBreakdown GetHitChanceBreakdown(CombatFrame attacker, CombatFrame target,
        EquippedWeapon weapon, int hexDistance, HexGrid grid)
    {
        int baseChance = weapon.BaseAccuracy;
        int gunneryBonus = attacker.PilotGunnery * 2;
        int evasionPenalty = target.Evasion;
        int rangeModifier = _positioning.GetHexRangeAccuracyModifier(weapon, hexDistance);
        int braceBonus = target.IsBracing ? 20 : 0;
        int sensorPenalty = attacker.HasSensorHit ? 10 : 0;

        var targetCell = grid.GetCell(target.HexPosition);
        int terrainDefense = targetCell != null ? HexGrid.GetTerrainDefenseBonus(targetCell.Terrain) : 0;

        var (losPenalty, interveningHexes) = grid.GetLOSPenalty(attacker.HexPosition, target.HexPosition);

        int actuatorPenalty = 0;
        if (weapon.MountLocation is HitLocation.LeftArm or HitLocation.RightArm)
        {
            actuatorPenalty = attacker.DamagedComponents.Count(c =>
                c.Type == ComponentDamageType.ActuatorDamaged
                && c.Location == weapon.MountLocation) * 10;
        }

        int equipmentModifier = GetEquipmentAccuracyModifiers(attacker, target, weapon, hexDistance);

        int raw = baseChance + gunneryBonus - evasionPenalty + rangeModifier
            - braceBonus - sensorPenalty - actuatorPenalty - terrainDefense - losPenalty + equipmentModifier;
        int final_ = Math.Clamp(raw, 5, 95);

        return new HitChanceBreakdown
        {
            BaseAccuracy = baseChance,
            GunneryBonus = gunneryBonus,
            EvasionPenalty = evasionPenalty,
            RangeModifier = rangeModifier,
            BraceBonus = braceBonus,
            SensorPenalty = sensorPenalty,
            ActuatorPenalty = actuatorPenalty,
            TerrainDefense = terrainDefense,
            LOSPenalty = losPenalty,
            EquipmentModifier = equipmentModifier,
            InterveningHexes = interveningHexes,
            FinalHitChance = final_
        };
    }

    #endregion

    #region Overwatch

    private List<CombatEvent> CheckOverwatchTriggers(CombatState state, CombatFrame movingFrame)
    {
        var events = new List<CombatEvent>();
        bool isPlayerFrame = state.PlayerFrames.Contains(movingFrame);
        var overwatchFrames = isPlayerFrame
            ? state.EnemyFrames.Where(f => f.IsOnOverwatch && !f.IsDestroyed)
            : state.PlayerFrames.Where(f => f.IsOnOverwatch && !f.IsDestroyed);

        foreach (var watcher in overwatchFrames)
        {
            int distance = HexCoord.Distance(watcher.HexPosition, movingFrame.HexPosition);
            int maxRange = PositioningSystem.GetFrameMaxWeaponRange(watcher);

            if (distance <= maxRange)
            {
                int bestGroup = GetBestOverwatchGroup(watcher, distance);
                if (bestGroup < 0) continue;

                events.Add(new CombatEvent
                {
                    Type = CombatEventType.Overwatch,
                    AttackerId = watcher.InstanceId,
                    AttackerName = watcher.CustomName,
                    Message = $"{watcher.CustomName} fires overwatch at {movingFrame.CustomName}!"
                });

                events.AddRange(ExecuteHexFireGroup(state, watcher, bestGroup, movingFrame, null));
                watcher.IsOnOverwatch = false;
            }
        }

        return events;
    }

    private List<CombatEvent> ResolveOverwatch(CombatState state)
    {
        var events = new List<CombatEvent>();

        foreach (var frame in state.AllFrames.Where(f => f.IsOnOverwatch && !f.IsDestroyed))
        {
            bool isPlayer = state.PlayerFrames.Contains(frame);
            var enemies = isPlayer ? state.AliveEnemyFrames : state.AlivePlayerFrames;
            if (!enemies.Any()) continue;

            var nearest = enemies.OrderBy(e => HexCoord.Distance(frame.HexPosition, e.HexPosition)).First();
            int distance = HexCoord.Distance(frame.HexPosition, nearest.HexPosition);
            int bestGroup = GetBestOverwatchGroup(frame, distance);
            if (bestGroup < 0) continue;

            events.Add(new CombatEvent
            {
                Type = CombatEventType.Overwatch,
                AttackerId = frame.InstanceId,
                AttackerName = frame.CustomName,
                Message = $"{frame.CustomName} fires overwatch!"
            });

            events.AddRange(ExecuteHexFireGroup(state, frame, bestGroup, nearest, null));
            frame.IsOnOverwatch = false;
        }

        return events;
    }

    private int GetBestOverwatchGroup(CombatFrame frame, int hexDistance)
    {
        int bestGroup = -1;
        int bestDamage = 0;

        foreach (var (groupId, weapons) in frame.WeaponGroups)
        {
            var functional = weapons.Where(w => !w.IsDestroyed).ToList();
            if (!functional.Any()) continue;

            bool inRange = functional.Any(w => hexDistance <= PositioningSystem.GetWeaponMaxRange(w.RangeClass));
            if (!inRange) continue;

            int damage = functional.Sum(w => w.Damage);
            if (damage > bestDamage)
            {
                bestDamage = damage;
                bestGroup = groupId;
            }
        }

        return bestGroup;
    }

    #endregion

    #region Combat End Checks

    private void CheckCombatEnd(CombatState state)
    {
        bool playerDead = !state.PlayerFrames.Any(f => !f.IsDestroyed);
        bool enemyDead = !state.EnemyFrames.Any(f => !f.IsDestroyed);

        if (playerDead && enemyDead)
        {
            state.Result = CombatResult.Victory;
            state.Phase = TurnPhase.CombatOver;
        }
        else if (playerDead)
        {
            state.Result = CombatResult.Defeat;
            state.Phase = TurnPhase.CombatOver;
        }
        else if (enemyDead)
        {
            state.Result = CombatResult.Victory;
            state.Phase = TurnPhase.CombatOver;
        }
    }

    #endregion
}
