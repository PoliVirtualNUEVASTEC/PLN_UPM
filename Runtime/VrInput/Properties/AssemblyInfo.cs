using System.Runtime.CompilerServices;

// Costura de prueba determinista de M7 (tasks.md 1.8): expone los miembros `internal` de
// `SpatialPhysicalActionSource` (EmitirParaPrueba, ProcesarMuestra, VrInputSettings,
// ISpatialSampler, ScriptedSpatialSampler) solo al ensamblado de pruebas, sin ampliar la
// superficie publica del modulo. No es un campo de `NpcAi.VrInput.asmdef`: es un atributo de
// C#, igual que en M1 (Runtime/Speech/Properties/AssemblyInfo.cs).
[assembly: InternalsVisibleTo("NpcAi.VrInput.Tests")]
