# LectorHuellas — Sistema de Asistencia por Huella Dactilar 🖐️

Sistema de control de asistencia de empleados usando el lector de huellas dactilares **Futronic FS80H** con **WPF (.NET 8)**.

## 📋 Características

- ✅ Registro de empleados con huella dactilar (enrollment biométrico SDK)
- ✅ Marcaje de asistencia por identificación de huella (1:N matching)
- ✅ Dashboard en tiempo real (presentes/ausentes hoy)
- ✅ Reportes de asistencia por rango de fechas
- ✅ Base de datos local SQLite
- ✅ Consola de diagnóstico integrada

## 🔧 Requisitos

### Software
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 o VS Code con extensión C#
- Windows 10/11 (x64)

### Hardware
- Lector de huellas **Futronic FS80H**

### SDK Futronic
Descargar e instalar el **Futronic SDK 4.2** desde:
- https://www.futronic-tech.com/ (sección de descargas)
- Ruta de instalación por defecto: `C:\Program Files (x86)\Futronic\SDK 4.2`

### Driver USB Futronic
> ⚠️ **CRÍTICO**: El driver USB debe instalarse SEPARADAMENTE del SDK.

1. Abrir **Administrador de dispositivos** de Windows
2. Buscar el dispositivo Futronic (puede aparecer como dispositivo desconocido)
3. Instalar driver desde: `C:\Program Files (x86)\Futronic\SDK 4.2\Drivers\`
4. Verificar que aparezca como **"Futronic FS80H"** en Administrador de dispositivos

## 🚀 Configuración en PC nueva

### 1. Clonar el repositorio
```bash
git clone <URL_DEL_REPO>
cd lector-huellas
```

### 2. Instalar Futronic SDK 4.2
Ejecutar el instalador del SDK.

### 3. Instalar driver USB Futronic
Seguir instrucciones de la sección "Driver USB Futronic" arriba.

### 4. Verificar DLLs
El proyecto incluye las DLLs necesarias en `src/libs/`:
- `ftrScanAPI.dll` (x64) — API de acceso al dispositivo
- `FTRAPI.dll` (x64) — API de alto nivel para enrollment, verificación e identificación

Estas se copian automáticamente al directorio de salida durante la compilación.

### 5. Compilar y ejecutar
```bash
cd src
dotnet build
dotnet run
```

## 🏗️ Arquitectura

El proyecto utiliza una **Arquitectura Orientada a Funciones (Feature-Oriented Architecture)** para mejorar la mantenibilidad y escalabilidad.

```
src/
├── Core/                # Lógica de negocio y datos fundamental
│   ├── Data/            # DBContext (Entity Framework)
│   ├── Interop/         # P/Invoke SDK Futronic
│   ├── Models/          # Entidades (Employee, AttendanceRecord)
│   └── Services/        # Lógica central (AttendanceService, FutronicService)
├── Shared/              # Recursos UI comunes
│   ├── Converters/      # Convertidores XAML
│   └── Resources/       # Estilos globales (Styles.xaml)
├── Features/            # Módulos independientes por funcionalidad (View + ViewModel)
│   ├── Dashboard/       # Pantalla principal y marcaje
│   ├── Employees/       # Gestión de personal y HandSelector
│   ├── Reports/         # Consultas de asistencia
│   ├── Settings/        # Configuración de base de datos
│   └── Main/            # Ventana principal y navegación
├── App.xaml             # Configuración global y estilos
└── MainWindow.xaml      # Contenedor principal de navegación
```

### Principios Aplicados
- **Vertical Slices**: El código de cada funcionalidad (Vista y ViewModel) vive junto.
- **Layering**: Separación clara entre el núcleo del sistema (`Core`) y la interfaz de usuario.
- **SOLID**: Mayor adhesión al principio de Responsabilidad Única.

### Flujo de Enrollment (Registro de Huella)
1. `FTRInitialize()` → inicializa el SDK
2. `FTRSetParam(FRAME_SOURCE, USB)` → conecta al lector
3. `FTREnrollX(PURPOSE_ENROLL)` → captura múltiples frames y genera template biométrico
4. Template se guarda en SQLite con el empleado

### Flujo de Identificación (Marcar Asistencia)
1. `FTREnrollX(PURPOSE_IDENTIFY)` → captura un template base del dedo
2. `FTRSetBaseTemplate()` → establece el template base
3. `FTRIdentifyN()` → compara 1:N contra todos los templates registrados
4. Se registra entrada/salida automáticamente

## 📦 Dependencias NuGet

| Paquete | Versión | Uso |
|---------|---------|-----|
| CommunityToolkit.Mvvm | 8.2.2 | Patrón MVVM |
| Microsoft.EntityFrameworkCore.Sqlite | 8.0.0 | Base de datos local |

## 🐛 Troubleshooting

Ver [CHANGELOG.md](CHANGELOG.md) para el historial detallado de issues y soluciones.

### Problemas Comunes

| Síntoma | Causa | Solución |
|---------|-------|----------|
| "Usando servicio simulado" | DLLs no encontradas | Verificar `libs/ftrScanAPI.dll` y `libs/FTRAPI.dll` |
| "Dispositivo no conectado" | Driver USB no instalado | Instalar driver desde SDK |
| "Huella no reconocida" | Templates incompatibles | Re-registrar huellas del empleado |
| Error 4306 en consola | Anti-spoofing no soportado | Ya corregido (se deshabilitó) |

## 📄 Licencia

Proyecto privado. SDK sujeto a licencia de Futronic Technology Company Ltd.
