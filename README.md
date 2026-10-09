# 🏛️ PergamonLens
> **AI-Powered Visual Catalog & Vault for Physical Media Collectors**  
> *Escanear. Identificar. Preservar.*

---

## 📖 El Origen: ¿Por qué PergamonLens?

Históricamente, la **Biblioteca de Pérgamo** fue la gran rival de la de Alejandría. Cuando Egipto prohibió la exportación de papiro para frenar su crecimiento, los sabios de Pérgamo inventaron el **pergamino**, permitiendo que miles de obras maestras sobrevivieran a los siglos.

**PergamonLens** rinde homenaje a ese espíritu de preservación: una plataforma moderna diseñada para coleccionistas de formato físico (**vinilos, CDs, libros y videojuegos**) que permite catalogar, enriquecer y custodiar colecciones enteras con tan solo **hacer una fotografía**.

---

## 🎯 El Problema y la Solución

### El Dolor del Coleccionista
Catalogar colecciones físicas es tradicionalmente lento y tedioso: teclear códigos de barras diminutos, transcribir números de matriz grabados en el vinilo o rellenar fichas a mano. Muchas aplicaciones existentes son lentas, no soportan múltiples formatos o carecen de metadatos fiables.

### La Solución PergamonLens
1. **Fotografía Rápida:** Tomas una foto de la portada, lomo o código de barras desde el móvil o navegador.
2. **Extracción Visual con IA:** Un modelo de visión multimodal identifica título, artista/autor, edición, sello discográfico o editorial.
3. **Enriquecimiento Asíncrono con APIs Especializadas:** El sistema cruza los datos automáticamente con bases de datos canónicas:
   - 🎵 **Música:** Discogs API & MusicBrainz.
   - 📚 **Libros:** Open Library & Google Books API.
   - 🎮 **Videojuegos:** IGDB (Twitch) & RAWG API.
4. **Bóveda Personal:** Tu colección organizada con estadísticas, valoración estimada, búsqueda instantánea y filtros avanzados.

---

## 🏗️ Arquitectura Técnica

El proyecto está diseñado siguiendo las mejores prácticas de ingeniería de software en **.NET moderno (C# / .NET 10)**, aplicando principios de **Clean Architecture**, alta cohesión y bajo acoplamiento:

```text
PergamonLens/
├── src/
│   ├── PergamonLens.Domain/          # Entidades puras, Value Objects, eventos de dominio
│   ├── PergamonLens.Application/     # Casos de uso (CQRS / MediatR), validaciones, DTOs
│   ├── PergamonLens.Infrastructure/  # EF Core, SQL Server, APIs externas (Discogs/IGDB), IA
│   └── PergamonLens.Web/             # ASP.NET Core Web API + Frontend Web Responsive
├── tests/
│   ├── PergamonLens.UnitTests/       # Pruebas unitarias de dominio y casos de uso
│   └── PergamonLens.IntegrationTests/# Pruebas de integración con Testcontainers
└── docker/
    └── docker-compose.yml            # Orquestación de App + SQL Server
```

### 🧱 Principios de Diseño
- **Clean Architecture & DDD:** Reglas de negocio independientes de frameworks, bases de datos e interfaces externas.
- **Concurrencia & Asincronía con Channels:** Procesamiento en segundo plano de imágenes e indexación mediante `System.Threading.Channels` para garantizar tiempos de respuesta inmediatos al usuario.
- **Resiliencia & Rate Limiting (Polly):** Control estricto de cuotas y políticas de reintento exponencial contra APIs externas que imponen límites de peticiones (ej. Discogs con 60 req/min).
- **Persistencia Robusta:** **Microsoft SQL Server** con **Entity Framework Core**, migraciones automatizadas e índices optimizados para búsqueda de colecciones.

---

## 🛠️ Stack Tecnológico

| Capa | Tecnología | Propósito |
| :--- | :--- | :--- |
| **Backend Core** | **.NET 10 / C#** | Alto rendimiento, sintaxis moderna y tipado estricto. |
| **Base de Datos** | **Microsoft SQL Server** | Almacenamiento relacional transaccional y robusto. |
| **ORM & Migraciones** | **Entity Framework Core** | Mapeo objeto-relacional y control de esquemas. |
| **Motor de Visión / IA** | **Multimodal Vision API** | Extracción de metadatos de imágenes de portadas y lomos. |
| **APIs Externas** | **Discogs, OpenLibrary, IGDB** | Enriquecimiento canónico de música, libros y videojuegos. |
| **Frontend Web** | **Web Responsive / PWA** | Interfaz optimizada para móvil con acceso directo a cámara. |
| **Testing** | **xUnit, FluentAssertions, Moq** | Cobertura exhaustiva de lógica de negocio y casos de uso. |
| **Contenedores** | **Docker & Docker Compose** | Entorno de desarrollo reproducible con un solo comando. |

---

## 🚀 Hoja de Ruta del MVP (Fase 1)

- [ ] **Hito 1: Core de Dominio y BBDD**
  - Entidades de Colección, Ítems de Música (Vinilo/CD), Metadatos y Estados de Procesamiento.
  - Configuración de EF Core y migraciones sobre SQL Server.
- [ ] **Hito 2: Integración de Visión & Pipeline Asíncrono**
  - Servicio de ingesta de imágenes.
  - Extractor visual multimodal (reconocimiento de texto y portadas).
  - Integración inicial con Discogs API (búsqueda y enriquecimiento de vinilos).
- [ ] **Hito 3: API & Interfaz Móvil**
  - Endpoints RESTful para escaneo, confirmación y consulta de colección.
  - Frontend web con botón de captura de cámara nativa móvil.
- [ ] **Hito 4: Docker & CI/CD**
  - `docker-compose.yml` para levantar API + SQL Server.
  - Pipeline de GitHub Actions para ejecución automática de tests en cada Pull Request.

---

## 💻 Inicio Rápido en Local

### Requisitos Previos
- [.NET 10 SDK](https://dotnet.microsoft.com/) (o .NET 8 SDK).
- [Docker Desktop](https://www.docker.com/) (para levantar SQL Server sin instalaciones adicionales).

### Pasos
1. Clonar el repositorio:
   ```bash
   git clone https://github.com/tu-usuario/PergamonLens.git
   cd PergamonLens
   ```
2. Levantar la base de datos SQL Server:
   ```bash
   docker compose up -d sqlserver
   ```
3. Ejecutar las migraciones y la aplicación:
   ```bash
   dotnet run --project src/PergamonLens.Web
   ```

---

## 📄 Licencia
Distribuido bajo la licencia MIT. Consulta el archivo `LICENSE` para más información.
