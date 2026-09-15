using System.Runtime.CompilerServices;

// Las costuras internas de M8 (ISpeechSynthesizer, IAnimationDriver, IMainThreadPump y
// NpcPresenter) son internal: ningun otro modulo las referencia (regla dura 3). Las pruebas
// EditMode del propio modulo si necesitan verlas. InternalsVisibleTo es un atributo de C#,
// no un campo de asmdef: NpcAi.Presentation.asmdef no se toca (mismo patron que M1).
[assembly: InternalsVisibleTo("NpcAi.Presentation.Tests")]
