# Easy Max Weighted Normal

3ds Max 2022 C#/.NET Object Space Modifier prototype. The modifier computes weighted vertex normals for TriObject meshes and writes them into `IMeshNormalSpec` as explicit normals.

## Build

```powershell
dotnet build ./EasyMaxWeightedNormal.csproj -c Release
```

The project targets .NET Framework 4.8 and references:

- `C:\Program Files\Autodesk\3ds Max 2022\Autodesk.Max.dll`
- `C:\Program Files\Autodesk\3ds Max 2022\AssemblyLoader.dll`

## Install For 3ds Max

Copy the built `EasyMaxWeightedNormal.dll` to:

```text
C:\Program Files\Autodesk\3ds Max 2022\bin\assemblies
```

Restart 3ds Max. The assembly loader calls `AssemblyMain()`, which registers `Easy Weighted Normal` through `COREInterface13.AddClass(...)`.

## Options

When the modifier is selected for editing, it opens an `Easy Weighted Normal Options` panel with:

- `Area weight`: use triangle area in each face-corner contribution.
- `Angle weight`: multiply each contribution by its corner angle.
- `Respect smoothing groups`: only merge corners when smoothing-group masks overlap.

Changing a checkbox updates the current modifier instance and requests stack reevaluation and viewport redraw. The `Reset` button restores the default `area * angle` behavior with smoothing groups enabled.

## Algorithm

For each triangular face corner:

- Compute the unnormalized face normal from `cross(v1 - v0, v2 - v0)`. Its length is proportional to twice the triangle area, so using it directly gives area weighting.
- Compute the corner angle at the vertex. Multiplying by this angle removes triangulation-direction bias, matching Autodesk's weighted-normal guidance.
- Accumulate contributions per vertex and smoothing-group-connected component.
- Normalize each accumulated vector and assign it as an explicit `IMeshNormalSpec` normal.

The default behavior uses `area * angle` weighting and respects smoothing groups. Faces with smoothing group `0` stay hard.

## Limits

- This is a TriObject modifier. Non-TriObject inputs are ignored by `ModifyObject()`.
- `IMeshNormalSpec` is documented by Autodesk as primarily viewport-facing; renderer support can vary.
- The options panel is a managed `MaxCustomControls.MaxForm` opened from the modifier edit lifecycle and owned by the main 3ds Max window. A native ParamBlock2 command-panel rollout can be added later if resource-backed automatic UI is required.
- Runtime loading still needs validation inside 3ds Max because Autodesk.Max plug-in registration runs only in the 3ds Max process.
