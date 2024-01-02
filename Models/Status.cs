using System.Text.Json;
using System.Text.Json.Serialization;

namespace WoodgroveDemo.Models;
public class Status
{
    public string RequestStateId { get; set; }
    public string RequestStatus { get; set; }
    public string Message { get; set; }
    public string JsonPayload { get; set; }
    public string Flow { get; set; }
    public string Scenario { get; set; }
    public string IndexedClaimValue { get; set; }
    public DateTime StartTime { get; set; }
    public List<string> Timing { get; set; } = new List<string>();

    public Status(string scenario, string flow): this()
    {
        this.Scenario = scenario;
        this.Flow = flow;
    }

    public Status()
    {
        StartTime = DateTime.Now;
        Timing.Add($"00:00:00 Started");
    }

    /// <summary>
    /// Serialize this object into a string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }

    /// <summary>
    /// Deserialize a JSON string into Status object
    /// </summary>
    /// <param name="JsonString">The JSON string to be loaded</param>
    /// <returns></returns>
    public static Status Parse(string JsonString)
    {
        return JsonSerializer.Deserialize<Status>(JsonString);
    }

    public string CalculateExecutionTime()
    {
        TimeSpan ts = DateTime.Now.Subtract(StartTime);
        return String.Format("{0:00}:{1:00}:{2:00}",
                ts.Hours, ts.Minutes, ts.Seconds);
    }

    public double CalculateExecutionSeconds()
    {
        TimeSpan ts = DateTime.Now.Subtract(StartTime);
        return ts.TotalSeconds;
    }
}


