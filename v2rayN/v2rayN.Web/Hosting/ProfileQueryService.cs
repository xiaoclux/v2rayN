// Ported from upstream ServiceLib/ViewModels/ProfilesViewModel.cs GetProfileItemsEx() (398-441) @5ea8ae64.

namespace v2rayN.Web.Hosting;

/// <summary>One row of the profile grid, with test results and traffic statistics joined in.</summary>
public sealed record ProfileRow(
    string IndexId,
    EConfigType ConfigType,
    string Remarks,
    string Address,
    int Port,
    string Network,
    string StreamSecurity,
    string Subid,
    string SubRemarks,
    bool IsActive,
    int Sort,
    int Delay,
    string DelayVal,
    string SpeedVal,
    string IpInfo,
    string TodayUp,
    string TodayDown,
    string TotalUp,
    string TotalDown);

/// <summary>Builds the profile grid rows shown on the main page.</summary>
public static class ProfileQueryService
{
    /// <param name="subId">Subscription to list; empty for all profiles. Must be validated by the caller.</param>
    /// <param name="filter">Remarks/address substring filter, already length-checked.</param>
    public static async Task<List<ProfileRow>> ListAsync(string subId, string filter)
    {
        var config = AppManager.Instance.Config;
        var models = await AppManager.Instance.ProfileModels(subId, filter) ?? [];
        await ConfigHandler.SetDefaultServer(config, models);

        var stats = (config.GuiItem.EnableStatistics ? StatisticsManager.Instance.ServerStat : null) ?? [];
        var statMap = stats.GroupBy(s => s.IndexId).ToDictionary(g => g.Key, g => g.First());
        var exMap = (await ProfileExManager.Instance.GetProfileExs()).GroupBy(e => e.IndexId).ToDictionary(g => g.Key, g => g.First());

        var rows = new List<ProfileRow>(models.Count);
        foreach (var t in models)
        {
            statMap.TryGetValue(t.IndexId, out var stat);
            exMap.TryGetValue(t.IndexId, out var ex);
            rows.Add(new ProfileRow(
                t.IndexId,
                t.ConfigType,
                t.Remarks,
                t.Address,
                t.Port,
                t.Network,
                t.StreamSecurity,
                t.Subid,
                t.SubRemarks,
                t.IndexId == config.IndexId,
                ex?.Sort ?? 0,
                ex?.Delay ?? 0,
                ex is { Delay: not 0 } ? $"{ex.Delay}" : string.Empty,
                ex?.Speed > 0 ? $"{ex.Speed}" : ex?.Message ?? string.Empty,
                ex?.IpInfo ?? string.Empty,
                stat == null ? string.Empty : Utils.HumanFy(stat.TodayUp),
                stat == null ? string.Empty : Utils.HumanFy(stat.TodayDown),
                stat == null ? string.Empty : Utils.HumanFy(stat.TotalUp),
                stat == null ? string.Empty : Utils.HumanFy(stat.TotalDown)));
        }
        return rows.OrderBy(r => r.Sort).ToList();
    }
}
