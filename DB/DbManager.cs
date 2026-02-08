using System;
using Dapper;
using MySqlConnector;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DbManager
{
    //설정 파일 불러오는 것으로 빼기. git 업로드시 public에서 다 보임
    public static string _connectionString { get; } = "***REMOVED***";

    //리스트 조회
    public static Task<List<T>> Find<T>(string sql, object param)
    {
        return DbTransactionWorker.Instance.PushQuery(db =>
            db.Query<T>(sql, param).ToList()
        );
    }

    //단일 조회
    private static Task<T> FindOne<T>(string sql, object param)
    {
        return DbTransactionWorker.Instance.PushQuery(db =>
            db.QueryFirstOrDefault<T>(sql, param)
        );
    }

    //update, delete, insert 분리하면 좋을 듯
    private static Task<int> Excute(string sql, object param)
    {
        return DbTransactionWorker.Instance.PushQuery(db =>
            db.Execute(sql, param)
        );
    }
    
    private static Task<T> ExecuteScalar<T>(string sql, object param)
    {
        return DbTransactionWorker.Instance.PushQuery(db =>
            db.ExecuteScalar<T>(sql, param)
        );
    }

    public static Task<LoginDto> LoginRequest(string username, string password)
        => FindOne<LoginDto>("SELECT id FROM users WHERE username = @Username AND password = @Password",
            new { Username = username, Password = password });

    /*public static LoginDto LoginRequest(IDbConnection db, string username, string password)
    {
        return db.QueryFirstOrDefault<LoginDto>(
            "SELECT id FROM users WHERE username = @Username AND password = @Password",
            new { Username = username, Password = password }
        );
    }*/

    public static Task<List<CharacterDto>> GetCharacterListByUserId(int userId)
        => Find<CharacterDto>("SELECT * FROM characters WHERE user_id = @UserId",
            new { UserId = userId });

    /*public static List<CharacterDto> GetCharacterListByUserId(IDbConnection db, int userId)
    {
        return db.Query<CharacterDto>(
            "SELECT * FROM characters WHERE user_id = @UserId",
            new { UserId = userId }
        ).ToList();
    }*/

    public static Task<CharacterDto> GetCharacterByCharacterId(int characterId)
        => FindOne<CharacterDto>("SELECT * FROM characters WHERE id = @CharacterId",
            new { CharacterId = characterId });

    /*public static CharacterDto GetCharacterByCharacterId(IDbConnection db, int characterId)
    {
        return db.QueryFirstOrDefault<CharacterDto>(
            "SELECT * FROM characters WHERE id = @CharacterId",
            new { CharacterId = characterId }
        );
    }*/

    public static Task<List<InventoryDto>> GetInventoryByOwnerId(int ownerId)
        => Find<InventoryDto>("SELECT * FROM inventory WHERE owner_id = @OwnerId AND count > 0",
            new { OwnerId = ownerId });

    /*public static List<InventoryDto> GetInventoryByOwnerId(IDbConnection db, int ownerId)
    {
        return db.Query<InventoryDto>(
            "SELECT * FROM inventory WHERE owner_id = @OwnerId",
            new { OwnerId = ownerId }
        ).ToList();
    }*/

    public static Task<int> DecreaseIntemCount(int ownerId, int inventoryId, int decreaseCount)
        => Excute("UPDATE inventory SET count = count - @DecreaseCount WHERE owner_id = @OwnerId AND id = @InventoryId",
            new { OwnerId = ownerId, InventoryId = inventoryId, DecreaseCount = decreaseCount });

    public static Task<int> InsertItem(InventoryDto inventoryDto)
        => ExecuteScalar<int>(@"INSERT INTO inventory (owner_id, item_id, count, is_equipped, enhancement)
            VALUES (@OwnerId, @ItemId, @Count, @IsEquipped, @Enhancement);
            SELECT LAST_INSERT_ID();",
            new
            {
                OwnerId = inventoryDto.owner_id,
                ItemId = inventoryDto.item_id,
                Count = inventoryDto.count,
                IsEquipped = inventoryDto.is_equipped,
                Enhancement = inventoryDto.enhancement
            });

    public static Task<int> ItemOwnerUpdate(int inventoryId, int ownerId)
        => Excute("UPDATE inventory SET owner_id = @OwnerId WHERE id = @InventoryId",
            new { OwnerId = ownerId, InventoryId = inventoryId });
}