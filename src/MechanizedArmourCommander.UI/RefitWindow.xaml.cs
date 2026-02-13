using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MechanizedArmourCommander.Core.Services;
using MechanizedArmourCommander.Data;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.UI;

public partial class RefitWindow : Window
{
    private readonly ManagementService _management;
    private readonly DatabaseContext _dbContext;
    private readonly int _instanceId;
    private readonly FrameInstance _frame;
    private readonly Chassis _chassis;

    // Original state for diff/reset
    private readonly List<Loadout> _originalLoadout;
    private readonly List<InventoryItem> _originalInventory;
    private readonly List<EquipmentLoadout> _originalEquipmentLoadout;
    private readonly List<EquipmentInventoryItem> _originalEquipmentInventory;

    // Staged state (changes applied here, not saved until confirm)
    private List<StagedWeapon> _stagedEquipped = new();
    private List<StagedInventory> _stagedInventory = new();
    private List<StagedEquipment> _stagedEquipment = new();        // equipped on frame
    private List<StagedEquipmentInv> _stagedEquipmentInv = new();  // in inventory

    // Hardpoint layout — fixed positions per chassis
    private List<HardpointDef> _hardpointDefs = new();

    // Interaction
    private int? _selectedSlotIndex;       // hardpoint slot selected on diagram
    private int? _selectedInventoryIndex;  // inventory weapon selected
    private int? _selectedEquipmentInvIndex; // inventory equipment selected

    // Cost config
    private const int CostPerChange = 500;
    private const int DaysPerChange = 1;

    public bool RefitApplied { get; private set; }

    public RefitWindow(DatabaseContext dbContext, int instanceId)
    {
        InitializeComponent();
        AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent,
            new RoutedEventHandler((_, _) => AudioService.PlayClick()));
        _dbContext = dbContext;
        _management = new ManagementService(dbContext);
        _instanceId = instanceId;

        _frame = _management.GetRoster().First(f => f.InstanceId == instanceId);
        _chassis = _frame.Chassis ?? _management.GetAllChassis().First(c => c.ChassisId == _frame.ChassisId);
        _originalLoadout = _management.GetLoadout(instanceId);
        _originalInventory = _management.GetInventory();
        _originalEquipmentLoadout = _management.GetEquipmentLoadout(instanceId);
        _originalEquipmentInventory = _management.GetEquipmentInventory();

        FrameHeaderText.Text = $"REFIT BAY — {_frame.CustomName} ({_chassis.Designation} {_chassis.Name})";

        GenerateHardpointLayout();
        ResetToOriginal();
    }

    #region Data Classes

    private class StagedWeapon
    {
        public int? OriginalLoadoutId { get; set; }
        public int WeaponId { get; set; }
        public string WeaponName { get; set; } = "";
        public string HardpointSize { get; set; } = "";
        public int SpaceCost { get; set; }
        public int Damage { get; set; }
        public int EnergyCost { get; set; }
        public string RangeClass { get; set; } = "";
        public string MountLocation { get; set; } = "";
        public int WeaponGroup { get; set; }
        public string HardpointSlot { get; set; } = "";
        public int SlotDefIndex { get; set; } = -1;
    }

    private class StagedInventory
    {
        public int? OriginalInventoryId { get; set; }
        public int WeaponId { get; set; }
        public string WeaponName { get; set; } = "";
        public string HardpointSize { get; set; } = "";
        public int SpaceCost { get; set; }
        public int Damage { get; set; }
        public int EnergyCost { get; set; }
        public string RangeClass { get; set; } = "";
    }

    private class HardpointDef
    {
        public int Index { get; set; }
        public string Location { get; set; } = "";
        public string Size { get; set; } = "";  // Small / Medium / Large
        public string SlotName { get; set; } = "";
    }

    private class StagedEquipment
    {
        public int? OriginalLoadoutId { get; set; }
        public int EquipmentId { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string? HardpointSize { get; set; }
        public int SpaceCost { get; set; }
        public int EnergyCost { get; set; }
        public string Effect { get; set; } = "";
        public string? Description { get; set; }
        public string? HardpointSlot { get; set; }   // null for Passive/Active
        public int SlotDefIndex { get; set; } = -1;   // -1 for Passive/Active
    }

    private class StagedEquipmentInv
    {
        public int? OriginalInventoryId { get; set; }
        public int EquipmentId { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string? HardpointSize { get; set; }
        public int SpaceCost { get; set; }
        public int EnergyCost { get; set; }
        public string Effect { get; set; } = "";
        public string? Description { get; set; }
    }

    #endregion

    #region Hardpoint Layout Generation

    private void GenerateHardpointLayout()
    {
        _hardpointDefs.Clear();
        int index = 0;

        // Fixed distribution — each chassis gets the same layout for its hardpoint counts
        // Large: CT, RT, LT (cycle)
        // Medium: RA, LA, RT, LT, CT (cycle)
        // Small: Head, RA, LA, RT, LT (cycle)
        var largeLocations = new[] { "CenterTorso", "RightTorso", "LeftTorso" };
        var mediumLocations = new[] { "RightArm", "LeftArm", "RightTorso", "LeftTorso", "CenterTorso" };
        var smallLocations = new[] { "Head", "RightArm", "LeftArm", "RightTorso", "LeftTorso" };

        for (int i = 0; i < _chassis.HardpointLarge; i++)
        {
            _hardpointDefs.Add(new HardpointDef
            {
                Index = index++,
                Location = largeLocations[i % largeLocations.Length],
                Size = "Large",
                SlotName = $"large_{i + 1}"
            });
        }

        for (int i = 0; i < _chassis.HardpointMedium; i++)
        {
            _hardpointDefs.Add(new HardpointDef
            {
                Index = index++,
                Location = mediumLocations[i % mediumLocations.Length],
                Size = "Medium",
                SlotName = $"medium_{i + 1}"
            });
        }

        for (int i = 0; i < _chassis.HardpointSmall; i++)
        {
            _hardpointDefs.Add(new HardpointDef
            {
                Index = index++,
                Location = smallLocations[i % smallLocations.Length],
                Size = "Small",
                SlotName = $"small_{i + 1}"
            });
        }
    }

    #endregion

    #region State Management

    private void ResetToOriginal()
    {
        _stagedEquipped.Clear();
        _stagedInventory.Clear();
        _stagedEquipment.Clear();
        _stagedEquipmentInv.Clear();
        _selectedSlotIndex = null;
        _selectedInventoryIndex = null;
        _selectedEquipmentInvIndex = null;

        var usedSlotIndices = new HashSet<int>();

        foreach (var l in _originalLoadout)
        {
            if (l.Weapon == null) continue;

            // Match to hardpoint def: slot name first, then location+size, then any matching size
            var slot = _hardpointDefs.FirstOrDefault(h =>
                h.SlotName == l.HardpointSlot && h.Size == l.Weapon.HardpointSize
                && !usedSlotIndices.Contains(h.Index));

            slot ??= _hardpointDefs.FirstOrDefault(h =>
                h.Location == l.MountLocation && h.Size == l.Weapon.HardpointSize
                && !usedSlotIndices.Contains(h.Index));

            slot ??= _hardpointDefs.FirstOrDefault(h =>
                h.Size == l.Weapon.HardpointSize
                && !usedSlotIndices.Contains(h.Index));

            if (slot != null) usedSlotIndices.Add(slot.Index);

            _stagedEquipped.Add(new StagedWeapon
            {
                OriginalLoadoutId = l.LoadoutId,
                WeaponId = l.WeaponId,
                WeaponName = l.Weapon.Name,
                HardpointSize = l.Weapon.HardpointSize,
                SpaceCost = l.Weapon.SpaceCost,
                Damage = l.Weapon.Damage,
                EnergyCost = l.Weapon.EnergyCost,
                RangeClass = l.Weapon.RangeClass,
                MountLocation = slot?.Location ?? l.MountLocation,
                WeaponGroup = l.WeaponGroup,
                HardpointSlot = slot?.SlotName ?? l.HardpointSlot,
                SlotDefIndex = slot?.Index ?? -1
            });
        }

        foreach (var i in _originalInventory)
        {
            if (i.Weapon == null) continue;
            _stagedInventory.Add(new StagedInventory
            {
                OriginalInventoryId = i.InventoryId,
                WeaponId = i.WeaponId,
                WeaponName = i.Weapon.Name,
                HardpointSize = i.Weapon.HardpointSize,
                SpaceCost = i.Weapon.SpaceCost,
                Damage = i.Weapon.Damage,
                EnergyCost = i.Weapon.EnergyCost,
                RangeClass = i.Weapon.RangeClass
            });
        }

        // Load equipped equipment
        foreach (var eq in _originalEquipmentLoadout)
        {
            if (eq.Equipment == null) continue;

            // Slot-type equipment: find matching hardpoint def
            int slotIdx = -1;
            string? slotName = null;
            if (eq.Equipment.Category == "Slot" && eq.HardpointSlot != null)
            {
                var slot = _hardpointDefs.FirstOrDefault(h =>
                    h.SlotName == eq.HardpointSlot && h.Size == eq.Equipment.HardpointSize
                    && !usedSlotIndices.Contains(h.Index));
                if (slot != null)
                {
                    slotIdx = slot.Index;
                    slotName = slot.SlotName;
                    usedSlotIndices.Add(slot.Index);
                }
            }

            _stagedEquipment.Add(new StagedEquipment
            {
                OriginalLoadoutId = eq.EquipmentLoadoutId,
                EquipmentId = eq.EquipmentId,
                Name = eq.Equipment.Name,
                Category = eq.Equipment.Category,
                HardpointSize = eq.Equipment.HardpointSize,
                SpaceCost = eq.Equipment.SpaceCost,
                EnergyCost = eq.Equipment.EnergyCost,
                Effect = eq.Equipment.Effect,
                Description = eq.Equipment.Description,
                HardpointSlot = slotName,
                SlotDefIndex = slotIdx
            });
        }

        // Load equipment inventory
        foreach (var ei in _originalEquipmentInventory)
        {
            if (ei.Equipment == null) continue;
            _stagedEquipmentInv.Add(new StagedEquipmentInv
            {
                OriginalInventoryId = ei.EquipmentInventoryId,
                EquipmentId = ei.EquipmentId,
                Name = ei.Equipment.Name,
                Category = ei.Equipment.Category,
                HardpointSize = ei.Equipment.HardpointSize,
                SpaceCost = ei.Equipment.SpaceCost,
                EnergyCost = ei.Equipment.EnergyCost,
                Effect = ei.Equipment.Effect,
                Description = ei.Equipment.Description
            });
        }

        RefreshAll();
    }

    private void RefreshAll()
    {
        UpdateHardpointSummary();
        DrawMechDiagram();
        UpdateInventoryPanel();
        UpdateCostDisplay();
    }

    private void UpdateHardpointSummary()
    {
        int sUsed = _stagedEquipped.Count(w => w.HardpointSize == "Small")
                  + _stagedEquipment.Count(e => e.HardpointSize == "Small");
        int mUsed = _stagedEquipped.Count(w => w.HardpointSize == "Medium")
                  + _stagedEquipment.Count(e => e.HardpointSize == "Medium");
        int lUsed = _stagedEquipped.Count(w => w.HardpointSize == "Large")
                  + _stagedEquipment.Count(e => e.HardpointSize == "Large");
        int spaceUsed = _stagedEquipped.Sum(w => w.SpaceCost)
                      + _stagedEquipment.Sum(e => e.SpaceCost);
        int eqCount = _stagedEquipment.Count(e => e.Category != "Slot");

        HardpointSummaryText.Text = $"Slots: {sUsed}/{_chassis.HardpointSmall}S  {mUsed}/{_chassis.HardpointMedium}M  {lUsed}/{_chassis.HardpointLarge}L  |  Space: {spaceUsed}/{_chassis.TotalSpace}  |  Equip: {eqCount}";
    }

    #endregion

    #region Mech Diagram

    private readonly Dictionary<string, Rect> _locationBounds = new();
    private readonly Dictionary<int, Rect> _slotBounds = new();

    private static readonly string[] AllLocations =
        { "Head", "LeftArm", "LeftTorso", "CenterTorso", "RightTorso", "RightArm", "Legs" };

    private void MechCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawMechDiagram();

    private void DrawMechDiagram()
    {
        MechCanvas.Children.Clear();
        _locationBounds.Clear();
        _slotBounds.Clear();

        double cw = MechCanvas.ActualWidth;
        double ch = MechCanvas.ActualHeight;
        if (cw < 50 || ch < 50) return;

        double cx = cw / 2;
        double s = Math.Min(cw, ch) / 420.0;

        DrawLocation("Head", cx - 20 * s, 10 * s, 40 * s, 45 * s, true, s);
        DrawLocation("CenterTorso", cx - 40 * s, 60 * s, 80 * s, 120 * s, false, s);
        DrawLocation("LeftTorso", cx - 40 * s - 55 * s, 70 * s, 55 * s, 100 * s, false, s);
        DrawLocation("RightTorso", cx + 40 * s, 70 * s, 55 * s, 100 * s, false, s);
        DrawLocation("LeftArm", cx - 40 * s - 55 * s - 40 * s, 60 * s, 38 * s, 130 * s, false, s);
        DrawLocation("RightArm", cx + 40 * s + 55 * s + 2 * s, 60 * s, 38 * s, 130 * s, false, s);
        DrawLocation("Legs", cx - 50 * s, 185 * s, 100 * s, 80 * s, false, s);
    }

    private void DrawLocation(string location, double x, double y, double w, double h, bool isCircle, double scale)
    {
        _locationBounds[location] = new Rect(x, y, w, h);

        var slotsHere = _hardpointDefs.Where(hp => hp.Location == location).ToList();
        bool hasSlots = slotsHere.Any();

        // Body parts without hardpoints are dimmed
        Color fillColor = hasSlots ? Color.FromRgb(12, 18, 12) : Color.FromRgb(10, 10, 10);
        Color strokeColor = hasSlots ? Color.FromRgb(0, 100, 0) : Color.FromRgb(30, 30, 30);

        Shape shape;
        if (isCircle)
        {
            shape = new Ellipse
            {
                Width = w, Height = h,
                Fill = new SolidColorBrush(fillColor),
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = 1.5
            };
        }
        else
        {
            shape = new Rectangle
            {
                Width = w, Height = h,
                Fill = new SolidColorBrush(fillColor),
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = 1.5
            };
        }
        Canvas.SetLeft(shape, x);
        Canvas.SetTop(shape, y);
        MechCanvas.Children.Add(shape);

        // Location abbreviation
        string abbrev = location switch
        {
            "CenterTorso" => "CT",
            "LeftTorso" => "LT",
            "RightTorso" => "RT",
            "LeftArm" => "LA",
            "RightArm" => "RA",
            _ => location.Length > 4 ? location[..4].ToUpper() : location.ToUpper()
        };

        double labelFontSize = Math.Max(7, w * 0.20);
        var label = new TextBlock
        {
            Text = abbrev,
            FontSize = labelFontSize,
            FontFamily = new FontFamily("Consolas"),
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(hasSlots ? Color.FromRgb(0, 160, 0) : Color.FromRgb(50, 50, 50)),
            TextAlignment = TextAlignment.Center
        };
        label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Canvas.SetLeft(label, x + w / 2 - label.DesiredSize.Width / 2);
        Canvas.SetTop(label, y + 2);
        MechCanvas.Children.Add(label);

        // Draw individual hardpoint slots
        double slotFontSize = Math.Max(6, Math.Min(w * 0.17, 10));
        double slotHeight = slotFontSize + 7;
        double slotPad = 3;
        double slotY = y + (isCircle ? h * 0.48 : labelFontSize + 10);

        for (int i = 0; i < slotsHere.Count; i++)
        {
            var slot = slotsHere[i];
            var equippedWeapon = _stagedEquipped.FirstOrDefault(e => e.SlotDefIndex == slot.Index);
            var equippedEquip = _stagedEquipment.FirstOrDefault(e => e.SlotDefIndex == slot.Index);
            bool slotSelected = _selectedSlotIndex == slot.Index;
            bool hasItem = equippedWeapon != null || equippedEquip != null;

            double slotX = x + slotPad;
            double slotW = w - slotPad * 2;
            var slotRect = new Rect(slotX, slotY, slotW, slotHeight);
            _slotBounds[slot.Index] = slotRect;

            // Slot background color
            Color slotFill, slotBorder;
            if (slotSelected)
            {
                slotFill = Color.FromRgb(0, 40, 60);
                slotBorder = Color.FromRgb(0, 160, 220);
            }
            else if (equippedEquip != null)
            {
                slotFill = Color.FromRgb(0, 15, 35);
                slotBorder = Color.FromRgb(0, 70, 140);
            }
            else if (equippedWeapon != null)
            {
                slotFill = Color.FromRgb(0, 25, 0);
                slotBorder = Color.FromRgb(0, 100, 0);
            }
            else
            {
                slotFill = Color.FromRgb(18, 18, 18);
                slotBorder = Color.FromRgb(50, 50, 50);
            }

            var slotBg = new Rectangle
            {
                Width = slotW, Height = slotHeight,
                Fill = new SolidColorBrush(slotFill),
                Stroke = new SolidColorBrush(slotBorder),
                StrokeThickness = slotSelected ? 1.5 : 0.8
            };
            Canvas.SetLeft(slotBg, slotX);
            Canvas.SetTop(slotBg, slotY);
            MechCanvas.Children.Add(slotBg);

            // Size indicator color: L=red, M=yellow, S=green
            string sizeChar = slot.Size[0].ToString();
            Color sizeColor = slot.Size switch
            {
                "Large" => Color.FromRgb(220, 80, 80),
                "Medium" => Color.FromRgb(220, 180, 0),
                _ => Color.FromRgb(100, 200, 100)
            };

            // Size letter (always visible)
            var sizeLabel = new TextBlock
            {
                Text = sizeChar,
                FontSize = slotFontSize,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(sizeColor)
            };
            Canvas.SetLeft(sizeLabel, slotX + 2);
            Canvas.SetTop(sizeLabel, slotY + 2);
            MechCanvas.Children.Add(sizeLabel);

            // Weapon/equipment name or empty indicator
            string contentText;
            Color contentColor;
            if (equippedWeapon != null)
            {
                int maxChars = Math.Max(2, (int)(slotW / (slotFontSize * 0.58)) - 3);
                string wName = equippedWeapon.WeaponName;
                if (wName.Length > maxChars) wName = wName[..maxChars];
                contentText = $"  {wName}";
                contentColor = Color.FromRgb(255, 200, 0);
            }
            else if (equippedEquip != null)
            {
                int maxChars = Math.Max(2, (int)(slotW / (slotFontSize * 0.58)) - 3);
                string eName = equippedEquip.Name;
                if (eName.Length > maxChars) eName = eName[..maxChars];
                contentText = $"  {eName}";
                contentColor = Color.FromRgb(100, 180, 255);
            }
            else
            {
                contentText = $"  ---";
                contentColor = Color.FromRgb(60, 60, 60);
            }

            var contentLabel = new TextBlock
            {
                Text = contentText,
                FontSize = slotFontSize,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(contentColor),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = slotW - 6
            };
            Canvas.SetLeft(contentLabel, slotX + 2);
            Canvas.SetTop(contentLabel, slotY + 2);
            MechCanvas.Children.Add(contentLabel);

            slotY += slotHeight + 2;
        }
    }

    private void MechCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(MechCanvas);

        // Check hardpoint slots first (more specific)
        foreach (var (slotIndex, bounds) in _slotBounds)
        {
            if (bounds.Contains(pos))
            {
                HandleSlotClick(slotIndex);
                return;
            }
        }
    }

    private void HandleSlotClick(int slotIndex)
    {
        var slot = _hardpointDefs.FirstOrDefault(h => h.Index == slotIndex);
        if (slot == null) return;

        var equippedWeapon = _stagedEquipped.FirstOrDefault(w => w.SlotDefIndex == slotIndex);
        var equippedEquip = _stagedEquipment.FirstOrDefault(e => e.SlotDefIndex == slotIndex);
        bool slotOccupied = equippedWeapon != null || equippedEquip != null;

        if (_selectedInventoryIndex.HasValue && !slotOccupied)
        {
            // Try equip selected inventory weapon to this slot
            TryEquipToSlot(_selectedInventoryIndex.Value, slot);
            _selectedInventoryIndex = null;
            _selectedSlotIndex = null;
        }
        else if (_selectedEquipmentInvIndex.HasValue && !slotOccupied)
        {
            // Try install selected equipment to this slot
            TryInstallEquipmentToSlot(_selectedEquipmentInvIndex.Value, slot);
            _selectedEquipmentInvIndex = null;
            _selectedSlotIndex = null;
        }
        else
        {
            // Toggle slot selection
            _selectedSlotIndex = _selectedSlotIndex == slotIndex ? null : slotIndex;
        }

        RefreshAll();
    }

    #endregion

    #region Inventory Panel

    private void UpdateInventoryPanel()
    {
        InventoryPanel.Children.Clear();

        // -- Equipped weapons section --
        var equippedHeader = new TextBlock
        {
            Text = "EQUIPPED WEAPONS",
            FontSize = 11, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(0, 0, 0, 4)
        };
        InventoryPanel.Children.Add(equippedHeader);

        if (!_stagedEquipped.Any())
        {
            AddInventoryText("  (no weapons equipped)", "#666666");
        }
        else
        {
            foreach (var weapon in _stagedEquipped)
            {
                string locAbbrev = weapon.MountLocation switch
                {
                    "CenterTorso" => "CT", "LeftTorso" => "LT", "RightTorso" => "RT",
                    "LeftArm" => "LA", "RightArm" => "RA", _ => weapon.MountLocation[..Math.Min(4, weapon.MountLocation.Length)]
                };

                var panel = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 51, 0)),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(Color.FromRgb(0, 15, 0)),
                    Padding = new Thickness(4),
                    Margin = new Thickness(0, 0, 0, 2)
                };

                var stack = new StackPanel { Orientation = Orientation.Horizontal };

                stack.Children.Add(new TextBlock
                {
                    Text = $"[{weapon.HardpointSize[0]}] {weapon.WeaponName} @ {locAbbrev}  {weapon.Damage}D {weapon.EnergyCost}E",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 0)),
                    FontFamily = new FontFamily("Consolas"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                });

                var removeBtn = new Button
                {
                    Content = "REMOVE",
                    Tag = _stagedEquipped.IndexOf(weapon),
                    Height = 22,
                    Padding = new Thickness(6, 0, 6, 0),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 8, FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromRgb(51, 0, 0)),
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 0)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(102, 34, 0)),
                    BorderThickness = new Thickness(1)
                };
                removeBtn.Click += RemoveWeapon_Click;
                stack.Children.Add(removeBtn);

                panel.Child = stack;
                InventoryPanel.Children.Add(panel);
            }
        }

        // -- Separator --
        InventoryPanel.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromRgb(0, 51, 0)),
            Margin = new Thickness(0, 10, 0, 10)
        });

        // -- Inventory section --
        var invHeader = new TextBlock
        {
            Text = "INVENTORY — Select weapon, then click a matching slot",
            FontSize = 10, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 0)),
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(0, 0, 0, 6)
        };
        InventoryPanel.Children.Add(invHeader);

        if (!_stagedInventory.Any())
        {
            AddInventoryText("  No weapons in inventory.", "#666666");
        }
        else
        {
            for (int i = 0; i < _stagedInventory.Count; i++)
            {
                var item = _stagedInventory[i];
                bool canEquip = CanEquip(item);
                bool isSelected = _selectedInventoryIndex == i;

                var panel = new Border
                {
                    BorderBrush = new SolidColorBrush(isSelected ? Color.FromRgb(0, 120, 180) : Color.FromRgb(0, 40, 0)),
                    BorderThickness = new Thickness(isSelected ? 2 : 1),
                    Background = new SolidColorBrush(canEquip ? Color.FromRgb(0, 12, 0) : Color.FromRgb(15, 15, 15)),
                    Padding = new Thickness(4),
                    Margin = new Thickness(0, 0, 0, 2)
                };

                var stack = new StackPanel { Orientation = Orientation.Horizontal };

                stack.Children.Add(new TextBlock
                {
                    Text = $"{item.WeaponName} ({item.HardpointSize[0]}) {item.Damage}D {item.EnergyCost}E {item.SpaceCost}sp {item.RangeClass}",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(canEquip ? Color.FromRgb(0, 200, 0) : Color.FromRgb(80, 80, 80)),
                    FontFamily = new FontFamily("Consolas"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                });

                if (canEquip)
                {
                    var selectBtn = new Button
                    {
                        Content = isSelected ? "SELECTED" : "SELECT",
                        Tag = i,
                        Height = 22,
                        Padding = new Thickness(6, 0, 6, 0),
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 8, FontWeight = FontWeights.Bold,
                        Background = new SolidColorBrush(isSelected ? Color.FromRgb(0, 40, 60) : Color.FromRgb(0, 20, 0)),
                        Foreground = new SolidColorBrush(isSelected ? Color.FromRgb(0, 200, 255) : Color.FromRgb(0, 255, 0)),
                        BorderBrush = new SolidColorBrush(isSelected ? Color.FromRgb(0, 140, 200) : Color.FromRgb(0, 68, 0)),
                        BorderThickness = new Thickness(1)
                    };
                    selectBtn.Click += SelectInventory_Click;
                    stack.Children.Add(selectBtn);
                }
                else
                {
                    string reason = GetCannotEquipReason(item);
                    stack.Children.Add(new TextBlock
                    {
                        Text = reason,
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Color.FromRgb(120, 60, 60)),
                        FontFamily = new FontFamily("Consolas"),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                panel.Child = stack;
                InventoryPanel.Children.Add(panel);
            }
        }

        if (_selectedInventoryIndex.HasValue)
        {
            var item = _stagedInventory[_selectedInventoryIndex.Value];
            string sizeChar = item.HardpointSize[0].ToString();
            AddInventoryText($"\nClick an empty [{sizeChar}] slot on the mech to equip.", "#00AAFF");
        }

        // -- Separator --
        InventoryPanel.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromRgb(0, 51, 102)),
            Margin = new Thickness(0, 10, 0, 10)
        });

        // -- Equipped Equipment section --
        var eqHeader = new TextBlock
        {
            Text = "EQUIPPED EQUIPMENT",
            FontSize = 11, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(0, 0, 0, 4)
        };
        InventoryPanel.Children.Add(eqHeader);

        if (!_stagedEquipment.Any())
        {
            AddInventoryText("  (no equipment installed)", "#666666");
        }
        else
        {
            foreach (var eq in _stagedEquipment)
            {
                string catTag = eq.Category == "Slot" ? $"[{eq.HardpointSize![0]}]" : $"[{eq.Category[0]}]";

                var panel = new Border
                {
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0, 40, 80)),
                    BorderThickness = new Thickness(1),
                    Background = new SolidColorBrush(Color.FromRgb(0, 10, 25)),
                    Padding = new Thickness(4),
                    Margin = new Thickness(0, 0, 0, 2)
                };

                var stack = new StackPanel { Orientation = Orientation.Horizontal };

                stack.Children.Add(new TextBlock
                {
                    Text = $"{catTag} {eq.Name}  {eq.SpaceCost}sp  {eq.Description ?? eq.Effect}",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                    FontFamily = new FontFamily("Consolas"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                });

                var removeBtn = new Button
                {
                    Content = "REMOVE",
                    Tag = _stagedEquipment.IndexOf(eq),
                    Height = 22,
                    Padding = new Thickness(6, 0, 6, 0),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 8, FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Color.FromRgb(51, 0, 0)),
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 0)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(102, 34, 0)),
                    BorderThickness = new Thickness(1)
                };
                removeBtn.Click += RemoveEquipment_Click;
                stack.Children.Add(removeBtn);

                panel.Child = stack;
                InventoryPanel.Children.Add(panel);
            }
        }

        // -- Separator --
        InventoryPanel.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromRgb(0, 51, 102)),
            Margin = new Thickness(0, 10, 0, 10)
        });

        // -- Equipment Inventory --
        var eqInvHeader = new TextBlock
        {
            Text = "EQUIPMENT INVENTORY — Select, then click slot or INSTALL",
            FontSize = 10, FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(80, 160, 220)),
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(0, 0, 0, 6)
        };
        InventoryPanel.Children.Add(eqInvHeader);

        if (!_stagedEquipmentInv.Any())
        {
            AddInventoryText("  No equipment in inventory.", "#666666");
        }
        else
        {
            for (int i = 0; i < _stagedEquipmentInv.Count; i++)
            {
                var eq = _stagedEquipmentInv[i];
                bool canInstall = CanInstallEquipment(eq);
                bool isSelected = _selectedEquipmentInvIndex == i;

                var panel = new Border
                {
                    BorderBrush = new SolidColorBrush(isSelected ? Color.FromRgb(0, 120, 180) : Color.FromRgb(0, 30, 60)),
                    BorderThickness = new Thickness(isSelected ? 2 : 1),
                    Background = new SolidColorBrush(canInstall ? Color.FromRgb(0, 8, 20) : Color.FromRgb(15, 15, 15)),
                    Padding = new Thickness(4),
                    Margin = new Thickness(0, 0, 0, 2)
                };

                var stack = new StackPanel { Orientation = Orientation.Horizontal };

                string catTag = eq.Category == "Slot" ? $"({eq.HardpointSize![0]})" : $"({eq.Category})";

                stack.Children.Add(new TextBlock
                {
                    Text = $"{eq.Name} {catTag} {eq.SpaceCost}sp {eq.Description ?? eq.Effect}",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(canInstall ? Color.FromRgb(80, 180, 255) : Color.FromRgb(80, 80, 80)),
                    FontFamily = new FontFamily("Consolas"),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                });

                if (canInstall)
                {
                    if (eq.Category == "Slot")
                    {
                        // Slot equipment needs to be selected then placed on hardpoint
                        var selectBtn = new Button
                        {
                            Content = isSelected ? "SELECTED" : "SELECT",
                            Tag = i,
                            Height = 22,
                            Padding = new Thickness(6, 0, 6, 0),
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 8, FontWeight = FontWeights.Bold,
                            Background = new SolidColorBrush(isSelected ? Color.FromRgb(0, 40, 60) : Color.FromRgb(0, 15, 35)),
                            Foreground = new SolidColorBrush(isSelected ? Color.FromRgb(0, 200, 255) : Color.FromRgb(80, 180, 255)),
                            BorderBrush = new SolidColorBrush(isSelected ? Color.FromRgb(0, 140, 200) : Color.FromRgb(0, 50, 100)),
                            BorderThickness = new Thickness(1)
                        };
                        selectBtn.Click += SelectEquipmentInv_Click;
                        stack.Children.Add(selectBtn);
                    }
                    else
                    {
                        // Passive/Active equipment installs directly (no slot needed)
                        var installBtn = new Button
                        {
                            Content = "INSTALL",
                            Tag = i,
                            Height = 22,
                            Padding = new Thickness(6, 0, 6, 0),
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 8, FontWeight = FontWeights.Bold,
                            Background = new SolidColorBrush(Color.FromRgb(0, 15, 35)),
                            Foreground = new SolidColorBrush(Color.FromRgb(80, 180, 255)),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 50, 100)),
                            BorderThickness = new Thickness(1)
                        };
                        installBtn.Click += InstallEquipment_Click;
                        stack.Children.Add(installBtn);
                    }
                }
                else
                {
                    string reason = GetCannotInstallEquipmentReason(eq);
                    stack.Children.Add(new TextBlock
                    {
                        Text = reason,
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Color.FromRgb(120, 60, 60)),
                        FontFamily = new FontFamily("Consolas"),
                        VerticalAlignment = VerticalAlignment.Center
                    });
                }

                panel.Child = stack;
                InventoryPanel.Children.Add(panel);
            }
        }

        if (_selectedEquipmentInvIndex.HasValue)
        {
            var eq = _stagedEquipmentInv[_selectedEquipmentInvIndex.Value];
            string sizeChar = eq.HardpointSize?[0].ToString() ?? "?";
            AddInventoryText($"\nClick an empty [{sizeChar}] slot on the mech to install.", "#5599DD");
        }
    }

    private void AddInventoryText(string text, string hexColor)
    {
        InventoryPanel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 9,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hexColor)),
            FontFamily = new FontFamily("Consolas"),
            Margin = new Thickness(0, 0, 0, 2)
        });
    }

    #endregion

    #region Equip / Remove Logic

    private bool CanEquip(StagedInventory item)
    {
        return HasFreeSlot(item.HardpointSize) && HasSpace(item.SpaceCost);
    }

    private string GetCannotEquipReason(StagedInventory item)
    {
        if (!HasFreeSlot(item.HardpointSize)) return $"(no {item.HardpointSize[0]} slot)";
        if (!HasSpace(item.SpaceCost)) return "(no space)";
        return "";
    }

    private bool HasFreeSlot(string size)
    {
        int totalSlots = _hardpointDefs.Count(h => h.Size == size);
        int usedByWeapons = _stagedEquipped.Count(w => w.HardpointSize == size);
        int usedByEquipment = _stagedEquipment.Count(e => e.HardpointSize == size);
        return (usedByWeapons + usedByEquipment) < totalSlots;
    }

    private bool HasSpace(int spaceCost)
    {
        int used = _stagedEquipped.Sum(w => w.SpaceCost) + _stagedEquipment.Sum(e => e.SpaceCost);
        return used + spaceCost <= _chassis.TotalSpace;
    }

    private void TryEquipToSlot(int inventoryIndex, HardpointDef slot)
    {
        if (inventoryIndex < 0 || inventoryIndex >= _stagedInventory.Count) return;

        var item = _stagedInventory[inventoryIndex];

        // Must match slot size
        if (item.HardpointSize != slot.Size) return;
        if (!HasSpace(item.SpaceCost)) return;
        if (_stagedEquipped.Any(w => w.SlotDefIndex == slot.Index)) return;

        int weaponGroup = _stagedEquipped.Any() ? _stagedEquipped.Max(w => w.WeaponGroup) + 1 : 1;

        _stagedEquipped.Add(new StagedWeapon
        {
            OriginalLoadoutId = null,
            WeaponId = item.WeaponId,
            WeaponName = item.WeaponName,
            HardpointSize = item.HardpointSize,
            SpaceCost = item.SpaceCost,
            Damage = item.Damage,
            EnergyCost = item.EnergyCost,
            RangeClass = item.RangeClass,
            MountLocation = slot.Location,
            WeaponGroup = weaponGroup,
            HardpointSlot = slot.SlotName,
            SlotDefIndex = slot.Index
        });

        _stagedInventory.RemoveAt(inventoryIndex);
    }

    private void SelectInventory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int index)
        {
            _selectedInventoryIndex = _selectedInventoryIndex == index ? null : index;
            _selectedSlotIndex = null;
            RefreshAll();
        }
    }

    private void RemoveWeapon_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int index && index >= 0 && index < _stagedEquipped.Count)
        {
            var weapon = _stagedEquipped[index];

            _stagedInventory.Add(new StagedInventory
            {
                OriginalInventoryId = null,
                WeaponId = weapon.WeaponId,
                WeaponName = weapon.WeaponName,
                HardpointSize = weapon.HardpointSize,
                SpaceCost = weapon.SpaceCost,
                Damage = weapon.Damage,
                EnergyCost = weapon.EnergyCost,
                RangeClass = weapon.RangeClass
            });

            _stagedEquipped.RemoveAt(index);
            _selectedSlotIndex = null;
            RefreshAll();
        }
    }

    #endregion

    #region Equipment Equip / Remove Logic

    private bool CanInstallEquipment(StagedEquipmentInv eq)
    {
        if (!HasSpace(eq.SpaceCost)) return false;
        if (eq.Category == "Slot" && eq.HardpointSize != null)
            return HasFreeSlot(eq.HardpointSize);
        return true; // Passive/Active just need space
    }

    private string GetCannotInstallEquipmentReason(StagedEquipmentInv eq)
    {
        if (!HasSpace(eq.SpaceCost)) return "(no space)";
        if (eq.Category == "Slot" && eq.HardpointSize != null && !HasFreeSlot(eq.HardpointSize))
            return $"(no {eq.HardpointSize[0]} slot)";
        return "";
    }

    private void InstallEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int index && index >= 0 && index < _stagedEquipmentInv.Count)
        {
            var eq = _stagedEquipmentInv[index];
            if (!HasSpace(eq.SpaceCost)) return;

            _stagedEquipment.Add(new StagedEquipment
            {
                OriginalLoadoutId = null,
                EquipmentId = eq.EquipmentId,
                Name = eq.Name,
                Category = eq.Category,
                HardpointSize = eq.HardpointSize,
                SpaceCost = eq.SpaceCost,
                EnergyCost = eq.EnergyCost,
                Effect = eq.Effect,
                Description = eq.Description,
                HardpointSlot = null,
                SlotDefIndex = -1
            });

            _stagedEquipmentInv.RemoveAt(index);
            _selectedEquipmentInvIndex = null;
            RefreshAll();
        }
    }

    private void SelectEquipmentInv_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int index)
        {
            _selectedEquipmentInvIndex = _selectedEquipmentInvIndex == index ? null : index;
            _selectedInventoryIndex = null;
            _selectedSlotIndex = null;
            RefreshAll();
        }
    }

    private void TryInstallEquipmentToSlot(int eqInvIndex, HardpointDef slot)
    {
        if (eqInvIndex < 0 || eqInvIndex >= _stagedEquipmentInv.Count) return;

        var eq = _stagedEquipmentInv[eqInvIndex];
        if (eq.HardpointSize != slot.Size) return;
        if (!HasSpace(eq.SpaceCost)) return;
        if (_stagedEquipped.Any(w => w.SlotDefIndex == slot.Index)) return;
        if (_stagedEquipment.Any(e => e.SlotDefIndex == slot.Index)) return;

        _stagedEquipment.Add(new StagedEquipment
        {
            OriginalLoadoutId = null,
            EquipmentId = eq.EquipmentId,
            Name = eq.Name,
            Category = eq.Category,
            HardpointSize = eq.HardpointSize,
            SpaceCost = eq.SpaceCost,
            EnergyCost = eq.EnergyCost,
            Effect = eq.Effect,
            Description = eq.Description,
            HardpointSlot = slot.SlotName,
            SlotDefIndex = slot.Index
        });

        _stagedEquipmentInv.RemoveAt(eqInvIndex);
    }

    private void RemoveEquipment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int index && index >= 0 && index < _stagedEquipment.Count)
        {
            var eq = _stagedEquipment[index];

            _stagedEquipmentInv.Add(new StagedEquipmentInv
            {
                OriginalInventoryId = null,
                EquipmentId = eq.EquipmentId,
                Name = eq.Name,
                Category = eq.Category,
                HardpointSize = eq.HardpointSize,
                SpaceCost = eq.SpaceCost,
                EnergyCost = eq.EnergyCost,
                Effect = eq.Effect,
                Description = eq.Description
            });

            _stagedEquipment.RemoveAt(index);
            _selectedSlotIndex = null;
            RefreshAll();
        }
    }

    #endregion

    #region Cost Calculation — Slot-by-Slot Diff

    private int CountChanges()
    {
        // Build original: slotName → weaponId
        var originalBySlot = new Dictionary<string, int>();
        var usedSlots = new HashSet<int>();

        foreach (var l in _originalLoadout)
        {
            if (l.Weapon == null) continue;

            var slot = _hardpointDefs.FirstOrDefault(h =>
                h.SlotName == l.HardpointSlot && h.Size == l.Weapon.HardpointSize
                && !usedSlots.Contains(h.Index));

            slot ??= _hardpointDefs.FirstOrDefault(h =>
                h.Location == l.MountLocation && h.Size == l.Weapon.HardpointSize
                && !usedSlots.Contains(h.Index));

            slot ??= _hardpointDefs.FirstOrDefault(h =>
                h.Size == l.Weapon.HardpointSize
                && !usedSlots.Contains(h.Index));

            if (slot != null)
            {
                originalBySlot[slot.SlotName] = l.WeaponId;
                usedSlots.Add(slot.Index);
            }
        }

        // Build current: slotName → weaponId
        var currentBySlot = new Dictionary<string, int>();
        foreach (var w in _stagedEquipped)
        {
            if (w.SlotDefIndex >= 0)
            {
                var slot = _hardpointDefs.FirstOrDefault(h => h.Index == w.SlotDefIndex);
                if (slot != null) currentBySlot[slot.SlotName] = w.WeaponId;
            }
        }

        // Count slots that differ between original and current
        int changes = 0;
        foreach (var slotName in _hardpointDefs.Select(h => h.SlotName))
        {
            bool hadOrig = originalBySlot.TryGetValue(slotName, out int origId);
            bool hasCur = currentBySlot.TryGetValue(slotName, out int curId);

            if (hadOrig && hasCur && origId == curId) continue; // same weapon, no change
            if (!hadOrig && !hasCur) continue;                   // both empty, no change
            changes++;
        }

        // Count equipment changes
        var origEqIds = _originalEquipmentLoadout
            .Select(e => e.EquipmentId).OrderBy(id => id).ToList();
        var curEqIds = _stagedEquipment
            .Select(e => e.EquipmentId).OrderBy(id => id).ToList();

        if (!origEqIds.SequenceEqual(curEqIds))
        {
            // Count individual adds/removes
            var origSet = new List<int>(origEqIds);
            var curSet = new List<int>(curEqIds);
            foreach (var id in origEqIds)
            {
                if (curSet.Remove(id))
                    origSet.Remove(id);
            }
            changes += origSet.Count + curSet.Count;
        }

        return changes;
    }

    private void UpdateCostDisplay()
    {
        int changes = CountChanges();
        int cost = changes * CostPerChange;
        int time = changes * DaysPerChange;

        CostDisplayText.Text = $"Changes: {changes} | Cost: {cost:N0} Credits | Time: {time} day{(time != 1 ? "s" : "")}";
        ConfirmButton.IsEnabled = changes > 0;
        ConfirmButton.Opacity = changes > 0 ? 1.0 : 0.5;
    }

    #endregion

    #region Buttons

    private void ResetButton_Click(object sender, RoutedEventArgs e) => ResetToOriginal();

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        int changes = CountChanges();
        int cost = changes * CostPerChange;
        int time = changes * DaysPerChange;

        var state = _management.GetPlayerState();
        if (state == null || state.Credits < cost)
        {
            MessageBox.Show($"Insufficient credits! Need {cost:N0}, have {state?.Credits ?? 0:N0}.",
                "Refit Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 1. Unequip all original weapons → inventory
        foreach (var orig in _originalLoadout)
        {
            _management.UnequipToInventory(_instanceId, orig.LoadoutId);
        }

        // 2. Get fresh inventory state
        var freshInventory = _management.GetInventory();

        // 3. Equip staged weapons from inventory
        foreach (var staged in _stagedEquipped)
        {
            var invItem = freshInventory.FirstOrDefault(i => i.WeaponId == staged.WeaponId);
            if (invItem != null)
            {
                _management.EquipFromInventory(_instanceId, invItem.InventoryId,
                    staged.HardpointSlot, staged.WeaponGroup, staged.MountLocation);
                freshInventory.Remove(invItem);
            }
        }

        // 3b. Save equipment loadout
        var newEquipmentLoadout = _stagedEquipment.Select(eq => new EquipmentLoadout
        {
            InstanceId = _instanceId,
            EquipmentId = eq.EquipmentId,
            HardpointSlot = eq.HardpointSlot
        }).ToList();
        _management.RefitEquipment(_instanceId, newEquipmentLoadout);

        // 3c. Update equipment inventory: remove all, then re-add staged
        foreach (var orig in _originalEquipmentLoadout)
        {
            _management.AddEquipmentToInventory(orig.EquipmentId);
        }
        var freshEqInv = _management.GetEquipmentInventory();
        foreach (var staged in _stagedEquipment)
        {
            var invItem = freshEqInv.FirstOrDefault(i => i.EquipmentId == staged.EquipmentId);
            if (invItem != null)
            {
                _management.RemoveEquipmentFromInventory(invItem.EquipmentInventoryId);
                freshEqInv.Remove(invItem);
            }
        }

        // 4. Deduct cost and add refit time
        state.Credits -= cost;
        _management.SavePlayerState(state);

        if (time > 0)
        {
            _frame.RepairTime = Math.Max(_frame.RepairTime, time);
        }

        RefitApplied = true;
        MessageBox.Show($"Refit complete!\nCost: {cost:N0} credits\nTime: {time} day{(time != 1 ? "s" : "")}",
            "Refit Complete", MessageBoxButton.OK);
        Close();
    }

    #endregion
}
