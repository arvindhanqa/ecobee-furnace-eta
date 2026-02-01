using EcobeeFurnaceEta.Blazor.Models;

namespace EcobeeFurnaceEta.Blazor.Services;

/// <summary>
/// Core prediction engine that calculates furnace ETA and temperature projections.
/// </summary>
public class PredictionEngine
{
    private readonly HeatLossCalculator _heatLossCalculator;

    public PredictionEngine(HeatLossCalculator heatLossCalculator)
    {
        _heatLossCalculator = heatLossCalculator;
    }

    /// <summary>
    /// Generates a complete furnace prediction based on current thermostat data and heat-up profile.
    /// </summary>
    public FurnacePrediction Predict(ThermostatData thermostat, HeatUpProfile profile)
    {
        var prediction = new FurnacePrediction();

        // Calculate basic values
        var currentTemp = thermostat.CurrentTempF;
        var setpoint = thermostat.SetpointF;
        var outdoorTemp = thermostat.OutdoorTempF;
        var deadband = thermostat.DeadbandF;

        // Temperature at which furnace kicks on
        var furnaceKickOnTemp = setpoint - deadband;
        prediction.FurnaceKickOnTempF = furnaceKickOnTemp;

        // Gap to target
        prediction.TempGapF = setpoint - currentTemp;

        // Get rates
        var heatLossRate = _heatLossCalculator.CalculateHeatLossRate(currentTemp, outdoorTemp);
        var heatUpRate = profile.GetHeatUpRate(outdoorTemp);
        var effectiveRate = heatUpRate - heatLossRate;

        prediction.HeatLossRatePerMin = heatLossRate;
        prediction.HeatUpRatePerMin = heatUpRate;
        prediction.EffectiveRatePerMin = effectiveRate;

        // Determine furnace status
        if (currentTemp >= setpoint)
        {
            prediction.Status = FurnaceStatus.AtTarget;
            prediction.MinutesToFurnaceOn = 0;
            prediction.MinutesToTarget = 0;
        }
        else if (currentTemp <= furnaceKickOnTemp)
        {
            // Temp is at or below kick-on threshold - furnace should be on
            prediction.Status = thermostat.FurnaceRunning ? FurnaceStatus.Running : FurnaceStatus.WillTurnOnNow;
            prediction.MinutesToFurnaceOn = 0;

            // Calculate time to reach target
            if (effectiveRate > 0)
            {
                prediction.MinutesToTarget = prediction.TempGapF / effectiveRate;
            }
            else
            {
                prediction.MinutesToTarget = double.MaxValue; // Can't reach target
            }
        }
        else
        {
            // Temp is above kick-on threshold but below setpoint
            // Waiting for temp to drop to kick-on point
            prediction.Status = FurnaceStatus.WaitingForDeadband;

            // Calculate when furnace will kick on (temp dropping to threshold)
            var gapToKickOn = currentTemp - furnaceKickOnTemp;
            if (heatLossRate > 0)
            {
                prediction.MinutesToFurnaceOn = gapToKickOn / heatLossRate;
            }
            else
            {
                prediction.MinutesToFurnaceOn = double.MaxValue;
            }

            // Total time = time to kick on + time to heat up
            var gapFromKickOnToTarget = setpoint - furnaceKickOnTemp;
            if (effectiveRate > 0)
            {
                prediction.MinutesToTarget = prediction.MinutesToFurnaceOn + (gapFromKickOnToTarget / effectiveRate);
            }
            else
            {
                prediction.MinutesToTarget = double.MaxValue;
            }
        }

        // Generate temperature projections for the next 60 minutes
        prediction.Projections = GenerateProjections(thermostat, profile, prediction);

        return prediction;
    }

    /// <summary>
    /// Generates temperature projections at 5-minute intervals for the next 60 minutes.
    /// </summary>
    private List<TemperatureProjection> GenerateProjections(
        ThermostatData thermostat,
        HeatUpProfile profile,
        FurnacePrediction basePrediction)
    {
        var projections = new List<TemperatureProjection>();
        var currentTemp = thermostat.CurrentTempF;
        var setpoint = thermostat.SetpointF;
        var outdoorTemp = thermostat.OutdoorTempF;
        var effectiveRate = basePrediction.EffectiveRatePerMin;
        var heatLossRate = basePrediction.HeatLossRatePerMin;
        var minutesToFurnaceOn = basePrediction.MinutesToFurnaceOn;

        for (int minutes = 0; minutes <= 60; minutes += 5)
        {
            double projectedTemp;

            if (basePrediction.Status == FurnaceStatus.AtTarget)
            {
                // Already at target, maintain setpoint
                projectedTemp = setpoint;
            }
            else if (basePrediction.Status == FurnaceStatus.WaitingForDeadband)
            {
                // Two phases: cooling until furnace on, then heating
                if (minutes < minutesToFurnaceOn)
                {
                    // Still cooling
                    projectedTemp = currentTemp - (heatLossRate * minutes);
                }
                else
                {
                    // Furnace has kicked on, now heating
                    var tempAtKickOn = basePrediction.FurnaceKickOnTempF;
                    var minutesSinceKickOn = minutes - minutesToFurnaceOn;
                    projectedTemp = tempAtKickOn + (effectiveRate * minutesSinceKickOn);
                }
            }
            else
            {
                // Furnace on or will turn on now - heating from current temp
                projectedTemp = currentTemp + (effectiveRate * minutes);
            }

            // Cap at setpoint (furnace cycles off at target)
            projectedTemp = Math.Min(projectedTemp, setpoint);

            projections.Add(new TemperatureProjection
            {
                MinutesFromNow = minutes,
                ProjectedTempF = Math.Round(projectedTemp, 1),
                AtOrAboveTarget = projectedTemp >= setpoint
            });
        }

        return projections;
    }
}
