using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using EcobeeFurnaceEta.Blazor.Models;

namespace EcobeeFurnaceEta.Blazor.Services;

/// <summary>
/// Ecobee API client. Uses real API when authenticated, falls back to mock data otherwise.
/// </summary>
public class EcobeeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly EcobeeAuthService _authService;
    private readonly SecureTokenStorage _tokenStorage;
    private const string EcobeeApiBase = "https://api.ecobee.com/1";

    public EcobeeApiClient(HttpClient httpClient, EcobeeAuthService authService, SecureTokenStorage tokenStorage)
    {
        _httpClient = httpClient;
        _authService = authService;
        _tokenStorage = tokenStorage;
    }

    /// <summary>
    /// Gets current thermostat data from Ecobee API or mock data if not authenticated.
    /// </summary>
    public async Task<ThermostatData> GetThermostatDataAsync()
    {
        if (!await _tokenStorage.IsAuthenticatedAsync())
        {
            return ThermostatData.CreateMockSaskatoonWinter();
        }

        try
        {
            // Try JWT token first, then fall back to OAuth
            var accessToken = await _tokenStorage.GetJwtTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = await _authService.GetValidAccessTokenAsync();
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                return ThermostatData.CreateMockSaskatoonWinter();
            }

            // Get thermostat ID if available
            var thermostatId = await _tokenStorage.GetThermostatIdAsync() ?? "";

            // Build the selection JSON for thermostat request
            var selection = new
            {
                selectionType = string.IsNullOrEmpty(thermostatId) ? "registered" : "thermostats",
                selectionMatch = thermostatId,
                includeRuntime = true,
                includeWeather = true,
                includeProgram = true,
                includeSettings = true
            };

            var selectionJson = JsonSerializer.Serialize(selection);
            var encodedSelection = HttpUtility.UrlEncode(selectionJson);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{EcobeeApiBase}/thermostat?json={{\"selection\":{selectionJson}}}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ecobee API error: {response.StatusCode}");
                return ThermostatData.CreateMockSaskatoonWinter();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<EcobeeThermostatResponse>();

            if (apiResponse?.ThermostatList == null || apiResponse.ThermostatList.Count == 0)
            {
                return ThermostatData.CreateMockSaskatoonWinter();
            }

            // Convert API response to our model
            var thermostat = apiResponse.ThermostatList[0];
            return ConvertToThermostatData(thermostat);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching thermostat data: {ex.Message}");
            return ThermostatData.CreateMockSaskatoonWinter();
        }
    }

    /// <summary>
    /// Gets heat-up profile. Returns mock profile (Phase 4 will build from history).
    /// </summary>
    public Task<HeatUpProfile> GetHeatUpProfileAsync()
    {
        // Phase 4 will build this from runtime history
        return Task.FromResult(HeatUpProfile.CreateMockSaskatoonProfile());
    }

    /// <summary>
    /// Gets runtime statistics including 24h history and projections.
    /// </summary>
    public async Task<RuntimeStats> GetRuntimeStatsAsync()
    {
        if (!await _tokenStorage.IsAuthenticatedAsync())
        {
            return RuntimeStats.CreateMockStats();
        }

        try
        {
            var accessToken = await _tokenStorage.GetJwtTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = await _authService.GetValidAccessTokenAsync();
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                return RuntimeStats.CreateMockStats();
            }

            var thermostatId = await _tokenStorage.GetThermostatIdAsync() ?? "";

            // Request includes extended runtime for last 24 hours
            var selection = new
            {
                selectionType = string.IsNullOrEmpty(thermostatId) ? "registered" : "thermostats",
                selectionMatch = thermostatId,
                includeRuntime = true,
                includeExtendedRuntime = true,
                includeWeather = true,
                includeEquipmentStatus = true
            };

            var selectionJson = JsonSerializer.Serialize(selection);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{EcobeeApiBase}/thermostat?json={{\"selection\":{selectionJson}}}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return RuntimeStats.CreateMockStats();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<EcobeeThermostatResponse>();

            if (apiResponse?.ThermostatList == null || apiResponse.ThermostatList.Count == 0)
            {
                return RuntimeStats.CreateMockStats();
            }

            return ConvertToRuntimeStats(apiResponse.ThermostatList[0]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching runtime stats: {ex.Message}");
            return RuntimeStats.CreateMockStats();
        }
    }

    private RuntimeStats ConvertToRuntimeStats(EcobeeThermostat thermostat)
    {
        var stats = new RuntimeStats
        {
            LastUpdated = DateTime.Now
        };

        // Check equipment status for current running state
        var equipmentStatus = thermostat.EquipmentStatus ?? "";
        stats.IsCurrentlyHeating = equipmentStatus.Contains("heat", StringComparison.OrdinalIgnoreCase) ||
                                   equipmentStatus.Contains("auxHeat", StringComparison.OrdinalIgnoreCase);
        stats.IsCurrentlyCooling = equipmentStatus.Contains("cool", StringComparison.OrdinalIgnoreCase);

        // Get extended runtime data if available
        if (thermostat.ExtendedRuntime != null)
        {
            // ExtendedRuntime contains 5-minute interval data
            // actualHeat/actualCool are arrays of runtime seconds per interval
            var heatRuntimes = thermostat.ExtendedRuntime.ActualHeat ?? new List<int>();
            var coolRuntimes = thermostat.ExtendedRuntime.ActualCool ?? new List<int>();

            // Sum up last 288 intervals (24 hours at 5-min intervals)
            var last24hHeat = heatRuntimes.TakeLast(288).Sum();
            var last24hCool = coolRuntimes.TakeLast(288).Sum();

            stats.TotalHeatingMinutes24h = last24hHeat / 60; // Convert seconds to minutes
            stats.TotalCoolingMinutes24h = last24hCool / 60;

            // Count heating cycles (transitions from 0 to non-zero)
            var cycles = 0;
            var wasHeating = false;
            foreach (var heat in heatRuntimes.TakeLast(288))
            {
                var isHeating = heat > 0;
                if (isHeating && !wasHeating) cycles++;
                wasHeating = isHeating;
            }
            stats.HeatingCycles24h = cycles;

            // Current cycle runtime (last non-zero sequence)
            if (stats.IsCurrentlyHeating && heatRuntimes.Count > 0)
            {
                var currentCycleSeconds = 0;
                for (int i = heatRuntimes.Count - 1; i >= 0; i--)
                {
                    if (heatRuntimes[i] > 0)
                        currentCycleSeconds += heatRuntimes[i];
                    else
                        break;
                }
                stats.CurrentCycleMinutes = currentCycleSeconds / 60;
            }
        }

        // Get weather data for projections
        if (thermostat.Weather?.Forecasts != null && thermostat.Weather.Forecasts.Count > 0)
        {
            // Current/recent outdoor temp
            stats.AvgOutdoorTemp24h = thermostat.Weather.Forecasts[0].Temperature / 10.0;

            // Tomorrow's forecast (if available, usually index 4+ is next day)
            if (thermostat.Weather.Forecasts.Count > 4)
            {
                stats.ForecastedAvgTempTomorrow = thermostat.Weather.Forecasts[4].Temperature / 10.0;
            }
            else
            {
                stats.ForecastedAvgTempTomorrow = stats.AvgOutdoorTemp24h;
            }

            // Project tomorrow's runtime based on temperature difference
            // Simple linear model: runtime increases as temp decreases
            if (stats.TotalHeatingMinutes24h > 0 && stats.AvgOutdoorTemp24h != stats.ForecastedAvgTempTomorrow)
            {
                var tempDiff = stats.AvgOutdoorTemp24h - stats.ForecastedAvgTempTomorrow;
                var runtimeAdjustment = 1.0 + (tempDiff * 0.05); // 5% more runtime per degree colder
                stats.ProjectedHeatingMinutesTomorrow = (int)(stats.TotalHeatingMinutes24h * runtimeAdjustment);
            }
            else
            {
                stats.ProjectedHeatingMinutesTomorrow = stats.TotalHeatingMinutes24h;
            }
        }

        return stats;
    }

    /// <summary>
    /// Gets today's schedule from Ecobee or mock data.
    /// </summary>
    public async Task<List<ScheduleEvent>> GetTodayScheduleAsync()
    {
        if (!await _tokenStorage.IsAuthenticatedAsync())
        {
            return GetMockSchedule();
        }

        try
        {
            // Try JWT token first, then fall back to OAuth
            var accessToken = await _tokenStorage.GetJwtTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = await _authService.GetValidAccessTokenAsync();
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                return GetMockSchedule();
            }

            // Get thermostat ID if available
            var thermostatId = await _tokenStorage.GetThermostatIdAsync() ?? "";

            var selection = new
            {
                selectionType = string.IsNullOrEmpty(thermostatId) ? "registered" : "thermostats",
                selectionMatch = thermostatId,
                includeProgram = true
            };

            var selectionJson = JsonSerializer.Serialize(selection);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{EcobeeApiBase}/thermostat?json={{\"selection\":{selectionJson}}}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return GetMockSchedule();
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<EcobeeThermostatResponse>();

            if (apiResponse?.ThermostatList == null || apiResponse.ThermostatList.Count == 0)
            {
                return GetMockSchedule();
            }

            return ConvertToScheduleEvents(apiResponse.ThermostatList[0].Program);
        }
        catch
        {
            return GetMockSchedule();
        }
    }

    private ThermostatData ConvertToThermostatData(EcobeeThermostat thermostat)
    {
        // Ecobee temperatures are in tenths of degrees Fahrenheit
        var currentTempF = thermostat.Runtime?.ActualTemperature / 10.0 ?? 68.0;
        var setpointF = thermostat.Runtime?.DesiredHeat / 10.0 ?? 72.0;
        var humidity = thermostat.Runtime?.ActualHumidity ?? 35.0;

        // Get outdoor temp from weather
        var outdoorTempF = 17.6; // Default Saskatoon winter
        if (thermostat.Weather?.Forecasts != null && thermostat.Weather.Forecasts.Count > 0)
        {
            outdoorTempF = thermostat.Weather.Forecasts[0].Temperature / 10.0;
        }

        return new ThermostatData
        {
            Name = thermostat.Name ?? "My Thermostat",
            CurrentTempF = currentTempF,
            SetpointF = setpointF,
            OutdoorTempF = outdoorTempF,
            HumidityPercent = humidity,
            DeadbandF = 1.0, // Ecobee default
            IsHeating = thermostat.Runtime?.DesiredHeat > thermostat.Runtime?.ActualTemperature,
            IsCooling = false,
            Timestamp = DateTime.Now
        };
    }

    private List<ScheduleEvent> ConvertToScheduleEvents(EcobeeProgram? program)
    {
        if (program?.Climates == null || program.Schedule == null)
        {
            return GetMockSchedule();
        }

        var schedule = new List<ScheduleEvent>();
        var today = (int)DateTime.Now.DayOfWeek;

        // Ecobee schedule is organized by day of week, then by 30-minute intervals
        if (program.Schedule.Count > today)
        {
            var todaySchedule = program.Schedule[today];
            string? currentClimate = null;
            int startInterval = 0;

            for (int i = 0; i < todaySchedule.Count && i < 48; i++)
            {
                var climate = todaySchedule[i];
                if (climate != currentClimate)
                {
                    if (currentClimate != null)
                    {
                        var climateInfo = program.Climates.FirstOrDefault(c => c.ClimateRef == currentClimate);
                        schedule.Add(new ScheduleEvent
                        {
                            Name = climateInfo?.Name ?? currentClimate,
                            StartTime = new TimeOnly(startInterval / 2, (startInterval % 2) * 30),
                            EndTime = new TimeOnly(i / 2, (i % 2) * 30),
                            SetpointF = (climateInfo?.HeatTemp ?? 720) / 10.0
                        });
                    }
                    currentClimate = climate;
                    startInterval = i;
                }
            }

            // Add last segment
            if (currentClimate != null)
            {
                var climateInfo = program.Climates.FirstOrDefault(c => c.ClimateRef == currentClimate);
                schedule.Add(new ScheduleEvent
                {
                    Name = climateInfo?.Name ?? currentClimate,
                    StartTime = new TimeOnly(startInterval / 2, (startInterval % 2) * 30),
                    EndTime = new TimeOnly(23, 59),
                    SetpointF = (climateInfo?.HeatTemp ?? 720) / 10.0
                });
            }
        }

        return schedule.Count > 0 ? schedule : GetMockSchedule();
    }

    private static List<ScheduleEvent> GetMockSchedule()
    {
        return new List<ScheduleEvent>
        {
            new() { StartTime = new TimeOnly(6, 0), EndTime = new TimeOnly(8, 0), SetpointF = 72.0, Name = "Wake" },
            new() { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0), SetpointF = 68.0, Name = "Away" },
            new() { StartTime = new TimeOnly(17, 0), EndTime = new TimeOnly(22, 0), SetpointF = 72.0, Name = "Home" },
            new() { StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(6, 0), SetpointF = 66.0, Name = "Sleep" }
        };
    }
}

#region Ecobee API Response Models

public class EcobeeThermostatResponse
{
    [JsonPropertyName("page")]
    public EcobeePage? Page { get; set; }

    [JsonPropertyName("thermostatList")]
    public List<EcobeeThermostat> ThermostatList { get; set; } = new();

    [JsonPropertyName("status")]
    public EcobeeStatus? Status { get; set; }
}

public class EcobeePage
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class EcobeeStatus
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
}

public class EcobeeThermostat
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("runtime")]
    public EcobeeRuntime? Runtime { get; set; }

    [JsonPropertyName("extendedRuntime")]
    public EcobeeExtendedRuntime? ExtendedRuntime { get; set; }

    [JsonPropertyName("weather")]
    public EcobeeWeather? Weather { get; set; }

    [JsonPropertyName("program")]
    public EcobeeProgram? Program { get; set; }

    [JsonPropertyName("settings")]
    public EcobeeSettings? Settings { get; set; }

    [JsonPropertyName("equipmentStatus")]
    public string? EquipmentStatus { get; set; }
}

public class EcobeeExtendedRuntime
{
    [JsonPropertyName("lastReadingTimestamp")]
    public string LastReadingTimestamp { get; set; } = "";

    [JsonPropertyName("runtimeDate")]
    public string RuntimeDate { get; set; } = "";

    [JsonPropertyName("runtimeInterval")]
    public int RuntimeInterval { get; set; }

    [JsonPropertyName("actualTemperature")]
    public List<int> ActualTemperature { get; set; } = new();

    [JsonPropertyName("actualHumidity")]
    public List<int> ActualHumidity { get; set; } = new();

    [JsonPropertyName("desiredHeat")]
    public List<int> DesiredHeat { get; set; } = new();

    [JsonPropertyName("desiredCool")]
    public List<int> DesiredCool { get; set; } = new();

    [JsonPropertyName("actualHeat")]
    public List<int> ActualHeat { get; set; } = new();

    [JsonPropertyName("actualCool")]
    public List<int> ActualCool { get; set; } = new();
}

public class EcobeeRuntime
{
    [JsonPropertyName("actualTemperature")]
    public int ActualTemperature { get; set; }

    [JsonPropertyName("actualHumidity")]
    public int ActualHumidity { get; set; }

    [JsonPropertyName("desiredHeat")]
    public int DesiredHeat { get; set; }

    [JsonPropertyName("desiredCool")]
    public int DesiredCool { get; set; }
}

public class EcobeeWeather
{
    [JsonPropertyName("forecasts")]
    public List<EcobeeWeatherForecast> Forecasts { get; set; } = new();
}

public class EcobeeWeatherForecast
{
    [JsonPropertyName("temperature")]
    public int Temperature { get; set; }

    [JsonPropertyName("relativeHumidity")]
    public int RelativeHumidity { get; set; }
}

public class EcobeeProgram
{
    [JsonPropertyName("schedule")]
    public List<List<string>> Schedule { get; set; } = new();

    [JsonPropertyName("climates")]
    public List<EcobeeClimate> Climates { get; set; } = new();
}

public class EcobeeClimate
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("climateRef")]
    public string ClimateRef { get; set; } = "";

    [JsonPropertyName("heatTemp")]
    public int HeatTemp { get; set; }

    [JsonPropertyName("coolTemp")]
    public int CoolTemp { get; set; }
}

public class EcobeeSettings
{
    [JsonPropertyName("hvacMode")]
    public string HvacMode { get; set; } = "";

    [JsonPropertyName("heatStages")]
    public int HeatStages { get; set; }
}

#endregion

/// <summary>
/// A scheduled temperature event.
/// </summary>
public class ScheduleEvent
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public double SetpointF { get; set; }
    public string Name { get; set; } = "";
}
