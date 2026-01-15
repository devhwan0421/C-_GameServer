using System;
using Dapper;
using MySqlConnector;
using System.Data;
using System.Collections.Generic;
using System.Linq;

public class DbManager
{
    public static string _connectionString { get; } = "***REMOVED***";

    public static LoginDto LoginRequest(IDbConnection db, string username, string password)
    {
        return db.QueryFirstOrDefault<LoginDto>(
            "SELECT id FROM users WHERE username = @Username AND password = @Password",
            new { Username = username, Password = password }
        );
    }

    public static List<CharacterDto> GetCharacterListByUserId(IDbConnection db, int userId)
    {
        return db.Query<CharacterDto>(
            "SELECT * FROM characters WHERE user_id = @UserId",
            new { UserId = userId }
        ).ToList();
    }

    public static CharacterDto GetCharacterByCharacterId(IDbConnection db, int characterId)
    {
        return db.QueryFirstOrDefault<CharacterDto>(
            "SELECT * FROM characters WHERE id = @CharacterId",
            new { CharacterId = characterId }
        );
    }
}