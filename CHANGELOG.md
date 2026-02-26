# Changelog — LectorHuellas

Historial detallado de issues encontrados y soluciones aplicadas durante el desarrollo.

---

## [v2.1.0] — 2026-02-26 — Detección Robusta y UI Refinada

### ✅ Nuevas Características
- **Detección de Hardware en Tiempo Real**: Monitoreo constante del estado del lector con alertas visuales inmediatas al desconectar/conectar el USB.
- **Documentación Contextual**: Sistema de ayuda interno integrado que detecta la pantalla actual y filtra contenido por rol de usuario.
- **Estética "Glassmorphism"**: Nueva interfaz de asistencia con fondos dinámicos y paneles de cristal ultra-legibles.
- **Reactivación Manual**: Botón de reintento (🔄) para reinicializar el lector sin cerrar la aplicación.

### 🛠️ Correcciones Técnicas
- Implementada detección de hardware vía `ftrScanGetNumberOfDevices` (evita conflictos de handles).
- Corregida legibilidad en Modo Claro mediante contraste dinámico y sombras sutiles.
- Filtrado de secciones sensibles en documentación para el rol de Capital Humano.

---

## [v2.0.0] — 2026-02-25 — Multi-huella y Configuración de BD

### ✅ Nuevas Características
- **Refactor Arquitectural**: Reorganización total a Arquitectura Orientada a Funciones (`Core`, `Shared`, `Features`).
- **Multi-finger enrollment**: Registro de hasta 10 dedos con selector visual de manos.
- **Feedback en tiempo real**: Mensajes de "Coloque/Retire el dedo" integrados desde el callback del SDK.
- **Configuración de DB**: Soporte para SQLite (local), PostgreSQL y MySQL mediante ventana de configuración.
- **Auto-migración**: Detección automática de esquema desactualizado y recreación de BD para SQLite.

### 🛠️ Correcciones Técnicas
- Corregido error de tipos en `AttendanceService` al manejar tuplas de identificación.
- Corregida secuencia de escape errónea en `SimulatedFingerprintService`.
- Implementado binding manual para `PasswordBox` mediante eventos para mantener seguridad.

---

## [v1.0.0] — 2026-02-25 — Primera versión funcional

### ✅ Estado Final
- Enrollment de huellas funcional con templates biométricos del SDK
- Identificación 1:N con `FTRIdentifyN` del SDK
- Dashboard con marcaje de asistencia por huella
- Gestión de empleados (CRUD)
- Reportes de asistencia

---

## Historial de Issues y Soluciones

### Issue #1: DLL no copiada al directorio de salida
**Síntoma**: La aplicación reportaba "ftrScanAPI.dll no encontrada" al iniciar.

**Causa**: El `.csproj` usaba `<None Include="libs\ftrScanAPI.dll">` pero mantenía la estructura de carpetas en la salida (`bin/Debug/.../libs/ftrScanAPI.dll`), mientras que P/Invoke buscaba la DLL en la raíz del directorio.

**Solución**: Agregar `<Link>ftrScanAPI.dll</Link>` al item del `.csproj`:
```xml
<None Include="libs\ftrScanAPI.dll">
  <Link>ftrScanAPI.dll</Link>
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

---

### Issue #2: Dispositivo FS80H no detectado ("Device not connected")
**Síntoma**: `ftrScanOpenDevice()` retornaba `INVALID_HANDLE`. La DLL se encontraba pero el dispositivo no se podía abrir.

**Causa**: El driver USB de Futronic NO viene incluido con el SDK. Debe instalarse separadamente.

**Solución**:
1. Abrir Administrador de dispositivos de Windows
2. Ubicar el dispositivo Futronic (aparecía como dispositivo desconocido)
3. Instalar driver manualmente desde `C:\Program Files (x86)\Futronic\SDK 4.2\Drivers\`
4. Verificar que aparezca como "Futronic FS80H"

---

### Issue #3: Error 4306 en `ftrScanGetFrame`
**Síntoma**: `ftrScanGetFrame` fallaba con error SDK 4306.

**Causa**: Se estaba habilitando la opción `FTR_OPTIONS_DETECT_FAKE_FINGER` (anti-spoofing/LFD), pero el FS80H no soporta esta característica.

**Solución**: Eliminar la llamada a `ftrScanSetOptions` con `FTR_OPTIONS_DETECT_FAKE_FINGER`.

---

### Issue #4: Error Win32 87 (`ERROR_INVALID_PARAMETER`) en captura
**Síntoma**: `ftrScanGetImage` fallaba con Win32 error 87 (parámetro inválido).

**Causa**: El parámetro `nDots` de `ftrScanGetImage` estaba pasando valores incorrectos para el dispositivo FS80H.

**Solución intermedia**: Se implementó un sistema de retry con fallback:
1. Primer intento con `ftrScanGetFrame` (más bajo nivel, más confiable)
2. Fallback a `ftrScanGetImage` con `nDots = 0` y luego `nDots = 1`

> ⚠️ Esta solución fue reemplazada completamente por el Issue #6.

---

### Issue #5: NullReferenceException al capturar
**Síntoma**: La app lanzaba `NullReferenceException` durante la captura de huella.

**Causa**: Los P/Invoke declarations no tenían `SetLastError = true` y la estructura `FTRSCAN_IMAGE_SIZE` no estaba correctamente marshalada, causando que `nImageSize` fuera 0 y el buffer se creara vacío.

**Solución**: 
- Agregar `SetLastError = true` a todas las declaraciones P/Invoke
- Verificar tamaño del buffer antes de intentar captura

---

### Issue #6: Huella no reconocida — NCC correlation demasiado baja (0.25-0.33)
**Síntoma**: El empleado se registraba exitosamente pero al marcar asistencia siempre decía "Huella no reconocida". La consola mostraba:
```
FTRAPI: NCC Correlación: 0.2568 (umbral: 0.4)
FTRAPI: NCC Correlación: 0.3355 (umbral: 0.4)
```

**Causa raíz**: Se estaba usando `ftrScanAPI.dll` directamente (API de bajo nivel) para capturar frames crudos y luego comparando imágenes con correlación NCC (Normalized Cross-Correlation). Este enfoque es fundamentalmente incorrecto porque:
- Las imágenes crudas varían significativamente según la posición/presión del dedo
- NCC no maneja rotación ni traslación
- No se extraen minutiae biométricas

**Solución definitiva**: Migrar a `FTRAPI.dll` (API de alto nivel del SDK):

1. **Enrollment** (registro de huella):
   - `FTRInitialize()` → inicializa el SDK
   - `FTRSetParam(FTR_PARAM_CB_FRAME_SOURCE, FSD_FUTRONIC_USB)` → conecta al USB
   - `FTRSetParam(FTR_PARAM_CB_CONTROL, callback)` → registra callback para UI
   - `FTREnrollX(FTR_PURPOSE_ENROLL)` → captura múltiples frames, extrae minutiae, genera template biométrico

2. **Identificación** (marcar asistencia):
   - `FTREnrollX(FTR_PURPOSE_IDENTIFY)` → captura y crea template base
   - `FTRSetBaseTemplate()` → establece el template base
   - `FTRIdentifyN()` → compara 1:N contra todos los templates almacenados

**Archivos modificados**:
- `Interop/FtrScanApi.cs` — P/Invoke completo para FTRAPI.dll
- `Services/FutronicService.cs` — Reescrito con enrollment y identificación del SDK
- `Services/IFingerprintService.cs` — Nuevos métodos `EnrollFingerprintAsync()` y `IdentifyFingerprintAsync()`
- `ViewModels/EmployeeFormViewModel.cs` — Usa `EnrollFingerprintAsync`
- `ViewModels/DashboardViewModel.cs` — Usa `IdentifyFingerprintAsync`
- `Services/AttendanceService.cs` — Nuevo `GetEmployeesWithTemplatesAsync()`
- `LectorHuellas.csproj` — Agrega `FTRAPI.dll` al output

**DLLs requeridas** (ambas x64 Redist):
- `ftrScanAPI.dll` — Acceso al hardware del lector
- `FTRAPI.dll` — API de alto nivel para biometría

---

### Issue #7: Lector no detectado automáticamente al desconectar
**Síntoma**: Si se desenchufaba el lector USB mientras la app estaba abierta, el estado seguía diciendo "Conectado" hasta intentar una operación.

**Causa**: La validación de estado solo ocurría al inicio o por demanda. Además, `StaticCheckPresence` intentaba abrir handles, lo cual fallaba si el SDK ya tenía el dispositivo bajo control.

**Solución**: 
- Implementar un `DispatcherTimer` en `MainViewModel` (polling cada 3s).
- Usar `ftrScanGetNumberOfDevices` para consulta directa al driver sin necesidad de abrir el dispositivo.
- Forzar actualización de propiedades en la UI mediante `OnPropertyChanged`.

---

### Issue #8: Texto ilegible en Modo Claro sobre imagen de fondo
**Síntoma**: El texto blanco del modo oscuro era invisible sobre el fondo blanco del modo claro. Algunos mensajes de estado no cambiaban de color.

**Causa**: Falta de contenedores de contraste y falta de triggers de tema en ciertos estilos de `TextBlock`.

**Solución**:
- Implementar fondos de cristal (Glassmorphism) con `Opacity` de 0.6.
- Cambiar colores de texto a negro/gris oscuro explícitamente en el tema claro.
- Usar triggers de `DynamicResource` en XAML para alternar estilos según el tema.

---

### Issue #9: Exposición de configuración técnica a perfiles no administrativos
**Síntoma**: Usuarios de Recursos Humanos podían ver detalles de conexión a base de datos en la documentación.

**Causa**: El manual era estático y contenía toda la información técnica.

**Solución**:
- Implementar `DocumentationWindow` dinámico.
- Pasar el `RolId` a la ventana de documentación.
- Filtrar bloques de `FlowDocument` programáticamente antes de mostrar la ventana.

---

## Lecciones Aprendidas

1. **Usar siempre la API de alto nivel del SDK**, no la API directa del device. `FTRAPI.dll` maneja internamente toda la complejidad de captura, extracción de minutiae y matching.

2. **Los drivers USB se instalan separadamente del SDK**. No asumir que el instalador del SDK incluye los drivers del dispositivo.

3. **Anti-spoofing (LFD) no está soportado en todos los modelos**. El FS80H no soporta `FTR_OPTIONS_DETECT_FAKE_FINGER`. Verificar las capacidades del dispositivo antes de habilitar opciones.

4. **La consola de diagnóstico fue esencial**. `AllocConsole()` + `Console.WriteLine()` fue mucho más útil que `Debug.WriteLine()` para diagnosticar problemas en tiempo real.

5. **Los SDKs de C++ para .NET no siempre son compatibles**. Los wrappers `ftrSDKHelper*.dll` son C++/CLI compilados para .NET Framework y no funcionan con .NET 8. La solución fue hacer P/Invoke directo a la DLL nativa.
