using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SaiGame.Services;
using UnityEngine;

// Giao diện gọi hệ thống Quest của SaiServer, tách khỏi IBackendClient để backend giả lập của Auto Test không phải cài đặt
public interface IQuestBackend
{
    // Lấy quest hôm nay của mọi Daily Quest pool đang bật (trừ pool điểm danh)
    Task<List<QuestEntry>> GetDailyQuestsAsync();
    // Lấy quest của mọi chuỗi nhiệm vụ đang bật
    Task<List<QuestEntry>> GetChainQuestsAsync();
    // Bắt đầu quest nếu chưa bắt đầu rồi kiểm tra tiến độ, cập nhật status của quest
    Task CheckQuestAsync(QuestEntry quest);
    // Nhận thưởng quest đã hoàn thành rồi đồng bộ ví
    Task<DailyRewardItem[]> ClaimQuestAsync(QuestEntry quest);
}

// Phần BackendClient xử lý danh sách quest, kiểm tra tiến độ và nhận thưởng qua SaiServer SDK
public partial class BackendClient : IQuestBackend
{
    [Tooltip("Số chuỗi nhiệm vụ tối đa tải về một lần")]
    const int ChainLimit = 50;

    // Lấy quest hôm nay của mọi Daily Quest pool đang bật, bỏ qua pool điểm danh
    public async Task<List<QuestEntry>> GetDailyQuestsAsync()
    {
        var list = new List<QuestEntry>();
        if (!await EnsureAuthenticatedAsync()) return list;
        DailyQuest dailyQuest = SaiServer.Instance.DailyQuest;
        if (dailyQuest == null) return list;

        DailyQuestPoolsResponse pools = await CallSdk<DailyQuestPoolsResponse>((ok, fail) => dailyQuest.GetPools(ok, fail));
        if (pools?.pools == null) return list;

        foreach (DailyQuestPoolData pool in pools.pools)
        {
            if (pool == null || !pool.is_active || pool.pool_key == DailyLoginPoolKey) continue;
            TodayQuestResponse today = await CallSdk<TodayQuestResponse>((ok, fail) => dailyQuest.GetTodayQuests(pool.id, ok, fail));
            if (today?.entries == null) continue;

            foreach (DailyQuestEntryData entry in today.entries)
            {
                if (entry?.quest == null) continue;
                list.Add(new QuestEntry
                {
                    source = QuestSource.Daily,
                    questDefinitionId = entry.quest.id,
                    assignmentId = entry.assignment?.id ?? "",
                    group = pool.display_name,
                    title = entry.quest.name,
                    description = entry.quest.description,
                    status = entry.status ?? "",
                    sortOrder = entry.quest.sort_order,
                    rewards = ToRewardItems(entry.rewards)
                });
            }
        }
        return list;
    }

    // Lấy quest của mọi chuỗi nhiệm vụ đang bật, bỏ quest ẩn hoặc tắt
    public async Task<List<QuestEntry>> GetChainQuestsAsync()
    {
        var list = new List<QuestEntry>();
        if (!await EnsureAuthenticatedAsync()) return list;
        ChainQuest chainQuest = SaiServer.Instance.ChainQuest;
        if (chainQuest == null) return list;

        ChainQuestResponse chains = await CallSdk<ChainQuestResponse>((ok, fail) => chainQuest.GetChains(ChainLimit, 0, ok, fail));
        if (chains?.chains == null) return list;

        foreach (ChainQuestData chain in chains.chains)
        {
            if (chain == null || !chain.is_active) continue;
            ChainMembersResponse members = await CallSdk<ChainMembersResponse>((ok, fail) => chainQuest.GetChainMembers(chain.id, ok, fail));
            if (members?.members == null) continue;

            foreach (ChainMemberData member in members.members)
            {
                QuestDefinitionData def = member?.definition;
                if (def == null || !def.is_active || def.is_hidden) continue;
                list.Add(new QuestEntry
                {
                    source = QuestSource.Chain,
                    questDefinitionId = def.id,
                    assignmentId = "",
                    group = chain.display_name,
                    title = def.name,
                    description = def.description,
                    status = member.status ?? "",
                    sortOrder = member.sort_order,
                    rewards = ToRewardItems(def.rewards)
                });
            }
        }
        return list;
    }

    // Start (nếu chưa bắt đầu) rồi Check quest, ghi trạng thái mới vào quest
    public async Task CheckQuestAsync(QuestEntry quest)
    {
        QuestProgressor progressor = await GetProgressorAsync();
        bool daily = !string.IsNullOrEmpty(quest.assignmentId);
        string id = daily ? quest.assignmentId : quest.questDefinitionId;

        if (quest.status != "in_progress" && quest.status != "completed")
        {
            try
            {
                await (daily
                    ? CallSdk<StartQuestResponse>((ok, fail) => progressor.StartDailyQuestAssignment(id, ok, fail))
                    : CallSdk<StartQuestResponse>((ok, fail) => progressor.StartQuest(id, ok, fail)));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BackendClient] Quest start warning ({quest.title}): {e.Message}");
            }
        }

        CheckQuestResponse check = await (daily
            ? CallSdk<CheckQuestResponse>((ok, fail) => progressor.CheckDailyQuestAssignment(id, ok, fail))
            : CallSdk<CheckQuestResponse>((ok, fail) => progressor.CheckQuest(id, ok, fail)));
        string status = check?.progress?.status ?? check?.status;
        if (!string.IsNullOrEmpty(status)) quest.status = status;
    }

    // Claim quest đã hoàn thành, đánh dấu đã nhận và đồng bộ ví
    public async Task<DailyRewardItem[]> ClaimQuestAsync(QuestEntry quest)
    {
        QuestProgressor progressor = await GetProgressorAsync();
        bool daily = !string.IsNullOrEmpty(quest.assignmentId);
        string id = daily ? quest.assignmentId : quest.questDefinitionId;

        ClaimQuestResponse claim = await (daily
            ? CallSdk<ClaimQuestResponse>((ok, fail) => progressor.ClaimDailyQuestAssignment(id, ok, fail))
            : CallSdk<ClaimQuestResponse>((ok, fail) => progressor.ClaimQuest(id, ok, fail)));
        quest.status = "claimed";
        await RefreshWalletAsync();
        return ToRewardItems(claim?.rewards_granted);
    }

    // Đảm bảo đã đăng nhập và lấy module QuestProgressor của SaiServer
    async Task<QuestProgressor> GetProgressorAsync()
    {
        if (!await EnsureAuthenticatedAsync())
            throw new InvalidOperationException("Not authenticated with SaiServer.");
        QuestProgressor progressor = SaiServer.Instance.QuestProgressor;
        if (progressor == null)
            throw new InvalidOperationException("QuestProgressor component is missing on SaiServer.");
        return progressor;
    }

    // Chuyển danh sách thưởng trong Quest Definition sang DailyRewardItem
    static DailyRewardItem[] ToRewardItems(QuestReward[] rewards)
    {
        var list = new List<DailyRewardItem>();
        if (rewards == null) return list.ToArray();
        foreach (QuestReward r in rewards)
        {
            if (r == null || !TryGetCurrency(r.item_definition_id, null, out CurrencyType currency)) continue;
            long amount = Math.Max(r.quantity_min, r.quantity_max);
            list.Add(new DailyRewardItem { currency = currency, amount = amount > 0 ? amount : r.amount });
        }
        return list.ToArray();
    }
}
