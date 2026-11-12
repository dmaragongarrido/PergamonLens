/**
 * ============================================================================
 * PERGAMONLENS — Frontend Application Controller
 * Vanilla JS SPA with WebRTC camera capture & REST API client
 * ============================================================================
 */

(function () {
  'use strict';

  // Constantes de almacenamiento
  const STORAGE_TOKEN_KEY = 'pergamonlens_auth_token';
  const STORAGE_EMAIL_KEY = 'pergamonlens_user_email';

  // Estado reactivo de la aplicación
  const state = {
    token: localStorage.getItem(STORAGE_TOKEN_KEY) || null,
    userEmail: localStorage.getItem(STORAGE_EMAIL_KEY) || null,
    allReleases: [],
    releases: [],
    statistics: null,
    currentCategory: 'music', // 'music' | 'books' | 'games'
    currentFilter: 'all',
    currentGenreFilter: 'all',
    categoryScrollPositions: { music: 0, books: 0, games: 0 },
    pendingRestoreScroll: null,
    searchQuery: '',
    searchDebounceTimer: null,
    selectedScanBlob: null,
    selectedScanFileName: null,
    cameraStream: null,
    cameraFacingMode: 'environment', // Trasera por defecto para capturar álbumes/libros
    authMode: 'login', // 'login' | 'register'
    enrichCategory: 'Music', // 'Music' | 'Books' | 'VideoGames'
    catalogSearchResults: [],
    selectedBookEdition: null,
    selectedGameEdition: null
  };

  const FORMAT_LABELS = {
    Vinyl: 'Vinilo',
    CD: 'CD',
    Cassette: 'Casete',
    BoxSet: 'Box Set',
    Hardcover: 'Tapa Dura',
    Paperback: 'Rústica',
    ComicManga: 'Cómic / Manga',
    BookSpecialEdition: 'Ed. Especial',
    OpticalDiscGame: 'Disco Óptico',
    Cartridge: 'Cartucho',
    GameCollectorEdition: 'Ed. Coleccionista'
  };

  const CANONICAL_GENRES = {
    music: [
      'Rock Progresivo',
      'Rock / Alternativo',
      'Heavy Metal',
      'Jazz / Bebop',
      'Electrónica / Synth-pop',
      'Hip-Hop / Rap',
      'Clásica / BSO',
      'Soul / Funk / R&B',
      'Folk / Cantautor',
      'Pop'
    ],
    books: [
      'Ciencia Ficción',
      'Fantasía',
      'Novela Histórica',
      'Thriller / Misterio',
      'Ensayo / Filosofía',
      'Cómic / Manga',
      'Terror / Gótico',
      'Biografía / Historia',
      'Divulgación Científica',
      'Clásicos Literarios'
    ],
    games: [
      'RPG / Action RPG',
      'Acción / Aventura',
      'Survival Horror',
      'Plataformas / Metroidvania',
      'Estrategia / Táctico',
      'Shooter / FPS',
      'Lucha / Arcade',
      'JRPG',
      'Simulación / Carreras',
      'Indie / Puzzle'
    ]
  };

  function getFormatDisplayLabel(format) {
    return FORMAT_LABELS[format] || format || 'Soporte';
  }

  function isBookRelease(r) {
    if (!r) return false;
    if (r.category === 'Books') return true;
    return ['Hardcover', 'Paperback', 'ComicManga', 'BookSpecialEdition'].includes(r.format);
  }

  function isGameRelease(r) {
    if (!r) return false;
    if (r.category === 'VideoGames') return true;
    return ['OpticalDiscGame', 'Cartridge', 'GameCollectorEdition'].includes(r.format);
  }

  function getReleaseCategoryKey(r) {
    if (isBookRelease(r)) return 'books';
    if (isGameRelease(r)) return 'games';
    return 'music';
  }

  // ============================================================================
  // Cliente HTTP REST con inyección de JWT y manejo RFC 7807 ProblemDetails
  // ============================================================================
  async function apiFetch(url, options = {}) {
    options.headers = options.headers || {};

    if (state.token) {
      options.headers['Authorization'] = `Bearer ${state.token}`;
    }

    if (!(options.body instanceof FormData) && !options.headers['Content-Type']) {
      options.headers['Content-Type'] = 'application/json';
    }

    try {
      const response = await fetch(url, options);

      if (response.status === 401) {
        if (url.includes('/auth/login')) {
          throw new Error('Correo electrónico o contraseña incorrectos.');
        }
        logout(false);
        showToast('Sesión Expirada', 'Por favor inicia sesión de nuevo para acceder a tu colección.', 'error');
        throw new Error('Unauthorized');
      }

      if (response.status === 204) {
        return null; // No Content
      }

      const contentType = response.headers.get('content-type') || '';
      const isJson = contentType.includes('application/json') || contentType.includes('application/problem+json');
      const data = isJson ? await response.json() : await response.text();

      if (!response.ok) {
        let errorDetail = 'Ha ocurrido un error en la solicitud.';
        if (isJson && data) {
          if (data.errors && typeof data.errors === 'object') {
            const messages = Object.entries(data.errors)
              .map(([field, msgs]) => `${field}: ${Array.isArray(msgs) ? msgs.join(', ') : msgs}`)
              .join(' | ');
            errorDetail = messages || data.title || errorDetail;
          } else if (data.detail) {
            errorDetail = data.detail;
          } else if (data.title) {
            errorDetail = data.title;
          }
        }
        throw new Error(errorDetail);
      }

      return data;
    } catch (err) {
      if (err.message !== 'Unauthorized') {
        console.error('Error de API:', err);
      }
      throw err;
    }
  }

  // ============================================================================
  // Notificaciones Toast
  // ============================================================================
  function showToast(title, message, type = 'info') {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;

    let iconSvg = '';
    if (type === 'success') {
      iconSvg = '<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg>';
    } else if (type === 'error') {
      iconSvg = '<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>';
    } else {
      iconSvg = '<svg class="toast-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>';
    }

    toast.innerHTML = `
      ${iconSvg}
      <div class="toast-content">
        <div class="toast-title">${escapeHtml(title)}</div>
        <div class="toast-msg">${escapeHtml(message)}</div>
      </div>
    `;

    container.appendChild(toast);

    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transform = 'translateX(100%)';
      toast.style.transition = 'all 0.3s ease';
      setTimeout(() => toast.remove(), 300);
    }, 4500);
  }

  function escapeHtml(text) {
    if (!text) return '';
    return String(text)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  // ============================================================================
  // Pilar 1: Pantalla de Bienvenida & Auth Flow
  // ============================================================================
  function switchWelcomeTab(mode) {
    state.authMode = mode;
    const tabLogin = document.getElementById('tabWelcomeLogin');
    const tabRegister = document.getElementById('tabWelcomeRegister');
    const submitBtnText = document.getElementById('btnWelcomeSubmitText');
    const passwordHelp = document.getElementById('welcomePasswordHelp');
    const errorContainer = document.getElementById('welcomeError');

    if (errorContainer) errorContainer.style.display = 'none';

    if (mode === 'login') {
      if (tabLogin) tabLogin.classList.add('active');
      if (tabRegister) tabRegister.classList.remove('active');
      if (submitBtnText) submitBtnText.textContent = 'Abrir mi Bóveda ➔';
      if (passwordHelp) passwordHelp.style.display = 'none';
    } else {
      if (tabRegister) tabRegister.classList.add('active');
      if (tabLogin) tabLogin.classList.remove('active');
      if (submitBtnText) submitBtnText.textContent = 'Crear Cuenta de Coleccionista ➔';
      if (passwordHelp) passwordHelp.style.display = 'block';
    }
  }

  function toggleWelcomePasswordVisibility() {
    const input = document.getElementById('welcomePassword');
    const iconOpen = document.getElementById('iconEyeOpen');
    const iconClosed = document.getElementById('iconEyeClosed');
    if (!input) return;

    if (input.type === 'password') {
      input.type = 'text';
      if (iconOpen) iconOpen.style.display = 'none';
      if (iconClosed) iconClosed.style.display = 'block';
    } else {
      input.type = 'password';
      if (iconOpen) iconOpen.style.display = 'block';
      if (iconClosed) iconClosed.style.display = 'none';
    }
  }

  function showWelcomeError(message) {
    const container = document.getElementById('welcomeError');
    const textEl = document.getElementById('welcomeErrorText');
    if (container && textEl) {
      textEl.textContent = message;
      container.style.display = 'flex';
    }
  }

  function transitionToVault() {
    const welcomeView = document.getElementById('welcomeView');
    const vaultApp = document.getElementById('vaultApp');
    const authModal = document.getElementById('authModal');

    if (authModal) authModal.classList.remove('active');
    if (welcomeView) {
      welcomeView.classList.remove('fade-out');
      welcomeView.style.display = 'none';
    }
    if (vaultApp) {
      vaultApp.style.display = 'flex';
      vaultApp.classList.add('fade-in');
      setTimeout(() => vaultApp.classList.remove('fade-in'), 400);
    }
    renderAuthStatus();
  }

  function transitionToWelcome() {
    const welcomeView = document.getElementById('welcomeView');
    const vaultApp = document.getElementById('vaultApp');

    if (vaultApp) vaultApp.style.display = 'none';
    if (welcomeView) {
      welcomeView.style.display = 'flex';
      welcomeView.classList.remove('fade-out');
    }
    const pwdInput = document.getElementById('welcomePassword');
    if (pwdInput) {
      pwdInput.value = '';
      pwdInput.type = 'password';
    }
    const iconOpen = document.getElementById('iconEyeOpen');
    const iconClosed = document.getElementById('iconEyeClosed');
    if (iconOpen) iconOpen.style.display = 'block';
    if (iconClosed) iconClosed.style.display = 'none';

    const err = document.getElementById('welcomeError');
    if (err) err.style.display = 'none';

    switchWelcomeTab('login');
  }

  async function handleWelcomeSubmit(e) {
    e.preventDefault();
    const errorContainer = document.getElementById('welcomeError');
    if (errorContainer) errorContainer.style.display = 'none';

    const emailInput = document.getElementById('welcomeEmail');
    const passwordInput = document.getElementById('welcomePassword');
    const email = emailInput ? emailInput.value.trim() : '';
    const password = passwordInput ? passwordInput.value : '';

    if (!email || !password) {
      showWelcomeError('Por favor ingresa tu correo electrónico y contraseña.');
      return;
    }

    if (state.authMode === 'register' && password.length < 6) {
      showWelcomeError('La contraseña debe contener al menos 6 caracteres.');
      return;
    }

    const submitBtn = document.getElementById('btnWelcomeSubmit');
    const submitSpinner = document.getElementById('btnWelcomeSubmitSpinner');
    const submitText = document.getElementById('btnWelcomeSubmitText');

    if (submitBtn) submitBtn.disabled = true;
    if (submitSpinner) submitSpinner.style.display = 'inline-block';
    if (submitText) {
      submitText.textContent = state.authMode === 'register'
        ? 'Creando cuenta de coleccionista...'
        : 'Verificando credenciales...';
    }

    try {
      if (state.authMode === 'register') {
        await apiFetch('/auth/register', {
          method: 'POST',
          body: JSON.stringify({ email, password })
        });
        showToast('Cuenta Creada', 'Registro completado con éxito. Iniciando sesión...', 'success');
      }

      const loginResult = await apiFetch('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password })
      });

      if (loginResult && loginResult.accessToken) {
        state.token = loginResult.accessToken;
        state.userEmail = email;
        localStorage.setItem(STORAGE_TOKEN_KEY, state.token);
        localStorage.setItem(STORAGE_EMAIL_KEY, state.userEmail);

        transitionToVault();
        showToast('Bienvenido a PergamonLens', `Sesión iniciada como ${email}`, 'success');

        await refreshAll();
      } else {
        throw new Error('No se recibió el token de acceso del servidor.');
      }
    } catch (err) {
      console.error('Error de autenticación:', err);
      showWelcomeError(err.message || 'Error al autenticar. Revisa tus credenciales.');
    } finally {
      if (submitBtn) submitBtn.disabled = false;
      if (submitSpinner) submitSpinner.style.display = 'none';
      if (submitText) {
        submitText.textContent = state.authMode === 'register'
          ? 'Crear Cuenta de Coleccionista ➔'
          : 'Abrir mi Bóveda ➔';
      }
    }
  }

  // ============================================================================
  // Autenticación de Usuario (Badge compacto sin desbordar en móvil)
  // ============================================================================
  function renderAuthStatus() {
    const container = document.getElementById('authStatusContainer');
    if (!container) return;

    if (state.token && state.userEmail) {
      const alias = state.userEmail.includes('@')
        ? state.userEmail.split('@')[0]
        : state.userEmail;

      container.innerHTML = `
        <div class="user-profile-badge" title="${escapeHtml(state.userEmail)}">
          <svg class="user-profile-icon" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
            <circle cx="12" cy="7" r="4"></circle>
          </svg>
          <span class="user-email-text">${escapeHtml(alias)}</span>
          <button type="button" class="btn-logout-pill" onclick="app.logout(true)" title="Cerrar Sesión">
            <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
              <polyline points="16 17 21 12 16 7"></polyline>
              <line x1="21" y1="12" x2="9" y2="12"></line>
            </svg>
            <span>Salir</span>
          </button>
        </div>
      `;
    } else {
      container.innerHTML = `
        <button class="btn btn-outline-gold btn-sm" onclick="app.openAuthModal('login')">
          Iniciar Sesión
        </button>
      `;
    }
  }

  function openAuthModal(mode = 'login') {
    state.authMode = mode;
    switchAuthTab(mode);
    const modal = document.getElementById('authModal');
    if (modal) modal.classList.add('active');
  }

  function closeAuthModal() {
    const modal = document.getElementById('authModal');
    if (modal) modal.classList.remove('active');
  }

  function switchAuthTab(mode) {
    state.authMode = mode;
    const tabLogin = document.getElementById('tabAuthLogin');
    const tabRegister = document.getElementById('tabAuthRegister');
    const modalTitle = document.getElementById('authModalTitle');
    const submitBtnText = document.getElementById('btnAuthSubmitText');
    const passwordHelp = document.getElementById('authPasswordHelp');

    if (mode === 'login') {
      tabLogin.classList.add('active');
      tabRegister.classList.remove('active');
      modalTitle.textContent = 'Iniciar Sesión de Coleccionista';
      submitBtnText.textContent = 'Iniciar Sesión';
      passwordHelp.style.display = 'none';
    } else {
      tabRegister.classList.add('active');
      tabLogin.classList.remove('active');
      modalTitle.textContent = 'Registro de Coleccionista';
      submitBtnText.textContent = 'Crear Cuenta';
      passwordHelp.style.display = 'block';
    }
  }

  async function handleAuthSubmit(e) {
    e.preventDefault();
    const email = document.getElementById('authEmail').value.trim();
    const password = document.getElementById('authPassword').value;

    if (!email || !password) {
      showToast('Campos Requeridos', 'Por favor ingresa tu correo y contraseña.', 'error');
      return;
    }

    const submitBtn = document.getElementById('btnAuthSubmit');
    submitBtn.disabled = true;

    try {
      if (state.authMode === 'register') {
        await apiFetch('/auth/register', {
          method: 'POST',
          body: JSON.stringify({ email, password })
        });
        showToast('Cuenta Creada', 'Registro completado con éxito. Iniciando sesión...', 'success');
      }

      const loginResult = await apiFetch('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password })
      });

      if (loginResult && loginResult.accessToken) {
        state.token = loginResult.accessToken;
        state.userEmail = email;
        localStorage.setItem(STORAGE_TOKEN_KEY, state.token);
        localStorage.setItem(STORAGE_EMAIL_KEY, state.userEmail);

        transitionToVault();
        closeAuthModal();
        showToast('Bienvenido a PergamonLens', `Sesión iniciada como ${email}`, 'success');

        await refreshAll();
      }
    } catch (err) {
      showToast('Error de Autenticación', err.message, 'error');
    } finally {
      submitBtn.disabled = false;
    }
  }

  function logout(showMessage = true) {
    state.token = null;
    state.userEmail = null;
    state.allReleases = [];
    state.releases = [];
    state.statistics = null;
    localStorage.removeItem(STORAGE_TOKEN_KEY);
    localStorage.removeItem(STORAGE_EMAIL_KEY);

    transitionToWelcome();
    renderStatistics(null);
    renderReleases([]);

    if (showMessage) {
      showToast('Sesión Cerrada', 'Has cerrado sesión correctamente.', 'info');
    }
  }

  // ============================================================================
  // Carga de Datos: Colección & Estadísticas Polimórficas
  // ============================================================================
  async function loadStatistics() {
    if (!state.token) return;
    try {
      const stats = await apiFetch('/api/collection/statistics');
      state.statistics = stats;
      renderStatistics(stats);
    } catch (err) {
      console.warn('No se pudieron cargar las estadísticas:', err);
    }
  }

  function updateCategoryCounters() {
    const musicItems = state.allReleases.filter(r => getReleaseCategoryKey(r) === 'music');
    const bookItems = state.allReleases.filter(r => getReleaseCategoryKey(r) === 'books');
    const gameItems = state.allReleases.filter(r => getReleaseCategoryKey(r) === 'games');

    const musicCountEl = document.getElementById('categoryMusicCount');
    if (musicCountEl) musicCountEl.textContent = String(musicItems.length);

    const booksCountEl = document.getElementById('categoryBooksCount');
    if (booksCountEl) booksCountEl.textContent = String(bookItems.length);

    const gamesCountEl = document.getElementById('categoryGamesCount');
    if (gamesCountEl) gamesCountEl.textContent = String(gameItems.length);

    const metricsBooksEl = document.getElementById('metricsBooksSummary');
    if (metricsBooksEl) {
      const totalPages = bookItems.reduce((acc, b) => acc + (b.pagesCount || 0), 0);
      metricsBooksEl.textContent = `${bookItems.length} ejemplar(es) registrados${totalPages > 0 ? ` • ${totalPages.toLocaleString('es-ES')} páginas catalogadas` : ' • Catálogo OpenLibrary & Bookland 978/979 activos'}`;
    }

    const metricsGamesEl = document.getElementById('metricsGamesSummary');
    if (metricsGamesEl) {
      metricsGamesEl.textContent = `${gameItems.length} edición(es) registradas • Graduación de caja, manual y soporte`;
    }
  }

  function renderStatistics(stats) {
    document.getElementById('statTotal').textContent = stats ? (stats.totalReleases || 0) : '0';
    document.getElementById('statVinyls').textContent = stats ? (stats.formatBreakdown?.Vinyl || 0) : '0';
    document.getElementById('statCds').textContent = stats ? (stats.formatBreakdown?.CD || 0) : '0';
    document.getElementById('statCassettes').textContent = stats ? (stats.formatBreakdown?.Cassette || 0) : '0';
    document.getElementById('statTracks').textContent = stats ? (stats.totalTracks || 0) : '0';
    document.getElementById('statConfirmed').textContent = stats ? (stats.confirmedReleases || 0) : '0';
    updateCategoryCounters();
  }

  async function loadReleases() {
    if (!state.token) {
      renderReleases([]);
      return;
    }

    const grid = document.getElementById('releasesGrid');
    const scrollStage = document.getElementById('vaultScrollStage');

    // Preservar la posición de scroll actual antes de la recarga
    if (scrollStage && state.pendingRestoreScroll === null) {
      state.pendingRestoreScroll = scrollStage.scrollTop;
    }

    const isInitialLoad = !state.allReleases || state.allReleases.length === 0;

    if (isInitialLoad && grid) {
      grid.innerHTML = `
        <div style="grid-column: 1 / -1; text-align: center; padding: 3rem; color: var(--text-secondary);">
          <div class="spinner" style="margin: 0 auto 1rem; width: 28px; height: 28px; border-width: 3px;"></div>
          Cargando bóveda...
        </div>
      `;
    } else if (grid) {
      // Bloquear altura mínima temporal para evitar que el contenedor de scroll colapse a 0 mientras la API responde
      const currentHeight = grid.offsetHeight;
      if (currentHeight > 0) {
        grid.style.minHeight = `${currentHeight}px`;
      }
    }

    try {
      const params = new URLSearchParams();
      if (state.searchQuery && state.searchQuery.trim().length > 0) {
        params.append('q', state.searchQuery.trim());
      }

      const queryString = params.toString() ? `?${params.toString()}` : '';
      const allReleases = await apiFetch(`/api/releases${queryString}`);
      state.allReleases = allReleases || [];

      if (!state.searchQuery) {
        updateCategoryCounters();
      }

      applyClientCategoryAndFormatFilter();
    } catch (err) {
      if (grid) grid.style.minHeight = '';
      grid.innerHTML = `
        <div class="vault-empty-state">
          <svg class="empty-state-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
          </svg>
          <h3>Error al conectar con la bóveda</h3>
          <p>${escapeHtml(err.message)}</p>
          <button class="btn btn-secondary" onclick="app.refreshAll()">Reintentar</button>
        </div>
      `;
    }
  }

  function renderGenreFilterOptions() {
    const select = document.getElementById('genreFilterSelect');
    if (!select) return;

    const activeCat = state.currentCategory || 'music';
    const catReleases = state.allReleases.filter(r => getReleaseCategoryKey(r) === activeCat);

    const uniqueGenresSet = new Set();
    catReleases.forEach(r => {
      if (r.genre && typeof r.genre === 'string') {
        r.genre
          .split(/[•,/|]/)
          .map(part => part.trim())
          .filter(Boolean)
          .forEach(g => uniqueGenresSet.add(g));
      }
    });

    const uniqueGenres = Array.from(uniqueGenresSet).sort((a, b) => a.localeCompare(b, 'es'));
    if (state.currentGenreFilter !== 'all' && !uniqueGenres.includes(state.currentGenreFilter)) {
      state.currentGenreFilter = 'all';
    }

    const defaultLabel = activeCat === 'books'
      ? '🎭 Todos los géneros literarios'
      : activeCat === 'games'
        ? '🎭 Todos los géneros gamer'
        : '🎭 Todos los estilos';

    select.innerHTML = [
      `<option value="all">${defaultLabel}</option>`,
      ...uniqueGenres.map(g => `<option value="${escapeHtml(g)}" ${state.currentGenreFilter === g ? 'selected' : ''}>${escapeHtml(g)}</option>`)
    ].join('');
  }

  function applyClientCategoryAndFormatFilter() {
    const activeCat = state.currentCategory || 'music';
    let filtered = state.allReleases.filter(r => getReleaseCategoryKey(r) === activeCat);

    renderGenreFilterOptions();

    if (state.currentFilter && state.currentFilter !== 'all') {
      filtered = filtered.filter(r => r.format === state.currentFilter);
    }

    if (state.currentGenreFilter && state.currentGenreFilter !== 'all') {
      const targetGenre = state.currentGenreFilter.toLowerCase();
      filtered = filtered.filter(r => {
        if (!r.genre) return false;
        const parts = r.genre.split(/[•,/|]/).map(p => p.trim().toLowerCase());
        return parts.includes(targetGenre) || r.genre.toLowerCase().includes(targetGenre);
      });
    }

    state.releases = filtered;
    renderReleases(state.releases);
  }

  function renderReleases(releases) {
    const grid = document.getElementById('releasesGrid');
    if (!grid) return;

    grid.classList.toggle('category-music', state.currentCategory === 'music');
    grid.classList.toggle('category-books', state.currentCategory === 'books');
    grid.classList.toggle('category-games', state.currentCategory === 'games');

    if (!state.token) {
      grid.innerHTML = `
        <div class="vault-empty-state">
          <div class="empty-state-icon">🏛️</div>
          <h3>Bienvenido a tu Bóveda Personal</h3>
          <p>Inicia sesión o crea una cuenta gratuita de coleccionista para empezar a catalogar y preservar tus discos, libros y videojuegos con IA.</p>
          <button class="btn btn-primary" onclick="app.openAuthModal('login')">Acceder a mi Colección</button>
        </div>
      `;
      return;
    }

    if (!releases || releases.length === 0) {
      const isBooks = state.currentCategory === 'books';
      const isGames = state.currentCategory === 'games';
      const emptyIcon = isBooks ? '📚' : isGames ? '🎮' : '💿';
      const emptyTitle = isBooks
        ? 'No se encontraron libros en tu biblioteca'
        : isGames
          ? 'No se encontraron videojuegos en tu bóveda'
          : 'No se encontraron lanzamientos';
      const emptyDesc = state.searchQuery
        ? 'Prueba a cambiar los términos de búsqueda o el filtro de formato.'
        : isBooks
          ? 'Tu bóveda bibliográfica está vacía. Escanea el código ISBN o la cubierta de tu primer libro con la cámara o añádelo manualmente.'
          : isGames
            ? 'Tu colección de videojuegos está vacía. Escanea la carátula o código de barras de tu primer juego físico o añádelo manualmente.'
            : 'Tu colección musical está vacía. Escanea tu primer soporte físico con la cámara o añádelo manualmente.';

      grid.innerHTML = `
        <div class="vault-empty-state">
          <div class="empty-state-icon" style="font-size: 2.3rem;">${emptyIcon}</div>
          <h3>${emptyTitle}</h3>
          <p>${emptyDesc}</p>
          <div style="display: flex; gap: 0.75rem;">
            <button class="btn btn-primary" onclick="app.openScannerModal()">📸 Escanear ${isBooks ? 'Libro / ISBN' : isGames ? 'Videojuego' : 'Portada'}</button>
            <button class="btn btn-secondary" onclick="app.openCreateModal()">➕ Alta Manual</button>
          </div>
        </div>
      `;
      grid.style.minHeight = '';
      const scrollStage = document.getElementById('vaultScrollStage');
      if (scrollStage && state.pendingRestoreScroll !== null) {
        scrollStage.scrollTop = state.pendingRestoreScroll;
        state.pendingRestoreScroll = null;
      }
      return;
    }

    grid.innerHTML = releases.map(release => createReleaseCardHtml(release)).join('');

    // Restauración limpia de scroll tras pintar y liberación de altura mínima
    requestAnimationFrame(() => {
      grid.style.minHeight = '';
      const scrollStage = document.getElementById('vaultScrollStage');
      if (scrollStage && state.pendingRestoreScroll !== null) {
        scrollStage.scrollTop = state.pendingRestoreScroll;
        state.pendingRestoreScroll = null;
      }
    });
  }

  function getGoldmineShortCode(condition) {
    if (!condition) return null;
    const map = {
      Mint: 'M',
      NearMint: 'NM',
      VeryGoodPlus: 'VG+',
      VeryGood: 'VG',
      GoodPlus: 'G+',
      Good: 'G',
      Fair: 'F',
      Poor: 'P'
    };
    return map[condition] || condition;
  }

  // ============================================================================
  // Tarjetas 100% Visuales (Crate Digging / Biblioteca Física)
  // ============================================================================
  function createReleaseCardHtml(r) {
    const formatClass = (r.format || 'Vinyl').toLowerCase();
    const statusClass = (r.status || 'PendingAnalysis').toLowerCase();
    const isBook = isBookRelease(r);
    const isGame = isGameRelease(r);

    const coverHtml = r.coverImageUrl
      ? `<img src="${escapeHtml(r.coverImageUrl)}" alt="${escapeHtml(r.title)}" class="cover-image" loading="lazy" onerror="if(this.src.includes('library_600x900.jpg')){this.src=this.src.replace('shared.akamai.steamstatic.com/store_item_assets/steam/apps/','cdn.akamai.steamstatic.com/steam/apps/').replace('library_600x900.jpg','header.jpg');return;}this.onerror=null; this.src=''; this.parentElement.innerHTML=app.getPlaceholderGroovesHtml('${escapeHtml(r.format)}');">`
      : getPlaceholderGroovesHtml(r.format);

    const shortCondition = getGoldmineShortCode(r.mediaCondition);
    const conditionOverlayHtml = shortCondition
      ? `<span class="goldmine-corner-badge" title="Estado de Conservación: ${escapeHtml(r.mediaCondition)}">🏅 ${escapeHtml(shortCondition)}</span>`
      : '';

    let statusLabel = r.status;
    if (r.status === 'PendingAnalysis') statusLabel = 'Pendiente IA';
    else if (r.status === 'Identified') statusLabel = 'Identificado';
    else if (r.status === 'Enriched') statusLabel = 'Enriquecido';
    else if (r.status === 'Confirmed') statusLabel = 'Confirmado';

    // Artefacto físico asomando según el soporte (Vinilo vs CD vs Libro vs Videojuego)
    let physicalArtifactHtml = '';
    if (formatClass === 'vinyl') {
      physicalArtifactHtml = `
        <div class="vinyl-disc-peek" aria-hidden="true">
          <div class="vinyl-disc-grooves">
            <div class="vinyl-disc-label"></div>
          </div>
        </div>
      `;
    } else if (formatClass === 'cd' || formatClass === 'opticaldiscgame') {
      physicalArtifactHtml = `
        <div class="cd-disc-peek" aria-hidden="true">
          <div class="cd-disc-hole"></div>
        </div>
      `;
    } else if (isBook) {
      physicalArtifactHtml = `
        <div class="book-pages-peek" aria-hidden="true"></div>
      `;
    } else if (formatClass === 'cartridge') {
      physicalArtifactHtml = `
        <div class="game-cartridge-peek" aria-hidden="true"></div>
      `;
    }

    const spineDecoratorHtml = formatClass === 'cd'
      ? '<div class="cd-spine-ribs" aria-hidden="true"></div><div class="cd-glare" aria-hidden="true"></div>'
      : isBook
        ? '<div class="book-spine-crease" aria-hidden="true"></div>'
        : isGame
          ? '<div class="game-case-glare" aria-hidden="true"></div>'
          : '<div class="sleeve-spine" aria-hidden="true"></div>';

    const badgeDisplayText = (isGame && r.platform)
      ? `🎮 ${r.platform}`
      : getFormatDisplayLabel(r.format);

    return `
      <article class="release-card crate-card pure-visual-card format-${formatClass} ${isBook ? 'book-visual-card' : ''} ${isGame ? 'game-visual-card' : ''}" data-id="${r.id}" onclick="app.openDetailModal('${r.id}')">
        <div class="crate-media-stage">
          ${physicalArtifactHtml}
          <div class="card-cover-wrapper">
            ${spineDecoratorHtml}
            ${coverHtml}
            <div class="card-badges-top">
              <span class="format-badge ${formatClass}">${escapeHtml(badgeDisplayText)}</span>
              <span class="status-badge ${statusClass}">
                <span class="status-dot"></span>
                ${escapeHtml(statusLabel)}
              </span>
            </div>
            <div class="card-cover-bottom-bar">
              <div class="card-cover-info">
                <div class="card-cover-artist" title="${escapeHtml(r.artist)}">${escapeHtml(r.artist)}</div>
                <div class="card-cover-title" title="${escapeHtml(r.title)}">${escapeHtml(r.title)}</div>
              </div>
              ${conditionOverlayHtml}
            </div>
          </div>
        </div>
      </article>
    `;
  }

  function getPlaceholderGroovesHtml(format) {
    const f = (format || '').toLowerCase();
    const isBook = f.includes('hardcover') || f.includes('paperback') || f.includes('comic') || f.includes('book');
    if (isBook) {
      return `
        <div class="vinyl-placeholder book-placeholder">
          <div style="font-size: 2.2rem; margin-bottom: 0.25rem;">📚</div>
          <div style="font-size: 0.72rem; font-weight: 700; color: var(--accent-gold); letter-spacing: 0.08em; text-transform: uppercase;">${escapeHtml(getFormatDisplayLabel(format))}</div>
        </div>
      `;
    }
    const isGame = f.includes('game') || f.includes('cartridge');
    if (isGame) {
      return `
        <div class="vinyl-placeholder game-placeholder">
          <div style="font-size: 2.2rem; margin-bottom: 0.25rem;">🎮</div>
          <div style="font-size: 0.72rem; font-weight: 700; color: var(--accent-cyan); letter-spacing: 0.08em; text-transform: uppercase;">${escapeHtml(getFormatDisplayLabel(format))}</div>
        </div>
      `;
    }
    const isVinyl = f.includes('vinyl');
    return `
      <div class="vinyl-placeholder">
        <div class="vinyl-grooves">
          <div class="vinyl-center-label">
            ${isVinyl ? 'LP' : (format || 'MED')}
          </div>
        </div>
      </div>
    `;
  }

  // ============================================================================
  // Navegación Polimórfica de Categorías (Música, Libros, Videojuegos)
  // ============================================================================
  function renderFormatFilterChips(category) {
    const formatFilters = document.getElementById('formatFilters');
    if (!formatFilters) return;

    if (category === 'books') {
      formatFilters.innerHTML = `
        <button class="chip-btn active" data-format="all" onclick="app.setFormatFilter('all')">Todos</button>
        <button class="chip-btn" data-format="Hardcover" onclick="app.setFormatFilter('Hardcover')">📕 Tapa Dura</button>
        <button class="chip-btn" data-format="Paperback" onclick="app.setFormatFilter('Paperback')">📖 Rústica / Bolsillo</button>
        <button class="chip-btn" data-format="ComicManga" onclick="app.setFormatFilter('ComicManga')">🗯️ Cómic / Manga</button>
        <button class="chip-btn" data-format="BookSpecialEdition" onclick="app.setFormatFilter('BookSpecialEdition')">✨ Ed. Especial</button>
      `;
    } else if (category === 'games') {
      formatFilters.innerHTML = `
        <button class="chip-btn active" data-format="all" onclick="app.setFormatFilter('all')">Todos</button>
        <button class="chip-btn" data-format="OpticalDiscGame" onclick="app.setFormatFilter('OpticalDiscGame')">💿 Disco Óptico</button>
        <button class="chip-btn" data-format="Cartridge" onclick="app.setFormatFilter('Cartridge')">🕹️ Cartucho / Tarjeta</button>
        <button class="chip-btn" data-format="GameCollectorEdition" onclick="app.setFormatFilter('GameCollectorEdition')">🏆 Ed. Coleccionista</button>
      `;
    } else {
      formatFilters.innerHTML = `
        <button class="chip-btn active" data-format="all" onclick="app.setFormatFilter('all')">Todos</button>
        <button class="chip-btn" data-format="Vinyl" onclick="app.setFormatFilter('Vinyl')">Vinilos</button>
        <button class="chip-btn" data-format="CD" onclick="app.setFormatFilter('CD')">CDs</button>
        <button class="chip-btn" data-format="Cassette" onclick="app.setFormatFilter('Cassette')">Casetes</button>
        <button class="chip-btn" data-format="BoxSet" onclick="app.setFormatFilter('BoxSet')">Box Sets</button>
      `;
    }
  }

  function setCategory(category) {
    if (state.currentCategory === category) return;

    const scrollStage = document.getElementById('vaultScrollStage');
    if (scrollStage && state.currentCategory) {
      state.categoryScrollPositions[state.currentCategory] = scrollStage.scrollTop;
    }

    state.currentCategory = category;
    state.currentFilter = 'all';
    state.currentGenreFilter = 'all';
    state.pendingRestoreScroll = state.categoryScrollPositions[category] || 0;

    const tabs = document.querySelectorAll('#categoryTabs .category-tab-btn');
    tabs.forEach(btn => {
      btn.classList.toggle('active', btn.dataset.category === category);
    });

    const searchInput = document.getElementById('searchInput');
    const formatFilters = document.getElementById('formatFilters');
    const grid = document.getElementById('releasesGrid');
    const comingSoon = document.getElementById('categoryComingSoonView');

    if (searchInput) {
      if (category === 'music') {
        searchInput.placeholder = 'Buscar por artista, álbum, género, sello, código de barras...';
      } else if (category === 'books') {
        searchInput.placeholder = 'Buscar por título, autor, género literario, editorial, ISBN...';
      } else if (category === 'games') {
        searchInput.placeholder = 'Buscar por título, consola (PS5, Switch...), género, estudio, EAN...';
      }
    }

    renderFormatFilterChips(category);
    if (formatFilters) formatFilters.style.display = 'flex';
    if (grid) grid.style.display = '';
    if (comingSoon) comingSoon.style.display = 'none';
    applyClientCategoryAndFormatFilter();
  }

  function switchMobileNav(section) {
    const navItems = {
      vault: document.getElementById('navBtnVault'),
      search: document.getElementById('navBtnSearch'),
      stats: document.getElementById('navBtnStats')
    };

    Object.entries(navItems).forEach(([key, el]) => {
      if (el) el.classList.toggle('active', key === section);
    });

    const catalogView = document.getElementById('vaultCatalogView');
    const metricsView = document.getElementById('vaultMetricsView');
    const scrollStage = document.getElementById('vaultScrollStage');

    if (section === 'stats') {
      if (catalogView) catalogView.style.display = 'none';
      if (metricsView) {
        metricsView.style.display = 'block';
        metricsView.scrollTo({ top: 0, behavior: 'smooth' });
      }
      return;
    }

    if (metricsView) metricsView.style.display = 'none';
    if (catalogView) catalogView.style.display = 'flex';

    if (section === 'vault') {
      if (scrollStage) scrollStage.scrollTo({ top: 0, behavior: 'smooth' });
    } else if (section === 'search') {
      const searchInput = document.getElementById('searchInput');
      if (searchInput) {
        searchInput.focus();
        searchInput.classList.add('pulse-highlight');
        setTimeout(() => searchInput.classList.remove('pulse-highlight'), 1000);
      }
    }
  }

  function setFormatFilter(format) {
    state.currentFilter = format;
    const chips = document.querySelectorAll('#formatFilters .chip-btn');
    chips.forEach(chip => {
      chip.classList.toggle('active', chip.dataset.format === format);
    });
    applyClientCategoryAndFormatFilter();
  }

  function setGenreFilter(genre) {
    state.currentGenreFilter = genre || 'all';
    applyClientCategoryAndFormatFilter();
  }

  function handleSearchInput(e) {
    clearTimeout(state.searchDebounceTimer);
    state.searchDebounceTimer = setTimeout(() => {
      state.searchQuery = e.target.value;
      loadReleases();
    }, 300);
  }

  // ============================================================================
  // Escáner "1-Tap Lens" Polimórfico (Música + Libros ISBN + Videojuegos)
  // ============================================================================
  function openScannerModal() {
    if (!state.token) {
      showToast('Autenticación Necesaria', 'Inicia sesión antes de escanear un soporte físico.', 'info');
      openAuthModal('login');
      return;
    }
    resetScanPreview();
    const modal = document.getElementById('scannerModal');
    if (modal) modal.classList.add('active');
    startCamera();
  }

  function closeScannerModal() {
    stopCamera();
    resetScanPreview();
    const modal = document.getElementById('scannerModal');
    if (modal) modal.classList.remove('active');
  }

  function switchScannerTab() {
    startCamera();
  }

  async function startCamera() {
    stopCamera();
    const video = document.getElementById('cameraVideo');
    if (!video || !navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      return;
    }

    try {
      const constraints = {
        video: {
          facingMode: state.cameraFacingMode,
          width: { ideal: 1280 },
          height: { ideal: 720 }
        },
        audio: false
      };

      const stream = await navigator.mediaDevices.getUserMedia(constraints);
      state.cameraStream = stream;
      video.srcObject = stream;
      await video.play();
    } catch (err) {
      console.warn('No se pudo acceder a la cámara WebRTC:', err.message);
    }
  }

  function stopCamera() {
    if (state.cameraStream) {
      state.cameraStream.getTracks().forEach(track => track.stop());
      state.cameraStream = null;
    }
    const video = document.getElementById('cameraVideo');
    if (video) video.srcObject = null;
  }

  function toggleCameraFacing() {
    state.cameraFacingMode = state.cameraFacingMode === 'environment' ? 'user' : 'environment';
    startCamera();
  }

  function captureFromCamera() {
    const video = document.getElementById('cameraVideo');
    const canvas = document.getElementById('cameraCanvas');
    if (!video || !canvas) return;

    if (!video.videoWidth || !video.videoHeight) {
      const fileInput = document.getElementById('fileUploadInput');
      if (fileInput) fileInput.click();
      return;
    }

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

    canvas.toBlob(blob => {
      if (blob) {
        state.selectedScanBlob = blob;
        state.selectedScanFileName = `camera_capture_${Date.now()}.jpg`;
        submitScan();
      }
    }, 'image/jpeg', 0.92);
  }

  function handleFileSelect(e) {
    const files = e.target.files;
    if (!files || files.length === 0) return;
    const file = files[0];
    state.selectedScanBlob = file;
    state.selectedScanFileName = file.name;
    submitScan();
  }

  function resetScanPreview() {
    state.selectedScanBlob = null;
    state.selectedScanFileName = null;
    const aiOverlay = document.getElementById('scanAiOverlay');
    const loadingState = document.getElementById('scanAiLoadingState');
    const errorState = document.getElementById('scanAiErrorState');
    if (aiOverlay) aiOverlay.style.display = 'none';
    if (loadingState) loadingState.style.display = 'flex';
    if (errorState) errorState.style.display = 'none';

    const fileUploadInput = document.getElementById('fileUploadInput');
    if (fileUploadInput) fileUploadInput.value = '';
  }

  function retryScannerCapture() {
    resetScanPreview();
    startCamera();
  }

  function switchScannerToManualCreate() {
    closeScannerModal();
    openCreateModal();
  }

  function selectScanFormat(format) {
    const input = document.getElementById('scanFormatSelect');
    if (input) input.value = format;
  }

  async function submitScan() {
    if (!state.selectedScanBlob) {
      showToast('Imagen requerida', 'Captura una foto con la cámara o selecciona una de la galería.', 'error');
      return;
    }

    const aiOverlay = document.getElementById('scanAiOverlay');
    const loadingState = document.getElementById('scanAiLoadingState');
    const errorState = document.getElementById('scanAiErrorState');
    const errorText = document.getElementById('scanAiErrorText');
    const aiTitle = document.getElementById('scanAiPhaseTitle');
    const aiSubtitle = document.getElementById('scanAiPhaseSubtitle');

    if (aiOverlay) {
      aiOverlay.style.display = 'flex';
      if (loadingState) loadingState.style.display = 'flex';
      if (errorState) errorState.style.display = 'none';
      if (aiTitle) aiTitle.textContent = 'Analizando Soporte Físico...';
      if (aiSubtitle) aiSubtitle.textContent = 'Fase 1: Buscando código EAN / ISBN con ZXing.Net (~2 ms)...';
    }

    const phaseTimer = setTimeout(() => {
      if (aiTitle) aiTitle.textContent = 'Inferencia Visual Multimodal';
      if (aiSubtitle) aiSubtitle.textContent = 'Fase 2: Identificando cubierta con Gemini Flash & Catálogo Oficial...';
    }, 450);

    const defaultFormat = state.currentCategory === 'books'
      ? 'Paperback'
      : state.currentCategory === 'games'
        ? 'OpticalDiscGame'
        : 'Vinyl';
    const formData = new FormData();
    formData.append('file', state.selectedScanBlob, state.selectedScanFileName || 'scan.jpg');
    formData.append('format', defaultFormat);

    try {
      const result = await apiFetch('/api/releases/scan', {
        method: 'POST',
        body: formData
      });

      clearTimeout(phaseTimer);

      const isUnrecognized = !result ||
        result.status === 'Failed' ||
        result.title === 'Lanzamiento pendiente de identificación' ||
        result.title === 'Libro pendiente de identificación' ||
        result.title === 'Videojuego pendiente de identificación';

      if (isUnrecognized) {
        if (loadingState) loadingState.style.display = 'none';
        if (errorState) errorState.style.display = 'flex';
        if (errorText) {
          errorText.textContent = 'No hemos podido identificar el título ni el código de barras en esta imagen. No se ha guardado ningún registro vacío en tu bóveda.';
        }
        return;
      }

      const detectedCategoryKey = getReleaseCategoryKey(result);
      if (detectedCategoryKey !== state.currentCategory) {
        setCategory(detectedCategoryKey);
      }

      const toastTitle = detectedCategoryKey === 'books'
        ? '📚 Libro Identificado'
        : detectedCategoryKey === 'games'
          ? '🎮 Videojuego Identificado'
          : '🎵 Soporte Identificado';

      showToast(
        toastTitle,
        `Detectado: "${result.title}" de ${result.artist}`,
        'success'
      );
      closeScannerModal();
      await refreshAll();

      if (result.id) {
        openConfirmModal(result.id);
      }
    } catch (err) {
      clearTimeout(phaseTimer);
      if (loadingState) loadingState.style.display = 'none';
      if (errorState) errorState.style.display = 'flex';
      if (errorText) {
        errorText.textContent = err.message || 'No se pudo completar la identificación visual. Inténtalo de nuevo o usa el Alta Manual.';
      }
    }
  }

  // ============================================================================
  // Alta Manual Contextual según Categoría Activa (Música / Libros / Juegos)
  // ============================================================================
  function openCreateModal() {
    if (!state.token) {
      showToast('Autenticación Necesaria', 'Inicia sesión antes de añadir un ejemplar.', 'info');
      openAuthModal('login');
      return;
    }
    const form = document.getElementById('createReleaseForm');
    if (form) form.reset();

    const cat = state.currentCategory || 'music';
    const modalTitle = document.getElementById('createModalTitle');
    const lblArtist = document.getElementById('labelCreateArtist');
    const inpArtist = document.getElementById('createArtist');
    const lblTitle = document.getElementById('labelCreateTitle');
    const inpTitle = document.getElementById('createTitle');
    const lblFormat = document.getElementById('labelCreateFormat');
    const selFormat = document.getElementById('createFormat');
    const lblBarcode = document.getElementById('labelCreateBarcode');
    const inpBarcode = document.getElementById('createBarcode');
    const lblCatNo = document.getElementById('labelCreateCatalogNumber');
    const inpCatNo = document.getElementById('createCatalogNumber');
    const lblLabel = document.getElementById('labelCreateLabel');
    const inpLabel = document.getElementById('createLabel');
    const lblGenre = document.getElementById('labelCreateGenre');
    const inpGenre = document.getElementById('createGenre');
    const inpOverview = document.getElementById('createOverview');
    const genreDatalist = document.getElementById('genreSuggestions');
    const bookFields = document.getElementById('bookSpecificFields');
    const gameFields = document.getElementById('gameSpecificFields');

    // Poblar sugerencias canónicas + géneros ya existentes del usuario para esta categoría
    if (genreDatalist) {
      const canonicalList = CANONICAL_GENRES[cat] || CANONICAL_GENRES.music;
      const existingSet = new Set(canonicalList);
      state.allReleases
        .filter(r => getReleaseCategoryKey(r) === cat && r.genre)
        .forEach(r => existingSet.add(r.genre.trim()));

      genreDatalist.innerHTML = Array.from(existingSet)
        .map(g => `<option value="${escapeHtml(g)}"></option>`)
        .join('');
    }

    if (cat === 'books') {
      if (modalTitle) modalTitle.textContent = '📚 Alta Manual — Libro / Cómic';
      if (lblArtist) lblArtist.innerHTML = 'Autor / Guionista <span class="required">*</span>';
      if (inpArtist) inpArtist.placeholder = 'Ej. Isaac Asimov, Alan Moore';
      if (lblTitle) lblTitle.innerHTML = 'Título de la Obra <span class="required">*</span>';
      if (inpTitle) inpTitle.placeholder = 'Ej. Fundación, Watchmen';
      if (lblFormat) lblFormat.innerHTML = 'Encuadernación / Formato <span class="required">*</span>';
      if (selFormat) {
        selFormat.innerHTML = `
          <option value="Hardcover">Tapa Dura (Cartoné)</option>
          <option value="Paperback">Rústica / Bolsillo</option>
          <option value="ComicManga">Cómic / Novela Gráfica / Manga</option>
          <option value="BookSpecialEdition">Edición Especial / Coleccionista</option>
        `;
      }
      if (lblBarcode) lblBarcode.textContent = 'Código ISBN-13 / EAN (978 / 979)';
      if (inpBarcode) inpBarcode.placeholder = 'Ej. 9788497594257';
      if (lblCatNo) lblCatNo.textContent = 'ISBN-10 / Colección Editorial';
      if (inpCatNo) inpCatNo.placeholder = 'Ej. 8497594258 o Nova #12';
      if (lblLabel) lblLabel.textContent = 'Editorial';
      if (inpLabel) inpLabel.placeholder = 'Ej. Minotauro, Debolsillo';
      if (lblGenre) lblGenre.textContent = 'Género Literario';
      if (inpGenre) inpGenre.placeholder = 'Ej. Ciencia Ficción, Fantasía, Ensayo...';
      if (inpOverview) inpOverview.placeholder = 'Ej. Primera entrega de la trilogía galáctica de Hari Seldon...';
      if (bookFields) bookFields.style.display = 'grid';
      if (gameFields) gameFields.style.display = 'none';
      toggleFormatFields('Hardcover');
    } else if (cat === 'games') {
      if (modalTitle) modalTitle.textContent = '🎮 Alta Manual — Videojuego Físico';
      if (lblArtist) lblArtist.innerHTML = 'Estudio / Desarrollador <span class="required">*</span>';
      if (inpArtist) inpArtist.placeholder = 'Ej. FromSoftware, Nintendo EPD';
      if (lblTitle) lblTitle.innerHTML = 'Título del Videojuego <span class="required">*</span>';
      if (inpTitle) inpTitle.placeholder = 'Ej. Bloodborne, Zelda: Ocarina of Time';
      if (lblFormat) lblFormat.innerHTML = 'Soporte Físico <span class="required">*</span>';
      if (selFormat) {
        selFormat.innerHTML = `
          <option value="OpticalDiscGame">Disco Óptico (Blu-ray / DVD / CD-ROM)</option>
          <option value="Cartridge">Cartucho / Tarjeta Física</option>
          <option value="GameCollectorEdition">Edición Coleccionista (Caja Grande)</option>
        `;
      }
      if (lblBarcode) lblBarcode.textContent = 'Código de Barras (EAN / UPC)';
      if (inpBarcode) inpBarcode.placeholder = 'Ej. 0045496420048';
      if (lblCatNo) lblCatNo.textContent = 'Código de Región / Serial';
      if (inpCatNo) inpCatNo.placeholder = 'Ej. CUSA-00207 / PAL-ESP';
      if (lblLabel) lblLabel.textContent = 'Publisher / Distribuidora';
      if (inpLabel) inpLabel.placeholder = 'Ej. Sony Interactive, Nintendo';
      if (lblGenre) lblGenre.textContent = 'Género del Videojuego';
      if (inpGenre) inpGenre.placeholder = 'Ej. Action RPG, Survival Horror, Plataformas...';
      if (inpOverview) inpOverview.placeholder = 'Ej. Obra maestra gótica ambientada en la ciudad maldita de Yharnam...';
      if (bookFields) bookFields.style.display = 'none';
      if (gameFields) gameFields.style.display = 'grid';
      toggleFormatFields('OpticalDiscGame');
    } else {
      if (modalTitle) modalTitle.textContent = '🎵 Alta Manual — Música';
      if (lblArtist) lblArtist.innerHTML = 'Artista / Grupo <span class="required">*</span>';
      if (inpArtist) inpArtist.placeholder = 'Ej. Pink Floyd';
      if (lblTitle) lblTitle.innerHTML = 'Título del Álbum <span class="required">*</span>';
      if (inpTitle) inpTitle.placeholder = 'Ej. The Dark Side of the Moon';
      if (lblFormat) lblFormat.innerHTML = 'Formato Físico <span class="required">*</span>';
      if (selFormat) {
        selFormat.innerHTML = `
          <option value="Vinyl">Vinilo</option>
          <option value="CD">Compact Disc (CD)</option>
          <option value="Cassette">Casete</option>
          <option value="BoxSet">Box Set</option>
        `;
      }
      if (lblBarcode) lblBarcode.textContent = 'Código de Barras (EAN / UPC)';
      if (inpBarcode) inpBarcode.placeholder = 'Ej. 5099902894523';
      if (lblCatNo) lblCatNo.textContent = 'Número de Catálogo';
      if (inpCatNo) inpCatNo.placeholder = 'Ej. SHVL 804';
      if (lblLabel) lblLabel.textContent = 'Sello Discográfico';
      if (inpLabel) inpLabel.placeholder = 'Ej. Harvest / EMI';
      if (lblGenre) lblGenre.textContent = 'Género / Estilo Musical';
      if (inpGenre) inpGenre.placeholder = 'Ej. Rock Progresivo, Jazz, Synth-pop...';
      if (inpOverview) inpOverview.placeholder = 'Ej. Grabado en Abbey Road en 1972 con sintetizadores analógicos...';
      if (bookFields) bookFields.style.display = 'none';
      if (gameFields) gameFields.style.display = 'none';
      toggleFormatFields('Vinyl');
    }

    const modal = document.getElementById('createModal');
    if (modal) modal.classList.add('active');
  }

  function closeCreateModal() {
    const modal = document.getElementById('createModal');
    if (modal) modal.classList.remove('active');
  }

  function toggleFormatFields(format) {
    const vinylFields = document.getElementById('vinylSpecificFields');
    if (vinylFields) {
      vinylFields.style.display = (format === 'Vinyl' && state.currentCategory === 'music') ? 'grid' : 'none';
    }
  }

  async function handleCreateSubmit(e) {
    e.preventDefault();
    const artist = document.getElementById('createArtist').value.trim();
    const title = document.getElementById('createTitle').value.trim();
    const format = document.getElementById('createFormat').value;
    const yearVal = document.getElementById('createYear').value;
    let barcode = document.getElementById('createBarcode').value.trim() || null;
    let catalogueNumber = document.getElementById('createCatalogNumber').value.trim() || null;
    const label = document.getElementById('createLabel').value.trim() || null;
    const country = document.getElementById('createCountry').value.trim() || null;
    const coverImageUrl = document.getElementById('createCoverUrl').value.trim() || null;
    const notes = document.getElementById('createNotes').value.trim() || null;
    const genreInput = document.getElementById('createGenre');
    const genre = genreInput ? (genreInput.value.trim() || null) : null;
    const overviewInput = document.getElementById('createOverview');
    const overview = overviewInput ? (overviewInput.value.trim() || null) : null;
    const pagesInput = document.getElementById('createPagesCount');
    const pagesVal = pagesInput ? pagesInput.value : '';
    const platformInput = document.getElementById('createPlatform');
    const platformVal = (platformInput && state.currentCategory === 'games') ? platformInput.value.trim() : '';

    // Si en Libros el usuario escribió un ISBN-10 en el campo Barcode, lo pasamos a catalogueNumber para no fallar el checksum EAN-13
    if (barcode) {
      const cleanDigits = barcode.replace(/[\s-]/g, '');
      if (cleanDigits.length === 10 && !catalogueNumber) {
        catalogueNumber = barcode;
        barcode = null;
      }
    }

    let vinylSize = null;
    let vinylSpeed = null;
    if (format === 'Vinyl' && state.currentCategory === 'music') {
      vinylSize = document.getElementById('createVinylSize').value;
      vinylSpeed = document.getElementById('createVinylSpeed').value;
    }

    const payload = {
      title,
      artist,
      format,
      releaseYear: yearVal ? parseInt(yearVal, 10) : null,
      barcode,
      catalogueNumber,
      label,
      country,
      coverImageUrl,
      notes,
      vinylSize,
      vinylSpeed,
      discsCount: 1,
      pagesCount: (pagesVal && parseInt(pagesVal, 10) > 0) ? parseInt(pagesVal, 10) : null,
      platform: platformVal || null,
      genre,
      overview
    };

    try {
      const created = await apiFetch('/api/releases', {
        method: 'POST',
        body: JSON.stringify(payload)
      });

      showToast('Guardado en Bóveda', `"${created.title}" se ha añadido a tu colección.`, 'success');
      closeCreateModal();

      const targetCategory = getReleaseCategoryKey(created);
      setCategory(targetCategory);
      await refreshAll();
    } catch (err) {
      showToast('Error de Validación', err.message, 'error');
    }
  }

  // ============================================================================
  // Modal de Detalle Polimórfico (Música con Tracklist / Libros con Ficha ISBN)
  // ============================================================================
  async function openDetailModal(id) {
    const modal = document.getElementById('detailModal');
    const body = document.getElementById('detailModalBody');
    const headerTitle = document.getElementById('detailHeaderTitle');
    const footer = document.getElementById('detailModalFooter');
    const backdropArt = document.getElementById('detailBackdropArt');

    if (backdropArt) {
      backdropArt.style.backgroundImage = 'none';
      backdropArt.classList.remove('active', 'has-back-cover');
    }

    modal.classList.add('active');
    body.innerHTML = '<div style="text-align: center; padding: 2rem;"><div class="spinner" style="margin: 0 auto 0.5rem;"></div>Cargando ficha...</div>';

    try {
      const release = await apiFetch(`/api/releases/${id}`);
      const isBook = isBookRelease(release);
      const isGame = isGameRelease(release);
      headerTitle.textContent = `${release.artist} — ${release.title}`;

      if (backdropArt && release.coverImageUrl) {
        backdropArt.style.backgroundImage = `url('${encodeURI(release.coverImageUrl)}')`;
        backdropArt.classList.add('active');
      }

      const discogsId = release.discogsReleaseId || release.discogsId;
      if (!isBook && !isGame && discogsId && backdropArt) {
        apiFetch(`/api/discogs/releases/${discogsId}/artwork`)
          .then(art => {
            if (art && art.backCoverImageUrl) {
              backdropArt.style.backgroundImage = `url('${encodeURI(art.backCoverImageUrl)}')`;
              backdropArt.classList.add('active', 'has-back-cover');
              const badgeContainer = document.getElementById('detailBackCoverBadgeSlot');
              if (badgeContainer) {
                badgeContainer.innerHTML = '<span class="back-cover-indicator-pill" title="Contraportada oficial de Discogs proyectada en el fondo">🖼️ Contraportada en fondo</span>';
              }
            }
          })
          .catch(() => { /* Fallback silencioso a la portada difuminada */ });
      }

      const shortCond = getGoldmineShortCode(release.mediaCondition);
      const shortSleeve = getGoldmineShortCode(release.sleeveCondition);

      let bottomSectionHtml = '';
      if (isBook) {
        const openLibRef = release.externalCatalogId
          ? `<a href="https://openlibrary.org/books/${encodeURIComponent(release.externalCatalogId)}" target="_blank" rel="noopener" style="color: var(--accent-cyan);">${escapeHtml(release.externalCatalogId)} ↗</a>`
          : 'Sin vincular (usa el botón Catálogo)';

        bottomSectionHtml = `
          <div style="margin-top: 0.75rem; background: rgba(15, 23, 42, 0.55); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 0.9rem 1rem;">
            <h4 style="font-size: 0.92rem; color: var(--accent-gold); margin-bottom: 0.55rem;">📖 Especificaciones Bibliográficas</h4>
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 0.5rem; font-size: 0.83rem; color: var(--text-secondary);">
              <div><strong>Encuadernación:</strong> ${escapeHtml(getFormatDisplayLabel(release.format))}</div>
              <div><strong>Páginas:</strong> ${release.pagesCount ? `${release.pagesCount} págs.` : 'N/D'}</div>
              <div><strong>Bloque Interior:</strong> ${shortCond ? `${shortCond} (${release.mediaCondition})` : 'Sin calificar'}</div>
              <div><strong>Cubierta / Lomo:</strong> ${shortSleeve ? `${shortSleeve} (${release.sleeveCondition})` : 'Sin calificar'}</div>
              <div style="grid-column: 1 / -1;"><strong>Referencia OpenLibrary:</strong> ${openLibRef}</div>
            </div>
          </div>
        `;
      } else if (isGame) {
        bottomSectionHtml = `
          <div style="margin-top: 0.75rem; background: rgba(15, 23, 42, 0.55); border: 1px solid var(--border-subtle); border-radius: var(--radius-md); padding: 0.9rem 1rem;">
            <h4 style="font-size: 0.92rem; color: var(--accent-cyan); margin-bottom: 0.55rem;">🕹️ Especificaciones CIB &amp; Plataforma</h4>
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 0.5rem; font-size: 0.83rem; color: var(--text-secondary);">
              <div><strong>Consola / Sistema:</strong> ${escapeHtml(release.platform || 'No especificada')}</div>
              <div><strong>Soporte Físico:</strong> ${escapeHtml(getFormatDisplayLabel(release.format))}</div>
              <div><strong>Disco / Cartucho:</strong> ${shortCond ? `${shortCond} (${release.mediaCondition})` : 'Sin calificar'}</div>
              <div><strong>Caja y Manual (CIB):</strong> ${shortSleeve ? `${shortSleeve} (${release.sleeveCondition})` : 'Sin calificar'}</div>
              <div style="grid-column: 1 / -1;"><strong>Referencia Catálogo:</strong> ${escapeHtml(release.externalCatalogId || 'Sin vincular (usa el botón Catálogo)')}</div>
            </div>
          </div>
        `;
      } else {
        const tracksHtml = (release.tracks && release.tracks.length > 0)
          ? `
            <div class="tracks-table-container">
              <table class="tracks-table">
                <thead>
                  <tr>
                    <th class="track-pos">#</th>
                    <th>Título de la Pista</th>
                    <th class="track-dur">Duración</th>
                  </tr>
                </thead>
                <tbody>
                  ${release.tracks.map(t => `
                    <tr>
                      <td class="track-pos">${escapeHtml(t.position)}</td>
                      <td>${escapeHtml(t.title)}</td>
                      <td class="track-dur">${escapeHtml(t.formattedDuration || t.duration || '--:--')}</td>
                    </tr>
                  `).join('')}
                </tbody>
              </table>
            </div>
          `
          : '<p style="font-size: 0.85rem; color: var(--text-muted); font-style: italic;">No hay pistas registradas todavía. Usa "Catálogo" para importar el tracklist y contraportada oficial.</p>';

        bottomSectionHtml = `
          <div style="margin-top: 0.5rem;">
            <h4 style="font-size: 0.95rem; color: #fff; margin-bottom: 0.65rem;">Lista de Pistas Oficial (${release.tracks ? release.tracks.length : 0})</h4>
            ${tracksHtml}
          </div>
        `;
      }

      const metaGridHtml = isBook
        ? `
          <div><strong>Año Publicación:</strong> ${release.releaseYear || 'Desconocido'}</div>
          <div><strong>Editorial:</strong> ${escapeHtml(release.label || 'N/D')}</div>
          <div><strong>Páginas:</strong> ${release.pagesCount ? `${release.pagesCount} págs.` : 'N/D'}</div>
          <div><strong>ISBN / Serie:</strong> ${escapeHtml(release.catalogueNumber || 'N/D')}</div>
          <div><strong>ISBN-13 / EAN:</strong> ${escapeHtml(release.barcode || 'N/D')}</div>
          <div><strong>País / Idioma:</strong> ${escapeHtml(release.country || 'N/D')}</div>
        `
        : isGame
          ? `
            <div><strong>Plataforma:</strong> ${escapeHtml(release.platform || 'N/D')}</div>
            <div><strong>Año Lanzamiento:</strong> ${release.releaseYear || 'Desconocido'}</div>
            <div><strong>Publisher:</strong> ${escapeHtml(release.label || 'N/D')}</div>
            <div><strong>Serial / Código:</strong> ${escapeHtml(release.catalogueNumber || 'N/D')}</div>
            <div><strong>Código EAN:</strong> ${escapeHtml(release.barcode || 'N/D')}</div>
            <div><strong>Región:</strong> ${escapeHtml(release.country || 'N/D')}</div>
          `
          : `
            <div><strong>Año:</strong> ${release.releaseYear || 'Desconocido'}</div>
            <div><strong>Sello:</strong> ${escapeHtml(release.label || 'N/D')}</div>
            <div><strong>País:</strong> ${escapeHtml(release.country || 'N/D')}</div>
            <div><strong>Catálogo:</strong> ${escapeHtml(release.catalogueNumber || 'N/D')}</div>
            <div><strong>Código EAN:</strong> ${escapeHtml(release.barcode || 'N/D')}</div>
            <div><strong>Ref. Oficial:</strong> ${discogsId ? `<a href="https://www.discogs.com/release/${discogsId}" target="_blank" rel="noopener" style="color: var(--accent-cyan);">#${discogsId} ↗</a>` : 'Sin vincular'}</div>
          `;

      const detailCoverWidth = isBook ? '122px' : isGame ? '132px' : '145px';
      const detailCoverHeight = isBook ? '183px' : isGame ? '176px' : '145px';

      const cleanOverview = release.overview
        ? release.overview.trim().replace(/^[“"«]+|[”"»]+$/g, '').trim()
        : '';

      body.innerHTML = `
        <div style="display: flex; gap: 1.25rem; flex-wrap: wrap;">
          <div style="width: ${detailCoverWidth}; height: ${detailCoverHeight}; flex-shrink: 0; border-radius: var(--radius-md); overflow: hidden; background: #000; box-shadow: 0 8px 24px rgba(0,0,0,0.65); border: 1px solid rgba(255,255,255,0.1);">
            ${release.coverImageUrl
              ? `<img src="${escapeHtml(release.coverImageUrl)}" alt="Portada" style="width: 100%; height: 100%; object-fit: cover;" onerror="if(this.src.includes('library_600x900.jpg')){this.src=this.src.replace('shared.akamai.steamstatic.com/store_item_assets/steam/apps/','cdn.akamai.steamstatic.com/steam/apps/').replace('library_600x900.jpg','header.jpg');}">`
              : getPlaceholderGroovesHtml(release.format)
            }
          </div>
          <div style="flex: 1; min-width: 200px;">
            <div style="display: flex; align-items: center; gap: 0.45rem; flex-wrap: wrap; margin-bottom: 0.4rem;">
              <span class="format-badge ${release.format.toLowerCase()}">${escapeHtml(getFormatDisplayLabel(release.format))}</span>
              ${(isGame && release.platform) ? `<span class="format-badge" style="background: rgba(56,189,248,0.2); color: #7dd3fc; border: 1px solid rgba(56,189,248,0.4);">🎮 ${escapeHtml(release.platform)}</span>` : ''}
              ${release.genre ? `<span class="genre-pill-badge" title="Género / Estilo: ${escapeHtml(release.genre)}">🎭 ${escapeHtml(release.genre)}</span>` : ''}
              <span class="status-badge ${release.status.toLowerCase()}">${escapeHtml(release.status)}</span>
              ${shortCond ? `<span class="condition-pill" title="Conservación: ${escapeHtml(release.mediaCondition)}">🏅 ${escapeHtml(shortCond)} (${escapeHtml(release.mediaCondition)})</span>` : ''}
              <span id="detailBackCoverBadgeSlot"></span>
            </div>
            <h3 style="font-size: 1.25rem; color: #fff; margin-bottom: 0.2rem;">${escapeHtml(release.title)}</h3>
            <div style="font-size: 1rem; color: var(--accent-gold); font-weight: 600; margin-bottom: 0.6rem;">${escapeHtml(release.artist)}</div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 0.4rem; font-size: 0.82rem; color: var(--text-secondary);">
              ${metaGridHtml}
            </div>
          </div>
        </div>

        ${cleanOverview ? `
          <blockquote class="curator-liner-notes">“${escapeHtml(cleanOverview)}”</blockquote>
        ` : ''}

        ${release.notes ? `
          <div style="background: rgba(11, 15, 23, 0.55); border-left: 3px solid var(--accent-gold); padding: 0.65rem 0.85rem; border-radius: var(--radius-sm); font-size: 0.84rem;">
            <strong>Notas de Custodia:</strong> ${escapeHtml(release.notes)}
          </div>
        ` : ''}

        ${bottomSectionHtml}
      `;

      footer.innerHTML = `
        <button type="button" class="btn-ghost-ruby" onclick="app.closeDetailModal(); app.deleteRelease('${release.id}')" title="Eliminar ejemplar de la bóveda">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="3 6 5 6 21 6"></polyline>
            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
          </svg>
          <span>Eliminar</span>
        </button>
        <div class="detail-footer-right">
          <button type="button" class="btn btn-outline-gold btn-collector-action" onclick="app.closeDetailModal(); app.openConfirmModal('${release.id}')" title="Calificar Estado de Conservación">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon>
            </svg>
            <span>Conservación</span>
          </button>
          <button type="button" class="btn btn-primary btn-collector-action" onclick="app.closeDetailModal(); app.openEnrichModal('${release.id}')" title="Buscar y vincular edición en el Catálogo Oficial">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="11" cy="11" r="8"></circle>
              <line x1="21" y1="21" x2="16.65" y2="16.65"></line>
            </svg>
            <span>Catálogo</span>
          </button>
        </div>
      `;
    } catch (err) {
      body.innerHTML = `<p style="color: var(--accent-ruby);">Error: ${escapeHtml(err.message)}</p>`;
    }
  }

  function closeDetailModal() {
    const modal = document.getElementById('detailModal');
    if (modal) modal.classList.remove('active');
  }

  // ============================================================================
  // Estado de Conservación Universal (Goldmine / Bibliográfica / CIB)
  // ============================================================================
  const GOLDMINE_DESCRIPTIONS = {
    media: {
      Mint: 'Mint (M) — Impecable, nuevo a estrenar',
      NearMint: 'Near Mint (NM) — Casi perfecto, sin marcas',
      VeryGoodPlus: 'Very Good Plus (VG+) — Excelente, marcas muy leves',
      VeryGood: 'Very Good (VG) — Buen estado, uso moderado',
      GoodPlus: 'Good Plus (G+) — Desgaste evidente pero funcional',
      Good: 'Good (G) — Desgaste pronunciado',
      Fair: 'Fair (F) — Daños visibles severos',
      Poor: 'Poor (P) — Deteriorado o incompleto'
    },
    sleeve: {
      Mint: 'Mint (M) — Impecable, precintado',
      NearMint: 'Near Mint (NM) — Sin esquinas dobladas ni roces',
      VeryGoodPlus: 'Very Good Plus (VG+) — Desgaste mínimo en lomo',
      VeryGood: 'Very Good (VG) — Roces suaves en bordes',
      GoodPlus: 'Good Plus (G+) — Rozaduras notables en cubierta',
      Good: 'Good (G) — Marcas o dobleces visibles',
      Fair: 'Fair (F) — Cubierta muy deteriorada',
      Poor: 'Poor (P) — Sin cubierta o destruida'
    }
  };

  function selectGoldmineGrade(target, grade) {
    const isMedia = target === 'media';
    const inputId = isMedia ? 'confirmMediaCondition' : 'confirmSleeveCondition';
    const gridId = isMedia ? 'mediaGradeGrid' : 'sleeveGradeGrid';
    const descId = isMedia ? 'mediaConditionDesc' : 'sleeveConditionDesc';

    const inputEl = document.getElementById(inputId);
    if (inputEl) inputEl.value = grade;

    const chips = document.querySelectorAll(`#${gridId} .goldmine-chip`);
    chips.forEach(chip => {
      chip.classList.toggle('active', chip.dataset.grade === grade);
    });

    const descEl = document.getElementById(descId);
    if (descEl && GOLDMINE_DESCRIPTIONS[target] && GOLDMINE_DESCRIPTIONS[target][grade]) {
      descEl.textContent = GOLDMINE_DESCRIPTIONS[target][grade];
    }
  }

  function openConfirmModal(releaseId) {
    document.getElementById('confirmReleaseId').value = releaseId;
    const release = state.allReleases.find(r => r.id === releaseId) || state.releases.find(r => r.id === releaseId);
    const mediaGrade = (release && release.mediaCondition) ? release.mediaCondition : 'NearMint';
    const sleeveGrade = (release && release.sleeveCondition) ? release.sleeveCondition : 'NearMint';

    const isBook = isBookRelease(release) || state.currentCategory === 'books';
    const isGame = isGameRelease(release) || state.currentCategory === 'games';

    const badgeEl = document.getElementById('conservationStandardBadge');
    const lblMedia = document.getElementById('labelConfirmMedia');
    const lblSleeve = document.getElementById('labelConfirmSleeve');
    if (isBook) {
      if (badgeEl) badgeEl.textContent = 'Escala Bibliográfica';
      if (lblMedia) lblMedia.textContent = 'Páginas e Interior (Bloque)';
      if (lblSleeve) lblSleeve.textContent = 'Cubierta, Lomo y Sobrecubierta';
    } else if (isGame) {
      if (badgeEl) badgeEl.textContent = 'Conservación CIB';
      if (lblMedia) lblMedia.textContent = 'Disco Óptico / Cartucho';
      if (lblSleeve) lblSleeve.textContent = 'Caja Original y Manual';
    } else {
      if (badgeEl) badgeEl.textContent = 'Estándar Goldmine';
      if (lblMedia) lblMedia.textContent = 'Estado del Disco / Soporte (Media)';
      if (lblSleeve) lblSleeve.textContent = 'Estado de Portada / Estuche (Sleeve)';
    }

    selectGoldmineGrade('media', mediaGrade);
    selectGoldmineGrade('sleeve', sleeveGrade);
    document.getElementById('confirmNotes').value = (release && release.notes) ? release.notes : '';

    const modal = document.getElementById('confirmModal');
    if (modal) modal.classList.add('active');
  }

  function closeConfirmModal() {
    const modal = document.getElementById('confirmModal');
    if (modal) modal.classList.remove('active');
  }

  async function submitConfirm() {
    const releaseId = document.getElementById('confirmReleaseId').value;
    const mediaCondition = document.getElementById('confirmMediaCondition').value;
    const sleeveCondition = document.getElementById('confirmSleeveCondition').value;
    const notes = document.getElementById('confirmNotes').value.trim() || null;

    try {
      await apiFetch(`/api/releases/${releaseId}/confirm`, {
        method: 'POST',
        body: JSON.stringify({
          mediaCondition,
          sleeveCondition,
          notes
        })
      });

      showToast('Estado de Conservación Guardado', 'La graduación de tu ejemplar ha quedado registrada.', 'success');
      closeConfirmModal();
      await refreshAll();
    } catch (err) {
      showToast('Error al Confirmar', err.message, 'error');
    }
  }

  // ============================================================================
  // "Buscar en Catálogo Oficial" Polimórfico (Discogs para Música | OpenLibrary para Libros)
  // ============================================================================
  function openEnrichModal(releaseId) {
    document.getElementById('enrichReleaseId').value = releaseId;
    document.getElementById('enrichDiscogsId').value = '';
    state.selectedBookEdition = null;
    state.selectedGameEdition = null;
    state.catalogSearchResults = [];

    const submitBtn = document.getElementById('btnSubmitEnrich');
    if (submitBtn) submitBtn.disabled = true;

    const resultsContainer = document.getElementById('enrichSearchResults');
    if (resultsContainer) resultsContainer.innerHTML = '';

    const release = state.allReleases.find(r => r.id === releaseId) || state.releases.find(r => r.id === releaseId);
    const isBook = isBookRelease(release);
    const isGame = isGameRelease(release);
    state.enrichCategory = isBook ? 'Books' : isGame ? 'VideoGames' : 'Music';

    const sourceBadge = document.getElementById('catalogSourceBadge');
    const searchLabel = document.getElementById('enrichSearchLabel');
    const searchHelp = document.getElementById('enrichSearchHelp');
    const queryInput = document.getElementById('enrichSearchQuery');

    if (isBook) {
      if (sourceBadge) sourceBadge.textContent = 'Fuente: OpenLibrary';
      if (searchLabel) searchLabel.textContent = 'Buscar Edición por Título, Autor o ISBN';
      if (searchHelp) {
        searchHelp.textContent = 'Toca una edición para seleccionarla (o vuelve a tocarla para desmarcarla). Al vincular se importará portada oficial, editorial, año, ISBN y número de páginas.';
      }
      if (queryInput) {
        queryInput.placeholder = 'Ej. Isaac Asimov Fundación o 9788497594257...';
      }
    } else if (isGame) {
      if (sourceBadge) sourceBadge.textContent = 'Fuente: Catálogo Gamer';
      if (searchLabel) searchLabel.textContent = 'Buscar Videojuego por Título, Consola o Estudio';
      if (searchHelp) {
        searchHelp.textContent = 'Toca una edición para seleccionarla (o vuelve a tocarla para desmarcarla). Al vincular se importará carátula oficial, estudio, publisher, año y consola.';
      }
      if (queryInput) {
        queryInput.placeholder = 'Ej. Bloodborne PlayStation 4 o Zelda Ocarina of Time...';
      }
    } else {
      if (sourceBadge) sourceBadge.textContent = 'Fuente: Discogs';
      if (searchLabel) searchLabel.textContent = 'Buscar Edición por Artista, Álbum, Sello o Catálogo';
      if (searchHelp) {
        searchHelp.textContent = 'Toca una edición para seleccionarla (o vuelve a tocarla para desmarcarla). Al vincular se importará portada oficial, sello y listado de pistas.';
      }
      if (queryInput) {
        queryInput.placeholder = 'Ej. Pink Floyd Dark Side...';
      }
    }

    if (release && queryInput) {
      if (isBook && release.barcode) {
        queryInput.value = release.barcode;
      } else if (isGame) {
        queryInput.value = `${release.title} ${release.platform || ''}`.trim();
      } else {
        queryInput.value = `${release.artist} ${release.title}`.trim();
      }
    }

    const modal = document.getElementById('enrichModal');
    if (modal) modal.classList.add('active');

    if (queryInput && queryInput.value.trim().length > 0) {
      searchDiscogsForEnrich();
    }
  }

  function closeEnrichModal() {
    const modal = document.getElementById('enrichModal');
    if (modal) modal.classList.remove('active');
  }

  function selectCatalogEdition(element, releaseIdOrKey) {
    const hiddenInput = document.getElementById('enrichDiscogsId');
    const submitBtn = document.getElementById('btnSubmitEnrich');
    if (!hiddenInput || !element) return;

    const alreadySelected = element.classList.contains('selected');

    const allItems = document.querySelectorAll('#enrichSearchResults .catalog-result-item');
    allItems.forEach(item => item.classList.remove('selected'));

    if (alreadySelected) {
      hiddenInput.value = '';
      state.selectedBookEdition = null;
      state.selectedGameEdition = null;
      if (submitBtn) submitBtn.disabled = true;
    } else {
      element.classList.add('selected');
      hiddenInput.value = String(releaseIdOrKey);
      if (state.enrichCategory === 'Books') {
        state.selectedBookEdition = state.catalogSearchResults.find(b => String(b.editionKey) === String(releaseIdOrKey)) || null;
      } else if (state.enrichCategory === 'VideoGames') {
        state.selectedGameEdition = state.catalogSearchResults.find(g => String(g.gameKey) === String(releaseIdOrKey)) || null;
      }
      if (submitBtn) submitBtn.disabled = false;
    }
  }

  async function searchDiscogsForEnrich() {
    const query = document.getElementById('enrichSearchQuery').value.trim();
    const resultsContainer = document.getElementById('enrichSearchResults');
    const hiddenInput = document.getElementById('enrichDiscogsId');
    const submitBtn = document.getElementById('btnSubmitEnrich');

    if (hiddenInput) hiddenInput.value = '';
    state.selectedBookEdition = null;
    state.selectedGameEdition = null;
    if (submitBtn) submitBtn.disabled = true;
    if (!query) return;

    const isBook = state.enrichCategory === 'Books';
    const isGame = state.enrichCategory === 'VideoGames';
    const sourceName = isBook ? 'OpenLibrary' : isGame ? 'Catálogo Gamer' : 'Discogs';
    resultsContainer.innerHTML = `
      <div style="font-size:0.84rem; color: var(--text-secondary); text-align: center; padding: 1.25rem;">
        <div class="spinner" style="display:inline-block; vertical-align: middle; margin-right:0.4rem;"></div>
        Consultando ediciones en ${sourceName}...
      </div>
    `;

    try {
      if (isBook) {
        const cleanDigits = query.replace(/[\s-]/g, '');
        const isIsbnQuery = (cleanDigits.length === 13 || cleanDigits.length === 10) && /^\d+[0-9Xx]?$/.test(cleanDigits);
        const endpoint = isIsbnQuery
          ? `/api/books/search?isbn=${encodeURIComponent(cleanDigits)}`
          : `/api/books/search?query=${encodeURIComponent(query)}`;

        const results = await apiFetch(endpoint);
        state.catalogSearchResults = results || [];

        if (!results || results.length === 0) {
          resultsContainer.innerHTML = `
            <div style="font-size:0.84rem; color: var(--text-muted); text-align: center; padding: 1.25rem;">
              No se encontraron ediciones en OpenLibrary para esta búsqueda. Prueba con Título y Autor.
            </div>
          `;
          return;
        }

        resultsContainer.innerHTML = results.map(item => {
          const editionKey = item.editionKey;
          const thumbHtml = item.coverImageUrl
            ? `<img src="${escapeHtml(item.coverImageUrl)}" alt="" class="catalog-result-thumb thumb-vertical" loading="lazy" onerror="this.outerHTML='<div class=\\'catalog-result-thumb thumb-vertical\\'>📚</div>'">`
            : `<div class="catalog-result-thumb thumb-vertical">📚</div>`;

          const authorAndTitle = item.author && item.author !== 'Autor desconocido'
            ? `${item.author} — ${item.title}`
            : item.title;

          const badges = [];
          if (item.format) badges.push(`<span class="catalog-meta-badge highlight">📕 ${escapeHtml(item.format)}</span>`);
          if (item.publishYear) badges.push(`<span class="catalog-meta-badge">📅 ${escapeHtml(item.publishYear)}</span>`);
          if (item.publisher) badges.push(`<span class="catalog-meta-badge">🏷️ ${escapeHtml(item.publisher)}</span>`);
          if (item.pagesCount) badges.push(`<span class="catalog-meta-badge">📄 ${escapeHtml(item.pagesCount)} págs.</span>`);
          if (item.isbn) badges.push(`<span class="catalog-meta-badge">🔢 ${escapeHtml(item.isbn)}</span>`);
          if (item.genre) badges.push(`<span class="catalog-meta-badge">🎭 ${escapeHtml(item.genre)}</span>`);

          const externalLink = `<a href="https://openlibrary.org/books/${encodeURIComponent(editionKey)}" target="_blank" rel="noopener noreferrer" class="catalog-external-link" onclick="event.stopPropagation()">Ver en OpenLibrary ↗</a>`;

          return `
            <div class="catalog-result-item" data-release-id="${escapeHtml(editionKey)}" onclick="app.selectCatalogEdition(this, '${escapeHtml(editionKey)}')">
              ${thumbHtml}
              <div class="catalog-result-info">
                <div class="catalog-result-title" title="${escapeHtml(authorAndTitle)}">${escapeHtml(authorAndTitle)}</div>
                <div class="catalog-result-badges">${badges.join('')}</div>
                <div class="catalog-result-footer">
                  <span class="catalog-result-id-pill">OL: ${escapeHtml(editionKey)}</span>
                  ${externalLink}
                </div>
              </div>
              <div class="catalog-check-indicator" aria-hidden="true">✓</div>
            </div>
          `;
        }).join('');
        return;
      }

      if (isGame) {
        const results = await apiFetch(`/api/games/search?query=${encodeURIComponent(query)}`);
        state.catalogSearchResults = results || [];

        if (!results || results.length === 0) {
          resultsContainer.innerHTML = `
            <div style="font-size:0.84rem; color: var(--text-muted); text-align: center; padding: 1.25rem;">
              No se encontraron videojuegos para esta búsqueda. Prueba escribiendo el título exacto en inglés o español.
            </div>
          `;
          return;
        }

        resultsContainer.innerHTML = results.map(item => {
          const gameKey = item.gameKey;
          const thumbHtml = item.coverImageUrl
            ? `<img src="${escapeHtml(item.coverImageUrl)}" alt="" class="catalog-result-thumb thumb-vertical" loading="lazy" onerror="if(this.src.includes('library_600x900.jpg')){this.src=this.src.replace('shared.akamai.steamstatic.com/store_item_assets/steam/apps/','cdn.akamai.steamstatic.com/steam/apps/').replace('library_600x900.jpg','header.jpg');return;}this.outerHTML='<div class=\\'catalog-result-thumb thumb-vertical\\'>🎮</div>'">`
            : `<div class="catalog-result-thumb thumb-vertical">🎮</div>`;

          const devAndTitle = item.developer && item.developer !== 'Estudio Oficial'
            ? `${item.developer} — ${item.title}`
            : item.title;

          const badges = [];
          if (item.platform) badges.push(`<span class="catalog-meta-badge highlight">🎮 ${escapeHtml(item.platform)}</span>`);
          if (item.format) badges.push(`<span class="catalog-meta-badge">🕹️ ${escapeHtml(item.format)}</span>`);
          if (item.releaseYear) badges.push(`<span class="catalog-meta-badge">📅 ${escapeHtml(item.releaseYear)}</span>`);
          if (item.publisher) badges.push(`<span class="catalog-meta-badge">🏷️ ${escapeHtml(item.publisher)}</span>`);
          if (item.genre) badges.push(`<span class="catalog-meta-badge">🎭 ${escapeHtml(item.genre)}</span>`);

          let externalUrl = '';
          let externalLabel = 'Ver Ficha Oficial ↗';
          if (gameKey.startsWith('STEAM-')) {
            const appId = gameKey.replace('STEAM-', '');
            externalUrl = `https://store.steampowered.com/app/${encodeURIComponent(appId)}/`;
            externalLabel = 'Ver en Steam Store ↗';
          } else if (gameKey.startsWith('WIKI-')) {
            const pageId = gameKey.replace('WIKI-', '');
            externalUrl = `https://en.wikipedia.org/?curid=${encodeURIComponent(pageId)}`;
            externalLabel = 'Ver en Wikipedia ↗';
          }

          const externalLink = externalUrl
            ? `<a href="${externalUrl}" target="_blank" rel="noopener noreferrer" class="catalog-external-link" onclick="event.stopPropagation()">${externalLabel}</a>`
            : '';

          return `
            <div class="catalog-result-item" data-release-id="${escapeHtml(gameKey)}" onclick="app.selectCatalogEdition(this, '${escapeHtml(gameKey)}')">
              ${thumbHtml}
              <div class="catalog-result-info">
                <div class="catalog-result-title" title="${escapeHtml(devAndTitle)}">${escapeHtml(devAndTitle)}</div>
                <div class="catalog-result-badges">${badges.join('')}</div>
                <div class="catalog-result-footer">
                  <span class="catalog-result-id-pill">ID: ${escapeHtml(gameKey)}</span>
                  ${externalLink}
                </div>
              </div>
              <div class="catalog-check-indicator" aria-hidden="true">✓</div>
            </div>
          `;
        }).join('');
        return;
      }

      const results = await apiFetch(`/api/discogs/search?query=${encodeURIComponent(query)}`);
      state.catalogSearchResults = results || [];
      if (!results || results.length === 0) {
        resultsContainer.innerHTML = `
          <div style="font-size:0.84rem; color: var(--text-muted); text-align: center; padding: 1.25rem;">
            No se encontraron ediciones para esta búsqueda. Prueba con otro término.
          </div>
        `;
        return;
      }

      resultsContainer.innerHTML = results.map(item => {
        const releaseId = item.releaseId ?? item.discogsReleaseId ?? item.id;
        const thumbHtml = item.coverThumbUrl
          ? `<img src="${escapeHtml(item.coverThumbUrl)}" alt="" class="catalog-result-thumb" loading="lazy" onerror="this.outerHTML='<div class=\\'catalog-result-thumb\\'>💿</div>'">`
          : `<div class="catalog-result-thumb">💿</div>`;

        const artistAndTitle = item.artist && item.artist !== 'Desconocido'
          ? `${item.artist} — ${item.title}`
          : item.title;

        const badges = [];
        if (item.format) badges.push(`<span class="catalog-meta-badge highlight">💿 ${escapeHtml(item.format)}</span>`);
        if (item.year) badges.push(`<span class="catalog-meta-badge">📅 ${escapeHtml(item.year)}</span>`);
        if (item.country) badges.push(`<span class="catalog-meta-badge">🌍 ${escapeHtml(item.country)}</span>`);
        if (item.label) badges.push(`<span class="catalog-meta-badge">🏷️ ${escapeHtml(item.label)}</span>`);
        if (item.catalogueNumber) badges.push(`<span class="catalog-meta-badge">🔢 ${escapeHtml(item.catalogueNumber)}</span>`);
        if (item.genre) badges.push(`<span class="catalog-meta-badge">🎭 ${escapeHtml(item.genre)}</span>`);

        const externalLink = `<a href="https://www.discogs.com/release/${encodeURIComponent(releaseId)}" target="_blank" rel="noopener noreferrer" class="catalog-external-link" onclick="event.stopPropagation()">Ver en Discogs ↗</a>`;

        return `
          <div class="catalog-result-item" data-release-id="${releaseId}" onclick="app.selectCatalogEdition(this, '${releaseId}')">
            ${thumbHtml}
            <div class="catalog-result-info">
              <div class="catalog-result-title" title="${escapeHtml(artistAndTitle)}">${escapeHtml(artistAndTitle)}</div>
              <div class="catalog-result-badges">${badges.join('')}</div>
              <div class="catalog-result-footer">
                <span class="catalog-result-id-pill">ID: #${escapeHtml(releaseId)}</span>
                ${externalLink}
              </div>
            </div>
            <div class="catalog-check-indicator" aria-hidden="true">✓</div>
          </div>
        `;
      }).join('');
    } catch (err) {
      resultsContainer.innerHTML = `<div style="font-size:0.82rem; color: var(--accent-ruby); text-align: center; padding: 0.75rem;">Error: ${escapeHtml(err.message)}</div>`;
    }
  }

  async function submitEnrich() {
    const releaseId = document.getElementById('enrichReleaseId').value;
    const selectedIdVal = document.getElementById('enrichDiscogsId').value;
    if (!selectedIdVal) {
      showToast('Selecciona una Edición', 'Toca una de las ediciones de la lista para vincularla.', 'error');
      return;
    }

    const submitBtn = document.getElementById('btnSubmitEnrich');
    if (submitBtn) submitBtn.disabled = true;

    try {
      if (state.enrichCategory === 'Books') {
        const bookEd = state.selectedBookEdition || {};
        await apiFetch(`/api/releases/${releaseId}/enrich-book`, {
          method: 'POST',
          body: JSON.stringify({
            editionKey: selectedIdVal,
            title: bookEd.title || null,
            author: bookEd.author || null,
            publisher: bookEd.publisher || null,
            publishYear: bookEd.publishYear || null,
            pagesCount: bookEd.pagesCount || null,
            coverImageUrl: bookEd.coverImageUrl || null,
            isbn: bookEd.isbn || null,
            genre: bookEd.genre || null,
            overview: bookEd.overview || null
          })
        });

        showToast('📚 Edición Bibliográfica Vinculada', 'Portada oficial, editorial y páginas importadas desde OpenLibrary.', 'success');
        closeEnrichModal();
        await refreshAll();
        return;
      }

      if (state.enrichCategory === 'VideoGames') {
        const gameEd = state.selectedGameEdition || {};
        await apiFetch(`/api/releases/${releaseId}/enrich-game`, {
          method: 'POST',
          body: JSON.stringify({
            gameKey: selectedIdVal,
            title: gameEd.title || null,
            developer: gameEd.developer || null,
            publisher: gameEd.publisher || null,
            releaseYear: gameEd.releaseYear || null,
            platform: gameEd.platform || null,
            coverImageUrl: gameEd.coverImageUrl || null,
            genre: gameEd.genre || null,
            overview: gameEd.overview || null
          })
        });

        showToast('🎮 Videojuego Vinculado', 'Carátula oficial, estudio y plataforma importados del catálogo.', 'success');
        closeEnrichModal();
        await refreshAll();
        return;
      }

      const discogsReleaseId = parseInt(selectedIdVal, 10);
      await apiFetch(`/api/releases/${releaseId}/enrich`, {
        method: 'POST',
        body: JSON.stringify({ discogsReleaseId })
      });

      showToast('Edición Vinculada', 'Portada oficial, sello y lista de pistas importados del catálogo.', 'success');
      closeEnrichModal();
      await refreshAll();
    } catch (err) {
      showToast('Error al Vincular', err.message, 'error');
      if (submitBtn) submitBtn.disabled = false;
    }
  }

  // ============================================================================
  // Eliminación de Ejemplares
  // ============================================================================
  async function deleteRelease(id) {
    const release = state.allReleases.find(r => r.id === id) || state.releases.find(r => r.id === id);
    const title = release ? `"${release.title}"` : 'este ejemplar';
    if (!confirm(`¿Estás seguro de que deseas eliminar ${title} de tu bóveda? Esta acción no se puede deshacer.`)) {
      return;
    }

    try {
      await apiFetch(`/api/releases/${id}`, { method: 'DELETE' });
      showToast('Ejemplar Eliminado', 'Se ha eliminado de tu bóveda.', 'info');
      await refreshAll();
    } catch (err) {
      showToast('Error al Eliminar', err.message, 'error');
    }
  }

  // ============================================================================
  // Ciclo de Vida e Inicialización
  // ============================================================================
  async function refreshAll() {
    await Promise.all([loadStatistics(), loadReleases()]);
  }

  async function init() {
    const scrollStage = document.getElementById('vaultScrollStage');
    if (scrollStage) {
      scrollStage.addEventListener('scroll', () => {
        if (state.currentCategory && state.pendingRestoreScroll === null) {
          state.categoryScrollPositions[state.currentCategory] = scrollStage.scrollTop;
        }
      }, { passive: true });
    }

    if (state.token) {
      try {
        const welcomeView = document.getElementById('welcomeView');
        const vaultApp = document.getElementById('vaultApp');
        if (welcomeView) welcomeView.style.display = 'none';
        if (vaultApp) vaultApp.style.display = 'flex';

        renderAuthStatus();
        await apiFetch('/auth/manage/info');
        await refreshAll();
      } catch (err) {
        console.warn('Token no válido al arrancar:', err);
        logout(false);
      }
    } else {
      transitionToWelcome();
    }
  }

  // Exponer interfaz pública a window.app
  window.app = {
    init,
    switchWelcomeTab,
    toggleWelcomePasswordVisibility,
    handleWelcomeSubmit,
    transitionToVault,
    transitionToWelcome,
    refreshAll,
    openAuthModal,
    closeAuthModal,
    switchAuthTab,
    handleAuthSubmit,
    logout,
    setCategory,
    switchMobileNav,
    setFormatFilter,
    setGenreFilter,
    handleSearchInput,
    openScannerModal,
    closeScannerModal,
    switchScannerTab,
    toggleCameraFacing,
    captureFromCamera,
    handleFileSelect,
    resetScanPreview,
    retryScannerCapture,
    switchScannerToManualCreate,
    selectScanFormat,
    submitScan,
    selectGoldmineGrade,
    openCreateModal,
    closeCreateModal,
    toggleFormatFields,
    handleCreateSubmit,
    openDetailModal,
    closeDetailModal,
    openConfirmModal,
    closeConfirmModal,
    submitConfirm,
    openEnrichModal,
    closeEnrichModal,
    selectCatalogEdition,
    searchDiscogsForEnrich,
    submitEnrich,
    deleteRelease,
    getPlaceholderGroovesHtml
  };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
