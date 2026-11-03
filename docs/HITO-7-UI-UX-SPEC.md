# 🏛️ Especificación de Diseño UI/UX: PergamonLens (Hito 7)

- **Documento:** `docs/HITO-7-UI-UX-SPEC.md`
- **Hito:** **Hito 7: Rediseño UI/UX Premium & Experiencia Móvil**
- **Autores:** Diego M. Aragón Garrido & G (Pair Programming)
- **Estado:** **Aprobado para implementación**

---

## 1. Visión y Principios de Diseño

PergamonLens no es un catálogo plano de base de datos; es una **bóveda digital privada de preservación física**. La interfaz debe transmitir sofisticación, reverencia por el soporte tangible y cero fricción en la interacción.

### Principios Rectores:
1. **Fricción Cero (*The 3-Second Rule*):** El usuario sostiene el soporte físico en una mano y el teléfono en la otra. El 95% de las acciones deben poder realizarse con **un solo toque del pulgar** sin escribir texto.
2. **Ergonomía Mobile-First (*The Thumb Zone*):** Los controles críticos (disparador de cámara, tabs principales, guardado, selector de estado) se ubican estrictamente en el **tercio inferior de la pantalla**.
3. **Arquitectura Visual Extensible y Polimórfica:** La UI está diseñada conceptualmente para soportar colecciones multiformato:
   - **Música (MVP actual):** Vinilos (fundas cuadradas con borde de acetato), CDs (caja transparente jewel case con brillo holográfico), Casetes.
   - **Libros (Próxima fase):** Formato vertical 2:3, aspecto de lomo y tapa dura/blanda, metadatos de ISBN y editorial.
   - **Videojuegos (Próxima fase):** Cajas de consola, cartuchos/discos, metadatos de plataforma y desarrollador.
4. **Tecnología sin Bloatware:** Construido sobre **Vanilla JavaScript moderno (ES6+) y CSS3 nativo**, sin frameworks de terceros, garantizando carga instantánea (<50 ms) y compatibilidad superior al 98% en navegadores móviles.

---

## 2. Mapa de Navegación y Arquitectura de Vistas

```mermaid
flowchart TD
    A["Estado de Autenticación"] -->|Sin Token / No Auth| B["Pilar 1: Welcome & Sign-In Screen<br/>(Pantalla completa inmersiva)"]
    A -->|Sesión Válida| C["Pilar 2: Bóveda de Colección<br/>(Grid Crate Digging & Filtros)"]
    
    C -->|Botón Cámara (Thumb FAB)| D["Pilar 3: Escáner Inmersivo<br/>(Visor WebRTC + Selector Formato)"]
    D -->|Foto Tomada| E["Feedback Dinámico de IA<br/>(Analizando surcos con Gemini...)"]
    E -->|Coincidencia Discogs| F["Confirmación Rápida<br/>(Calificación Goldmine en 1 toque)"]
    F -->|Guardar| C
    
    C -->|Tap en Disco/Tarjeta| G["Pilar 4: Bottom Sheet Deslizante<br/>(Ficha Técnica, Tracklist & Notas)"]
```

---

## 3. Especificación Detallada de los 4 Pilares

### Pilar 1: Pantalla de Bienvenida & Acceso (*Welcome & Vault Door*)
- **Contexto:** Se renderiza si `state.token == null`. Oculta por completo la barra de navegación y la bóveda para evitar mostrar esqueletos vacíos.
- **Visual:**
  - Fondo profundo `#0b0f17` con halo radial dorado tenue y emblema 🏛️ centrado.
  - Título en tipografía con personalidad y claim de autor: *"Tu bóveda visual de coleccionismo físico con IA multimodal"*.
- **Controles:**
  - Selector táctil de pastilla (*Pill Tabs*): **[ Iniciar Sesión ]** | **[ Crear Cuenta ]**.
  - Campo Email con icono ✉️ y validación nativa.
  - Campo Contraseña con icono 🔒 y botón interactivo `👁️` para alternar visibilidad.
  - Botón principal de acción con resplandor dorado: **[ Abrir mi Bóveda ➔ ]** (con spinner dinámico integrado durante la petición).
  - Bloque de errores inline sutil (sin alerts del navegador).

---

### Pilar 2: La Bóveda (*The Collector's Vault*)
- **Contexto:** Vista principal de colección del usuario autenticado.
- **Header & Filtros Polimórficos:**
  - Emblema compacto de PergamonLens + Avatar/Email del usuario con botón discreto de salir.
  - **Selector de Categorías Superior:**
    - `[ 🎵 Música ]` *(activo)* $\rightarrow$ Despliega subfiltros: *Todos*, *Vinilos*, *CDs*, *Casetes*.
    - `[ 📚 Libros ]` *(preparado para Bookland ISBN)*.
    - `[ 🎮 Videojuegos ]` *(preparado para plataformas)*.
  - **Barra de Búsqueda Rápida:** Con debounce de 300 ms para filtrar en vivo por artista, título o sello.
- **Grid de Colección (*Crate Digging Style*):**
  - Tarjetas visuales adaptadas a cada soporte:
    - **Vinilo:** Funda cuadrada con simulación de disco de vinilo negro asomando sutilmente por el borde derecho con ranuras concéntricas.
    - **CD:** Proporción cuadrada con marco sutil simulando estuche de plástico *jewel case* y badge cian.
  - Badge de condición Goldmine visible (*Mint, NM, VG+*).
  - Título y artista en tipografía legible, año y pistas.

---

### Pilar 3: El Flujo de Escaneo Rápido (*The Lens / Zero-Friction Scan*)
- **Contexto:** El momento clímax de la aplicación.
- **Visor de Cámara WebRTC:**
  - Ocupa la pantalla completa o modal inmersivo con guías de encuadre en las esquinas.
  - Selector rápido de medio físico en la parte inferior:
    `[ Auto (IA) ]` | `[ Vinilo ]` | `[ CD ]` | `[ Libro ]`
  - Disparador circular grande en la zona del pulgar con botón para alternar cámara trasera/frontal.
  - Alternativa para subir foto desde la galería/archivos del móvil.
- **Transición de Reconocimiento:**
  - Al capturar la foto, la cámara muestra un overlay translúcido con animación suave de escaneo:
    *"Extrayendo metadatos visuales con Gemini Flash..."* $\rightarrow$ *"Consultando catálogo canónico de Discogs..."*.
- **Panel de Confirmación de 1 Toque:**
  - En cuanto Discogs responde (auto-enriquecimiento Caso 1 o Caso 2):
    - Portada oficial en alta resolución.
    - Título, artista y soporte físico auto-detectado (CD / Vinilo).
    - **Selector táctil rápido de condición Goldmine:**
      `[ M ]` `[ NM ]` `[ VG+ ]` `[ VG ]` `[ G+ ]` `[ Fair ]`
    - Botón de guardado instantáneo: **[ Preservar en Bóveda ]**.

---

### Pilar 4: Ficha de Detalle (*Bottom Sheet* Deslizante) y Navegación Móvil
- **Adiós a los modales toscos centrados:**
  - Al pulsar una tarjeta en la Bóveda, emerge un panel desde la parte inferior (*Bottom Sheet*) que cubre el 85% de la pantalla.
  - Barra superior de arrastre (*drag handle*) que permite cerrarlo deslizando el pulgar hacia abajo.
  - Visualización completa:
    - Portada a gran tamaño con zoom sutil.
    - Metadatos canónicos: Sello, Año, País, Código de barras, Número de catálogo.
    - Tabla limpia y contrastada con el tracklist oficial (posición, título, duración).
    - Notas del coleccionista y botón para re-calificar condición Goldmine o consultar en Discogs.
- **Mobile Bottom Navigation Bar (Fija abajo):**
  - 🏛️ **Bóveda** (Colección de álbumes).
  - 📷 **Escanear** (Botón circular flotante central, elevado y dorado, accesible con una mano).
  - 📊 **Estadísticas** (Resumen de lanzamientos, desglose por formatos y valor).

---

## 4. Plan de Implementación por Subfases (Hito 7)

| Subfase | Alcance Técnico | Entregables |
| :--- | :--- | :--- |
| **7.1** | **Pantalla de Bienvenida & Auth Flow** | Implementación de `#welcomeView`, tabs login/register, toggle password, transiciones y gestión de sesión. |
| **7.2** | **Navegación Móvil & Filtros Polimórficos** | *Bottom Navigation Bar*, tabs de categoría (Música, Libros, Videojuegos) y selector de soportes. |
| **7.3** | **Grid Crate Digging & Tarjetas de Soporte** | Estilo visual de vinilos con ranuras, jewel cases para CDs y tarjetas polimórficas. |
| **7.4** | **Escáner Inmersivo & Bottom Sheet** | Feedback dinámico de escaneo, confirmación Goldmine de 1 toque y panel deslizable de detalle. |
