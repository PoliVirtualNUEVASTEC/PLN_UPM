using System.Runtime.CompilerServices;

// Costura de prueba determinista (requisito "Costura de prueba determinista sin
// microfono ni modelo" de reconocimiento-voz-m1): expone los miembros `internal` de
// `OfflineSpeechToText` solo al ensamblado de pruebas, sin ampliar la superficie
// publica del modulo. No es un campo de `NpcAi.Speech.asmdef`: es un atributo de C#.
[assembly: InternalsVisibleTo("NpcAi.Speech.Tests")]
