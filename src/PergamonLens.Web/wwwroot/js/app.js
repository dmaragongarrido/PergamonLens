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
    releases: [],
    statistics: null,
    currentFilter: 'all',
    searchQuery: '',
    searchDebounceTimer: null,
    selectedScanBlob: null,
    selectedScanFileName: null,
    cameraStream: null,
    cameraFacingMode: 'environment', // Trasera por defecto para capturar álbumes
    authMode: 'login' // 'login' | 'register'
  };

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
        // Token caducado o inválido
        logout(false);
        showToast('Sesión Expirada', 'Por favor inicia sesión de nuevo para acceder a tu colección.', 'error');
        openAuthModal('login');
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
            // Formato RFC 7807 ValidationProblemDetails
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
  // Autenticación de Usuario
  // ============================================================================
  function renderAuthStatus() {
    const container = document.getElementById('authStatusContainer');
    if (!container) return;

    if (state.token && state.userEmail) {
      container.innerHTML = `
        <div class="user-profile-badge">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
            <circle cx="12" cy="7" r="4"></circle>
          </svg>
          <span class="user-email" title="${escapeHtml(state.userEmail)}">${escapeHtml(state.userEmail)}</span>
          <button class="btn btn-sm btn-secondary" onclick="app.logout(true)" title="Cerrar Sesión" style="padding: 0.2rem 0.5rem; margin-left: 0.25rem;">
            Salir
          </button>
        </div>
      `;
    } else {
      container.innerHTML = `
        <button class="btn btn-outline-gold btn-sm" onclick="app.openAuthModal('login')">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"></path>
            <polyline points="10 17 15 12 10 7"></polyline>
            <line x1="15" y1="12" x2="3" y2="12"></line>
          </svg>
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
        // 1. Registro
        await apiFetch('/auth/register', {
          method: 'POST',
          body: JSON.stringify({ email, password })
        });
        showToast('Cuenta Creada', 'Registro completado con éxito. Iniciando sesión...', 'success');
      }

      // 2. Login para obtener el token Bearer
      const loginResult = await apiFetch('/auth/login', {
        method: 'POST',
        body: JSON.stringify({ email, password })
      });

      if (loginResult && loginResult.accessToken) {
        state.token = loginResult.accessToken;
        state.userEmail = email;
        localStorage.setItem(STORAGE_TOKEN_KEY, state.token);
        localStorage.setItem(STORAGE_EMAIL_KEY, state.userEmail);

        renderAuthStatus();
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
    state.releases = [];
    state.statistics = null;
    localStorage.removeItem(STORAGE_TOKEN_KEY);
    localStorage.removeItem(STORAGE_EMAIL_KEY);

    renderAuthStatus();
    renderStatistics(null);
    renderReleases([]);

    if (showMessage) {
      showToast('Sesión Cerrada', 'Has cerrado sesión correctamente.', 'info');
    }
  }

  // ============================================================================
  // Carga de Datos: Colección & Estadísticas
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

  function renderStatistics(stats) {
    document.getElementById('statTotal').textContent = stats ? (stats.totalReleases || 0) : '0';
    document.getElementById('statVinyls').textContent = stats ? (stats.formatBreakdown?.Vinyl || 0) : '0';
    document.getElementById('statCds').textContent = stats ? (stats.formatBreakdown?.CD || 0) : '0';
    document.getElementById('statCassettes').textContent = stats ? (stats.formatBreakdown?.Cassette || 0) : '0';
    document.getElementById('statTracks').textContent = stats ? (stats.totalTracks || 0) : '0';
    document.getElementById('statConfirmed').textContent = stats ? (stats.confirmedReleases || 0) : '0';
  }

  async function loadReleases() {
    if (!state.token) {
      renderReleases([]);
      return;
    }

    const grid = document.getElementById('releasesGrid');
    grid.innerHTML = `
      <div style="grid-column: 1 / -1; text-align: center; padding: 3rem; color: var(--text-secondary);">
        <div class="spinner" style="margin: 0 auto 1rem; width: 28px; height: 28px; border-width: 3px;"></div>
        Cargando bóveda de lanzamientos...
      </div>
    `;

    try {
      const params = new URLSearchParams();
      if (state.currentFilter && state.currentFilter !== 'all') {
        params.append('format', state.currentFilter);
      }
      if (state.searchQuery && state.searchQuery.trim().length > 0) {
        params.append('q', state.searchQuery.trim());
      }

      const queryString = params.toString() ? `?${params.toString()}` : '';
      const releases = await apiFetch(`/api/releases${queryString}`);
      state.releases = releases || [];
      renderReleases(state.releases);
    } catch (err) {
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

  function renderReleases(releases) {
    const grid = document.getElementById('releasesGrid');
    if (!grid) return;

    if (!state.token) {
      grid.innerHTML = `
        <div class="vault-empty-state">
          <div class="empty-state-icon">🏛️</div>
          <h3>Bienvenido a tu Bóveda Personal</h3>
          <p>Inicia sesión o crea una cuenta gratuita de coleccionista para empezar a catalogar y preservar tus vinilos, CDs y casetes con IA.</p>
          <button class="btn btn-primary" onclick="app.openAuthModal('login')">Acceder a mi Colección</button>
        </div>
      `;
      return;
    }

    if (!releases || releases.length === 0) {
      grid.innerHTML = `
        <div class="vault-empty-state">
          <svg class="empty-state-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="10"></circle>
            <circle cx="12" cy="12" r="3"></circle>
          </svg>
          <h3>No se encontraron lanzamientos</h3>
          <p>${state.searchQuery ? 'Prueba a cambiar los términos de búsqueda o filtros.' : 'Tu colección está vacía. Escanea tu primer álbum físico con la cámara o añádelo manualmente.'}</p>
          <div style="display: flex; gap: 0.75rem;">
            <button class="btn btn-primary" onclick="app.openScannerModal()">Escanear Portada</button>
            <button class="btn btn-secondary" onclick="app.openCreateModal()">Alta Manual</button>
          </div>
        </div>
      `;
      return;
    }

    grid.innerHTML = releases.map(release => createReleaseCardHtml(release)).join('');
  }

  function createReleaseCardHtml(r) {
    const formatClass = (r.format || 'Vinyl').toLowerCase();
    const statusClass = (r.status || 'PendingAnalysis').toLowerCase();

    // Portada o representación visual con ranuras de vinilo
    const coverHtml = r.coverImageUrl
      ? `<img src="${escapeHtml(r.coverImageUrl)}" alt="${escapeHtml(r.title)}" class="cover-image" loading="lazy" onerror="this.onerror=null; this.src=''; this.parentElement.innerHTML=app.getPlaceholderGroovesHtml('${escapeHtml(r.format)}');">`
      : getPlaceholderGroovesHtml(r.format);

    // Pill de condición Goldmine si está disponible
    const conditionHtml = r.mediaCondition
      ? `<div class="condition-pill" title="Condición Goldmine">🏅 ${escapeHtml(r.mediaCondition)}</div>`
      : '';

    // Estado amigable
    let statusLabel = r.status;
    if (r.status === 'PendingAnalysis') statusLabel = 'Pendiente IA';
    else if (r.status === 'Identified') statusLabel = 'Identificado';
    else if (r.status === 'Enriched') statusLabel = 'Enriquecido';
    else if (r.status === 'Confirmed') statusLabel = 'Confirmado';

    const yearLabel = r.releaseYear ? `${r.releaseYear}` : 'Año desc.';
    const tracksCount = r.tracks ? r.tracks.length : 0;

    return `
      <article class="release-card" data-id="${r.id}">
        <div class="card-cover-wrapper" onclick="app.openDetailModal('${r.id}')" style="cursor: pointer;">
          ${coverHtml}
          <div class="card-badges-top">
            <span class="format-badge ${formatClass}">${escapeHtml(r.format)}</span>
            <span class="status-badge ${statusClass}">
              <span class="status-dot"></span>
              ${escapeHtml(statusLabel)}
            </span>
          </div>
        </div>

        <div class="card-body">
          <h4 class="card-title" title="${escapeHtml(r.title)}">${escapeHtml(r.title)}</h4>
          <div class="card-artist" title="${escapeHtml(r.artist)}">${escapeHtml(r.artist)}</div>

          <div class="card-meta-row">
            <span>${yearLabel}</span>
            <span class="card-meta-dot">•</span>
            <span>${escapeHtml(r.label || 'Sello no esp.')}</span>
            <span class="card-meta-dot">•</span>
            <span>${tracksCount} pistas</span>
          </div>

          ${conditionHtml ? `<div style="margin-bottom: 0.75rem;">${conditionHtml}</div>` : ''}

          <div class="card-actions">
            <button class="card-btn" onclick="app.openDetailModal('${r.id}')" title="Ver Ficha y Pistas">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path><circle cx="12" cy="12" r="3"></circle></svg>
              Ver
            </button>
            <button class="card-btn" onclick="app.openConfirmModal('${r.id}')" title="Graduación Goldmine">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"></polygon></svg>
              Calificar
            </button>
            <button class="card-btn" onclick="app.openEnrichModal('${r.id}')" title="Enriquecer con Discogs">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21.5 2v6h-6M21.34 15.57a10 10 0 1 1-.57-8.38l5.67-5.67"/></svg>
              Discogs
            </button>
            <button class="card-btn card-btn-danger" onclick="app.deleteRelease('${r.id}')" title="Eliminar de la Colección" style="flex: 0 0 34px;">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
            </button>
          </div>
        </div>
      </article>
    `;
  }

  function getPlaceholderGroovesHtml(format) {
    const isVinyl = (format || '').toLowerCase().includes('vinyl');
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
  // Filtros & Búsqueda
  // ============================================================================
  function setFormatFilter(format) {
    state.currentFilter = format;
    const chips = document.querySelectorAll('#formatFilters .chip-btn');
    chips.forEach(chip => {
      chip.classList.toggle('active', chip.dataset.format === format);
    });
    loadReleases();
  }

  function handleSearchInput(e) {
    clearTimeout(state.searchDebounceTimer);
    state.searchDebounceTimer = setTimeout(() => {
      state.searchQuery = e.target.value;
      loadReleases();
    }, 350);
  }

  // ============================================================================
  // Escáner de Cámara & Subida de Imágenes
  // ============================================================================
  function openScannerModal() {
    if (!state.token) {
      showToast('Autenticación Necesaria', 'Inicia sesión antes de escanear un lanzamiento.', 'info');
      openAuthModal('login');
      return;
    }
    const modal = document.getElementById('scannerModal');
    if (modal) modal.classList.add('active');
    switchScannerTab('camera');
    startCamera();
  }

  function closeScannerModal() {
    stopCamera();
    resetScanPreview();
    const modal = document.getElementById('scannerModal');
    if (modal) modal.classList.remove('active');
  }

  function switchScannerTab(tab) {
    const tabCam = document.getElementById('tabLiveCamera');
    const tabUp = document.getElementById('tabUploadFile');
    const secCam = document.getElementById('liveCameraSection');
    const secUp = document.getElementById('uploadFileSection');

    if (tab === 'camera') {
      tabCam.classList.add('active');
      tabUp.classList.remove('active');
      secCam.style.display = 'block';
      secUp.style.display = 'none';
      startCamera();
    } else {
      tabUp.classList.add('active');
      tabCam.classList.remove('active');
      secCam.style.display = 'none';
      secUp.style.display = 'block';
      stopCamera();
    }
  }

  async function startCamera() {
    stopCamera();
    const video = document.getElementById('cameraVideo');
    if (!video || !navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      console.warn('getUserMedia no soportado en este entorno. Usa subida de archivo o móvil nativo.');
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
      // El usuario puede seguir usando el input nativo móvil o la pestaña de archivo
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
      showToast('Cámara No Lista', 'Espera a que el flujo de vídeo de la cámara esté activo.', 'error');
      return;
    }

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

    canvas.toBlob(blob => {
      if (blob) {
        setScanFile(blob, `camera_capture_${Date.now()}.jpg`, 'Foto capturada con cámara');
      }
    }, 'image/jpeg', 0.92);
  }

  function handleFileSelect(e) {
    const files = e.target.files;
    if (!files || files.length === 0) return;
    const file = files[0];
    setScanFile(file, file.name, `Archivo (${(file.size / 1024).toFixed(1)} KB)`);
  }

  function setScanFile(blob, filename, description) {
    state.selectedScanBlob = blob;
    state.selectedScanFileName = filename;

    const previewContainer = document.getElementById('scanPreviewContainer');
    const previewImg = document.getElementById('scanPreviewImg');
    const previewName = document.getElementById('scanPreviewName');
    const previewSize = document.getElementById('scanPreviewSize');
    const btnSubmit = document.getElementById('btnSubmitScan');

    const objectUrl = URL.createObjectURL(blob);
    previewImg.src = objectUrl;
    previewName.textContent = filename;
    previewSize.textContent = description;

    previewContainer.style.display = 'flex';
    btnSubmit.disabled = false;
  }

  function resetScanPreview() {
    state.selectedScanBlob = null;
    state.selectedScanFileName = null;
    const previewContainer = document.getElementById('scanPreviewContainer');
    const btnSubmit = document.getElementById('btnSubmitScan');
    if (previewContainer) previewContainer.style.display = 'none';
    if (btnSubmit) btnSubmit.disabled = true;

    const fileUploadInput = document.getElementById('fileUploadInput');
    const mobileInput = document.getElementById('mobileDirectCameraInput');
    if (fileUploadInput) fileUploadInput.value = '';
    if (mobileInput) mobileInput.value = '';
  }

  async function submitScan() {
    if (!state.selectedScanBlob) {
      showToast('Imagen requerida', 'Captura una foto con la cámara o selecciona un archivo.', 'error');
      return;
    }

    const format = document.getElementById('scanFormatSelect').value;
    const btnSubmit = document.getElementById('btnSubmitScan');
    const btnSubmitText = document.getElementById('btnSubmitScanText');

    btnSubmit.disabled = true;
    btnSubmitText.innerHTML = '<span class="spinner" style="display:inline-block; vertical-align:middle; margin-right:0.4rem;"></span> Ingestando con IA...';

    const formData = new FormData();
    formData.append('file', state.selectedScanBlob, state.selectedScanFileName || 'scan.jpg');
    const effectiveFormat = format === 'Auto' ? 'Vinyl' : format;
    formData.append('format', effectiveFormat);

    try {
      const result = await apiFetch('/api/releases/scan', {
        method: 'POST',
        body: formData
      });

      showToast('Álbum Identificado', `Detectado: "${result.title}" de ${result.artist}`, 'success');
      closeScannerModal();
      await refreshAll();

      // Abrir automáticamente la ficha o modal de calificación Goldmine
      if (result.id) {
        openConfirmModal(result.id);
      }
    } catch (err) {
      showToast('Fallo en Análisis de Imagen', err.message, 'error');
    } finally {
      btnSubmit.disabled = false;
      btnSubmitText.textContent = 'Analizar con IA Multimodal';
    }
  }

  // ============================================================================
  // Alta Manual de Lanzamientos
  // ============================================================================
  function openCreateModal() {
    if (!state.token) {
      showToast('Autenticación Necesaria', 'Inicia sesión antes de añadir un lanzamiento.', 'info');
      openAuthModal('login');
      return;
    }
    const form = document.getElementById('createReleaseForm');
    if (form) form.reset();
    toggleFormatFields('Vinyl');
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
      vinylFields.style.display = format === 'Vinyl' ? 'grid' : 'none';
    }
  }

  async function handleCreateSubmit(e) {
    e.preventDefault();
    const artist = document.getElementById('createArtist').value.trim();
    const title = document.getElementById('createTitle').value.trim();
    const format = document.getElementById('createFormat').value;
    const yearVal = document.getElementById('createYear').value;
    const barcode = document.getElementById('createBarcode').value.trim() || null;
    const catalogueNumber = document.getElementById('createCatalogNumber').value.trim() || null;
    const label = document.getElementById('createLabel').value.trim() || null;
    const country = document.getElementById('createCountry').value.trim() || null;
    const coverImageUrl = document.getElementById('createCoverUrl').value.trim() || null;
    const notes = document.getElementById('createNotes').value.trim() || null;

    let vinylSize = null;
    let vinylSpeed = null;
    if (format === 'Vinyl') {
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
      discsCount: 1
    };

    try {
      const created = await apiFetch('/api/releases', {
        method: 'POST',
        body: JSON.stringify(payload)
      });

      showToast('Guardado', `"${created.title}" se ha añadido a tu colección.`, 'success');
      closeCreateModal();
      await refreshAll();
    } catch (err) {
      showToast('Error de Validación', err.message, 'error');
    }
  }

  // ============================================================================
  // Modal de Detalle & Lista de Pistas (Tracklist)
  // ============================================================================
  async function openDetailModal(id) {
    const modal = document.getElementById('detailModal');
    const body = document.getElementById('detailModalBody');
    const headerTitle = document.getElementById('detailHeaderTitle');
    const footer = document.getElementById('detailModalFooter');

    modal.classList.add('active');
    body.innerHTML = '<div style="text-align: center; padding: 2rem;"><div class="spinner" style="margin: 0 auto 0.5rem;"></div>Cargando ficha...</div>';

    try {
      const release = await apiFetch(`/api/releases/${id}`);
      headerTitle.textContent = `${release.artist} — ${release.title}`;

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
                    <td class="track-dur">${escapeHtml(t.duration || '--:--')}</td>
                  </tr>
                `).join('')}
              </tbody>
            </table>
          </div>
        `
        : '<p style="font-size: 0.85rem; color: var(--text-muted); font-style: italic;">No hay pistas registradas todavía. Puedes enriquecer este álbum con Discogs para importar el tracklist oficial.</p>';

      body.innerHTML = `
        <div style="display: flex; gap: 1.25rem; flex-wrap: wrap;">
          <div style="width: 140px; height: 140px; flex-shrink: 0; border-radius: var(--radius-md); overflow: hidden; background: #000;">
            ${release.coverImageUrl
              ? `<img src="${escapeHtml(release.coverImageUrl)}" alt="Portada" style="width: 100%; height: 100%; object-fit: cover;">`
              : getPlaceholderGroovesHtml(release.format)
            }
          </div>
          <div style="flex: 1; min-width: 200px;">
            <div style="display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.35rem;">
              <span class="format-badge ${release.format.toLowerCase()}">${escapeHtml(release.format)}</span>
              <span class="status-badge ${release.status.toLowerCase()}">${escapeHtml(release.status)}</span>
              ${release.mediaCondition ? `<span class="condition-pill">🏅 ${escapeHtml(release.mediaCondition)}</span>` : ''}
            </div>
            <h3 style="font-size: 1.25rem; color: #fff; margin-bottom: 0.2rem;">${escapeHtml(release.title)}</h3>
            <div style="font-size: 1rem; color: var(--accent-gold); font-weight: 600; margin-bottom: 0.6rem;">${escapeHtml(release.artist)}</div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 0.4rem; font-size: 0.82rem; color: var(--text-secondary);">
              <div><strong>Año:</strong> ${release.releaseYear || 'Desconocido'}</div>
              <div><strong>Sello:</strong> ${escapeHtml(release.label || 'N/D')}</div>
              <div><strong>País:</strong> ${escapeHtml(release.country || 'N/D')}</div>
              <div><strong>Catálogo:</strong> ${escapeHtml(release.catalogueNumber || 'N/D')}</div>
              <div><strong>Código de Barras:</strong> ${escapeHtml(release.barcode || 'N/D')}</div>
              <div><strong>ID Discogs:</strong> ${release.discogsReleaseId ? `<a href="https://www.discogs.com/release/${release.discogsReleaseId}" target="_blank" rel="noopener" style="color: var(--accent-cyan);">${release.discogsReleaseId} ↗</a>` : 'Sin vincular'}</div>
            </div>
          </div>
        </div>

        ${release.notes ? `
          <div style="background: rgba(11, 15, 23, 0.4); border-left: 3px solid var(--accent-gold); padding: 0.65rem 0.85rem; border-radius: var(--radius-sm); font-size: 0.84rem;">
            <strong>Notas del Coleccionista:</strong> ${escapeHtml(release.notes)}
          </div>
        ` : ''}

        <div style="margin-top: 0.5rem;">
          <h4 style="font-size: 0.95rem; color: #fff; margin-bottom: 0.65rem;">Lista de Pistas Oficial (${release.tracks ? release.tracks.length : 0})</h4>
          ${tracksHtml}
        </div>
      `;

      footer.innerHTML = `
        <button class="btn btn-secondary" onclick="app.closeDetailModal()">Cerrar</button>
        <button class="btn btn-outline-gold" onclick="app.closeDetailModal(); app.openConfirmModal('${release.id}')">Calificar Goldmine</button>
        <button class="btn btn-primary" onclick="app.closeDetailModal(); app.openEnrichModal('${release.id}')">Enriquecer con Discogs</button>
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
  // Graduación Goldmine & Confirmación
  // ============================================================================
  function openConfirmModal(releaseId) {
    document.getElementById('confirmReleaseId').value = releaseId;
    const release = state.releases.find(r => r.id === releaseId);
    if (release) {
      if (release.mediaCondition) document.getElementById('confirmMediaCondition').value = release.mediaCondition;
      if (release.sleeveCondition) document.getElementById('confirmSleeveCondition').value = release.sleeveCondition;
      if (release.notes) document.getElementById('confirmNotes').value = release.notes;
    }
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

      showToast('Álbum Confirmado', 'Graduación Goldmine guardada con éxito.', 'success');
      closeConfirmModal();
      await refreshAll();
    } catch (err) {
      showToast('Error al Confirmar', err.message, 'error');
    }
  }

  // ============================================================================
  // Enriquecimiento con Discogs API
  // ============================================================================
  function openEnrichModal(releaseId) {
    document.getElementById('enrichReleaseId').value = releaseId;
    document.getElementById('enrichDiscogsId').value = '';
    document.getElementById('enrichSearchResults').innerHTML = '';

    const release = state.releases.find(r => r.id === releaseId);
    if (release) {
      document.getElementById('enrichSearchQuery').value = `${release.artist} ${release.title}`;
    }

    const modal = document.getElementById('enrichModal');
    if (modal) modal.classList.add('active');
  }

  function closeEnrichModal() {
    const modal = document.getElementById('enrichModal');
    if (modal) modal.classList.remove('active');
  }

  async function searchDiscogsForEnrich() {
    const query = document.getElementById('enrichSearchQuery').value.trim();
    const resultsContainer = document.getElementById('enrichSearchResults');
    if (!query) return;

    resultsContainer.innerHTML = '<div style="font-size:0.8rem; color: var(--text-secondary); text-align: center; padding: 0.5rem;"><div class="spinner" style="display:inline-block; margin-right:0.3rem;"></div> Buscando en Discogs...</div>';

    try {
      const results = await apiFetch(`/api/discogs/search?query=${encodeURIComponent(query)}`);
      if (!results || results.length === 0) {
        resultsContainer.innerHTML = '<div style="font-size:0.8rem; color: var(--text-muted); text-align: center; padding: 0.5rem;">No se encontraron resultados en Discogs.</div>';
        return;
      }

      resultsContainer.innerHTML = results.map(item => `
        <div style="background: var(--bg-card); border: 1px solid var(--border-subtle); padding: 0.45rem 0.75rem; border-radius: var(--radius-sm); display: flex; align-items: center; justify-content: space-between; cursor: pointer; transition: border-color 0.15s;"
             onclick="document.getElementById('enrichDiscogsId').value = '${item.discogsReleaseId}'; this.style.borderColor='var(--accent-gold)';"
             onmouseover="this.style.background='var(--bg-card-hover)'"
             onmouseout="this.style.background='var(--bg-card)'">
          <div style="font-size: 0.82rem; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; margin-right: 0.5rem;">
            <strong>${escapeHtml(item.title)}</strong> ${item.year ? `(${item.year})` : ''}
          </div>
          <span style="font-size: 0.75rem; color: var(--accent-gold); font-weight: 700; white-space: nowrap;">ID: ${item.discogsReleaseId}</span>
        </div>
      `).join('');
    } catch (err) {
      resultsContainer.innerHTML = `<div style="font-size:0.8rem; color: var(--accent-ruby); text-align: center; padding: 0.5rem;">Error: ${escapeHtml(err.message)}</div>`;
    }
  }

  async function submitEnrich() {
    const releaseId = document.getElementById('enrichReleaseId').value;
    const discogsIdVal = document.getElementById('enrichDiscogsId').value;
    if (!discogsIdVal) {
      showToast('ID Requerido', 'Introduce un ID de Discogs o selecciona uno de los resultados.', 'error');
      return;
    }

    const discogsReleaseId = parseInt(discogsIdVal, 10);

    try {
      await apiFetch(`/api/releases/${releaseId}/enrich`, {
        method: 'POST',
        body: JSON.stringify({ discogsReleaseId })
      });

      showToast('Álbum Enriquecido', 'Metadatos y lista de pistas importados de Discogs.', 'success');
      closeEnrichModal();
      await refreshAll();
    } catch (err) {
      showToast('Error al Enriquecer', err.message, 'error');
    }
  }

  // ============================================================================
  // Eliminación de Lanzamientos
  // ============================================================================
  async function deleteRelease(id) {
    const release = state.releases.find(r => r.id === id);
    const title = release ? `"${release.title}"` : 'este lanzamiento';
    if (!confirm(`¿Estás seguro de que deseas eliminar ${title} de tu colección? Esta acción no se puede deshacer.`)) {
      return;
    }

    try {
      await apiFetch(`/api/releases/${id}`, { method: 'DELETE' });
      showToast('Lanzamiento Eliminado', 'Se ha eliminado de tu bóveda.', 'info');
      await refreshAll();
    } catch (err) {
      showToast('Error al Eliminar', err.message, 'error');
    }
  }

  // ============================================================================
  // Ciclo de Vida y Drag & Drop
  // ============================================================================
  async function refreshAll() {
    await Promise.all([loadStatistics(), loadReleases()]);
  }

  function setupDragAndDrop() {
    const dropZone = document.getElementById('dropZone');
    if (!dropZone) return;

    ['dragenter', 'dragover'].forEach(eventName => {
      dropZone.addEventListener(eventName, e => {
        e.preventDefault();
        e.stopPropagation();
        dropZone.classList.add('dragover');
      }, false);
    });

    ['dragleave', 'drop'].forEach(eventName => {
      dropZone.addEventListener(eventName, e => {
        e.preventDefault();
        e.stopPropagation();
        dropZone.classList.remove('dragover');
      }, false);
    });

    dropZone.addEventListener('drop', e => {
      const dt = e.dataTransfer;
      const files = dt.files;
      if (files && files.length > 0) {
        const file = files[0];
        setScanFile(file, file.name, `Archivo (${(file.size / 1024).toFixed(1)} KB)`);
      }
    }, false);
  }

  // Inicialización de la aplicación
  async function init() {
    renderAuthStatus();
    setupDragAndDrop();

    if (state.token) {
      // Comprobar validez de sesión con GET /auth/manage/info
      try {
        await apiFetch('/auth/manage/info');
        await refreshAll();
      } catch (err) {
        console.warn('Token no válido al arrancar:', err);
        logout(false);
      }
    } else {
      renderReleases([]);
    }
  }

  // Exponer interfaz pública a window.app
  window.app = {
    init,
    refreshAll,
    openAuthModal,
    closeAuthModal,
    switchAuthTab,
    handleAuthSubmit,
    logout,
    setFormatFilter,
    handleSearchInput,
    openScannerModal,
    closeScannerModal,
    switchScannerTab,
    toggleCameraFacing,
    captureFromCamera,
    handleFileSelect,
    resetScanPreview,
    submitScan,
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
    searchDiscogsForEnrich,
    submitEnrich,
    deleteRelease,
    getPlaceholderGroovesHtml
  };

  // Arrancar al cargar DOM
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();

