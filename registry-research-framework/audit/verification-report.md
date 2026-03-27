# RegProbe Cutover Verification

Generated: 2026-03-27

## Host
- `dotnet build RegProbe.sln -c Release`: passed
- `dotnet test tests/tests.csproj -c Release --no-build -v minimal`: passed (`191/191`)

## Residual scans
- `OpenTraceProject` residuals outside protected roots/build artifacts: `0`
- hardware/dashboard residuals outside protected roots/build artifacts: `0`
- excluded historical/build roots: `evidence/`, `research/`, `Docs/`, `.git/`, `publish/`, `publish_final/`, `registry-research-framework/audit/`, `**/bin/**`, `**/obj/**`, `DEVELOPMENT_ROADMAP.md`, `DEVELOPMENT_STATUS.md`

## Protection
- protected root file counts unchanged: `true`
- protected root SHA256s unchanged: `true`
- protected roots: `evidence/`, `research/`, `Docs/`

## VM
- shell health before smoke: healthy
- app launch smoke: passed
- shell health after smoke: healthy
- smoke executable: `C:\Tools\AppSmoke\RegProbe.App.exe`
- smoke main window title: `RegProbe`
