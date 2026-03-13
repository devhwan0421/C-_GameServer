using Dapper;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class DbManager
{
    public static string _connectionString { get; } = File.ReadAllText("db.config");

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

    public static Task<List<QuestDto>> GetQuestByCharacterId(int characterId)
        => Find<QuestDto>("SELECT * FROM quest WHERE character_id = @CharacterId",
            new { CharacterId = characterId });

    public static Task<List<QuestProgressDto>> GetQuestProgressByCharacterId(int characterId)
        => Find<QuestProgressDto>("SELECT * FROM questProgress WHERE character_id = @CharacterId",
            new { CharacterId = characterId });

    public static Task<int> DecreaseIntemCount(int ownerId, int inventoryId, int decreaseCount)
        => Excute("UPDATE inventory SET count = count - @DecreaseCount WHERE owner_id = @OwnerId AND id = @InventoryId",
            new { OwnerId = ownerId, InventoryId = inventoryId, DecreaseCount = decreaseCount });

    public static Task<int> InsertItem(InventoryDto inventoryDto)
        => ExecuteScalar<int>(@"INSERT INTO inventory (owner_id, item_id, count, is_equipped, enhancement)
            VALUES (@owner_id, @item_id, @count, @is_equipped, @enhancement);
            SELECT LAST_INSERT_ID();", inventoryDto);
    /*public static Task<int> InsertItem(Item item)
        => ExecuteScalar<int>(@"INSERT INTO inventory (owner_id, item_id, count, is_equipped, enhancement)
            VALUES (@OwnerId, @ItemId, @Count, @IsEquipped, @Enhancement);
            SELECT LAST_INSERT_ID();",
            new
            {
                item.OwnerId,
                item.ItemId,
                item.Count,
                IsEquipped = item.IsEquipped ? 1 : 0,
                item.Enhancement
            });*/
    /*public static Task<int> InsertItem(InventoryDto inventoryDto)
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
            });*/
    /*public class InventoryDto
    {
        public int id { get; set; }
        public int owner_id { get; set; }
        public int item_id { get; set; }
        public int count { get; set; }
        public int is_equipped { get; set; }
        public int enhancement { get; set; }
    }*/

    public static Task<int> ItemOwnerUpdate(int inventoryId, int ownerId)
        => Excute("UPDATE inventory SET owner_id = @OwnerId WHERE id = @InventoryId",
            new { OwnerId = ownerId, InventoryId = inventoryId });

    public static Task<bool> CompleteQuest(QuestDtoSet questDtoSet)
    {
        var quest = questDtoSet.Quests.FirstOrDefault();
        if (quest == null) return Task.FromResult(false);

        return DbTransactionWorker.Instance.PushQuery(db =>
        {
            using (var transaction = db.BeginTransaction())
            {
                try
                {
                    string questUpdateSql = @"INSERT INTO quest (quest_id, character_id, state)
                                              VALUES (@quest_id, @character_id, @state)
                                              ON DUPLICATE KEY UPDATE state = VALUES(state)";
                    db.Execute(questUpdateSql, quest, transaction);

                    string questProgressDeleteSql = @"DELETE FROM questProgress
                                                      WHERE character_id = @character_id AND quest_id = @quest_id";
                    db.Execute(questProgressDeleteSql, quest, transaction);

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error($"[DB] 퀘스트 완료 처리 실패: character_id {quest.character_id}, quest_id {quest.quest_id}, Error: {ex.Message}");
                    return false;
                }
            }
        });
    }

    //public static Task<bool> SavePlayerData(CharacterDto characterDto, List<InventoryDto> items, List<QuestDto> quests, List<QuestProgressDto> questProgresses)
    public static Task<bool> SavePlayerData(CharacterDto characterDto, List<InventoryDto> items, QuestDtoSet questDtoSet)
    {
        return DbTransactionWorker.Instance.PushQuery(db =>
        {
            using (var transaction = db.BeginTransaction())
            {
                try
                {
                    if (characterDto != null)
                    {
                        string characterSql = @"UPDATE characters SET
                                                level=@level, exp=@exp, map=@map, pos_x=@pos_x,
                                                pos_y=@pos_y, pos_z=@pos_z, hp=@hp, max_hp=@max_hp, damage=@damage
                                                WHERE id=@id";
                        db.Execute(characterSql, characterDto, transaction);
                    }

                    if (items != null && items.Count > 0)
                    {
                        string itemSql = @"UPDATE inventory SET
                                           owner_id=@owner_id, count=@count, is_equipped=@is_equipped, enhancement=@enhancement
                                           WHERE id=@id";
                        db.Execute(itemSql, items, transaction);
                    }

                    if (questDtoSet != null)
                    {
                        if (questDtoSet.Quests != null && questDtoSet.Quests.Count > 0)
                        {
                            string questSql = @"INSERT INTO quest (quest_id, character_id, state)
                                              VALUES (@quest_id, @character_id, @state)
                                              ON DUPLICATE KEY UPDATE state = VALUES(state)";
                            db.Execute(questSql, questDtoSet.Quests, transaction);
                        }

                        if (questDtoSet.QuestProgresses != null && questDtoSet.QuestProgresses.Count > 0)
                        {
                            string questProgressSql = @"INSERT INTO questProgress (character_id, quest_id, monster_id, current_count)
                                                    VALUES (@character_id, @quest_id, @monster_id, @current_count)
                                                    ON DUPLICATE KEY UPDATE current_count = VALUES(current_count)";
                            db.Execute(questProgressSql, questDtoSet.QuestProgresses, transaction);
                        }
                    }

                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error($"[DB ERROR] 데이터 저장 실패 {ex.Message}");
                    return false;
                }
            }
        });
    }

    public static Task<List<Item>> InsertItemList(List<InventoryDto> inventoryDtos)
    {
        return DbTransactionWorker.Instance.PushQuery(db =>
        {
            using (var transaction = db.BeginTransaction())
            {
                try
                {
                    List<Item> items = new List<Item>();
                    if (inventoryDtos != null && inventoryDtos.Count > 0)
                    {
                        string itemSql = @"INSERT INTO inventory (owner_id, item_id, count, is_equipped, enhancement)
                                           VALUES (@owner_id, @item_id, @count, @is_equipped, @enhancement);
                                           SELECT LAST_INSERT_ID()";
                        foreach (var inventoryDto in inventoryDtos)
                        {
                            int newInventoryId = db.ExecuteScalar<int>(itemSql, inventoryDto, transaction);
                            items.Add(new Item
                            {
                                InventoryId = newInventoryId,
                                OwnerId = inventoryDto.owner_id,
                                ItemId = inventoryDto.item_id,
                                Count = inventoryDto.count,
                                IsEquipped = inventoryDto.is_equipped == 1,
                                Enhancement = inventoryDto.enhancement
                            });
                        }
                    }

                    transaction.Commit();
                    return items;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Log.Error($"[DB ERROR] InsertItemList 실패 {ex.Message}");
                    return null;
                }
            }
        });
    }


}