using EduSphereDomain.Repositories;

public class TeacherConflictChecker
{
    private readonly ITeacherAvailabilityProvider _provider;
    private readonly Guid _schoolId;

    // Loaded once per generation run, per teacher
    private readonly Dictionary<Guid, HashSet<(DayOfWeek, TimeSpan)>> _cache = new();

    public TeacherConflictChecker(ITeacherAvailabilityProvider provider, Guid schoolId)
    {
        _provider = provider;
        _schoolId = schoolId;
    }

    public async Task PreloadAsync(IEnumerable<Guid> teacherIds)
    {
        // Call this once before generation starts — loads all teachers in one go
        // so we never hit the DB mid-placement
        foreach (var teacherId in teacherIds.Distinct())
        {
            if (!_cache.ContainsKey(teacherId))
                _cache[teacherId] = await _provider.GetBusySlots(teacherId, _schoolId);
        }
    }

    public bool IsTeacherBusy(Guid teacherId, DayOfWeek day, TimeSpan startTime)
    {
        if (teacherId == Guid.Empty) return false;
        if (!_cache.TryGetValue(teacherId, out var busy)) return false;
        return busy.Contains((day, startTime));
    }

    public void MarkBusy(Guid teacherId, DayOfWeek day, TimeSpan startTime)
    {
        if (teacherId == Guid.Empty) return;
        if (!_cache.ContainsKey(teacherId))
            _cache[teacherId] = new HashSet<(DayOfWeek, TimeSpan)>();

        _cache[teacherId].Add((day, startTime));
    }
}