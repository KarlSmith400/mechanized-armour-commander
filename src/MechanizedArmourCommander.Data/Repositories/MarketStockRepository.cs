using Microsoft.Data.Sqlite;
using MechanizedArmourCommander.Data.Models;

namespace MechanizedArmourCommander.Data.Repositories;

public class MarketStockRepository
{
    private readonly DatabaseContext _context;

    public MarketStockRepository(DatabaseContext context)
    {
        _context = context;
    }

    public List<MarketStock> GetByPlanet(int planetId)
    {
        var items = new List<MarketStock>();
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT MarketStockId, PlanetId, ItemType, ItemId, Quantity, GeneratedOnDay
            FROM MarketStock
            WHERE PlanetId = @planetId
            ORDER BY ItemType, ItemId";
        command.Parameters.AddWithValue("@planetId", planetId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new MarketStock
            {
                MarketStockId = reader.GetInt32(reader.GetOrdinal("MarketStockId")),
                PlanetId = reader.GetInt32(reader.GetOrdinal("PlanetId")),
                ItemType = reader.GetString(reader.GetOrdinal("ItemType")),
                ItemId = reader.GetInt32(reader.GetOrdinal("ItemId")),
                Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                GeneratedOnDay = reader.GetInt32(reader.GetOrdinal("GeneratedOnDay"))
            });
        }

        return items;
    }

    public int GetGenerationDay(int planetId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT MIN(GeneratedOnDay) FROM MarketStock WHERE PlanetId = @planetId";
        command.Parameters.AddWithValue("@planetId", planetId);

        var result = command.ExecuteScalar();
        return result is DBNull || result == null ? 0 : Convert.ToInt32(result);
    }

    public void DeleteByPlanet(int planetId)
    {
        var connection = _context.GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM MarketStock WHERE PlanetId = @planetId";
        command.Parameters.AddWithValue("@planetId", planetId);
        command.ExecuteNonQuery();
    }

    public void InsertBatch(List<MarketStock> items)
    {
        var connection = _context.GetConnection();

        foreach (var item in items)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO MarketStock (PlanetId, ItemType, ItemId, Quantity, GeneratedOnDay)
                VALUES (@planetId, @itemType, @itemId, @quantity, @generatedOnDay)";
            command.Parameters.AddWithValue("@planetId", item.PlanetId);
            command.Parameters.AddWithValue("@itemType", item.ItemType);
            command.Parameters.AddWithValue("@itemId", item.ItemId);
            command.Parameters.AddWithValue("@quantity", item.Quantity);
            command.Parameters.AddWithValue("@generatedOnDay", item.GeneratedOnDay);
            command.ExecuteNonQuery();
        }
    }

    public bool DecrementQuantity(int marketStockId)
    {
        var connection = _context.GetConnection();

        using var updateCmd = connection.CreateCommand();
        updateCmd.CommandText = @"
            UPDATE MarketStock SET Quantity = Quantity - 1
            WHERE MarketStockId = @id AND Quantity > 0";
        updateCmd.Parameters.AddWithValue("@id", marketStockId);
        int rows = updateCmd.ExecuteNonQuery();

        if (rows > 0)
        {
            using var cleanupCmd = connection.CreateCommand();
            cleanupCmd.CommandText = "DELETE FROM MarketStock WHERE MarketStockId = @id AND Quantity <= 0";
            cleanupCmd.Parameters.AddWithValue("@id", marketStockId);
            cleanupCmd.ExecuteNonQuery();
        }

        return rows > 0;
    }
}
