# Project layout

```
BikeFit.Web/
  Models/          BikeGeometry.cs, FitModels.cs, CockpitPreset.cs, FitProfile.cs, UserData.cs
  Services/        CockpitMath.cs, FitSolver.cs, BikeDrawing.cs,
                   Catalogue.cs, BikeLibrary.cs, FitLibrary.cs, UserDataExchange.cs,
                   GeometryImporter.cs
  Components/
    Pages/Home.razor
    Diagrams/BikeDiagram.razor, CockpitDiagram.razor
  Components/Shared/DataTransfer.razor   export / upload
  Data/bikes.json          sample archetypes (read-only)
  Data/cockpits.json       catalogue cockpit presets (read-only)
```
