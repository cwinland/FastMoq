# FastMoq API Reference

This API reference is generated from XML comments in the source projects.

## Generate locally

1. Restore the pinned local tools:

   dotnet tool restore

2. From the repository root, generate metadata and build the site:

   dotnet tool run docfx docfx.json

3. Open the generated site:

   Help/index.html

## Notes

- The generated site output is written to the Help folder.
- The metadata step is pinned to net8.0 for consistent CI generation.
- DocFX is pinned through `.config/dotnet-tools.json` so local and CI builds use the same version.
