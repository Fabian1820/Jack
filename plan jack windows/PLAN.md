# Plan: Jack para Windows con el diseño «agente en curso»

Objetivo: que la ventana de Jack para Windows (`Windows/Jack.WinUI`) se vea **igual** que la referencia `referencia/Main.dc.html` (captura: `referencia/referencia.png`, 1280×860, tema oscuro).

Solo modo Normal, solo `Windows/`. No se toca la app de macOS ni Light.

## 1. Referencia

| Archivo | Qué es |
|---|---|
| `referencia/Main.dc.html` | Maqueta. Los valores exactos están en sus `style="…"` y en el bloque `<style>`. |
| `referencia/referencia.png` | Captura de la maqueta para comparar (generada con Chrome headless, 1280×860). |
| `referencia/README.md`, `support.js`, `vendor/` | Runtime del exportador. No forman parte del diseño. |

Para volver a verla: `cd referencia && python3 -m http.server 8765` y abrir `http://localhost:8765/Main.dc.html`.

## 2. Qué cambia respecto a hoy

La referencia es un Fluent oscuro **plano**: un solo fondo, un acento cian y dos superficies. La app actual es lo contrario: lienzo con aurora, paneles de cristal, acento naranja con halo, fichas de color por proveedor.

| Tema | Hoy (`App.xaml`, `MainWindow.xaml`, `Look.cs`) | Referencia |
|---|---|---|
| Fondo | `#0B0D14` + aurora (halos en `BuildAurora`) | `#14161B` plano |
| Paneles | Cristal `#0DFFFFFF`, radio 18, margen 12/6 | Barra lateral **sin** panel; panel principal `#1E2128`, borde `#14FFFFFF`, radio 8, margen `0 8 8 0` |
| Acento | Degradado naranja `#FFB15C→#FF5A1F` con halo | Cian plano `#4CC2FF`, texto sobre él `#06131C` |
| Barra de título | 48 px, logo con degradado | 40 px, logo 18×18 radio 5, «J» 11 px, texto 12 px atenuado |
| Barra lateral | 300 px, contador de agentes, `ListView` con fichas de proveedor | 272 px, grupo de carpeta, filas con spinner/punto y subtítulo |
| Cabecera de chat | Ficha 42 px, título 18 px, ruta | 60 px, título 15 px semibold, `proyecto · proveedor · estado`, botón «Detener» |
| Mensajes | Agente con ficha y nombre; usuario con degradado naranja | Agente sin ficha ni nombre (texto plano); usuario en burbuja gris `#17FFFFFF` |
| Herramientas | Filas sueltas / tarjeta de diff | Una tarjeta plegable `#23262E` con resumen, texto y filas de archivos |
| Compositor | Texto + fila con proveedor y carpeta | Caja `#262A33` con borde inferior de acento, adjuntar, 3 píldoras, botón redondo de enviar/detener |
| Barra de estado | 32 px abajo | **No existe** |
| Tipografía | Predeterminada | Segoe UI Variable Text 14/1.45; mono Cascadia Code 12 |

## 3. Tokens (copiar tal cual)

**Color**

| Token | Valor | Uso |
|---|---|---|
| `JackCanvasBrush` | `#14161B` | Fondo de ventana y barra lateral |
| `JackSurfaceBrush` | `#1E2128` | Panel principal |
| `JackCardBrush` | `#23262E` | Tarjeta de herramientas |
| `JackComposerBrush` | `#262A33` | Compositor |
| `JackText` | `#E9EBF0` | Texto |
| `JackMuted` | `#A3A9B5` | Secundario, placeholder, etiquetas |
| `JackAccent` | `#4CC2FF` | Acento (en la maqueta es una propiedad `accent`) |
| `JackAccentInk` | `#06131C` | Texto sobre el acento |
| `JackSuccess` | `#6CCB8A` | Check de herramienta hecha |
| `JackIdleDot` | `#6B7280` | Punto de agente inactivo |
| Hover | blanco 8 % (`#14FFFFFF`) en botones; 5 % (`#0DFFFFFF`) en filas |
| Fila seleccionada | blanco 8 % |
| Búsqueda | fondo blanco 6 %, borde inferior blanco 25 % |
| Borde panel / compositor / píldora / «Detener» | blanco 8 % / 12 % / 12 % / 14 % |
| Separador cabecera | blanco 7 % |
| Burbuja de usuario | blanco 9 % |
| Borde tarjeta | blanco 9 % |

**Medidas (px)**: barra de título 40 · botones de ventana 46×40 · barra lateral 272 (padding `4 12 12 12`, separación 8) · botón/campo estándar 36 alto, radio 8 · «Nuevo agente» 40 alto · fila de agente padding `8 10`, indicador de selección 3 de ancho · cabecera 60 · columna de mensajes máx. 760, padding `24 24 8`, separación 18 · tarjeta radio 12, padding 6 · fila de archivo min. 36 · compositor radio 12, padding `10 10 8`, borde inferior 2 · píldoras radio 999, padding `0 12`, 13 px · botón de enviar 36 redondo · burbuja de usuario radio `16 16 4 16`, padding `10 14` · iconos 16×16, trazo 1,6 redondeado.

**Texto**: base 14 · cabecera 15 semibold · subtítulos y etiquetas 12 · pie del compositor 11 · mono 12 · etiqueta de grupo 12 semibold atenuado.

## 4. Fases

Cada fase deja la app compilando y con captura comparable.

### Fase 1 · Tokens y lienzo (`App.xaml`, `Look.cs`, `MainWindow.xaml.cs`)
- Sustituir los diccionarios de colores por los de la sección 3 (Default y Dark idénticos). Mantener un Light coherente (mismo esquema, claros) o declarar que la referencia solo cubre oscuro y dejar Light como está hasta tener maqueta.
- Quitar `Aurora`, `BuildAurora`, `Look.Glow` y el título con degradado.
- Reemplazar `JackAccentBrush` (degradado) y el estilo `JackCta` por un acento sólido. Revisar todos los usos con `grep JackAccentBrush JackCta`.
- Fuente: `FontFamily="Segoe UI Variable Text"` en `Root`; `Cascadia Code, Consolas` para mono.
- Barra de título: 40 px, logo 18×18, botones de ventana a 46×40 (`TitleBarHeightOption.Standard`, botones transparentes con hover blanco 8 %).

### Fase 2 · Barra lateral
- Quitar el panel de cristal: la barra va sobre el lienzo, 272 px.
- «Nuevo agente»: botón de acento 40 px con icono «+», texto semibold y atajo `Ctrl N` a la derecha. Añadir el `KeyboardAccelerator` (hoy solo abre con clic).
- Campo «Buscar»: lupa + `TextBox` sin borde, fondo blanco 6 %, línea inferior. Filtra `ConversationList` por título.
- Etiqueta de grupo con icono de carpeta («Jack» en la maqueta = nombre de la carpeta del proyecto). Agrupar por carpeta con `CollectionViewSource` o encabezados manuales.
- Fila de agente: indicador de estado (spinner si trabaja, punto gris si no) + título + subtítulo `modelo · tiempo`. Seleccionada: fondo blanco 8 % y barra de acento de 3 px a la izquierda.
- Pie: botón «Ajustes» con icono de engranaje. Hoy no hay pantalla de ajustes: el botón queda visible pero sin acción hasta que exista (anotarlo en el README).
- Eliminar `AgentCount` y las fichas de proveedor de la lista.
- Por debajo de 760 px de ancho la barra lateral se oculta (`@media(max-width:760px)` de la maqueta): `VisualStateManager` con `AdaptiveTrigger MinWindowWidth=760`.

### Fase 3 · Panel principal y cabecera
- `Border` con fondo `#1E2128`, borde 1 px, radio 8, margen `0 8 8 0`; lo contiene todo (cabecera, mensajes, compositor).
- Cabecera de 60 px: título (15 semibold, recortado con elipsis) y debajo `Carpeta · Proveedor · ` + estado en acento («Trabajando…»). A la derecha, «Detener» con borde blanco 14 %; visible solo si el agente está en curso.
- Estado vacío: la maqueta no lo define. Mantener el actual pero sin anillos ni fichas de color; mismo lienzo plano y acento cian. **Pendiente de decisión con el usuario** si quiere una maqueta del estado vacío.

### Fase 4 · Mensajes y tarjeta de herramientas (`Look.cs`, plantillas de `MainWindow.xaml`)
- Columna centrada de 760 px, separación 18.
- `AgentTurn`: texto plano, sin ficha ni nombre. Admitir fragmentos en mono (`<span class="k">`): `Run` con `Cascadia Code` 12 y color muted; usar `RichTextBlock` o un campo `Inlines` en `Turn`.
- `UserTurn`: burbuja alineada a la derecha, blanco 9 %, radio `16 16 4 16`, sin negrita ni degradado.
- `ToolTurn`/`DiffTurn` → una **tarjeta agrupada** (`ToolGroupTurn`): cabecera plegable (check verde · «Explorar» · resumen atenuado · chevron), párrafo explicativo 13 px muted, etiqueta «Editar · N archivos» y filas (`✓` verde o spinner de acento · nombre mono · acción a la derecha, y «Trabajando…» en acento si está en curso). El `Turn` actual es plano: hace falta un modelo de grupo (`ToolGroup { Header, Summary, Note, Files[] }`).
- Spinner: arco de 16 px que gira a 1 s lineal (`ProgressRing` pequeño con el color del acento, o `Path` animado). Hay que probar que no consuma CPU en reposo.
- Decidir qué pasa con el visor de diff actual: la maqueta no lo muestra. Propuesta: conservarlo dentro de la fila de archivo al desplegarla.

### Fase 5 · Compositor
- Caja `#262A33`, radio 12, borde 1 px blanco 12 % y **línea inferior de 2 px de acento**, máx. 760, padding `10 10 8`.
- `TextBox` de 2 filas, sin borde ni fondo, placeholder «Escribe un mensaje…».
- Fila inferior: adjuntar (36×36, icono de clip) · píldoras «Sonnet 5.5 ˅», «Esfuerzo · Alto ˅», «Permisos · Auto ˅» (`DropDownButton` con plantilla de píldora y `MenuFlyout`) · espacio · botón redondo de acento: cuadrado (detener) si hay agente en curso, flecha (enviar) si no.
- Pie centrado de 11 px: «Intro envía · Mayús+Intro nueva línea».
- Hoy el compositor está desactivado (`IsEnabled="False"`, sin CLI). Ver riesgos.

### Fase 6 · Retirar lo que sobra
- Barra de estado inferior (`StatusText`, `StatusPath`, fila 32 px) y `ReadyCard`.
- Código muerto de la fase 1 (aurora, brillo, respiración de anillos si se eliminan).
- Actualizar `Windows/README.md` y la sección de captura de `JACK_CAPTURE=demo` con la conversación de la maqueta.

### Fase 7 · Verificación
- Añadir al modo `demo` los datos de la referencia: agente «Responde solo con la palabra OK» (Sonnet 5.5, en curso), dos «Nuevo agente» (hace 6 y 17 min), el mensaje, la respuesta, la tarjeta con 5 archivos, spinner en `Composer.xaml`.
- Capturar a **1280×860** (hoy `ApplyCaptureMode` usa 1000×700; añadir un modo `JACK_CAPTURE=reference` o parametrizar el tamaño) y comparar con `referencia.png` lado a lado y superpuesto (diferencia de píxeles con una tolerancia pequeña por el antialiasing de fuentes).
- Probar en la VM de Windows 11 ARM (`/Volumes/Brian/os/jack-win/`; pantalla fija 800×600, así que se verá en modo estrecho sin barra lateral) y en el workflow «Jack Windows» de CI.
- Revisar: foco con Tab, contraste (el muted `#A3A9B5` sobre `#1E2128` ≈ 6:1, correcto), tema claro, escalado 125/150 %, redimensionado.

## 5. Archivos a tocar

`Windows/Jack.WinUI/App.xaml` · `MainWindow.xaml` · `MainWindow.xaml.cs` · `Look.cs` · `Windows/README.md`. Probable archivo nuevo: `Windows/Jack.WinUI/Icons.xaml` (los 12 iconos de la maqueta como `PathIcon`/`Geometry`; todos son trazos de 16×16 y se copian del SVG).

Iconos usados: minimizar, maximizar, cerrar, más, lupa, carpeta, check, chevron, adjuntar, detener, engranaje, spinner.

## 6. Riesgos y decisiones abiertas

1. **La maqueta muestra funciones que la app no tiene**: modelo, esfuerzo, permisos, «Detener», estado «Trabajando…», agrupación por carpeta, tarjeta de herramientas con archivos. `Jack.Core` no se conecta todavía a ninguna CLI. Este plan entrega **el aspecto**; las píldoras y botones se muestran con datos de ejemplo hasta que exista la conexión. Pregunta para el usuario: ¿las píldoras son decorativas (desactivadas) o se cablean ya a valores por agente en `Jack.Core`?
2. **Tema claro**: la maqueta es solo oscura. Propuesta: Light = misma estructura con paleta invertida, sin diseño nuevo, o fijar Dark mientras no haya maqueta.
3. **Acento configurable**: la maqueta lo expone como propiedad (`#4CC2FF` por defecto). Se deja como recurso único `JackAccent` para poder cambiarlo luego; no se añade selector.
4. **Fuente**: Segoe UI Variable viene con Windows 11; en Windows 10 cae a Segoe UI. Cascadia Code solo en 11 y Terminal; si falta, Consolas (ya previsto en la maqueta).
5. **Fidelidad exacta**: WinUI no hace sombras ni desenfoques en XAML sin trucos; la maqueta no usa ninguno, así que no es problema. Las diferencias esperables son el suavizado de texto (ClearType frente a Chrome) y el alto de línea en `TextBlock`.
6. **Regresiones**: se retira el diseño aprobado en `af4fd1d` y `c95fe02` (aurora, cristal, tarjetas de agente). Confirmado por el usuario al pedir este plan; conviene hacerlo en una rama.

## 7. Criterio de «hecho»

La captura de `JACK_CAPTURE=reference` a 1280×860 es indistinguible de `referencia/referencia.png` a simple vista (mismas posiciones, colores, radios y tipografía), el flujo de crear/seleccionar agentes sigue funcionando y `dotnet run --project Windows/Jack.Core.Checks` pasa.
