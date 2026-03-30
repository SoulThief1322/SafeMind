using System.Text.Json;
using System.Linq;
using SafeMind.Models;
using SafeMind.Data.Models;

namespace SafeMind.Services;

public class SlotsService
{
    public bool TryParseSlots(string? rawJson, out int doctorId, out List<SlotVM>? slots, out string error)
    {
        doctorId = 0;
        slots = null;
        error = "Please select at least one session.";

        if (string.IsNullOrWhiteSpace(rawJson))
            return false;

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var trimmed = rawJson.TrimStart();

            List<SlotInput>? parsedSlots;

            if (trimmed.StartsWith("{"))
            {
                var payload = JsonSerializer.Deserialize<SlotPayload>(rawJson, options);
                doctorId = payload?.DoctorId ?? 0;
                parsedSlots = payload?.Slots;
            }
            else
            {
                parsedSlots = JsonSerializer.Deserialize<List<SlotInput>>(rawJson, options);
            }

            if (parsedSlots == null || parsedSlots.Count == 0)
                return false;

            slots = parsedSlots
                .Where(p => DateTime.TryParse(p.Date, out _) && TimeSpan.TryParse(p.Time, out _))
                .Select(p => new SlotVM
                {
                    Date = DateTime.Parse(p.Date!).Date,
                    Time = TimeSpan.Parse(p.Time!)
                })
                .DistinctBy(s => new { s.Date, s.Time })
                .OrderBy(s => s.Date)
                .ThenBy(s => s.Time)
                .ToList();

            return slots.Count > 0;
        }
        catch
        {
            error = "Invalid slot selection.";
            return false;
        }
    }

    public List<NormalizedSlot> NormalizeSlots(IEnumerable<SlotVM> slots, int durationMinutes)
    {
        return slots.Select(s =>
        {
            var startUtc = DateTime.SpecifyKind(s.Date + s.Time, DateTimeKind.Utc);
            var start = new DateTimeOffset(startUtc);

            return new NormalizedSlot
            {
                StartTime = start,
                EndTime = start.AddMinutes(durationMinutes)
            };
        }).ToList();
    }

    public IReadOnlyCollection<string> BuildSlots(Doctor doctor, DateOnly date, IEnumerable<TimeOnly> bookedTimes)
    {
        var booked = new HashSet<TimeOnly>(bookedTimes);
        var slots = new List<string>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentTimeUtc = TimeOnly.FromDateTime(DateTime.UtcNow);

        var start = date.ToDateTime(doctor.WorkStart);
        var end = date.ToDateTime(doctor.WorkEnd);
        var duration = TimeSpan.FromMinutes(doctor.SessionDuration);

        for (var current = start; current.Add(duration) <= end; current = current.Add(duration))
        {
            var time = TimeOnly.FromDateTime(current);
            if (date == today && time <= currentTimeUtc)
                continue;
            if (!booked.Contains(time))
                slots.Add(current.ToString("HH:mm"));
        }

        return slots;
    }

    private sealed class SlotInput
    {
        public string? Date { get; set; }
        public string? Time { get; set; }
    }

    private sealed class SlotPayload
    {
        public int DoctorId { get; set; }
        public List<SlotInput>? Slots { get; set; }
    }

    public sealed class NormalizedSlot
    {
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
    }
}