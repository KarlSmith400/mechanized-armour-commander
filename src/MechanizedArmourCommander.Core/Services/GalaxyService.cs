using MechanizedArmourCommander.Data;
using MechanizedArmourCommander.Data.Models;
using MechanizedArmourCommander.Data.Repositories;

namespace MechanizedArmourCommander.Core.Services;

/// <summary>
/// Manages galaxy travel, fuel, and location queries
/// </summary>
public class GalaxyService
{
    private readonly DatabaseContext _dbContext;
    private readonly StarSystemRepository _systemRepo;
    private readonly PlanetRepository _planetRepo;
    private readonly JumpRouteRepository _jumpRouteRepo;
    private readonly PlayerStateRepository _stateRepo;

    public const int MaxFuel = 100;
    public const int FuelPricePerUnit = 500;
    public const int IntraSystemFuelCost = 5;
    public const int IntraSystemTravelDays = 1;

    public GalaxyService(DatabaseContext dbContext)
    {
        _dbContext = dbContext;
        _systemRepo = new StarSystemRepository(dbContext);
        _planetRepo = new PlanetRepository(dbContext);
        _jumpRouteRepo = new JumpRouteRepository(dbContext);
        _stateRepo = new PlayerStateRepository(dbContext);
    }

    // === Location Queries ===

    public StarSystem? GetCurrentSystem()
    {
        var state = _stateRepo.Get();
        if (state == null) return null;
        return _systemRepo.GetById(state.CurrentSystemId);
    }

    public Planet? GetCurrentPlanet()
    {
        var state = _stateRepo.Get();
        if (state == null) return null;
        return _planetRepo.GetById(state.CurrentPlanetId);
    }

    public List<Planet> GetSystemPlanets(int systemId)
    {
        return _planetRepo.GetBySystem(systemId);
    }

    public List<StarSystem> GetAllSystems()
    {
        return _systemRepo.GetAll();
    }

    public List<JumpRoute> GetAllRoutes()
    {
        return _jumpRouteRepo.GetAll();
    }

    /// <summary>
    /// Returns available jump routes from the current system, with destination info
    /// </summary>
    public List<(JumpRoute Route, StarSystem Destination)> GetAvailableJumps()
    {
        var state = _stateRepo.Get();
        if (state == null) return new();

        var routes = _jumpRouteRepo.GetBySystem(state.CurrentSystemId);
        var jumps = new List<(JumpRoute, StarSystem)>();

        foreach (var route in routes)
        {
            int destinationId = route.FromSystemId == state.CurrentSystemId
                ? route.ToSystemId
                : route.FromSystemId;

            var dest = _systemRepo.GetById(destinationId);
            if (dest != null)
                jumps.Add((route, dest));
        }

        return jumps;
    }

    // === Travel ===

    /// <summary>
    /// Travel to another planet within the same system (5 fuel, 1 day)
    /// </summary>
    public TravelResult TravelToPlanet(int planetId, ManagementService management)
    {
        var state = _stateRepo.Get();
        if (state == null) return new TravelResult { Success = false, Message = "No player state." };

        var planet = _planetRepo.GetById(planetId);
        if (planet == null) return new TravelResult { Success = false, Message = "Planet not found." };
        if (planet.SystemId != state.CurrentSystemId)
            return new TravelResult { Success = false, Message = "Planet is in another system. Use a jump route." };
        if (planet.PlanetId == state.CurrentPlanetId)
            return new TravelResult { Success = false, Message = "Already at this location." };

        if (state.Fuel < IntraSystemFuelCost)
            return new TravelResult { Success = false, Message = $"Insufficient fuel. Need {IntraSystemFuelCost}, have {state.Fuel}." };

        state.Fuel -= IntraSystemFuelCost;
        state.CurrentPlanetId = planetId;
        _stateRepo.Update(state);

        // Advance 1 day
        var dayReport = management.AdvanceDay();

        return new TravelResult
        {
            Success = true,
            Message = $"Arrived at {planet.Name}. ({IntraSystemFuelCost} fuel, {IntraSystemTravelDays} day)",
            DaysElapsed = IntraSystemTravelDays,
            FuelSpent = IntraSystemFuelCost,
            DayReports = new List<DayReport> { dayReport }
        };
    }

    /// <summary>
    /// Jump to another star system via a route (variable fuel and days)
    /// </summary>
    public TravelResult JumpToSystem(int routeId, int targetPlanetId, ManagementService management)
    {
        var state = _stateRepo.Get();
        if (state == null) return new TravelResult { Success = false, Message = "No player state." };

        var routes = _jumpRouteRepo.GetBySystem(state.CurrentSystemId);
        var route = routes.FirstOrDefault(r => r.RouteId == routeId);
        if (route == null) return new TravelResult { Success = false, Message = "Invalid jump route." };

        int destinationSystemId = route.FromSystemId == state.CurrentSystemId
            ? route.ToSystemId
            : route.FromSystemId;

        var targetPlanet = _planetRepo.GetById(targetPlanetId);
        if (targetPlanet == null || targetPlanet.SystemId != destinationSystemId)
            return new TravelResult { Success = false, Message = "Invalid destination planet." };

        if (state.Fuel < route.Distance)
            return new TravelResult { Success = false, Message = $"Insufficient fuel. Need {route.Distance}, have {state.Fuel}." };

        var destSystem = _systemRepo.GetById(destinationSystemId);

        state.Fuel -= route.Distance;
        state.CurrentSystemId = destinationSystemId;
        state.CurrentPlanetId = targetPlanetId;
        _stateRepo.Update(state);

        // Advance days for travel
        var dayReports = new List<DayReport>();
        for (int i = 0; i < route.TravelDays; i++)
        {
            dayReports.Add(management.AdvanceDay());
        }

        return new TravelResult
        {
            Success = true,
            Message = $"Jumped to {destSystem?.Name ?? "Unknown"} — arrived at {targetPlanet.Name}. ({route.Distance} fuel, {route.TravelDays} days)",
            DaysElapsed = route.TravelDays,
            FuelSpent = route.Distance,
            DayReports = dayReports
        };
    }

    // === Fuel ===

    public bool PurchaseFuel(int amount)
    {
        var state = _stateRepo.Get();
        if (state == null) return false;

        int actualAmount = Math.Min(amount, MaxFuel - state.Fuel);
        if (actualAmount <= 0) return false;

        int cost = actualAmount * FuelPricePerUnit;
        if (state.Credits < cost) return false;

        state.Credits -= cost;
        state.Fuel += actualAmount;
        _stateRepo.Update(state);
        return true;
    }

    public int GetFuelCapacity() => MaxFuel;
}

/// <summary>
/// Result of a travel action (intra-system or jump)
/// </summary>
public class TravelResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DaysElapsed { get; set; }
    public int FuelSpent { get; set; }
    public List<DayReport> DayReports { get; set; } = new();
}
