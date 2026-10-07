# 🔧 ACCIONES CORRECTIVAS PRIORITARIAS

**Proyecto:** Proyecto de Grado - Unity 2D  
**Fecha:** 22/04/2026  
**Severidad:** Mixta (Crítica → Importante → Advertencia)

---

## 🔴 CRÍTICAS (Resolver INMEDIATAMENTE)

### 1. ⚠️ SCRIPTS DUPLICADOS: FollowCamera vs FollowPlayer

**Problema:**
- Dos scripts implementan la **misma funcionalidad: cámara que sigue al jugador**
- `Camara.cs` (Clase: `FollowCamera`)
- `FollowPlayer.cs`
- Si ambos están activos en la escena: **comportamiento impredecible**

**Líneas afectadas:**
- [Camara.cs](Camara.cs#L1-L30) - FollowCamera
- [FollowPlayer.cs](FollowPlayer.cs#L1-L20) - FollowPlayer

**Impacto:** 🔴 CRÍTICO
- Posible jitter o saltos de cámara
- Comportamiento no determinista según cuál se ejecute primero
- Confusión en el equipo de desarrollo

**Solución Recomendada:**

**Opción A (Mantener FollowPlayer.cs):**
```
1. Verificar que FollowPlayer.cs está siendo usado en Escena Historia.unity
2. Eliminar Camara.cs completamente (una copia redundante)
3. En Gamestarter o escena, verificar que SOLO FollowPlayer.cs está asignada
4. Commit: "Remover script duplicado Camara.cs (redundante con FollowPlayer.cs)"
```

**Opción B (Mantener Camara.cs):**
```
1. Eliminar FollowPlayer.cs
2. Renombrar Camara.cs → FollowPlayer.cs (mejor nombre)
3. Renombrar clase FollowCamera → FollowPlayer (consistencia)
```

**Recomendación:** Elegir **Opción A** (FollowPlayer mejor nombrado)

---

### 2. 🔤 INCONSISTENCIA EN NOMENCLATURA: Robert_abajo vs robert_arriba

**Problema:**
- Archivo: `Assets/robert/Robert_abajo.aseprite` (con mayúscula R)
- Archivos: `Assets/robert/robert_arriba.aseprite` (minúscula r)
- Otros personajes: `albert_abajo.aseprite` (consistentemente minúscula)

**Archivos afectados:**
```
Assets/robert/Robert_abajo.aseprite          ← ❌ Mayúscula inconsistente
Assets/robert/robert_arriba.aseprite         ← ✅ Minúscula correcta
Assets/robert/robert_derecha.aseprite        ← ✅ Minúscula correcta
Assets/robert/robert_izquierda.aseprite      ← ✅ Minúscula correcta
```

**Impacto:** 🔴 CRÍTICO (si sistema de búsqueda/carga es case-sensitive)
- Errores al buscar/cargar sprites por referencia string
- En Windows (case-insensitive) puede no fallar, pero en Mac/Linux fallará
- Confusión visual

**Solución:**
```
1. En Assets/robert/:
   - Renombrar "Robert_abajo.aseprite" → "robert_abajo.aseprite"
   - Verificar en Inspector de Unity que sprite se refleja correctamente
   - Actualizar cualquier referencia en código (buscar "Robert_abajo" en proyecto)

2. Commit: "Estandarizar nombres de sprites robert (minúscula)"
```

**Comando PowerShell (Windows):**
```powershell
cd "c:\Users\nnico\Proyecto de grado\Assets\robert"
Rename-Item -Path "Robert_abajo.aseprite" -NewName "robert_abajo.aseprite"
Rename-Item -Path "Robert_abajo.aseprite.meta" -NewName "robert_abajo.aseprite.meta"
```

---

### 3. 🏷️ CARPETA NOMBRADA INCORRECTAMENTE: "1Creador" → debería ser "haber"

**Problema:**
- Carpeta: `Assets/1Creador/` contiene sprites de **Fritz Haber**
- Nombre inconsistente respecto a otros personajes (albert, curie, bosch, robert, carla)
- Contador "1" al inicio sugiere orden (pero no es así)

**Carpeta afectada:**
```
Assets/1Creador/          ← ❌ Nombre confuso
├─ haber_abajo.aseprite   (contenido correcto)
├─ haber_arriba.aseprite
├─ haber_derecha.aseprite
├─ haber_izquierda.aseprite
└─ [controllers, animaciones]
```

**Referencia correcta debería ser:**
```
Assets/haber/
├─ haber_abajo.aseprite
├─ ...
```

**Impacto:** 🔴 CRÍTICO
- Búsqueda confusa para desarrolladores
- Importaciones pueden fallar si hay rutas hardcodeadas
- Asset organization inconsistente

**Solución:**
```
1. En el proyecto:
   - Click derecho en Assets/1Creador/ → Rename
   - Cambiar a "haber"
   - Unity actualiza automáticamente referencias .meta

2. Búsqueda y reemplazo de strings:
   - Buscar "1Creador" en todos los scripts
   - Cambiar a "haber"

3. Commit: "Renombrar carpeta 1Creador → haber para consistencia"
```

**Verificar referencias:**
```csharp
// Buscar en proyecto:
⌨️ Ctrl+Shift+F → "1Creador"
```

---

## 🟠 IMPORTANTES (Resolver en próximas 2 semanas)

### 4. 📦 SOLO 1 PREFAB - FALTA REUTILIZACIÓN

**Problema:**
- Solo existe: `Assets/Prefabs/MissionRowPrefab.prefab` (prefab de UI row)
- **Componentes reutilizables sin prefabs:**
  - NPCs (personajes históricos)
  - Objetos interactuables
  - Paneles de UI generales

**Impacto:** 
- ❌ Si necesitas 5 NPCs iguales, tienes que duplicar manualmente GameObjects
- ❌ Cambios a uno no se replican a otros
- ❌ Dificultad en mantener consistencia visual/código

**Solución Recomendada:**

**A. Crear Prefab: NPC Genérico**
```
Pasos:
1. En escena Historia.unity, seleccionar un NPC GameObject
2. Arrastrar a Assets/Prefabs/ → "NPCCharacter.prefab"
3. Componentes esperados:
   ├─ SpriteRenderer
   ├─ Animator (controller de personaje)
   ├─ Collider2D (con IsTrigger = true)
   ├─ Dialogo.cs script
   └─ Tag: "Interactable"
4. Hacer que otros NPCs hereden de este prefab (Make variant)
```

**B. Crear Prefab: Objeto Interactable**
```
Assets/Prefabs/InteractableObject.prefab
├─ SpriteRenderer
├─ Collider2D (trigger)
├─ Dialogo.cs O custom script
└─ Tag: "Interactable"
```

**C. Crear Prefab: Panel Diálogo**
```
Assets/Prefabs/DialogPanel.prefab
├─ Canvas component (local)
├─ TMP_Text (texto del diálogo)
├─ AnimationController (entrada/salida)
└─ Dialogo.cs script
```

**Estimado de tiempo:** 3-4 horas

---

### 5. 🏷️ ARCHIVOS UUID SIN NOMBRE DESCRIPTIVO

**Problema:**
- Archivos en `Assets/Imagenes/`:
  - `53969238-e5ea-4d7f-9113-f0096256cd8b.jpg` ← ¿Qué es?
  - `53969238-e5ea-4d7f-9113-f00962cd8b.jpg` ← Similar, posible duplicado

**Impacto:**
- Desconocimiento del propósito del asset
- Posible contaminación de proyecto
- Assets huérfanos (probables no usados)

**Solución:**
```
1. Abrir ambas imágenes (click derecho → Open in Explorer)
2. Ver contenido visualmente:
   - Si es splash screen → renombrar: "splash_screen.jpg"
   - Si es logo → "logo.jpg"
   - Si es artwork concepto → "concept_art_001.jpg"
3. Verificar si están realmente usadas:
   ⌨️ Ctrl+Shift+F → buscar UUID en proyecto
   Si no hay referencias: ✂️ Eliminar como asset huérfano
4. Actualizar .meta correspondence

5. Commit: "Renombrar assets genéricos a nombres descriptivos"
```

**Estructura recomendada despues:**
```
Assets/
├─ Art/
│  ├─ UI/
│  │  ├─ splash_screen.jpg
│  │  ├─ logo.jpg
│  │  └─ background.jpg
```

---

### 6. ⚠️ CARPETA "Imagenes" - NOMBRE GENÉRICO

**Problema:**
- Carpeta: `Assets/Imagenes/` (nombre muy genérico)
- Contenido: Imagenes editariales/UI general

**Recomendación:**
```
Renombrar a "Assets/UI/" ó "Assets/Editorial/"

Assets/
├─ Art/
│  ├─ UI/          ← (renombrar de Imagenes)
│  │  ├─ splash_screen.jpg
│  │  ├─ logo.jpg
│  │  ├─ background.jpg
│  │  ├─ Icons/
│  │  └─ Buttons/
│  ├─ Characters/
│  ├─ Environment/
│  └─ Effects/
```
```

**Estimado:** 30 minutos (renombrar + referenciar)

---

### 7. 🔄 REVISAR DUPLICACIÓN: Personajes en dos ubicaciones

**Problema:**
```
Assets/albert/albert_*.aseprite           (4 sprites)
Assets/Personajes/Albert einstein.aseprite (1 sprite - diferente nombre/formato)
```

**¿Cuál es la versión oficial?** ¿Está deprecated una?

**Acción:**
```
1. Abrir ambas versiones (inspector)
2. Comparar resolución, dimensión, calidad
3. Mantener SOLO la mejor versión
4. Eliminar duplicados
5. Buscar referencias: ¿Cuál se usa en código/Animator?
6. Commit: "Eliminar sprites duplicados de personajes"
```

---

## 🟡 ADVERTENCIAS (Resolver en proximas 4 semanas)

### 8. 📝 FALTA DE DOCUMENTACIÓN XML EN SCRIPTS

**Problema:**
- Scripts principales carecen de comentarios XML para documentación automática
- Métodos públicos sin `/// <summary>` tags

**Ejemplo Correcto:**
```csharp
/// <summary>
/// Desbloquea el movimiento en dirección hacia arriba.
/// Notifica al ObjectiveManager del desbloqueo.
/// </summary>
public void DesbloquearMovimientoArriba()
{
    movimientoArribaDesbloqueado = true;
    OnMovementUnlocked?.Invoke("arriba");
}
```

**Archivos a mejorar:**
- [PlayerController.cs](PlayerController.cs) - Métodos públicos
- [ComandoSystem.cs](ComandoSystem.cs) - ConfiguraciÓn pública
- [ObjectiveManager.cs](ObjectiveManager.cs) - API pública

**Tiempo estimado:** 2-3 horas

---

### 9. 🎮 CANVAS DE UI - POTENCIAL PERFORMANCE ISSUE

**Problema:**
- Múltiples Canvas en escena: ComandoSystem, ObjectiveUI, PauseMenuController
- Si todos se instancian simultáneamente: posible lag de UI

**Recomendación:**
```
Implementar Canvas Pooling:
1. Crear CanvasPool.cs script
2. Pre-instanciar Canvas importantes al inicio
3. Activar/deactivar en lugar de Instantiate/Destroy

// Pseudocódigo
public class CanvasPool : MonoBehaviour
{
    private Dictionary<CanvasType, Queue<Canvas>> pool;
    
    public Canvas GetCanvas(CanvasType type)
    {
        if (pool[type].Count > 0)
            return pool[type].Dequeue();
        else
            return Instantiate(canvasPrefab);
    }
    
    public void ReturnCanvas(CanvasType type, Canvas canvas)
    {
        canvas.gameObject.SetActive(false);
        pool[type].Enqueue(canvas);
    }
}
```

**Tiempo estimado:** 3-4 horas

---

### 10. 🎬 VIDEOS EN .GITIGNORE

**Problema:**
- Carpeta `Assets/videos/` tiene scripts pero NO archivos .mp4/.webm
- Posiblemente archivos de video estén en `.gitignore` (archivos grandes)

**Verifi car:**
```
1. Abrir .gitignore en proyecto root
2. Buscar: *.mp4, *.webm, *.mov
3. Si está ignorado: agregar excepción para videos necesarios:
   # Excepto cinemáticas críticas
   !Assets/videos/*.mp4
```

**O:**
```
Agregar videos a Git LFS (Large File Storage):
git lfs track "*.mp4"
git add .gitattributes
```

---

### 11. 🔗 CORRUTINES SIN CANCELLATION

**Problema:**
- `RelativeLayerOrder.cs` inicia corrutine en `Start()` pero no hay `StopCoroutine` en `OnDestroy()`

[RelativeLayerOrder.cs](RelativeLayerOrder.cs#L35-L43):
```csharp
void Start()
{
    // ...
    StartCoroutine(UpdateNearbyObjectsRoutine());  // ← Sin referencia guardada
}

// ❌ FALTA: OnDestroy() para cancelar corrutine
```

**Impacto:** Memory leak si objeto se destruye (corrutine sigue corriendo)

**Solución:**
```csharp
private Coroutine updateRoutine;

void Start()
{
    updateRoutine = StartCoroutine(UpdateNearbyObjectsRoutine());
}

void OnDestroy()
{
    if (updateRoutine != null)
        StopCoroutine(updateRoutine);  // ✅ Cancela limpiamente
}
```

**Archivos a revisar:**
- RelativeLayerOrder.cs (línea ~35)
- Cualquier otro script con `StartCoroutine`

**Tiempo estimado:** 30 minutos

---

### 12. ⚠️ ASEPRITE COMO FORMATO EN PRODUCTION

**Problema:**
- Archivos `.aseprite` en Assets (formato propietario de Aseprite)
- Aseprite puede NO estar disponible en todas las máquinas de build
- Export a `.png` es necesario para distribución

**Recomendación:**
```
Workflow:
1. Mantener .aseprite como SOURCE files (Git)
2. Exportar a .png PNG cuando sea necesario
3. Usar .png para Referencias en Sprite Renderer

O:
Configurar Aseprite CLI para auto-export:
aseprite.exe --batch --sheet output.png input.aseprite
```

**Estructura recomendada:**
```
Assets/Art/Characters/
├─ SOURCE/
│  └─ Albert/
│     └─ albert.aseprite      (← Original Aseprite)
└─ EXPORTED/
   └─ Albert/
      ├─ albert_abajo.png     (← Exportado)
      ├─ albert_arriba.png
      └─ albert_sheet_big.png (← Sprite sheet si es necesario)
```

---

## ✅ CHECKLIST DE ACCIONES

### INMEDIATO (Esta semana):
- [ ] Eliminar script duplicado (FollowPlayer vs Camara)
- [ ] Renombrar robert_abajo (mayúscula inconsistente)
- [ ] Renombrar carpeta 1Creador → haber
- [ ] Renombrar UUIDs JPG en Imagenes/

### PRÓXIMAS 2 SEMANAS:
- [ ] Crear prefabs: NPCCharacter, InteractableObject, DialogPanel
- [ ] Revisar duplicación de personajes
- [ ] Agregar documentación XML a scripts públicos

### PRÓXIMAS 4 SEMANAS:
- [ ] Implementar Canvas Pooling
- [ ] Verificar .gitignore para videos
- [ ] Revisar corrutines sin cancellation
- [ ] Configurar Aseprite export workflow

---

## 📊 RESUMEN DE TIEMPO ESTIMADO

| Tarea | Severidad | Tiempo | Prioridad |
|-------|-----------|--------|-----------|
| Eliminar script duplicado | 🔴 Crítica | 15 min | 1 |
| Renombrar robert_abajo | 🔴 Crítica | 5 min | 2 |
| Renombrar carpeta 1Creador | 🔴 Crítica | 10 min | 3 |
| Renombrar UUIDs JPG | 🟠 Importante | 20 min | 4 |
| Crear prefabs NPCs | 🟠 Importante | 4 hrs | 5 |
| XML documentation | 🟡 Advertencia | 3 hrs | 6 |
| Canvas Pooling | 🟡 Advertencia | 4 hrs | 7 |
| Corrutines cleanup | 🟡 Advertencia | 30 min | 8 |

**Total Crítico:** 30 minutos  
**Total Importante:** 4.5 horas  
**Total Advertencias:** 7.5 horas  
**Total Recomendado:** ~12 horas

---

## 🚀 PRÓXIMOS PASOS DESPUÉS DE CORRECCIONES

1. **Expand Content:**
   - Más misiones/objetivos
   - Más diálogos para personajes
   - Más minijuegos

2. **Gameplay:**
   - Sistema de inventario
   - NPCs con IA simple (patrullas)
   - Efectos de sonido/música

3. **Polish:**
   - Animaciones de transición
   - Efectos visuales (particles)
   - Feedback de usuario mejorado

4. **Export/Build:**
   - Configurar Build Settings
   - Testar en Web GL
   - Publicar en itch.io o similar

