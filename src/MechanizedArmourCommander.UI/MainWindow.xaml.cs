using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MechanizedArmourCommander.Core.Combat;
using MechanizedArmourCommander.Core.Models;
using MechanizedArmourCommander.Core.Services;
using MechanizedArmourCommander.Data;

namespace MechanizedArmourCommander.UI;

public partial class MainWindow : Window
{
    private readonly CombatService _combatService;
    private readonly DatabaseContext _dbContext;
    private List<CombatFrame> _playerFrames = new();
    private List<CombatFrame> _enemyFrames = new();
    private TacticalOrders _playerOrders = new();

    // Playback state (auto-resolve, round-by-round)
    private DispatcherTimer? _playbackTimer;
    private CombatRound? _playbackCurrentRound;
    private int _playbackEventIdx;
    private int _playbackRoundNumber;
    private bool _isPlayingBack;

    // Tactical planning state
    private bool _isTacticalMode;
    private int _tacticalRound;
    private int _selectedFrameIdx;
    private Dictionary<int, List<PlannedAction>> _framePlans = new();
    private Dictionary<int, int?> _frameFocusTargets = new();
    private TacticalOrders _enemyOrders = new();

    // Campaign state
    private bool _isCampaignMode;
    private Mission? _currentMission;
    private ManagementService? _managementService;
    private MissionService? _missionService;

    public MainWindow(string dbPath)
    {
        InitializeComponent();

        _dbContext = new DatabaseContext(dbPath);
        _dbContext.Initialize();

        _combatService = new CombatService();
        _managementService = new ManagementService(_dbContext);
        _missionService = new MissionService(_dbContext);

        // Defer HQ window to after MainWindow is shown
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;
        OpenManagementWindow();
    }

    protected override void OnClosed(EventArgs e)
    {
        _dbContext?.Dispose();
        base.OnClosed(e);
    }

    #region Initialization & Campaign Flow

    private void OpenManagementWindow()
    {
        var mgmtWindow = new ManagementWindow(_dbContext);
        mgmtWindow.Owner = this;
        this.Hide();

        var result = mgmtWindow.ShowDialog();

        this.Show();

        if (result == true && mgmtWindow.ReturnToMainMenu)
        {
            // Return to main menu — close MainWindow entirely
            Close();
            return;
        }

        if (result == true && mgmtWindow.LaunchCombat && mgmtWindow.SelectedMission != null)
        {
            // Campaign deployment
            _isCampaignMode = true;
            _currentMission = mgmtWindow.SelectedMission;

            _playerFrames = _managementService!.BuildCombatFrames(mgmtWindow.DeployedFrameIds);
            _enemyFrames = _missionService!.BuildEnemyForce(_currentMission);

            ClearFeed();
            AppendFeedLine($"MISSION: {_currentMission.Title}", "#FFAA00", true);
            AppendFeedLine($"Difficulty: {new string('*', _currentMission.Difficulty)}", "#FF6600");
            AppendFeedLine("", "#000000");
            AppendFeedLine("Deploy your lance. Ready for combat.", "#00FF00");

            UpdateFrameLists();
        }
        else if (result == true)
        {
            // Quick combat (test scenario)
            _isCampaignMode = false;
            _currentMission = null;
            InitializeTestScenario();
            ClearFeed();
            DisplayDatabaseStats();
        }
        else
        {
            // Closed without action — return to main menu
            Close();
        }
    }

    private void HandlePostCombat()
    {
        if (!_isCampaignMode || _currentMission == null || _managementService == null || _missionService == null)
            return;

        // Determine outcome
        bool playerAlive = _playerFrames.Any(f => !f.IsDestroyed);
        bool enemyAlive = _enemyFrames.Any(f => !f.IsDestroyed);

        CombatResult outcome = (!playerAlive && !enemyAlive) ? CombatResult.Victory :
                               !playerAlive ? CombatResult.Defeat :
                               !enemyAlive ? CombatResult.Victory :
                               CombatResult.Withdrawal;

        // Process results
        var results = _missionService.ProcessResults(_currentMission, outcome, _playerFrames, _enemyFrames);

        // Apply damage to persistent frames
        foreach (var frame in _playerFrames)
        {
            _managementService.ApplyPostCombatDamage(frame);
        }

        // Show post-combat window (player picks salvage here)
        var postCombat = new PostCombatWindow(results, _currentMission, _dbContext);
        postCombat.Owner = this;
        postCombat.ShowDialog();

        // Apply mission results AFTER salvage selection (credits, XP, salvage to inventory)
        _missionService.ApplyResults(results, _managementService);

        // Return to management
        OpenManagementWindow();
    }

    private void DisplayDatabaseStats()
    {
        var chassisRepo = new MechanizedArmourCommander.Data.Repositories.ChassisRepository(_dbContext);
        var weaponRepo = new MechanizedArmourCommander.Data.Repositories.WeaponRepository(_dbContext);

        var allChassis = chassisRepo.GetAll();
        var allWeapons = weaponRepo.GetAll();

        AppendFeedLine($"DATABASE INITIALIZED", "#00AA00");
        AppendFeedLine($"Chassis: {allChassis.Count}  Weapons: {allWeapons.Count}", "#006600");
        AppendFeedLine("", "#000000");
        AppendFeedLine("Ready for combat.", "#00FF00");
    }

    private void InitializeTestScenario()
    {
        // Mirror match: both sides get the same frames and weapons for fair AI testing
        _playerFrames = new List<CombatFrame>
        {
            CreateTestFrame(1, "Alpha", "EN-50", "Enforcer", "Medium",
                reactorOutput: 17, movementEnergyCost: 5, speed: 6, evasion: 15,
                pilotCallsign: "Razor", gunnery: 5, piloting: 4, tactics: 5,
                structureHead: 3, structureCT: 8, structureST: 6, structureArm: 4, structureLegs: 6,
                armorHead: 4, armorCT: 12, armorLT: 8, armorRT: 8, armorLA: 6, armorRA: 6, armorLegs: 10,
                weapons: new List<(string name, string type, int energy, int ammo, string ammoType, int dmg, string range, int acc, int group, HitLocation mount)>
                {
                    ("Medium Laser", "Energy", 6, 0, "", 10, "Medium", 80, 1, HitLocation.RightArm),
                    ("Autocannon-5", "Ballistic", 1, 5, "AC5", 8, "Long", 80, 2, HitLocation.LeftArm)
                },
                ammo: new Dictionary<string, int> { { "AC5", 40 } }),

            CreateTestFrame(2, "Bravo", "WD-60", "Warden", "Heavy",
                reactorOutput: 20, movementEnergyCost: 6, speed: 4, evasion: 10,
                pilotCallsign: "Anvil", gunnery: 4, piloting: 3, tactics: 5,
                structureHead: 4, structureCT: 12, structureST: 8, structureArm: 6, structureLegs: 8,
                armorHead: 6, armorCT: 16, armorLT: 12, armorRT: 12, armorLA: 8, armorRA: 8, armorLegs: 14,
                weapons: new List<(string name, string type, int energy, int ammo, string ammoType, int dmg, string range, int acc, int group, HitLocation mount)>
                {
                    ("Heavy Laser", "Energy", 12, 0, "", 18, "Long", 75, 1, HitLocation.RightTorso),
                    ("SRM-6", "Missile", 1, 6, "SRM", 12, "Short", 80, 2, HitLocation.LeftTorso)
                },
                ammo: new Dictionary<string, int> { { "SRM", 36 } })
        };

        _enemyFrames = new List<CombatFrame>
        {
            CreateTestFrame(101, "Hostile-1", "EN-50", "Enforcer", "Medium",
                reactorOutput: 17, movementEnergyCost: 5, speed: 6, evasion: 15,
                pilotCallsign: null, gunnery: 5, piloting: 4, tactics: 5,
                structureHead: 3, structureCT: 8, structureST: 6, structureArm: 4, structureLegs: 6,
                armorHead: 4, armorCT: 12, armorLT: 8, armorRT: 8, armorLA: 6, armorRA: 6, armorLegs: 10,
                weapons: new List<(string name, string type, int energy, int ammo, string ammoType, int dmg, string range, int acc, int group, HitLocation mount)>
                {
                    ("Medium Laser", "Energy", 6, 0, "", 10, "Medium", 80, 1, HitLocation.RightArm),
                    ("Autocannon-5", "Ballistic", 1, 5, "AC5", 8, "Long", 80, 2, HitLocation.LeftArm)
                },
                ammo: new Dictionary<string, int> { { "AC5", 40 } }),

            CreateTestFrame(102, "Hostile-2", "WD-60", "Warden", "Heavy",
                reactorOutput: 20, movementEnergyCost: 6, speed: 4, evasion: 10,
                pilotCallsign: null, gunnery: 4, piloting: 3, tactics: 5,
                structureHead: 4, structureCT: 12, structureST: 8, structureArm: 6, structureLegs: 8,
                armorHead: 6, armorCT: 16, armorLT: 12, armorRT: 12, armorLA: 8, armorRA: 8, armorLegs: 14,
                weapons: new List<(string name, string type, int energy, int ammo, string ammoType, int dmg, string range, int acc, int group, HitLocation mount)>
                {
                    ("Heavy Laser", "Energy", 12, 0, "", 18, "Long", 75, 1, HitLocation.RightTorso),
                    ("SRM-6", "Missile", 1, 6, "SRM", 12, "Short", 80, 2, HitLocation.LeftTorso)
                },
                ammo: new Dictionary<string, int> { { "SRM", 36 } })
        };

        UpdateFrameLists();
    }

    private CombatFrame CreateTestFrame(
        int id, string name, string designation, string chassisName, string frameClass,
        int reactorOutput, int movementEnergyCost, int speed, int evasion,
        string? pilotCallsign, int gunnery, int piloting, int tactics,
        int structureHead, int structureCT, int structureST, int structureArm, int structureLegs,
        int armorHead, int armorCT, int armorLT, int armorRT, int armorLA, int armorRA, int armorLegs,
        List<(string name, string type, int energy, int ammo, string ammoType, int dmg, string range, int acc, int group, HitLocation mount)> weapons,
        Dictionary<string, int> ammo)
    {
        var frame = new CombatFrame
        {
            InstanceId = id,
            CustomName = name,
            ChassisDesignation = designation,
            ChassisName = chassisName,
            Class = frameClass,
            ReactorOutput = reactorOutput,
            CurrentEnergy = reactorOutput,
            ReactorStress = 0,
            MovementEnergyCost = movementEnergyCost,
            Speed = speed,
            Evasion = evasion,
            PilotCallsign = pilotCallsign,
            PilotGunnery = gunnery,
            PilotPiloting = piloting,
            PilotTactics = tactics,
            ActionPoints = 2,
            MaxActionPoints = 2,
            CurrentRange = RangeBand.Long,
            Armor = new Dictionary<HitLocation, int>
            {
                { HitLocation.Head, armorHead },
                { HitLocation.CenterTorso, armorCT },
                { HitLocation.LeftTorso, armorLT },
                { HitLocation.RightTorso, armorRT },
                { HitLocation.LeftArm, armorLA },
                { HitLocation.RightArm, armorRA },
                { HitLocation.Legs, armorLegs }
            },
            MaxArmor = new Dictionary<HitLocation, int>
            {
                { HitLocation.Head, armorHead },
                { HitLocation.CenterTorso, armorCT },
                { HitLocation.LeftTorso, armorLT },
                { HitLocation.RightTorso, armorRT },
                { HitLocation.LeftArm, armorLA },
                { HitLocation.RightArm, armorRA },
                { HitLocation.Legs, armorLegs }
            },
            Structure = new Dictionary<HitLocation, int>
            {
                { HitLocation.Head, structureHead },
                { HitLocation.CenterTorso, structureCT },
                { HitLocation.LeftTorso, structureST },
                { HitLocation.RightTorso, structureST },
                { HitLocation.LeftArm, structureArm },
                { HitLocation.RightArm, structureArm },
                { HitLocation.Legs, structureLegs }
            },
            MaxStructure = new Dictionary<HitLocation, int>
            {
                { HitLocation.Head, structureHead },
                { HitLocation.CenterTorso, structureCT },
                { HitLocation.LeftTorso, structureST },
                { HitLocation.RightTorso, structureST },
                { HitLocation.LeftArm, structureArm },
                { HitLocation.RightArm, structureArm },
                { HitLocation.Legs, structureLegs }
            },
            AmmoByType = new Dictionary<string, int>(ammo)
        };

        int weaponId = id * 100;
        foreach (var w in weapons)
        {
            if (!frame.WeaponGroups.ContainsKey(w.group))
                frame.WeaponGroups[w.group] = new List<EquippedWeapon>();

            frame.WeaponGroups[w.group].Add(new EquippedWeapon
            {
                WeaponId = weaponId++,
                Name = w.name,
                WeaponType = w.type,
                EnergyCost = w.energy,
                AmmoPerShot = w.ammo,
                AmmoType = w.ammoType,
                Damage = w.dmg,
                RangeClass = w.range,
                BaseAccuracy = w.acc,
                WeaponGroup = w.group,
                MountLocation = w.mount
            });
        }

        return frame;
    }

    #endregion

    #region Combat Feed (colored events)

    private void ClearFeed()
    {
        CombatFeedPanel.Children.Clear();
    }

    private void AppendFeedLine(string text, string colorHex, bool bold = false)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 1)
        };
        CombatFeedPanel.Children.Add(tb);
        CombatFeedScroller.ScrollToEnd();
    }

    private void AppendCombatEvent(CombatEvent evt)
    {
        string prefix;
        string color;
        bool bold = false;

        switch (evt.Type)
        {
            case CombatEventType.Movement:
                prefix = "  "; color = "#558855"; break;
            case CombatEventType.Hit:
                prefix = "  > "; color = "#00FF00"; break;
            case CombatEventType.Miss:
                prefix = "  > "; color = "#555555"; break;
            case CombatEventType.Critical:
                prefix = "  ** "; color = "#FFFF00"; bold = true; break;
            case CombatEventType.ComponentDamage:
                prefix = "  ! "; color = "#FFAA00"; break;
            case CombatEventType.AmmoExplosion:
                prefix = "  !! "; color = "#FF4400"; bold = true; break;
            case CombatEventType.LocationDestroyed:
                prefix = "  >> "; color = "#FF6600"; bold = true; break;
            case CombatEventType.FrameDestroyed:
                prefix = "  >>> "; color = "#FF0000"; bold = true; break;
            case CombatEventType.ReactorOverload:
            case CombatEventType.ReactorShutdown:
                prefix = "  ~ "; color = "#FF8800"; break;
            case CombatEventType.ReactorVent:
                prefix = "  ~ "; color = "#888800"; break;
            case CombatEventType.DamageTransfer:
                prefix = "  -> "; color = "#AA6600"; break;
            case CombatEventType.Brace:
            case CombatEventType.Overwatch:
                prefix = "  "; color = "#4488AA"; break;
            case CombatEventType.RoundSummary:
                prefix = "  --- "; color = "#888888"; break;
            default:
                prefix = "  "; color = "#00AA00"; break;
        }

        AppendFeedLine($"{prefix}{evt.Message}", color, bold);
    }

    private void AppendRoundHeader(int roundNumber)
    {
        AppendFeedLine("", "#000000");
        AppendFeedLine($"===== ROUND {roundNumber} =====", "#00FFFF", true);
    }

    private void AppendResultBanner(CombatResult result)
    {
        AppendFeedLine("", "#000000");
        switch (result)
        {
            case CombatResult.Victory:
                AppendFeedLine("=== VICTORY ===", "#00FF00", true);
                break;
            case CombatResult.Defeat:
                AppendFeedLine("=== DEFEAT ===", "#FF0000", true);
                break;
            case CombatResult.Withdrawal:
                AppendFeedLine("=== WITHDRAWAL ===", "#FFAA00", true);
                break;
            default:
                AppendFeedLine("=== COMBAT ENDED ===", "#888888", true);
                break;
        }
    }

    #endregion

    #region Frame List Display

    private void UpdateFrameLists()
    {
        PlayerFramesList.Items.Clear();
        foreach (var frame in _playerFrames)
        {
            string status = frame.IsDestroyed ? "DESTROYED" :
                           frame.IsShutDown ? "SHUTDOWN" :
                           frame.ArmorPercent < 25 ? "CRITICAL" :
                           frame.ArmorPercent < 50 ? "DAMAGED" : "OK";

            string armorBar = BuildArmorBar(frame.ArmorPercent, 10);
            var destroyedLocs = frame.DestroyedLocations.Count > 0
                ? $"  LOST: {string.Join(",", frame.DestroyedLocations.Select(l => ShortLoc(l)))}"
                : "";

            int funcWeapons = frame.FunctionalWeapons.Count();
            int totalWeapons = frame.WeaponGroups.Values.SelectMany(g => g).Count();

            PlayerFramesList.Items.Add($"{frame.CustomName} ({frame.Class[0]}) [{status}]");
            PlayerFramesList.Items.Add($" Armor: {armorBar} {frame.ArmorPercent:F0}%");
            PlayerFramesList.Items.Add($" R:{frame.CurrentEnergy}/{frame.EffectiveReactorOutput} W:{funcWeapons}/{totalWeapons} Rng:{ShortRange(frame.CurrentRange)}");
            if (destroyedLocs.Length > 0)
                PlayerFramesList.Items.Add(destroyedLocs);
            PlayerFramesList.Items.Add("");
        }

        EnemyFramesList.Items.Clear();
        foreach (var frame in _enemyFrames)
        {
            string status = frame.IsDestroyed ? "DESTROYED" :
                           frame.IsShutDown ? "SHUTDOWN" :
                           frame.ArmorPercent < 25 ? "CRITICAL" :
                           frame.ArmorPercent < 50 ? "DAMAGED" : "OK";

            string armorBar = BuildArmorBar(frame.ArmorPercent, 10);
            var destroyedLocs = frame.DestroyedLocations.Count > 0
                ? $"  LOST: {string.Join(",", frame.DestroyedLocations.Select(l => ShortLoc(l)))}"
                : "";

            EnemyFramesList.Items.Add($"{frame.CustomName} ({frame.Class[0]}) [{status}]");
            EnemyFramesList.Items.Add($" Armor: {armorBar} {frame.ArmorPercent:F0}%");
            if (destroyedLocs.Length > 0)
                EnemyFramesList.Items.Add(destroyedLocs);
            EnemyFramesList.Items.Add("");
        }

        UpdateBattlefieldMap();
    }

    private string BuildArmorBar(float percent, int width)
    {
        int filled = (int)(percent / 100f * width);
        filled = Math.Clamp(filled, 0, width);
        return "[" + new string('#', filled) + new string('-', width - filled) + "]";
    }

    private string ShortLoc(HitLocation loc) => loc switch
    {
        HitLocation.Head => "HD",
        HitLocation.CenterTorso => "CT",
        HitLocation.LeftTorso => "LT",
        HitLocation.RightTorso => "RT",
        HitLocation.LeftArm => "LA",
        HitLocation.RightArm => "RA",
        HitLocation.Legs => "LG",
        _ => "?"
    };

    private string ShortRange(RangeBand r) => r switch
    {
        RangeBand.PointBlank => "PB",
        RangeBand.Short => "S",
        RangeBand.Medium => "M",
        RangeBand.Long => "L",
        _ => "?"
    };

    #endregion

    #region Battlefield Map

    private void UpdateBattlefieldMap()
    {
        BattlefieldCanvas.Children.Clear();

        double canvasW = BattlefieldCanvas.ActualWidth;
        double canvasH = BattlefieldCanvas.ActualHeight;

        if (canvasW < 10 || canvasH < 10)
        {
            canvasW = 1200;
            canvasH = 180;
        }

        DrawMapGrid(canvasW, canvasH);
        AssignMapPositions(canvasW, canvasH);
        DrawRangeLines(canvasW, canvasH);

        foreach (var frame in _playerFrames)
            DrawUnitMarker(frame, isPlayer: true);

        foreach (var frame in _enemyFrames)
            DrawUnitMarker(frame, isPlayer: false);

        UpdateMapRangeLabel();
    }

    private void DrawMapGrid(double w, double h)
    {
        int gridSpacing = 40;
        var gridBrush = new SolidColorBrush(Color.FromArgb(20, 0, 255, 0));

        for (double x = 0; x < w; x += gridSpacing)
        {
            BattlefieldCanvas.Children.Add(new Line
            {
                X1 = x, Y1 = 0, X2 = x, Y2 = h,
                Stroke = gridBrush, StrokeThickness = 0.5
            });
        }

        for (double y = 0; y < h; y += gridSpacing)
        {
            BattlefieldCanvas.Children.Add(new Line
            {
                X1 = 0, Y1 = y, X2 = w, Y2 = y,
                Stroke = gridBrush, StrokeThickness = 0.5
            });
        }
    }

    private void AssignMapPositions(double canvasW, double canvasH)
    {
        double margin = 60;
        double fieldW = canvasW - margin * 2;

        double PlayerRangeToX(RangeBand range)
        {
            double t = range switch
            {
                RangeBand.Long => 0.0,
                RangeBand.Medium => 0.30,
                RangeBand.Short => 0.60,
                RangeBand.PointBlank => 0.85,
                _ => 0.0
            };
            return margin + t * (fieldW / 2.0);
        }

        double EnemyRangeToX(RangeBand range)
        {
            double t = range switch
            {
                RangeBand.Long => 0.0,
                RangeBand.Medium => 0.30,
                RangeBand.Short => 0.60,
                RangeBand.PointBlank => 0.85,
                _ => 0.0
            };
            return canvasW - margin - t * (fieldW / 2.0);
        }

        double yPad = 25;
        double usableH = canvasH - yPad * 2;

        int playerIdx = 0;
        int playerCount = _playerFrames.Count;
        foreach (var frame in _playerFrames)
        {
            frame.MapX = PlayerRangeToX(frame.CurrentRange);
            frame.MapY = playerCount > 1
                ? yPad + usableH * ((double)playerIdx / (playerCount - 1))
                : canvasH / 2.0;
            playerIdx++;
        }

        int enemyIdx = 0;
        int enemyCount = _enemyFrames.Count;
        foreach (var frame in _enemyFrames)
        {
            frame.MapX = EnemyRangeToX(frame.CurrentRange);
            frame.MapY = enemyCount > 1
                ? yPad + usableH * ((double)enemyIdx / (enemyCount - 1))
                : canvasH / 2.0;
            enemyIdx++;
        }
    }

    private void DrawRangeLines(double canvasW, double canvasH)
    {
        var aliveEnemies = _enemyFrames.Where(f => !f.IsDestroyed).ToList();
        if (aliveEnemies.Count == 0) return;

        foreach (var pf in _playerFrames.Where(f => !f.IsDestroyed))
        {
            var closest = aliveEnemies
                .OrderBy(e => Math.Abs(pf.MapX - e.MapX) + Math.Abs(pf.MapY - e.MapY))
                .First();

            var rangeBand = pf.CurrentRange;
            var lineColor = RangeBandToColor(rangeBand);
            var lineBrush = new SolidColorBrush(Color.FromArgb(40, lineColor.R, lineColor.G, lineColor.B));

            BattlefieldCanvas.Children.Add(new Line
            {
                X1 = pf.MapX, Y1 = pf.MapY,
                X2 = closest.MapX, Y2 = closest.MapY,
                Stroke = lineBrush, StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 3, 5 }
            });

            double midX = (pf.MapX + closest.MapX) / 2;
            double midY = (pf.MapY + closest.MapY) / 2;

            var rangeTag = new TextBlock
            {
                Text = PositioningSystem.FormatRangeBand(rangeBand),
                FontSize = 7,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(Color.FromArgb(100, lineColor.R, lineColor.G, lineColor.B))
            };
            Canvas.SetLeft(rangeTag, midX - 15);
            Canvas.SetTop(rangeTag, midY - 6);
            BattlefieldCanvas.Children.Add(rangeTag);
        }
    }

    private Color RangeBandToColor(RangeBand range) => range switch
    {
        RangeBand.PointBlank => (Color)ColorConverter.ConvertFromString("#FF4444"),
        RangeBand.Short => (Color)ColorConverter.ConvertFromString("#FFAA44"),
        RangeBand.Medium => (Color)ColorConverter.ConvertFromString("#AAFF44"),
        RangeBand.Long => (Color)ColorConverter.ConvertFromString("#44AAFF"),
        _ => Colors.Gray
    };

    private void UpdateMapRangeLabel()
    {
        var alivePlayer = _playerFrames.FirstOrDefault(f => !f.IsDestroyed);
        if (alivePlayer != null)
        {
            MapRangeLabel.Text = $"Engagement: {PositioningSystem.FormatRangeBand(alivePlayer.CurrentRange)}";
            var c = RangeBandToColor(alivePlayer.CurrentRange);
            MapRangeLabel.Foreground = new SolidColorBrush(c);
        }
        else
        {
            MapRangeLabel.Text = "";
        }
    }

    private void DrawUnitMarker(CombatFrame frame, bool isPlayer)
    {
        string baseColor;
        string bgColor;
        string borderColor;

        if (frame.IsDestroyed)
        {
            baseColor = "#444444"; bgColor = "#0A0A0A"; borderColor = "#222222";
        }
        else if (frame.IsShutDown)
        {
            baseColor = "#AAAA00"; bgColor = "#111100"; borderColor = "#555500";
        }
        else if (frame.ArmorPercent < 25)
        {
            baseColor = isPlayer ? "#FFAA00" : "#FF4400";
            bgColor = isPlayer ? "#1A1100" : "#1A0800";
            borderColor = isPlayer ? "#664400" : "#661800";
        }
        else
        {
            baseColor = isPlayer ? "#00FF00" : "#FF3333";
            bgColor = isPlayer ? "#001A00" : "#1A0000";
            borderColor = isPlayer ? "#005500" : "#550000";
        }

        var fgBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(baseColor));

        string classTag = frame.Class switch
        {
            "Light" => "L", "Medium" => "M", "Heavy" => "H", "Assault" => "A", _ => "?"
        };

        int armorPct = (int)frame.ArmorPercent;
        string statusLine = frame.IsDestroyed ? "DESTROYED" :
                            frame.IsShutDown ? "SHUTDOWN" :
                            $"A:{armorPct}% E:{frame.CurrentEnergy}";

        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = $"[{classTag}] {frame.CustomName}",
            FontSize = 8, FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Consolas"),
            Foreground = fgBrush,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = statusLine,
            FontSize = 7,
            FontFamily = new FontFamily("Consolas"),
            Foreground = fgBrush,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var marker = new Border
        {
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderColor)),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(3, 1, 3, 1),
            Child = panel
        };

        double markerX = frame.MapX - 40;
        if (markerX < 0) markerX = 0;

        Canvas.SetLeft(marker, markerX);
        Canvas.SetTop(marker, frame.MapY - 14);
        BattlefieldCanvas.Children.Add(marker);

        var dot = new Ellipse { Width = 5, Height = 5, Fill = fgBrush };
        Canvas.SetLeft(dot, frame.MapX - 2.5);
        Canvas.SetTop(dot, frame.MapY - 2.5);
        BattlefieldCanvas.Children.Add(dot);
    }

    #endregion

    #region Auto-Resolve Playback (round-by-round with live map)

    private void StartCombatButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isPlayingBack) return;

        ClearFeed();

        if (TacticalModeCheckBox.IsChecked == true)
        {
            StartTacticalCombat();
        }
        else
        {
            StartAutoResolve();
        }
    }

    private void StartAutoResolve()
    {
        var positioning = new PositioningSystem();
        positioning.InitializeRangeBands(_playerFrames, _enemyFrames);

        _playbackRoundNumber = 1;
        _playbackCurrentRound = null;
        _playbackEventIdx = -1;
        _isPlayingBack = true;

        StartCombatButton.IsEnabled = false;
        SkipButton.Visibility = Visibility.Visible;
        ResetButton.IsEnabled = false;
        FrameSelectorButton.IsEnabled = false;

        AppendFeedLine("COMBAT INITIATED", "#00FF00", true);
        UpdateFrameLists();

        _playbackTimer = new DispatcherTimer();
        _playbackTimer.Interval = TimeSpan.FromMilliseconds(120);
        _playbackTimer.Tick += PlaybackTick;
        _playbackTimer.Start();
    }

    private void PlaybackTick(object? sender, EventArgs e)
    {
        // Check if combat is over
        bool playerAlive = _playerFrames.Any(f => !f.IsDestroyed);
        bool enemyAlive = _enemyFrames.Any(f => !f.IsDestroyed);

        // If we need to execute a new round
        if (_playbackCurrentRound == null)
        {
            if (!playerAlive && !enemyAlive)
            {
                AppendResultBanner(CombatResult.Victory);
                AppendFeedLine("Mutual destruction.", "#888888");
                StopPlayback(); return;
            }
            if (!playerAlive)
            {
                AppendResultBanner(CombatResult.Defeat);
                StopPlayback(); return;
            }
            if (!enemyAlive)
            {
                AppendResultBanner(CombatResult.Victory);
                StopPlayback(); return;
            }
            if (_playbackRoundNumber > 30)
            {
                AppendFeedLine("=== STALEMATE ===", "#888888", true);
                StopPlayback(); return;
            }

            // Generate AI decisions for both sides and execute round
            var ai = new CombatAI();
            var actionSystem = new ActionSystem();

            var playerDecisions = new RoundTacticalDecision();
            var activeEnemies = _enemyFrames.Where(f => !f.IsDestroyed).ToList();
            foreach (var frame in _playerFrames.Where(f => !f.IsDestroyed && !f.IsShutDown))
            {
                playerDecisions.FrameOrders[frame.InstanceId] =
                    ai.GenerateActions(frame, activeEnemies, _playerOrders, actionSystem);
            }

            var round = _combatService.ExecuteRound(
                _playerFrames, _enemyFrames,
                playerDecisions, _playerOrders, _enemyOrders, _playbackRoundNumber);

            _playbackCurrentRound = round;
            _playbackEventIdx = -1; // -1 = show header next

            // Pause for round header
            if (_playbackTimer != null)
                _playbackTimer.Interval = TimeSpan.FromMilliseconds(400);
            return;
        }

        // Show round header
        if (_playbackEventIdx == -1)
        {
            AppendRoundHeader(_playbackCurrentRound.RoundNumber);
            _playbackEventIdx = 0;

            if (_playbackTimer != null)
                _playbackTimer.Interval = TimeSpan.FromMilliseconds(150);
            return;
        }

        // Show events one at a time
        if (_playbackEventIdx < _playbackCurrentRound.Events.Count)
        {
            AppendCombatEvent(_playbackCurrentRound.Events[_playbackEventIdx]);
            _playbackEventIdx++;

            if (_playbackTimer != null)
                _playbackTimer.Interval = TimeSpan.FromMilliseconds(100);
        }
        else
        {
            // Round finished — update map and frame lists with current state
            UpdateFrameLists();
            _playbackCurrentRound = null;
            _playbackRoundNumber++;

            // Longer pause between rounds
            if (_playbackTimer != null)
                _playbackTimer.Interval = TimeSpan.FromMilliseconds(500);
        }
    }

    private void SkipPlayback()
    {
        if (!_isPlayingBack) return;

        _playbackTimer?.Stop();

        // Show remaining events from current round if any
        if (_playbackCurrentRound != null)
        {
            if (_playbackEventIdx == -1)
                AppendRoundHeader(_playbackCurrentRound.RoundNumber);

            int startEvt = Math.Max(0, _playbackEventIdx);
            for (int ev = startEvt; ev < _playbackCurrentRound.Events.Count; ev++)
                AppendCombatEvent(_playbackCurrentRound.Events[ev]);

            _playbackRoundNumber++;
        }

        // Run remaining rounds instantly
        var ai = new CombatAI();
        var actionSystem = new ActionSystem();

        while (_playbackRoundNumber <= 30)
        {
            bool playerAlive = _playerFrames.Any(f => !f.IsDestroyed);
            bool enemyAlive = _enemyFrames.Any(f => !f.IsDestroyed);

            if (!playerAlive && !enemyAlive)
            { AppendResultBanner(CombatResult.Victory); break; }
            if (!playerAlive)
            { AppendResultBanner(CombatResult.Defeat); break; }
            if (!enemyAlive)
            { AppendResultBanner(CombatResult.Victory); break; }

            var playerDecisions = new RoundTacticalDecision();
            var activeEnemies = _enemyFrames.Where(f => !f.IsDestroyed).ToList();
            foreach (var frame in _playerFrames.Where(f => !f.IsDestroyed && !f.IsShutDown))
            {
                playerDecisions.FrameOrders[frame.InstanceId] =
                    ai.GenerateActions(frame, activeEnemies, _playerOrders, actionSystem);
            }

            var round = _combatService.ExecuteRound(
                _playerFrames, _enemyFrames,
                playerDecisions, _playerOrders, _enemyOrders, _playbackRoundNumber);

            AppendRoundHeader(round.RoundNumber);
            foreach (var evt in round.Events)
                AppendCombatEvent(evt);

            _playbackRoundNumber++;
        }

        if (_playbackRoundNumber > 30 && _playerFrames.Any(f => !f.IsDestroyed) && _enemyFrames.Any(f => !f.IsDestroyed))
            AppendFeedLine("=== STALEMATE ===", "#888888", true);

        StopPlayback();
        UpdateFrameLists();
    }

    private void StopPlayback()
    {
        _isPlayingBack = false;
        _playbackTimer?.Stop();
        _playbackTimer = null;
        _playbackCurrentRound = null;

        StartCombatButton.IsEnabled = true;
        SkipButton.Visibility = Visibility.Collapsed;
        ResetButton.IsEnabled = true;
        FrameSelectorButton.IsEnabled = true;

        if (_isCampaignMode)
            HandlePostCombat();
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        SkipPlayback();
    }

    private void CombatFeed_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_isPlayingBack)
            SkipPlayback();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_isPlayingBack && (e.Key == Key.Space || e.Key == Key.Enter || e.Key == Key.Escape))
            SkipPlayback();
    }

    #endregion

    #region Tactical Combat (Inline Planning)

    private void StartTacticalCombat()
    {
        _isTacticalMode = true;
        _tacticalRound = 1;
        _enemyOrders = new TacticalOrders();

        var positioning = new PositioningSystem();
        positioning.InitializeRangeBands(_playerFrames, _enemyFrames);

        AppendFeedLine("TACTICAL COMBAT INITIATED", "#00FFFF", true);
        AppendFeedLine("Plan actions for your frames, then execute.", "#006666");

        ShowTacticalPanel();
    }

    private void ShowTacticalPanel()
    {
        // Check end conditions
        bool playerAlive = _playerFrames.Any(f => !f.IsDestroyed);
        bool enemyAlive = _enemyFrames.Any(f => !f.IsDestroyed);

        if (!playerAlive && !enemyAlive)
        {
            AppendResultBanner(CombatResult.Victory);
            EndTacticalMode();
            return;
        }
        if (!playerAlive)
        {
            AppendResultBanner(CombatResult.Defeat);
            EndTacticalMode();
            return;
        }
        if (!enemyAlive)
        {
            AppendResultBanner(CombatResult.Victory);
            EndTacticalMode();
            return;
        }
        if (_tacticalRound > 30)
        {
            AppendFeedLine("=== STALEMATE ===", "#888888", true);
            EndTacticalMode();
            return;
        }

        // Switch panels
        SetupPanel.Visibility = Visibility.Collapsed;
        TacticalPanel.Visibility = Visibility.Visible;
        StartCombatButton.IsEnabled = false;
        FrameSelectorButton.IsEnabled = false;

        TacticalRoundHeader.Text = $"ROUND {_tacticalRound}";

        // Initialize plans for this round
        _framePlans.Clear();
        _frameFocusTargets.Clear();
        foreach (var frame in _playerFrames.Where(f => !f.IsDestroyed && !f.IsShutDown))
        {
            _framePlans[frame.InstanceId] = new List<PlannedAction>();
            _frameFocusTargets[frame.InstanceId] = null;
        }

        // Build frame tabs
        BuildFrameTabs();

        // Update enemy status text
        UpdateEnemyStatusDisplay();

        // Select first frame
        _selectedFrameIdx = 0;
        var activeFrames = _playerFrames.Where(f => !f.IsDestroyed && !f.IsShutDown).ToList();
        if (activeFrames.Count > 0)
            ShowFrameActions(activeFrames[0]);
    }

    private void BuildFrameTabs()
    {
        FrameTabsPanel.Children.Clear();
        int idx = 0;
        foreach (var frame in _playerFrames.Where(f => !f.IsDestroyed && !f.IsShutDown))
        {
            int capturedIdx = idx;
            var btn = new Button
            {
                Content = $"{frame.CustomName} ({frame.Class[0]})",
                Tag = frame.InstanceId,
                Width = 120, Height = 24,
                Margin = new Thickness(0, 0, 3, 3),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    capturedIdx == _selectedFrameIdx ? "#003300" : "#0A0A0A")),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    capturedIdx == _selectedFrameIdx ? "#00FF00" : "#006600")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    capturedIdx == _selectedFrameIdx ? "#00FF00" : "#003300")),
                BorderThickness = new Thickness(1)
            };
            btn.Click += (s, e) =>
            {
                _selectedFrameIdx = capturedIdx;
                var activeFrames = _playerFrames.Where(f => !f.IsDestroyed && !f.IsShutDown).ToList();
                if (capturedIdx < activeFrames.Count)
                    ShowFrameActions(activeFrames[capturedIdx]);
                BuildFrameTabs(); // Refresh highlight
            };
            FrameTabsPanel.Children.Add(btn);
            idx++;
        }
    }

    private void ShowFrameActions(CombatFrame frame)
    {
        // Show frame info
        int ap = frame.IsShutDown ? 0 : (frame.HasGyroHit ? 1 : 2);
        int energy = frame.EffectiveReactorOutput;
        var plans = _framePlans.GetValueOrDefault(frame.InstanceId) ?? new List<PlannedAction>();
        int usedAP = plans.Sum(a => ActionSystem.GetActionCost(a.Action));
        int remainingAP = ap - usedAP;

        FrameInfoText.Text = $"=== {frame.CustomName} ({frame.Class}) ===\n" +
                             $"Reactor: {energy}  Move: {frame.MovementEnergyCost}E\n" +
                             $"AP: {remainingAP}/{ap}  Range: {PositioningSystem.FormatRangeBand(frame.CurrentRange)}\n" +
                             $"Armor: {frame.ArmorPercent:F0}%  Stress: {frame.ReactorStress}";

        // Update planned actions display
        UpdatePlannedActionsDisplay(frame);

        // Build fire buttons for this frame's weapon groups
        BuildFireButtons(frame);

        // Enable/disable action buttons based on remaining AP
        bool canAct = remainingAP >= 1;
        bool canMove = canAct && !frame.DestroyedLocations.Contains(HitLocation.Legs);

        BtnMoveClose.IsEnabled = canMove && frame.CurrentRange > RangeBand.PointBlank;
        BtnMovePullBack.IsEnabled = canMove && frame.CurrentRange < RangeBand.Long;
        BtnBrace.IsEnabled = canAct;
        BtnOverwatch.IsEnabled = canAct;
        BtnVent.IsEnabled = canAct && frame.ReactorStress > 0;

        // Visual feedback for disabled buttons
        SetButtonEnabled(BtnMoveClose, BtnMoveClose.IsEnabled);
        SetButtonEnabled(BtnMovePullBack, BtnMovePullBack.IsEnabled);
        SetButtonEnabled(BtnBrace, BtnBrace.IsEnabled);
        SetButtonEnabled(BtnOverwatch, BtnOverwatch.IsEnabled);
        SetButtonEnabled(BtnVent, BtnVent.IsEnabled);
    }

    private void BuildFireButtons(CombatFrame frame)
    {
        FireButtonsPanel.Children.Clear();

        var plans = _framePlans.GetValueOrDefault(frame.InstanceId) ?? new List<PlannedAction>();
        int ap = frame.IsShutDown ? 0 : (frame.HasGyroHit ? 1 : 2);
        int usedAP = plans.Sum(a => ActionSystem.GetActionCost(a.Action));
        int remainingAP = ap - usedAP;

        foreach (var (groupId, weapons) in frame.WeaponGroups)
        {
            var funcWeapons = weapons.Where(w => !w.IsDestroyed).ToList();
            if (!funcWeapons.Any()) continue;

            string name = funcWeapons.First().Name;
            int totalEnergy = funcWeapons.Sum(w => w.EnergyCost);
            int totalDmg = funcWeapons.Sum(w => w.Damage);

            var btn = new Button
            {
                Content = $"G{groupId}: {name} {totalDmg}D {totalEnergy}E",
                Tag = $"Fire_{groupId}",
                Height = 28,
                Margin = new Thickness(0, 0, 3, 3),
                Padding = new Thickness(6, 0, 6, 0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                BorderThickness = new Thickness(1)
            };

            bool canFire = remainingAP >= 1;
            btn.IsEnabled = canFire;
            SetButtonEnabled(btn, canFire);
            btn.Click += ActionButton_Click;

            FireButtonsPanel.Children.Add(btn);
        }
    }

    private void SetButtonEnabled(Button btn, bool enabled)
    {
        if (enabled)
        {
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#001A00"));
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
            btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#004400"));
        }
        else
        {
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A"));
            btn.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A"));
        }
    }

    private void UpdatePlannedActionsDisplay(CombatFrame frame)
    {
        var plans = _framePlans.GetValueOrDefault(frame.InstanceId) ?? new List<PlannedAction>();

        if (plans.Count == 0)
        {
            PlannedActionsText.Text = "  (none)";
            PlannedActionsText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#444444"));
            return;
        }

        PlannedActionsText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00FF00"));
        var lines = new List<string>();
        for (int i = 0; i < plans.Count; i++)
        {
            lines.Add($"  {i + 1}. {FormatPlannedAction(plans[i])}");
        }
        PlannedActionsText.Text = string.Join("\n", lines);
    }

    private string FormatPlannedAction(PlannedAction action) => action.Action switch
    {
        CombatAction.Move when action.MoveDirection == MovementDirection.Close => "Move >> Close",
        CombatAction.Move when action.MoveDirection == MovementDirection.PullBack => "Move << Pull Back",
        CombatAction.FireGroup => $"Fire Group {action.WeaponGroupId}",
        CombatAction.Brace => "Brace (+defense)",
        CombatAction.Overwatch => "Overwatch (interrupt)",
        CombatAction.VentReactor => "Vent Reactor",
        CombatAction.Sprint => $"Sprint {action.MoveDirection}",
        CombatAction.CalledShot => $"Called Shot G{action.WeaponGroupId} -> {action.CalledShotLocation}",
        _ => action.Action.ToString()
    };

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        string tag = btn.Tag?.ToString() ?? "";

        var activeFrames = _playerFrames.Where(f => !f.IsDestroyed && !f.IsShutDown).ToList();
        if (_selectedFrameIdx >= activeFrames.Count) return;
        var frame = activeFrames[_selectedFrameIdx];

        var plans = _framePlans.GetValueOrDefault(frame.InstanceId);
        if (plans == null) return;

        int ap = frame.IsShutDown ? 0 : (frame.HasGyroHit ? 1 : 2);
        int usedAP = plans.Sum(a => ActionSystem.GetActionCost(a.Action));

        PlannedAction? newAction = tag switch
        {
            "MoveClose" => new PlannedAction { Action = CombatAction.Move, MoveDirection = MovementDirection.Close },
            "MovePullBack" => new PlannedAction { Action = CombatAction.Move, MoveDirection = MovementDirection.PullBack },
            "Brace" => new PlannedAction { Action = CombatAction.Brace },
            "Overwatch" => new PlannedAction { Action = CombatAction.Overwatch },
            "Vent" => new PlannedAction { Action = CombatAction.VentReactor },
            _ when tag.StartsWith("Fire_") => new PlannedAction
            {
                Action = CombatAction.FireGroup,
                WeaponGroupId = int.Parse(tag.Replace("Fire_", ""))
            },
            _ => null
        };

        if (newAction == null) return;

        int cost = ActionSystem.GetActionCost(newAction.Action);
        if (usedAP + cost > ap) return;

        plans.Add(newAction);
        ShowFrameActions(frame);
    }

    private void ClearActions_Click(object sender, RoutedEventArgs e)
    {
        var activeFrames = _playerFrames.Where(f => !f.IsDestroyed && !f.IsShutDown).ToList();
        if (_selectedFrameIdx >= activeFrames.Count) return;
        var frame = activeFrames[_selectedFrameIdx];

        if (_framePlans.ContainsKey(frame.InstanceId))
            _framePlans[frame.InstanceId].Clear();

        ShowFrameActions(frame);
    }

    private void ExecuteOrders_Click(object sender, RoutedEventArgs e)
    {
        if (!_isTacticalMode) return;

        // Build player decisions
        var playerDecisions = new RoundTacticalDecision();
        foreach (var (frameId, plans) in _framePlans)
        {
            var frameActions = new FrameActions();
            frameActions.Actions.AddRange(plans);
            if (_frameFocusTargets.TryGetValue(frameId, out var targetId) && targetId.HasValue)
                frameActions.FocusTargetId = targetId.Value;
            playerDecisions.FrameOrders[frameId] = frameActions;
        }

        ExecuteTacticalRound(playerDecisions);
    }

    private void AIDecides_Click(object sender, RoutedEventArgs e)
    {
        if (!_isTacticalMode) return;

        var playerDecisions = new RoundTacticalDecision();
        var ai = new CombatAI();
        var actionSystem = new ActionSystem();
        var activeEnemies = _enemyFrames.Where(f => !f.IsDestroyed).ToList();

        foreach (var frame in _playerFrames.Where(f => !f.IsDestroyed && !f.IsShutDown))
        {
            playerDecisions.FrameOrders[frame.InstanceId] =
                ai.GenerateActions(frame, activeEnemies, _playerOrders, actionSystem);
        }

        ExecuteTacticalRound(playerDecisions);
    }

    private void ExecuteTacticalRound(RoundTacticalDecision playerDecisions)
    {
        var round = _combatService.ExecuteRound(
            _playerFrames, _enemyFrames,
            playerDecisions, _playerOrders, _enemyOrders, _tacticalRound);

        // Display round events with color
        AppendRoundHeader(round.RoundNumber);
        foreach (var evt in round.Events)
            AppendCombatEvent(evt);

        UpdateFrameLists();
        _tacticalRound++;

        // Check for auto-resolve
        if (AutoResolveCheckBox.IsChecked == true)
        {
            AppendFeedLine("", "#000000");
            AppendFeedLine("--- AUTO-RESOLVING ---", "#FFAA00", true);

            var log = _combatService.ExecuteCombat(_playerFrames, _enemyFrames, _playerOrders, _enemyOrders);

            foreach (var r in log.Rounds)
            {
                AppendRoundHeader(r.RoundNumber);
                foreach (var evt in r.Events)
                    AppendCombatEvent(evt);
            }

            AppendResultBanner(log.Result);
            UpdateFrameLists();
            EndTacticalMode();
            return;
        }

        // Show planning for next round
        ShowTacticalPanel();
    }

    private void UpdateEnemyStatusDisplay()
    {
        var lines = new List<string>();
        foreach (var frame in _enemyFrames.Where(f => !f.IsDestroyed))
        {
            string bar = BuildArmorBar(frame.ArmorPercent, 8);
            lines.Add($"{frame.CustomName} ({frame.Class[0]}) {bar}");
        }
        EnemyStatusText.Text = lines.Count > 0 ? string.Join("\n", lines) : "(all destroyed)";
    }

    private void EndTacticalMode()
    {
        _isTacticalMode = false;
        SetupPanel.Visibility = Visibility.Visible;
        TacticalPanel.Visibility = Visibility.Collapsed;
        StartCombatButton.IsEnabled = true;
        FrameSelectorButton.IsEnabled = true;

        if (_isCampaignMode)
            HandlePostCombat();
    }

    #endregion

    #region Button Handlers

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isPlayingBack)
        {
            _isCampaignMode = false; // Prevent post-combat on manual reset
            StopPlayback();
        }

        _isCampaignMode = false; // Prevent post-combat on manual reset
        EndTacticalMode();

        // Return to HQ if management is available
        if (_managementService != null)
        {
            OpenManagementWindow();
        }
        else
        {
            InitializeTestScenario();
            ClearFeed();
            AppendFeedLine("Combat reset. Ready.", "#00FF00");
        }
    }

    private void FrameSelectorButton_Click(object sender, RoutedEventArgs e)
    {
        var frameSelectorWindow = new FrameSelectorWindow(_dbContext);

        if (frameSelectorWindow.ShowDialog() == true)
        {
            var selectedChassis = frameSelectorWindow.SelectedChassis;
            var selectedLoadout = frameSelectorWindow.SelectedLoadout;

            if (selectedChassis != null)
            {
                var newFrame = CreateCombatFrameFromSelection(selectedChassis, selectedLoadout);

                if (_playerFrames.Count < 4)
                    _playerFrames.Add(newFrame);
                else
                    _playerFrames[_playerFrames.Count - 1] = newFrame;

                UpdateFrameLists();

                var weaponSummary = string.Join(", ",
                    newFrame.WeaponGroups.SelectMany(g =>
                        g.Value.Select(w => $"[G{g.Key}] {w.Name}")));

                ClearFeed();
                AppendFeedLine($"Frame added: {newFrame.CustomName}", "#00FF00", true);
                AppendFeedLine($"  {selectedChassis.Designation} {selectedChassis.Name}", "#00AA00");
                AppendFeedLine($"  Reactor: {newFrame.ReactorOutput}  Move: {newFrame.MovementEnergyCost}E", "#008800");
                AppendFeedLine($"  Weapons: {weaponSummary}", "#008800");
            }
        }
    }

    private CombatFrame CreateCombatFrameFromSelection(
        MechanizedArmourCommander.Data.Models.Chassis chassis,
        Dictionary<string, MechanizedArmourCommander.Data.Models.Weapon?> loadout)
    {
        var instanceId = _playerFrames.Any() ? _playerFrames.Max(f => f.InstanceId) + 1 : 1;

        var frame = new CombatFrame
        {
            InstanceId = instanceId,
            CustomName = $"Frame-{instanceId}",
            ChassisDesignation = chassis.Designation,
            ChassisName = chassis.Name,
            Class = chassis.Class,
            ReactorOutput = chassis.ReactorOutput,
            CurrentEnergy = chassis.ReactorOutput,
            ReactorStress = 0,
            MovementEnergyCost = chassis.MovementEnergyCost,
            Speed = chassis.BaseSpeed,
            Evasion = chassis.BaseEvasion,
            PilotCallsign = "Pilot-" + instanceId,
            PilotGunnery = 5,
            PilotPiloting = 5,
            PilotTactics = 5,
            ActionPoints = 2,
            MaxActionPoints = 2,
            CurrentRange = RangeBand.Long,
            Structure = new Dictionary<HitLocation, int>
            {
                { HitLocation.Head, chassis.StructureHead },
                { HitLocation.CenterTorso, chassis.StructureCenterTorso },
                { HitLocation.LeftTorso, chassis.StructureSideTorso },
                { HitLocation.RightTorso, chassis.StructureSideTorso },
                { HitLocation.LeftArm, chassis.StructureArm },
                { HitLocation.RightArm, chassis.StructureArm },
                { HitLocation.Legs, chassis.StructureLegs }
            },
            MaxStructure = new Dictionary<HitLocation, int>
            {
                { HitLocation.Head, chassis.StructureHead },
                { HitLocation.CenterTorso, chassis.StructureCenterTorso },
                { HitLocation.LeftTorso, chassis.StructureSideTorso },
                { HitLocation.RightTorso, chassis.StructureSideTorso },
                { HitLocation.LeftArm, chassis.StructureArm },
                { HitLocation.RightArm, chassis.StructureArm },
                { HitLocation.Legs, chassis.StructureLegs }
            }
        };

        DistributeArmorEvenly(frame, chassis.MaxArmorTotal);

        int weaponId = instanceId * 100;
        int groupCounter = 1;
        foreach (var (slotKey, weapon) in loadout)
        {
            if (weapon == null) continue;

            HitLocation mountLoc = slotKey.StartsWith("Large") ? HitLocation.CenterTorso :
                                   groupCounter % 2 == 0 ? HitLocation.LeftArm : HitLocation.RightArm;

            if (!frame.WeaponGroups.ContainsKey(groupCounter))
                frame.WeaponGroups[groupCounter] = new List<EquippedWeapon>();

            frame.WeaponGroups[groupCounter].Add(new EquippedWeapon
            {
                WeaponId = weaponId++,
                Name = weapon.Name,
                HardpointSize = weapon.HardpointSize,
                WeaponType = weapon.WeaponType,
                EnergyCost = weapon.EnergyCost,
                AmmoPerShot = weapon.AmmoPerShot,
                AmmoType = weapon.AmmoPerShot > 0 ? weapon.Name.Replace(" ", "") : "",
                Damage = weapon.Damage,
                RangeClass = weapon.RangeClass,
                BaseAccuracy = weapon.BaseAccuracy,
                WeaponGroup = groupCounter,
                MountLocation = mountLoc,
                SpecialEffect = weapon.SpecialEffect
            });

            if (weapon.AmmoPerShot > 0)
            {
                string ammoKey = weapon.Name.Replace(" ", "");
                frame.AmmoByType[ammoKey] = frame.AmmoByType.GetValueOrDefault(ammoKey) + weapon.AmmoPerShot * 10;
            }

            groupCounter++;
        }

        return frame;
    }

    private void DistributeArmorEvenly(CombatFrame frame, int totalArmor)
    {
        var weights = new Dictionary<HitLocation, float>
        {
            { HitLocation.Head, 0.06f },
            { HitLocation.CenterTorso, 0.22f },
            { HitLocation.LeftTorso, 0.14f },
            { HitLocation.RightTorso, 0.14f },
            { HitLocation.LeftArm, 0.10f },
            { HitLocation.RightArm, 0.10f },
            { HitLocation.Legs, 0.24f }
        };

        frame.Armor = new Dictionary<HitLocation, int>();
        frame.MaxArmor = new Dictionary<HitLocation, int>();

        foreach (var (loc, weight) in weights)
        {
            int armor = (int)(totalArmor * weight);
            frame.Armor[loc] = armor;
            frame.MaxArmor[loc] = armor;
        }
    }

    #endregion
}
