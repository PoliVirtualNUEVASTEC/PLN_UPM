using System.Runtime.CompilerServices;

// Costura de prueba de la cascara de M11 (design.md, AD11): expone los miembros `internal` de
// `HarnessBehaviour` (los 5 campos serializados y las cuatro costuras CablearParaPrueba,
// ArrancarParaPrueba, DescablearParaPrueba y SalirParaPrueba) solo al ensamblado de pruebas,
// sin ampliar la superficie publica del modulo. No es un campo de `NpcAi.Harness.Unity.asmdef`:
// es un atributo de C#, igual que en M1, M7 y M8 (Runtime/VrInput/Properties/AssemblyInfo.cs).
[assembly: InternalsVisibleTo("NpcAi.Harness.Tests")]
