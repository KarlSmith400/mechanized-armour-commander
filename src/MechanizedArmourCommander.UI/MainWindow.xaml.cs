using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MechanizedArmourCommander.Core.Combat;
using MechanizedArmourCommander.Core.Models;
using MechanizedArmourCommander.Core.Services;
using MechanizedArmourCommander.Data;

namespace MechanizedArmourCommander.UI;

public partial class MainWindow : Window
{
    // Services
    private readonly CombatService _combatService;
    private readonly DatabaseContext _dbContext;
    private ManagementService? _managementService;
    private MissionService? _missionService;

    // Combat state
    private CombatState? _combatState;
    private TacticalOrders _playerOrders = new();
    private TacticalOrders _enemyOrders = new();
    private bool _isCampaignMode;
    private Mission? _currentMission;

    // Hex rendering
    private double _hexSize;
    private double _renderOffsetX;
    private double _renderOffsetY;

    // Terrain tile images keyed by landscape type (cached)
    private static readonly Dictionary<string, Dictionary<HexTerrain, BitmapImage>> _tileSets = LoadAllTileSets();

    private static Dictionary<string, Dictionary<HexTerrain, BitmapImage>> LoadAllTileSets()
    {
        var sets = new Dictionary<string, Dictionary<HexTerrain, BitmapImage>>();

        // Default nature tiles (used for Habitable, Mining, Outpost, etc.)
        sets["default"] = LoadTileSet(new Dictionary<HexTerrain, string>
        {
            { HexTerrain.Open,   "terrain_open.png" },
            { HexTerrain.Forest, "terrain_forest.png" },
            { HexTerrain.Rocks,  "terrain_rocks.png" },
            { HexTerrain.Rough,  "terrain_rough.png" },
            { HexTerrain.Sand,   "terrain_sand.png" }
        });

        // Station tiles (extracted from space station tileset)
        sets["Station"] = LoadTileSet(new Dictionary<HexTerrain, string>
        {
            { HexTerrain.Open,  "station_open.png" },
            { HexTerrain.Rocks, "station_rocks.png" },
            { HexTerrain.Rough, "station_rough.png" }
        });

        // Industrial reuses station tiles with nature rocks fallback
        sets["Industrial"] = LoadTileSet(new Dictionary<HexTerrain, string>
        {
            { HexTerrain.Open,  "station_open.png" },
            { HexTerrain.Rocks, "station_rocks.png" },
            { HexTerrain.Rough, "station_rough.png" },
            { HexTerrain.Sand,  "terrain_sand.png" }
        });

        return sets;
    }

    private static Dictionary<HexTerrain, BitmapImage> LoadTileSet(Dictionary<HexTerrain, string> mapping)
    {
        var tiles = new Dictionary<HexTerrain, BitmapImage>();
        foreach (var (terrain, file) in mapping)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri($"pack://application:,,,/Resources/Hex/{file}");
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                tiles[terrain] = bmp;
            }
            catch { /* fallback to polygon rendering if image missing */ }
        }
        return tiles;
    }

    private static Dictionary<HexTerrain, BitmapImage> GetTilesForLandscape(string landscape)
    {
        if (_tileSets.TryGetValue(landscape, out var set))
            return set;
        return _tileSets["default"];
    }

    // Player interaction state
    private CombatAction? _selectedAction;
    private int? _selectedWeaponGroup;
    private HashSet<HexCoord> _highlightedMoveHexes = new();
    private HashSet<HexCoord> _highlightedAttackHexes = new();

    // Targeting cursor state
    private HexCoord? _hoveredHex;
    private CombatFrame? _hoveredTarget;
    private List<HexCoord>? _losLine;
    private List<(HexCoord coord, int penalty)>? _losInterveningHexes;

    // AI turn animation
    private DispatcherTimer? _aiTimer;
    private Queue<CombatEvent>? _aiEventQueue;

    // Deployment phase
    private CombatFrame? _selectedDeployFrame;
    private HashSet<HexCoord> _deploymentZoneHexes = new();

    public MainWindow(string dbPath)
    {
        InitializeComponent();
        _dbContext = new DatabaseContext(dbPath);
        _combatService = new CombatService();

        _managementService = new ManagementService(_dbContext);
        _missionService = new MissionService(_dbContext);

        AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent,
            new RoutedEventHandler((_, _) => AudioService.PlayClick()));

        Loaded += (_, _) => OpenManagementWindow();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _dbContext.Dispose();
    }

    #region Campaign Flow

    private void OpenManagementWindow()
    {
        this.Hide();
        var mgmtWindow = new ManagementWindow(_dbContext);
        mgmtWindow.Owner = this;
        mgmtWindow.ShowDialog();

        if (mgmtWindow.ReturnToMainMenu)
        {
            this.Close();
            return;
        }

        this.Show();

        if (mgmtWindow.LaunchCombat && mgmtWindow.SelectedMission != null)
        {
            _isCampaignMode = true;
            _currentMission = mgmtWindow.SelectedMission;

            var playerFrames = _managementService!.BuildCombatFrames(mgmtWindow.DeployedFrameIds);
            var enemyFrames = _missionService!.BuildEnemyForce(_currentMission);

            AppendFeedText($"CONTRACT: {_currentMission.Title}", "#FFAA00");
            AppendFeedText($"Employer: {_currentMission.EmployerFactionName} vs {_currentMission.OpponentFactionName}", "#888888");
            AppendFeedText($"Difficulty: {_currentMission.Difficulty} | Map: {_currentMission.MapSize} | Terrain: {_currentMission.Landscape}", "#888888");
            AppendFeedText($"Deploying {playerFrames.Count} frames against {enemyFrames.Count} hostiles", "#00CC00");
            AppendFeedText("", "#000000");

            // Initialize combat with manual deployment (landscape determines terrain generation)
            _combatState = _combatService.InitializeCombatForDeployment(playerFrames, enemyFrames, _currentMission.MapSize, _currentMission.Landscape);
            _deploymentZoneHexes = _combatState.Grid.GetFullDeploymentZone(true);
            StartCombatButton.IsEnabled = false;
            StartCombatButton.Opacity = 0.5;
            UpdateDeploymentUI();
            RenderFullMap();
        }
        else
        {
            OpenManagementWindow();
        }
    }

    private void HandlePostCombat()
    {
        if (!_isCampaignMode || _currentMission == null || _combatState == null) return;

        var results = _missionService!.ProcessResults(_currentMission, _combatState.Result,
            _combatState.PlayerFrames, _combatState.EnemyFrames);

        foreach (var frame in _combatState.PlayerFrames)
            _managementService!.ApplyPostCombatDamage(frame);

        var postCombatWindow = new PostCombatWindow(results, _currentMission, _dbContext);
        postCombatWindow.Owner = this;
        postCombatWindow.ShowDialog();

        _missionService.ApplyResults(results, _managementService!);

        _isCampaignMode = false;
        _currentMission = null;
        _combatState = null;
        CombatFeedPanel.Children.Clear();

        OpenManagementWindow();
    }

    #endregion

    #region Deployment Phase

    private void UpdateDeploymentUI()
    {
        if (_combatState == null) return;

        // Use the PreCombatPanel area — clear XAML children and rebuild dynamically
        PlayerFramesList.Items.Clear();
        EnemyFramesList.Items.Clear();

        // Hide the static XAML elements, use them for deployment
        PlayerFramesList.Visibility = Visibility.Collapsed;
        EnemyFramesList.Visibility = Visibility.Collapsed;

        // Clear any existing dynamic deployment elements (tagged)
        var toRemove = PreCombatPanel.Children.OfType<FrameworkElement>()
            .Where(e => e.Tag?.ToString() == "deploy_dynamic").ToList();
        foreach (var el in toRemove) PreCombatPanel.Children.Remove(el);

        // Header
        var header = new TextBlock
        {
            Text = "DEPLOYMENT PHASE",
            FontSize = 12, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0, 180, 255)),
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(0, 0, 0, 4),
            Tag = "deploy_dynamic"
        };
        PreCombatPanel.Children.Add(header);

        var instructions = new TextBlock
        {
            Text = "Select a frame, then click a blue hex to deploy.",
            FontSize = 9,
            Foreground = new SolidColorBrush(Color.FromRgb(0, 140, 200)),
            FontFamily = new FontFamily("Consolas"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
            Tag = "deploy_dynamic"
        };
        PreCombatPanel.Children.Add(instructions);

        // Undeployed frames
        var undeployed = _combatState.PlayerFrames
            .Where(f => f.HexPosition == default(HexCoord)).ToList();

        if (undeployed.Any())
        {
            var label = new TextBlock
            {
                Text = "AWAITING DEPLOYMENT:",
                FontSize = 10, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0)),
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 4),
                Tag = "deploy_dynamic"
            };
            PreCombatPanel.Children.Add(label);

            foreach (var frame in undeployed)
            {
                bool isSelected = _selectedDeployFrame?.InstanceId == frame.InstanceId;
                var btn = new Button
                {
                    Content = $"{frame.CustomName} ({frame.Class})",
                    Tag = "deploy_dynamic",
                    DataContext = frame,
                    Height = 28,
                    Margin = new Thickness(0, 0, 0, 2),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 10, FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(isSelected ? Color.FromRgb(0, 40, 60) : Color.FromRgb(0, 20, 30)),
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 255)),
                    BorderBrush = new SolidColorBrush(isSelected ? Color.FromRgb(0, 140, 200) : Color.FromRgb(0, 60, 80)),
                    BorderThickness = new Thickness(isSelected ? 2 : 1)
                };
                btn.Click += DeployFrameButton_Click;
                PreCombatPanel.Children.Add(btn);
            }
        }

        // Deployed frames
        var deployed = _combatState.PlayerFrames
            .Where(f => f.HexPosition != default(HexCoord)).ToList();

        if (deployed.Any())
        {
            var deployedLabel = new TextBlock
            {
                Text = $"DEPLOYED ({deployed.Count}/{_combatState.PlayerFrames.Count}):",
                FontSize = 10, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 10, 0, 4),
                Tag = "deploy_dynamic"
            };
            PreCombatPanel.Children.Add(deployedLabel);

            foreach (var frame in deployed)
            {
                var text = new TextBlock
                {
                    Text = $"  {frame.CustomName} @ {frame.HexPosition}",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 170, 0)),
                    FontFamily = new FontFamily("Consolas"),
                    Tag = "deploy_dynamic"
                };
                PreCombatPanel.Children.Add(text);
            }
        }

        // Enemy forces
        var enemyLabel = new TextBlock
        {
            Text = $"ENEMY FORCES ({_combatState.EnemyFrames.Count}):",
            FontSize = 10, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(255, 60, 60)),
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(0, 10, 0, 4),
            Tag = "deploy_dynamic"
        };
        PreCombatPanel.Children.Add(enemyLabel);

        foreach (var frame in _combatState.EnemyFrames)
        {
            var text = new TextBlock
            {
                Text = $"  {frame.CustomName} ({frame.Class})",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(170, 60, 60)),
                FontFamily = new FontFamily("Consolas"),
                Tag = "deploy_dynamic"
            };
            PreCombatPanel.Children.Add(text);
        }

        // Reset deployment button
        var resetBtn = new Button
        {
            Content = "RESET DEPLOYMENT",
            Tag = "deploy_dynamic",
            Height = 30,
            Margin = new Thickness(0, 12, 0, 0),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10, FontWeight = FontWeights.Bold,
            Background = new SolidColorBrush(Color.FromRgb(51, 34, 0)),
            Foreground = new SolidColorBrush(Color.FromRgb(255, 170, 0)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(102, 68, 0)),
            BorderThickness = new Thickness(1)
        };
        resetBtn.Click += ResetDeployment_Click;
        PreCombatPanel.Children.Add(resetBtn);

        // Update START COMBAT button
        bool allDeployed = !_combatState.PlayerFrames.Any(f => f.HexPosition == default(HexCoord));
        StartCombatButton.IsEnabled = allDeployed;
        StartCombatButton.Opacity = allDeployed ? 1.0 : 0.5;
    }

    private void DeployFrameButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is CombatFrame frame)
        {
            _selectedDeployFrame = frame;
            AppendFeedText($"Selected {frame.CustomName} — click a blue hex to deploy", "#00AAFF");
            UpdateDeploymentUI();
            RenderFullMap();
        }
    }

    private void ResetDeployment_Click(object sender, RoutedEventArgs e)
    {
        if (_combatState == null) return;

        foreach (var frame in _combatState.PlayerFrames)
        {
            if (frame.HexPosition != default(HexCoord))
            {
                _combatState.Grid.RemoveFrame(frame.HexPosition);
                frame.HexPosition = default;
            }
        }

        _selectedDeployFrame = null;
        AppendFeedText("Deployment reset", "#FFAA00");
        UpdateDeploymentUI();
        RenderFullMap();
    }

    private void HandleDeploymentClick(HexCoord hex)
    {
        if (_combatState == null || _selectedDeployFrame == null) return;

        if (!_deploymentZoneHexes.Contains(hex))
        {
            AppendFeedText("Must deploy in the blue zone", "#FF4444");
            return;
        }

        if (_combatState.Grid.IsOccupied(hex))
        {
            AppendFeedText("Hex already occupied", "#FF4444");
            return;
        }

        // Remove from previous position if re-deploying
        if (_selectedDeployFrame.HexPosition != default(HexCoord))
            _combatState.Grid.RemoveFrame(_selectedDeployFrame.HexPosition);

        _selectedDeployFrame.HexPosition = hex;
        _combatState.Grid.PlaceFrame(_selectedDeployFrame.InstanceId, hex);

        AppendFeedText($"{_selectedDeployFrame.CustomName} deployed at {hex}", "#00FF00");
        _selectedDeployFrame = null;
        UpdateDeploymentUI();
        RenderFullMap();
    }

    #endregion

    #region Combat Start

    private void StartCombatButton_Click(object sender, RoutedEventArgs e)
    {
        if (_combatState == null) return;

        // Transition from deployment to combat
        _combatState.Phase = TurnPhase.RoundStart;
        _deploymentZoneHexes.Clear();

        // Clear deployment UI elements
        var toRemove = PreCombatPanel.Children.OfType<FrameworkElement>()
            .Where(el => el.Tag?.ToString() == "deploy_dynamic").ToList();
        foreach (var el in toRemove) PreCombatPanel.Children.Remove(el);

        StartCombatButton.Visibility = Visibility.Collapsed;
        AutoResolveButton.Visibility = Visibility.Visible;
        WithdrawButton.Visibility = Visibility.Visible;
        PreCombatPanel.Visibility = Visibility.Collapsed;

        // Start first round
        var events = _combatService.StartRound(_combatState);
        DisplayEvents(events);
        AppendFeedText($"===== ROUND {_combatState.RoundNumber} =====", "#FFFF00");

        UpdateTurnOrder();
        AdvanceToNextUnit();
    }

    #endregion

    #region Turn Flow

    private void AdvanceToNextUnit()
    {
        if (_combatState == null) return;

        // Check if combat is over
        if (_combatState.Result != CombatResult.Ongoing)
        {
            EndCombat();
            return;
        }

        var frame = _combatService.AdvanceActivation(_combatState);

        if (frame == null)
        {
            // Round is over — process end of round
            var endEvents = _combatService.EndRound(_combatState);
            DisplayEvents(endEvents);

            if (_combatState.Result != CombatResult.Ongoing)
            {
                EndCombat();
                return;
            }

            // Start next round
            var startEvents = _combatService.StartRound(_combatState);
            DisplayEvents(startEvents);
            AppendFeedText($"===== ROUND {_combatState.RoundNumber} =====", "#FFFF00");
            UpdateTurnOrder();
            AdvanceToNextUnit();
            return;
        }

        UpdateTurnOrder();

        if (_combatState.IsPlayerTurn)
        {
            // Player's turn — show UI
            ShowPlayerTurnUI(frame);
        }
        else
        {
            // AI turn — execute with delay
            ShowAITurnUI(frame);
            ExecuteAITurnWithDelay(frame);
        }

        RenderFullMap();
    }

    private void ShowPlayerTurnUI(CombatFrame frame)
    {
        AudioService.PlayTurnStart();
        ActiveUnitHeader.Text = $"{frame.CustomName} ({frame.Class})";
        ActiveUnitHeader.Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0));

        var info = new System.Text.StringBuilder();
        info.AppendLine($"AP: {frame.ActionPoints}/{frame.MaxActionPoints}");
        info.AppendLine($"Reactor: {frame.CurrentEnergy}/{frame.EffectiveReactorOutput}E | Stress: {frame.ReactorStress}");
        info.AppendLine($"Armor: {frame.ArmorPercent:F0}% | Move: {frame.HexMovement} hexes");
        var terrainCell = _combatState?.Grid.GetCell(frame.HexPosition);
        string terrainName = terrainCell != null ? HexGrid.GetTerrainName(terrainCell.Terrain) : "?";
        int terrainDef = terrainCell != null ? HexGrid.GetTerrainDefenseBonus(terrainCell.Terrain) : 0;
        string terrainInfo = terrainDef > 0 ? $"{terrainName} (+{terrainDef} def)" : terrainName;
        info.AppendLine($"Terrain: {terrainInfo} | Pos: {frame.HexPosition}");
        ActiveUnitInfo.Text = info.ToString();

        // Show weapon groups
        var wg = new System.Text.StringBuilder();
        wg.AppendLine("WEAPONS:");
        foreach (var (groupId, weapons) in frame.WeaponGroups)
        {
            var functional = weapons.Where(w => !w.IsDestroyed).ToList();
            if (!functional.Any()) continue;
            int totalDmg = functional.Sum(w => w.Damage);
            int totalE = functional.Sum(w => w.EnergyCost);
            string names = string.Join(", ", functional.Select(w => w.Name));
            wg.AppendLine($"  G{groupId}: {names} ({totalDmg}dmg, {totalE}E)");
        }
        WeaponGroupsText.Text = wg.ToString();

        // Show action buttons
        var available = new ActionSystem().GetAvailableActions(frame);
        ActionsLabel.Visibility = Visibility.Visible;
        BtnMove.Visibility = available.Contains(CombatAction.Move) ? Visibility.Visible : Visibility.Collapsed;
        BtnSprint.Visibility = available.Contains(CombatAction.Sprint) ? Visibility.Visible : Visibility.Collapsed;
        BtnBrace.Visibility = available.Contains(CombatAction.Brace) ? Visibility.Visible : Visibility.Collapsed;
        BtnOverwatch.Visibility = available.Contains(CombatAction.Overwatch) ? Visibility.Visible : Visibility.Collapsed;
        BtnVent.Visibility = available.Contains(CombatAction.VentReactor) ? Visibility.Visible : Visibility.Collapsed;
        BtnEndTurn.Visibility = Visibility.Visible;

        // Build fire buttons
        BuildFireButtons(frame);

        // Highlight movement range on map
        _highlightedMoveHexes = HexPathfinding.GetReachableHexes(
            _combatState!.Grid, frame.HexPosition, PositioningSystem.GetEffectiveHexMovement(frame));

        ClearSelectedAction();
    }

    private void ShowAITurnUI(CombatFrame frame)
    {
        ActiveUnitHeader.Text = $"{frame.CustomName} ({frame.Class})";
        ActiveUnitHeader.Foreground = new SolidColorBrush(Color.FromRgb(200, 50, 50));
        ActiveUnitInfo.Text = $"Enemy activating...\nAP: {frame.ActionPoints}/{frame.MaxActionPoints}";
        WeaponGroupsText.Text = "";

        HideActionButtons();
    }

    private void HideActionButtons()
    {
        ActionsLabel.Visibility = Visibility.Collapsed;
        BtnMove.Visibility = Visibility.Collapsed;
        BtnSprint.Visibility = Visibility.Collapsed;
        BtnBrace.Visibility = Visibility.Collapsed;
        BtnOverwatch.Visibility = Visibility.Collapsed;
        BtnVent.Visibility = Visibility.Collapsed;
        BtnEndTurn.Visibility = Visibility.Collapsed;
        FireButtonsPanel.Children.Clear();
    }

    private void BuildFireButtons(CombatFrame frame)
    {
        FireButtonsPanel.Children.Clear();
        foreach (var (groupId, weapons) in frame.WeaponGroups)
        {
            var functional = weapons.Where(w => !w.IsDestroyed).ToList();
            if (!functional.Any()) continue;

            int totalDmg = functional.Sum(w => w.Damage);
            int totalE = functional.Sum(w => w.EnergyCost);

            var btn = new Button
            {
                Content = $"FIRE G{groupId} ({totalDmg}d {totalE}E)",
                Tag = $"Fire_{groupId}",
                Height = 28,
                Margin = new Thickness(0, 0, 0, 2),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Color.FromRgb(0, 26, 0)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 68, 0)),
                BorderThickness = new Thickness(1),
                Visibility = frame.ActionPoints >= 1 ? Visibility.Visible : Visibility.Collapsed
            };
            btn.Click += ActionButton_Click;
            FireButtonsPanel.Children.Add(btn);
        }
    }

    private void ClearSelectedAction()
    {
        _selectedAction = null;
        _selectedWeaponGroup = null;
        _highlightedAttackHexes.Clear();
        _hoveredTarget = null;
        _hoveredHex = null;
        _losLine = null;
        _losInterveningHexes = null;
        HexCanvas.Cursor = Cursors.Arrow;
        MapHeaderLabel.Text = "TACTICAL MAP";
        RenderFullMap();
    }

    #endregion

    #region AI Turn Execution

    private void ExecuteAITurnWithDelay(CombatFrame frame)
    {
        var events = _combatService.ExecuteAITurn(_combatState!, frame, _enemyOrders);
        _combatService.EndActivation(_combatState!);

        // Queue events for display with small delays
        _aiEventQueue = new Queue<CombatEvent>(events);
        _aiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _aiTimer.Tick += AIEventTick;
        _aiTimer.Start();
    }

    private void AIEventTick(object? sender, EventArgs e)
    {
        if (_aiEventQueue == null || !_aiEventQueue.Any())
        {
            _aiTimer?.Stop();
            _aiTimer = null;
            RenderFullMap();
            AdvanceToNextUnit();
            return;
        }

        var evt = _aiEventQueue.Dequeue();
        AppendCombatEvent(evt);

        // Refresh map periodically for movement events
        if (evt.Type == CombatEventType.Movement || evt.Type == CombatEventType.FrameDestroyed)
            RenderFullMap();
    }

    #endregion

    #region Player Action Handling

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_combatState == null || _combatState.ActiveFrame == null || !_combatState.IsPlayerTurn) return;

        var btn = (Button)sender;
        var tag = btn.Tag?.ToString() ?? "";
        var frame = _combatState.ActiveFrame;

        switch (tag)
        {
            case "Move":
                _selectedAction = CombatAction.Move;
                _selectedWeaponGroup = null;
                _highlightedAttackHexes.Clear();
                _highlightedMoveHexes = HexPathfinding.GetReachableHexes(
                    _combatState.Grid, frame.HexPosition, PositioningSystem.GetEffectiveHexMovement(frame));
                AppendFeedText("Select a hex to move to...", "#888888");
                break;

            case "Sprint":
                _selectedAction = CombatAction.Sprint;
                _selectedWeaponGroup = null;
                _highlightedAttackHexes.Clear();
                _highlightedMoveHexes = HexPathfinding.GetReachableHexes(
                    _combatState.Grid, frame.HexPosition, PositioningSystem.GetSprintRange(frame));
                AppendFeedText("Select a hex to sprint to...", "#888888");
                break;

            case "Brace":
                ExecutePlayerAction(CombatAction.Brace);
                return;

            case "Overwatch":
                ExecutePlayerAction(CombatAction.Overwatch);
                return;

            case "Vent":
                ExecutePlayerAction(CombatAction.VentReactor);
                return;

            default:
                if (tag.StartsWith("Fire_"))
                {
                    int groupId = int.Parse(tag.Substring(5));
                    _selectedAction = CombatAction.FireGroup;
                    _selectedWeaponGroup = groupId;
                    _highlightedMoveHexes.Clear();

                    // Highlight enemy hexes in range
                    int maxRange = GetWeaponGroupMaxRange(frame, groupId);
                    var enemyIds = _combatState.AliveEnemyFrames.Select(f => f.InstanceId).ToHashSet();
                    _highlightedAttackHexes = HexPathfinding.GetTargetableHexes(
                        _combatState.Grid, frame.HexPosition, maxRange, enemyIds);
                    AppendFeedText($"Select an enemy target for Group {groupId}...", "#888888");
                }
                break;
        }

        RenderFullMap();
    }

    private void HexCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_combatState == null) return;

        var pos = e.GetPosition(HexCanvas);
        double adjX = pos.X - _renderOffsetX;
        double adjY = pos.Y - _renderOffsetY;
        var clickedHex = HexCoord.FromPixel(adjX, adjY, _hexSize);

        if (!_combatState.Grid.IsValid(clickedHex)) return;

        // Handle deployment phase
        if (_combatState.Phase == TurnPhase.Deployment)
        {
            HandleDeploymentClick(clickedHex);
            return;
        }

        // Handle normal combat
        if (_combatState.ActiveFrame != null && _combatState.IsPlayerTurn)
            HandleHexClick(clickedHex);
    }

    private void HandleHexClick(HexCoord hex)
    {
        if (_combatState == null || _combatState.ActiveFrame == null) return;

        var frame = _combatState.ActiveFrame;

        if (_selectedAction == CombatAction.Move && _highlightedMoveHexes.Contains(hex))
        {
            ExecutePlayerAction(CombatAction.Move, targetHex: hex);
        }
        else if (_selectedAction == CombatAction.Sprint && _highlightedMoveHexes.Contains(hex))
        {
            ExecutePlayerAction(CombatAction.Sprint, targetHex: hex);
        }
        else if (_selectedAction == CombatAction.FireGroup && _highlightedAttackHexes.Contains(hex))
        {
            var cell = _combatState.Grid.GetCell(hex);
            if (cell?.OccupantFrameId != null)
            {
                AudioService.PlayFire();
                ExecutePlayerAction(CombatAction.FireGroup, targetFrameId: cell.OccupantFrameId, weaponGroupId: _selectedWeaponGroup);
            }
        }
    }

    private void HexCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_combatState?.Grid == null || _selectedAction != CombatAction.FireGroup) return;
        if (_combatState.ActiveFrame == null || !_combatState.IsPlayerTurn) return;

        var pos = e.GetPosition(HexCanvas);
        double adjX = pos.X - _renderOffsetX;
        double adjY = pos.Y - _renderOffsetY;
        var hex = HexCoord.FromPixel(adjX, adjY, _hexSize);

        if (_hoveredHex.HasValue && _hoveredHex.Value == hex) return; // same hex, skip
        _hoveredHex = hex;

        var attacker = _combatState.ActiveFrame;

        // Check if hovered hex has a targetable enemy
        if (_highlightedAttackHexes.Contains(hex))
        {
            var cell = _combatState.Grid.GetCell(hex);
            if (cell?.OccupantFrameId != null)
            {
                var target = _combatState.AllFrames.FirstOrDefault(f => f.InstanceId == cell.OccupantFrameId);
                if (target != null && !target.IsDestroyed && _combatState.EnemyFrames.Contains(target))
                {
                    _hoveredTarget = target;
                    _losLine = HexCoord.LineDraw(attacker.HexPosition, target.HexPosition);
                    var (_, intervening) = _combatState.Grid.GetLOSPenalty(attacker.HexPosition, target.HexPosition);
                    _losInterveningHexes = intervening;
                    RenderFullMap();
                    ShowTargetingInfo(attacker, target);
                    HexCanvas.Cursor = Cursors.Cross;
                    return;
                }
            }
        }

        // Not over a valid target — clear targeting visuals
        if (_hoveredTarget != null)
        {
            _hoveredTarget = null;
            _losLine = null;
            _losInterveningHexes = null;
            RenderFullMap();
            HexCanvas.Cursor = Cursors.Arrow;
            MapHeaderLabel.Text = "TACTICAL MAP";
        }
    }

    private void HexCanvas_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_hoveredTarget != null)
        {
            _hoveredTarget = null;
            _hoveredHex = null;
            _losLine = null;
            _losInterveningHexes = null;
            RenderFullMap();
            HexCanvas.Cursor = Cursors.Arrow;
            MapHeaderLabel.Text = "TACTICAL MAP";
        }
    }

    private void ShowTargetingInfo(CombatFrame attacker, CombatFrame target)
    {
        int hexDistance = HexCoord.Distance(attacker.HexPosition, target.HexPosition);

        // Get best weapon group breakdown
        if (!_selectedWeaponGroup.HasValue) return;
        if (!attacker.WeaponGroups.TryGetValue(_selectedWeaponGroup.Value, out var weapons)) return;
        if (!weapons.Any()) return;

        var bestWeapon = weapons.OrderByDescending(w => w.Damage).First();
        var breakdown = _combatService.GetHitChanceBreakdown(attacker, target, bestWeapon, hexDistance, _combatState!.Grid);

        var parts = new List<string>();
        parts.Add($"TARGET: {target.CustomName} | {hexDistance} hex | {breakdown.FinalHitChance}% hit");
        parts.Add($"  Base {breakdown.BaseAccuracy}%");
        if (breakdown.GunneryBonus != 0) parts.Add($"+{breakdown.GunneryBonus} gun");
        if (breakdown.RangeModifier != 0) parts.Add($"{(breakdown.RangeModifier >= 0 ? "+" : "")}{breakdown.RangeModifier} rng");
        if (breakdown.EvasionPenalty != 0) parts.Add($"-{breakdown.EvasionPenalty} eva");
        if (breakdown.TerrainDefense != 0) parts.Add($"-{breakdown.TerrainDefense} cover");
        if (breakdown.LOSPenalty != 0) parts.Add($"-{breakdown.LOSPenalty} LOS");
        if (breakdown.BraceBonus != 0) parts.Add($"-{breakdown.BraceBonus} brace");
        if (breakdown.SensorPenalty != 0) parts.Add($"-{breakdown.SensorPenalty} sensor");
        if (breakdown.ActuatorPenalty != 0) parts.Add($"-{breakdown.ActuatorPenalty} arm");

        MapHeaderLabel.Text = string.Join(" | ", parts);
    }

    private void ExecutePlayerAction(CombatAction action, HexCoord? targetHex = null,
        int? targetFrameId = null, int? weaponGroupId = null)
    {
        if (_combatState?.ActiveFrame == null) return;

        var frame = _combatState.ActiveFrame;
        var events = _combatService.ExecutePlayerAction(_combatState, frame, action,
            targetHex, targetFrameId, weaponGroupId);

        DisplayEvents(events);
        ClearSelectedAction();

        // Check if frame still has AP
        if (frame.ActionPoints <= 0 || frame.IsDestroyed)
        {
            _combatService.EndActivation(_combatState);
            HideActionButtons();
            AdvanceToNextUnit();
        }
        else
        {
            // Refresh UI for remaining AP
            ShowPlayerTurnUI(frame);
        }
    }

    private void EndTurn_Click(object sender, RoutedEventArgs e)
    {
        if (_combatState == null) return;
        _combatService.EndActivation(_combatState);
        HideActionButtons();
        ClearSelectedAction();
        AdvanceToNextUnit();
    }

    private void AutoResolveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_combatState == null) return;

        // Stop any AI animation
        _aiTimer?.Stop();

        AppendFeedText("=== AUTO-RESOLVING COMBAT ===", "#FFAA00");

        var log = _combatService.AutoResolveCombat(_combatState, _playerOrders, _enemyOrders);

        foreach (var round in log.Rounds)
        {
            AppendFeedText($"--- Round {round.RoundNumber} ---", "#666666");
            foreach (var evt in round.Events)
                AppendCombatEvent(evt);
        }

        RenderFullMap();
        EndCombat();
    }

    private void WithdrawButton_Click(object sender, RoutedEventArgs e)
    {
        if (_combatState == null) return;

        _combatState.Result = CombatResult.Withdrawal;
        AppendFeedText("Player forces withdraw from combat!", "#FF6600");
        EndCombat();
    }

    #endregion

    #region Combat End

    private void EndCombat()
    {
        if (_combatState == null) return;

        string resultText = _combatState.Result switch
        {
            CombatResult.Victory => "VICTORY!",
            CombatResult.Defeat => "DEFEAT",
            CombatResult.Withdrawal => "WITHDRAWAL",
            CombatResult.Stalemate => "STALEMATE",
            _ => "COMBAT OVER"
        };

        string resultColor = _combatState.Result == CombatResult.Victory ? "#00FF00" : "#FF4444";

        if (_combatState.Result == CombatResult.Victory)
            AudioService.PlayVictory();
        else
            AudioService.PlayDefeat();

        AppendFeedText($"=== {resultText} ===", resultColor);

        HideActionButtons();
        ActiveUnitHeader.Text = resultText;
        ActiveUnitInfo.Text = "";
        WeaponGroupsText.Text = "";
        TurnOrderPanel.Children.Clear();

        AutoResolveButton.Visibility = Visibility.Collapsed;
        WithdrawButton.Visibility = Visibility.Collapsed;
        ResetButton.Visibility = Visibility.Visible;

        if (_isCampaignMode)
        {
            HandlePostCombat();
        }
    }

    #endregion

    #region Hex Map Rendering

    private void HexCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderFullMap();
    }

    private void RenderFullMap()
    {
        HexCanvas.Children.Clear();
        if (_combatState?.Grid == null) return;

        double canvasW = HexCanvas.ActualWidth;
        double canvasH = HexCanvas.ActualHeight;
        if (canvasW < 10 || canvasH < 10) return;

        var grid = _combatState.Grid;
        double margin = 15;

        // Pointy-top hex sizing: width = sqrt(3)*size, height = 2*size
        double sqrt3 = Math.Sqrt(3.0);
        double hexSizeFromW = (canvasW - margin * 2) / (sqrt3 * (grid.Width + 0.5));
        double hexSizeFromH = (canvasH - margin * 2) / (1.5 * (grid.Height - 1) + 2.0);
        _hexSize = Math.Min(hexSizeFromW, hexSizeFromH);
        _hexSize = Math.Max(_hexSize, 8);

        // Calculate total grid pixel bounds to center it
        double minX = double.MaxValue, minY = double.MaxValue;
        double maxX = double.MinValue, maxY = double.MinValue;

        foreach (var cell in grid.AllCells)
        {
            var (px, py) = cell.Coord.ToPixel(_hexSize);
            double halfW = sqrt3 * _hexSize / 2.0;
            minX = Math.Min(minX, px - halfW);
            maxX = Math.Max(maxX, px + halfW);
            minY = Math.Min(minY, py - _hexSize);
            maxY = Math.Max(maxY, py + _hexSize);
        }

        double gridW = maxX - minX;
        double gridH = maxY - minY;
        _renderOffsetX = (canvasW - gridW) / 2 - minX;
        _renderOffsetY = (canvasH - gridH) / 2 - minY;

        // Draw terrain hex tiles (landscape-aware images) or fallback polygons
        double hexW = sqrt3 * _hexSize;
        double hexH = 2.0 * _hexSize;
        string landscape = grid.Landscape;
        var activeTiles = GetTilesForLandscape(landscape);

        foreach (var cell in grid.AllCells)
        {
            var (px, py) = cell.Coord.ToPixel(_hexSize);
            double cx = px + _renderOffsetX;
            double cy = py + _renderOffsetY;

            if (activeTiles.TryGetValue(cell.Terrain, out var tileBmp))
            {
                // Render tile image — tiles are pointy-top hex PNGs with transparent corners
                var img = new Image
                {
                    Source = tileBmp,
                    Width = hexW + 1,   // +1 to eliminate hairline gaps
                    Height = hexH + 1,
                    Stretch = Stretch.Fill,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(img, cx - (hexW + 1) / 2);
                Canvas.SetTop(img, cy - (hexH + 1) / 2);
                HexCanvas.Children.Add(img);
            }
            else
            {
                // Fallback: colored polygon (landscape-aware)
                var (fill, border, _) = GetTerrainColors(cell.Terrain, landscape);
                var polygon = CreateHexPolygon(cx, cy, _hexSize, fill, border, 0.8);
                HexCanvas.Children.Add(polygon);
            }
        }

        // Draw deployment zone highlights
        if (_combatState.Phase == TurnPhase.Deployment)
        {
            foreach (var coord in _deploymentZoneHexes)
            {
                if (_combatState.Grid.IsOccupied(coord)) continue;
                var (dpx, dpy) = coord.ToPixel(_hexSize);
                double dcx = dpx + _renderOffsetX;
                double dcy = dpy + _renderOffsetY;
                var overlay = CreateHexPolygon(dcx, dcy, _hexSize * 0.92,
                    new SolidColorBrush(Color.FromArgb(60, 0, 120, 220)),
                    new SolidColorBrush(Color.FromArgb(160, 0, 160, 255)), 1.5);
                HexCanvas.Children.Add(overlay);
            }
        }

        // Draw highlight overlays (semi-transparent polygons on top of tiles)
        foreach (var coord in _highlightedMoveHexes)
        {
            var (px, py) = coord.ToPixel(_hexSize);
            double cx = px + _renderOffsetX;
            double cy = py + _renderOffsetY;
            var overlay = CreateHexPolygon(cx, cy, _hexSize * 0.95,
                new SolidColorBrush(Color.FromArgb(100, 0, 200, 60)),
                new SolidColorBrush(Color.FromArgb(160, 0, 255, 0)), 1.5);
            HexCanvas.Children.Add(overlay);
        }

        foreach (var coord in _highlightedAttackHexes)
        {
            var (px, py) = coord.ToPixel(_hexSize);
            double cx = px + _renderOffsetX;
            double cy = py + _renderOffsetY;
            var overlay = CreateHexPolygon(cx, cy, _hexSize * 0.95,
                new SolidColorBrush(Color.FromArgb(100, 200, 30, 30)),
                new SolidColorBrush(Color.FromArgb(160, 255, 0, 0)), 1.5);
            HexCanvas.Children.Add(overlay);
        }

        // Draw LOS line when targeting
        if (_losLine != null && _losLine.Count >= 2 && _hoveredTarget != null)
        {
            var interveningSet = _losInterveningHexes?
                .ToDictionary(h => h.coord, h => h.penalty) ?? new();

            // Draw line segments connecting hex centers
            for (int i = 0; i < _losLine.Count - 1; i++)
            {
                var (p1x, p1y) = _losLine[i].ToPixel(_hexSize);
                var (p2x, p2y) = _losLine[i + 1].ToPixel(_hexSize);
                var line = new Line
                {
                    X1 = p1x + _renderOffsetX, Y1 = p1y + _renderOffsetY,
                    X2 = p2x + _renderOffsetX, Y2 = p2y + _renderOffsetY,
                    Stroke = new SolidColorBrush(Color.FromArgb(180, 255, 255, 0)),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    IsHitTestVisible = false
                };
                HexCanvas.Children.Add(line);
            }

            // Highlight intervening hexes that cause penalty (skip first and last)
            for (int i = 1; i < _losLine.Count - 1; i++)
            {
                var coord = _losLine[i];
                var (lx, ly) = coord.ToPixel(_hexSize);
                double lcx = lx + _renderOffsetX;
                double lcy = ly + _renderOffsetY;

                if (interveningSet.TryGetValue(coord, out int penalty))
                {
                    // Red-tinted overlay for blocking terrain
                    var overlay = CreateHexPolygon(lcx, lcy, _hexSize * 0.85,
                        new SolidColorBrush(Color.FromArgb(80, 255, 60, 0)),
                        new SolidColorBrush(Color.FromArgb(200, 255, 100, 0)), 1.5);
                    HexCanvas.Children.Add(overlay);

                    // Penalty text
                    if (_hexSize >= 14)
                    {
                        var penaltyText = new TextBlock
                        {
                            Text = $"-{penalty}",
                            FontSize = Math.Max(8, _hexSize * 0.35),
                            FontFamily = new FontFamily("Consolas"),
                            FontWeight = FontWeights.Bold,
                            Foreground = new SolidColorBrush(Color.FromRgb(255, 120, 0)),
                            TextAlignment = TextAlignment.Center
                        };
                        penaltyText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        Canvas.SetLeft(penaltyText, lcx - penaltyText.DesiredSize.Width / 2);
                        Canvas.SetTop(penaltyText, lcy - penaltyText.DesiredSize.Height / 2);
                        HexCanvas.Children.Add(penaltyText);
                    }
                }
                else
                {
                    // Clear line-of-sight hex — subtle green dot
                    var clearOverlay = CreateHexPolygon(lcx, lcy, _hexSize * 0.3,
                        new SolidColorBrush(Color.FromArgb(60, 200, 255, 0)),
                        Brushes.Transparent, 0);
                    HexCanvas.Children.Add(clearOverlay);
                }
            }
        }

        // Draw unit shapes (only those with a position assigned)
        foreach (var frame in _combatState.AllFrames.Where(f => !f.IsDestroyed && f.HexPosition != default(HexCoord)))
        {
            var (px, py) = frame.HexPosition.ToPixel(_hexSize);
            double cx = px + _renderOffsetX;
            double cy = py + _renderOffsetY;

            bool isPlayer = _combatState.PlayerFrames.Contains(frame);
            bool isActive = frame == _combatState.ActiveFrame;

            DrawUnitShape(cx, cy, frame, isPlayer, isActive);
        }

        // Draw active unit highlight border
        if (_combatState.ActiveFrame != null && !_combatState.ActiveFrame.IsDestroyed
            && _combatState.IsPlayerTurn)
        {
            var (px, py) = _combatState.ActiveFrame.HexPosition.ToPixel(_hexSize);
            double cx = px + _renderOffsetX;
            double cy = py + _renderOffsetY;

            var highlight = CreateHexPolygon(cx, cy, _hexSize + 2,
                Brushes.Transparent,
                new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                2.5);
            HexCanvas.Children.Add(highlight);
        }
    }

    private static (SolidColorBrush fill, SolidColorBrush border, string label) GetTerrainColors(HexTerrain terrain, string landscape = "Habitable")
    {
        // Station/Industrial: gray metal and blue-lit palette
        if (landscape is "Station" or "Industrial")
        {
            return terrain switch
            {
                HexTerrain.Rocks  => (new SolidColorBrush(Color.FromRgb(50, 55, 65)),
                                      new SolidColorBrush(Color.FromRgb(70, 80, 95)), "R"),
                HexTerrain.Rough  => (new SolidColorBrush(Color.FromRgb(55, 50, 40)),
                                      new SolidColorBrush(Color.FromRgb(80, 72, 55)), "~"),
                HexTerrain.Sand   => (new SolidColorBrush(Color.FromRgb(65, 60, 45)),
                                      new SolidColorBrush(Color.FromRgb(90, 82, 60)), ""),
                _                 => (new SolidColorBrush(Color.FromRgb(35, 40, 50)),
                                      new SolidColorBrush(Color.FromRgb(50, 58, 72)), "")
            };
        }

        // Default nature palette
        return terrain switch
        {
            HexTerrain.Forest => (new SolidColorBrush(Color.FromRgb(15, 65, 25)),
                                  new SolidColorBrush(Color.FromRgb(25, 100, 40)), "F"),
            HexTerrain.Rocks  => (new SolidColorBrush(Color.FromRgb(60, 60, 68)),
                                  new SolidColorBrush(Color.FromRgb(90, 90, 100)), "R"),
            HexTerrain.Rough  => (new SolidColorBrush(Color.FromRgb(65, 50, 22)),
                                  new SolidColorBrush(Color.FromRgb(95, 72, 35)), "~"),
            HexTerrain.Sand   => (new SolidColorBrush(Color.FromRgb(80, 70, 38)),
                                  new SolidColorBrush(Color.FromRgb(110, 95, 55)), ""),
            _                 => (new SolidColorBrush(Color.FromRgb(18, 42, 18)),
                                  new SolidColorBrush(Color.FromRgb(35, 78, 35)), "")
        };
    }

    // Pointy-top hex polygon: vertices start at -30 degrees
    private Polygon CreateHexPolygon(double cx, double cy, double size,
        Brush fill, Brush stroke, double strokeThickness)
    {
        var points = new PointCollection();
        for (int i = 0; i < 6; i++)
        {
            double angleDeg = 60.0 * i - 30.0; // pointy-top starts at -30°
            double angleRad = Math.PI / 180.0 * angleDeg;
            double px = cx + size * Math.Cos(angleRad);
            double py = cy + size * Math.Sin(angleRad);
            points.Add(new Point(px, py));
        }

        return new Polygon
        {
            Points = points,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness
        };
    }

    private void DrawUnitShape(double cx, double cy, CombatFrame frame, bool isPlayer, bool isActive)
    {
        double unitRadius = frame.Class switch
        {
            "Light" => _hexSize * 0.30,
            "Medium" => _hexSize * 0.35,
            "Heavy" => _hexSize * 0.40,
            "Assault" => _hexSize * 0.45,
            _ => _hexSize * 0.35
        };

        int sides = frame.Class switch
        {
            "Light" => 4,
            "Medium" => 5,
            "Heavy" => 6,
            "Assault" => 8,
            _ => 5
        };

        Color fillColor;
        if (frame.IsDestroyed)
            fillColor = Color.FromRgb(80, 80, 80);
        else if (frame.ArmorPercent < 25)
            fillColor = isPlayer ? Color.FromRgb(200, 150, 0) : Color.FromRgb(200, 80, 0);
        else
            fillColor = isPlayer ? Color.FromRgb(0, 180, 0) : Color.FromRgb(200, 50, 50);

        double rotOffset = (frame.Class == "Light") ? 45.0 : 0.0;
        var points = new PointCollection();
        for (int i = 0; i < sides; i++)
        {
            double angle = (360.0 / sides * i + rotOffset - 90) * Math.PI / 180.0;
            points.Add(new Point(
                cx + unitRadius * Math.Cos(angle),
                cy + unitRadius * Math.Sin(angle)));
        }

        var shape = new Polygon
        {
            Points = points,
            Fill = new SolidColorBrush(fillColor),
            Stroke = Brushes.Black,
            StrokeThickness = 1.5
        };
        HexCanvas.Children.Add(shape);

        // Class letter label
        string label = frame.Class.Length > 0 ? frame.Class[0].ToString() : "?";
        var textBlock = new TextBlock
        {
            Text = label,
            FontSize = unitRadius * 0.9,
            FontFamily = new FontFamily("Consolas"),
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.Black,
            TextAlignment = TextAlignment.Center
        };
        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(textBlock, cx - textBlock.DesiredSize.Width / 2);
        Canvas.SetTop(textBlock, cy - textBlock.DesiredSize.Height / 2);
        HexCanvas.Children.Add(textBlock);

        // Name label below unit
        if (_hexSize >= 18)
        {
            var nameLabel = new TextBlock
            {
                Text = frame.CustomName.Length > 8 ? frame.CustomName[..8] : frame.CustomName,
                FontSize = Math.Max(7, _hexSize * 0.28),
                FontFamily = new FontFamily("Consolas"),
                Foreground = isPlayer
                    ? new SolidColorBrush(Color.FromRgb(0, 200, 0))
                    : new SolidColorBrush(Color.FromRgb(200, 100, 100)),
                TextAlignment = TextAlignment.Center
            };
            nameLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(nameLabel, cx - nameLabel.DesiredSize.Width / 2);
            Canvas.SetTop(nameLabel, cy + unitRadius + 1);
            HexCanvas.Children.Add(nameLabel);
        }
    }

    #endregion

    #region Turn Order Display

    private void UpdateTurnOrder()
    {
        TurnOrderPanel.Children.Clear();
        if (_combatState == null) return;

        TurnOrderHeader.Text = $"TURN ORDER — ROUND {_combatState.RoundNumber}";

        foreach (var frame in _combatState.InitiativeOrder)
        {
            bool isPlayer = _combatState.PlayerFrames.Contains(frame);
            bool isActive = frame == _combatState.ActiveFrame;
            bool hasActed = frame.HasActedThisRound;

            string marker = isActive ? " >>>" : (hasActed ? " [done]" : "");
            string classLetter = frame.Class.Length > 0 ? frame.Class[0].ToString() : "?";

            Color textColor;
            if (frame.IsDestroyed) textColor = Color.FromRgb(80, 80, 80);
            else if (isActive) textColor = isPlayer ? Color.FromRgb(0, 255, 0) : Color.FromRgb(255, 100, 100);
            else if (hasActed) textColor = Color.FromRgb(80, 80, 80);
            else textColor = isPlayer ? Color.FromRgb(0, 170, 0) : Color.FromRgb(170, 60, 60);

            var tb = new TextBlock
            {
                Text = $" {frame.CustomName} ({classLetter}){marker}",
                FontSize = 9,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(textColor),
                FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(0, 1, 0, 0)
            };
            TurnOrderPanel.Children.Add(tb);
        }
    }

    #endregion

    #region Combat Feed

    private void DisplayEvents(List<CombatEvent> events)
    {
        foreach (var evt in events)
            AppendCombatEvent(evt);
    }

    private void AppendCombatEvent(CombatEvent evt)
    {
        string color = evt.Type switch
        {
            CombatEventType.Hit => "#FF8800",
            CombatEventType.Critical => "#FF0000",
            CombatEventType.Miss => "#666666",
            CombatEventType.Movement => "#4488FF",
            CombatEventType.ComponentDamage => "#FF4400",
            CombatEventType.AmmoExplosion => "#FF0000",
            CombatEventType.LocationDestroyed => "#FF2200",
            CombatEventType.FrameDestroyed => "#FF0000",
            CombatEventType.ReactorOverload => "#FFAA00",
            CombatEventType.ReactorShutdown => "#FF6600",
            CombatEventType.ReactorVent => "#44AAFF",
            CombatEventType.DamageTransfer => "#CC6600",
            CombatEventType.Brace => "#00AAFF",
            CombatEventType.Overwatch => "#00AAFF",
            CombatEventType.RoundSummary => "#888888",
            _ => "#00CC00"
        };

        // Combat sound effects
        switch (evt.Type)
        {
            case CombatEventType.Hit:
            case CombatEventType.Critical:
                AudioService.PlayHit();
                break;
            case CombatEventType.Miss:
                AudioService.PlayMiss();
                break;
            case CombatEventType.FrameDestroyed:
                AudioService.PlayDestroyed();
                break;
        }

        AppendFeedText(CombatService.FormatEvent(evt), color);
    }

    private void AppendFeedText(string text, string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 9,
            FontFamily = new FontFamily("Consolas"),
            Foreground = new SolidColorBrush(color),
            TextWrapping = TextWrapping.Wrap
        };
        CombatFeedPanel.Children.Add(tb);
        CombatFeedScroller.ScrollToEnd();
    }

    #endregion

    #region Helpers

    private int GetWeaponGroupMaxRange(CombatFrame frame, int groupId)
    {
        if (!frame.WeaponGroups.TryGetValue(groupId, out var weapons))
            return 0;

        int maxRange = 0;
        foreach (var w in weapons.Where(w => !w.IsDestroyed))
        {
            int range = PositioningSystem.GetWeaponMaxRange(w.RangeClass);
            if (range > maxRange) maxRange = range;
        }
        return maxRange;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _aiTimer?.Stop();
        _isCampaignMode = false;
        _currentMission = null;
        _combatState = null;
        CombatFeedPanel.Children.Clear();
        TurnOrderPanel.Children.Clear();
        HexCanvas.Children.Clear();
        HideActionButtons();

        StartCombatButton.Visibility = Visibility.Visible;
        AutoResolveButton.Visibility = Visibility.Collapsed;
        WithdrawButton.Visibility = Visibility.Collapsed;
        PreCombatPanel.Visibility = Visibility.Visible;

        ActiveUnitHeader.Text = "AWAITING COMBAT";
        ActiveUnitInfo.Text = "";
        WeaponGroupsText.Text = "";

        OpenManagementWindow();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // Space to skip AI animation
        if (e.Key == Key.Space && _aiTimer != null && _aiTimer.IsEnabled)
        {
            _aiTimer.Stop();
            while (_aiEventQueue != null && _aiEventQueue.Any())
            {
                AppendCombatEvent(_aiEventQueue.Dequeue());
            }
            _aiTimer = null;
            RenderFullMap();
            AdvanceToNextUnit();
        }
    }

    #endregion
}
