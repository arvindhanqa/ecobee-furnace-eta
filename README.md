# Ecobee Furnace ETA

Predicts furnace kick-on time and ETA to target temperature using Ecobee thermostat data.

## What This Does

Ecobee's dashboard shows current temp and setpoint but doesn't tell you:

- When the furnace will actually turn on (deadband/hysteresis means it doesn't kick on the second you hit setpoint)
- How long until your home reaches the target temperature
- How fast your home is losing heat right now
- How outdoor temperature affects your specific home's heat-up rate

This tool calculates all of that and shows it in a clean web dashboard.

## Quick Start

```bash
cd src/EcobeeFurnaceEta.Blazor
dotnet run
```

Then open `https://localhost:5001` in your browser.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## How the Prediction Works

```
Current Temp --> Gap to Setpoint --> Historical Heat-Up Rate --> ETA
                                          |
                 Outdoor Temp ------------+
                 (interpolated from your past runtime data)

Heat Loss Rate  = (Indoor - Outdoor Temp) x thermal constant
Effective Rate  = Heat-Up Rate - Heat Loss Rate
Minutes to Target = Gap / Effective Rate
Furnace On ETA  = now, if current temp < (setpoint - deadband)
```

The heat-up rate is learned from your own Ecobee runtime history (Phase 4).

## Project Structure

```
ecobee-furnace-eta/
├── src/EcobeeFurnaceEta.Blazor/
│   ├── Pages/
│   │   └── Index.razor              # Main dashboard page
│   ├── Components/
│   │   ├── TempGauge.razor          # Visual temperature gauge
│   │   ├── ETACountdown.razor       # Furnace on/target reached countdown
│   │   ├── TempCurve.razor          # SVG projected temperature curve
│   │   ├── ScheduleTimeline.razor   # Today's ecobee schedule
│   │   └── StatsPanel.razor         # Heat loss/rate statistics
│   ├── Models/
│   │   ├── ThermostatData.cs        # Thermostat data model
│   │   ├── HeatUpProfile.cs         # Learned heat-up rates per outdoor temp
│   │   └── FurnacePrediction.cs     # Prediction output model
│   ├── Services/
│   │   ├── PredictionEngine.cs      # Core math - gap to rate to ETA
│   │   ├── HeatLossCalculator.cs    # Newton's cooling law, simplified
│   │   └── EcobeeApiClient.cs       # Ecobee API (stub in Phase 1-2)
│   ├── Layout/
│   │   └── MainLayout.razor         # App layout
│   ├── wwwroot/
│   │   ├── index.html               # HTML host
│   │   └── css/
│   │       └── app.css              # Dark theme dashboard styles
│   ├── Program.cs                   # Blazor entry point
│   ├── App.razor                    # App router
│   ├── _Imports.razor               # Global usings
│   └── EcobeeFurnaceEta.Blazor.csproj
└── README.md
```

## Current Phase: 1 (Mock Data)

Phase 1 uses hardcoded mock data for a Saskatoon winter scenario:
- Current temp: 68°F
- Setpoint: 72°F
- Outdoor temp: -8°C (17.6°F)
- Deadband: 1°F
- Heat-up rate: 0.28°F/min

## Roadmap

| Phase | Description | Status |
|-------|-------------|--------|
| **1** | Blazor dashboard with mock data | Done |
| **2** | Interactive controls - adjust inputs live | Planned |
| **3** | Ecobee API integration - live data | Planned |
| **4** | Heat-up rate learning from runtime history | Planned |
| **5** | Mobile polish - responsive layout | Planned |

## Tech Stack

- **Language**: C# / .NET 8
- **Framework**: Blazor WebAssembly
- **UI**: Hand-rolled CSS (dark theme)
- **Charts**: Inline SVG

## License

MIT
