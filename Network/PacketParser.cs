using Serilog;
using System;
using System.Collections.Generic;
using System.Text.Json;

public static class PacketParser
{
    private static readonly Dictionary<PacketID, Func<string, IPacket>> _parsers = new Dictionary<PacketID, Func<string, IPacket>>();

    static PacketParser()
    {
        _parsers.Add(PacketID.LoginRequest, (json) => JsonSerializer.Deserialize<LoginRequest>(json));
        _parsers.Add(PacketID.EnterWorldRequest, (json) => JsonSerializer.Deserialize<EnterWorldRequest>(json));
        _parsers.Add(PacketID.PlayerMoveRequest, (json) => JsonSerializer.Deserialize<PlayerMoveRequest>(json));
        _parsers.Add(PacketID.UseItemRequest, (json) => JsonSerializer.Deserialize<UseItemRequest>(json));
        _parsers.Add(PacketID.DropItemRequest, (json) => JsonSerializer.Deserialize<DropItemRequest>(json));
        _parsers.Add(PacketID.MoveMapRequest, (json) => JsonSerializer.Deserialize<MoveMapRequest>(json));
        _parsers.Add(PacketID.DropItemDestroyResponse, (json) => JsonSerializer.Deserialize<DropItemDestroyResponse>(json));
        _parsers.Add(PacketID.PickUpItemRequest, (json) => JsonSerializer.Deserialize<PickUpItemRequest>(json));
        _parsers.Add(PacketID.PlayerTakeDamageRequest, (json) => JsonSerializer.Deserialize<PlayerTakeDamageRequest>(json));
        _parsers.Add(PacketID.PlayerHitRequest, (json) => JsonSerializer.Deserialize<PlayerHitRequest>(json));
        _parsers.Add(PacketID.NpcTalkRequest, (json) => JsonSerializer.Deserialize<NpcTalkRequest>(json));
    }

    public static IPacket Parse(PacketID id, string json)
    {
        if (_parsers.TryGetValue(id, out var parser))
        {
            try
            {
                return parser.Invoke(json);
            }
            catch (Exception ex)
            {
                Log.Error($"[PacketParser] Parsing Error: {id} | {ex.Message}");
            }
        }
        return null;
    }
}