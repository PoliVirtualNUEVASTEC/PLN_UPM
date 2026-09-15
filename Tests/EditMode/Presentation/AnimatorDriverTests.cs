using System.Collections.Generic;
using NpcAi.Presentation.Config;
using NUnit.Framework;
using UnityEngine;

namespace NpcAi.Presentation.Tests
{
    /// <summary>
    /// <see cref="AnimatorDriver"/> tolera la ausencia de <c>Animator</c>, de controller y de
    /// mapa, e ignora los cues fuera del mapa — sin lanzar en ningún caso. El "aplica el
    /// parámetro correcto" contra un <c>AnimatorController</c> real es prueba manual de escena
    /// (el rig es del anfitrión).
    /// </summary>
    public class AnimatorDriverTests
    {
        [Test]
        public void Sin_animator_no_lanza()
        {
            var d = new AnimatorDriver(null, new Dictionary<string, ParametroDeAnimacion>());
            Assert.DoesNotThrow(() => d.Aplicar("molesto", "cruzar_brazos"));
        }

        [Test]
        public void Con_mapa_nulo_o_tags_nulos_no_lanza()
        {
            var d = new AnimatorDriver(null, null);
            Assert.DoesNotThrow(() => d.Aplicar("molesto", "cruzar_brazos"));
            Assert.DoesNotThrow(() => d.Aplicar(null, null));
        }

        [Test]
        public void Con_animator_sin_controller_es_no_op_y_no_lanza()
        {
            var go = new GameObject("m8-anim-test");
            var animator = go.AddComponent<Animator>();
            var mapa = new Dictionary<string, ParametroDeAnimacion>
            {
                ["asentir"] = new ParametroDeAnimacion("Asentir", TipoDeParametroDeAnimacion.Trigger),
                ["molesto"] = new ParametroDeAnimacion("Enojo", TipoDeParametroDeAnimacion.Bool),
            };
            var d = new AnimatorDriver(animator, mapa);

            Assert.DoesNotThrow(() => d.Aplicar("molesto", "asentir")); // ambos en el mapa, sin controller
            Assert.DoesNotThrow(() => d.Aplicar("desconocido", "otro")); // fuera del mapa

            Object.DestroyImmediate(go);
        }
    }
}
