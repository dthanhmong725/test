// CyberForum - Notification Bell Module v2
class NotificationBell {
  static _pollInterval = null;
  static _POLL_MS = 15000; // 15 giây
  static _isOpen = false;
  static _notifications = [];
  static _unreadCount = 0;
  static _initialized = false;

  // ============================================================
  // KHỞI TẠO & HỦY
  // ============================================================
  static init() {
    if (!AuthManager.isAuthenticated()) return;
    if (this._initialized) return;
    this._initialized = true;

    this._injectUI();
    this._fetchCount();
    this._pollInterval = setInterval(() => this._fetchCount(), this._POLL_MS);

    // Đóng dropdown khi click bên ngoài
    document.addEventListener('click', (e) => {
      const btn = document.getElementById('notifBellBtn');
      const panel = document.getElementById('notifDropdown');
      if (!panel || !btn) return;
      if (!panel.contains(e.target) && !btn.contains(e.target)) {
        this._closeDropdown();
      }
    });
  }

  static destroy() {
    if (this._pollInterval) { clearInterval(this._pollInterval); this._pollInterval = null; }
    this._initialized = false;
    this._unreadCount = 0;
    this._notifications = [];
    this._isOpen = false;
    const badge = document.getElementById('notifBadge');
    if (badge) badge.style.display = 'none';
    const panel = document.getElementById('notifDropdown');
    if (panel) panel.remove();
  }

  // ============================================================
  // INJECT UI (badge + dropdown)
  // ============================================================
  static _injectUI() {
    const btn = document.getElementById('notificationBtn');
    if (!btn) return;

    // Đổi ID để quản lý dễ hơn
    btn.id = 'notifBellBtn';
    btn.style.position = 'relative';

    // --- Badge ---
    if (!document.getElementById('notifBadge')) {
      const badge = document.createElement('span');
      badge.id = 'notifBadge';
      badge.style.cssText = `
        display: none; position: absolute; top: -4px; right: -4px;
        background: #ff5a5f; color: #fff; font-size: 0.65rem; font-weight: 700;
        min-width: 18px; height: 18px; border-radius: 9px; padding: 0 4px;
        align-items: center; justify-content: center;
        border: 2px solid var(--bg-surface, #0d1117);
        z-index: 10; pointer-events: none;
      `;
      btn.appendChild(badge);
    }

    // --- Dropdown panel – gắn vào body để tránh bị overflow:hidden che ---
    if (document.getElementById('notifDropdown')) return;

    const panel = document.createElement('div');
    panel.id = 'notifDropdown';
    panel.style.cssText = `
      display: none;
      position: fixed;
      top: 60px; right: 16px;
      width: 380px; max-height: 520px;
      background: rgba(13, 17, 23, 0.97);
      backdrop-filter: blur(20px);
      -webkit-backdrop-filter: blur(20px);
      border: 1px solid rgba(255,255,255,0.08);
      border-radius: 16px;
      box-shadow: 0 20px 60px rgba(0,0,0,0.6), 0 0 0 1px rgba(0,229,160,0.05);
      z-index: 9999;
      overflow: hidden;
      flex-direction: column;
      transform-origin: top right;
      transition: opacity 0.2s ease, transform 0.2s ease;
      opacity: 0; transform: scale(0.95);
    `;
    panel.innerHTML = `
      <div style="display:flex;align-items:center;justify-content:space-between;padding:16px 20px;border-bottom:1px solid rgba(255,255,255,0.06);">
        <span style="font-size:1rem;font-weight:700;color:var(--text-primary,#e6edf3);display:flex;align-items:center;gap:8px;">
          <i class="ti ti-bell-ringing" style="color:var(--accent,#00e5a0);"></i> Thông báo
        </span>
        <button id="notifMarkAllBtn" title="Đánh dấu tất cả đã đọc" style="
          background: rgba(0,229,160,0.08); border: 1px solid rgba(0,229,160,0.2);
          color: var(--accent,#00e5a0); border-radius:8px; padding:5px 10px;
          font-size:0.75rem; font-weight:600; cursor:pointer; display:flex; align-items:center; gap:5px;
          transition: all 0.2s;
        ">
          <i class="ti ti-checks"></i> Đọc tất cả
        </button>
      </div>
      <div id="notifList" style="overflow-y:auto;max-height:420px;"></div>
      <div style="padding:10px 20px;border-top:1px solid rgba(255,255,255,0.06);text-align:center;">
        <span style="font-size:0.75rem;color:var(--text-muted,#7d8590);">Cập nhật mỗi 15 giây tự động</span>
      </div>
    `;
    document.body.appendChild(panel);

    // Gắn sự kiện toggle
    btn.addEventListener('click', (e) => {
      e.stopPropagation();
      this._isOpen ? this._closeDropdown() : this._openDropdown();
    });

    // Gắn sự kiện "Đọc tất cả"
    document.getElementById('notifMarkAllBtn')?.addEventListener('click', (e) => {
      e.stopPropagation();
      this._markAllRead();
    });
  }

  // ============================================================
  // VỊ TRÍ DROPDOWN theo nút chuông
  // ============================================================
  static _positionDropdown() {
    const btn = document.getElementById('notifBellBtn');
    const panel = document.getElementById('notifDropdown');
    if (!btn || !panel) return;
    const rect = btn.getBoundingClientRect();
    panel.style.top = (rect.bottom + 8) + 'px';
    // Đảm bảo không bị tràn ra ngoài màn hình
    const panelWidth = 380;
    let left = rect.right - panelWidth;
    if (left < 8) left = 8;
    panel.style.right = 'auto';
    panel.style.left = left + 'px';
  }

  // ============================================================
  // FETCH DỮ LIỆU
  // ============================================================
  static async _fetchCount() {
    if (!AuthManager.isAuthenticated()) return;
    try {
      const result = await API.notifications.getUnreadCount();
      // Hỗ trợ cả camelCase lẫn PascalCase từ server
      const count = result?.data?.unreadCount ?? result?.Data?.unreadCount ?? result?.data?.UnreadCount ?? 0;
      this._renderBadge(count);
    } catch (e) {
      console.warn('[NotificationBell] _fetchCount error:', e.message);
    }
  }

  static async _fetchNotifications() {
    const list = document.getElementById('notifList');
    if (list) {
      list.innerHTML = `
        <div style="display:flex;justify-content:center;align-items:center;padding:40px;color:var(--text-muted,#7d8590);">
          <div style="width:28px;height:28px;border:3px solid rgba(0,229,160,0.3);border-top-color:var(--accent,#00e5a0);border-radius:50%;animation:spin 0.8s linear infinite;"></div>
        </div>`;
    }
    try {
      const result = await API.notifications.getAll({ page: 1, pageSize: 20 });
      // Hỗ trợ cả camelCase (success) và PascalCase (Success)
      const ok = result?.success ?? result?.Success ?? false;
      if (ok) {
        this._notifications = result.data ?? result.Data ?? [];
        this._renderList();
      } else {
        throw new Error(result?.message || result?.Message || 'API trả về thất bại');
      }
    } catch (e) {
      console.error('[NotificationBell] _fetchNotifications error:', e);
      if (list) list.innerHTML = `
        <div style="display:flex;flex-direction:column;align-items:center;padding:40px;gap:8px;color:var(--text-muted,#7d8590);">
          <i class="ti ti-wifi-off" style="font-size:2rem;"></i>
          <span>Không thể tải thông báo</span>
          <span style="font-size:0.7rem;opacity:0.5;">${e.message || ''}</span>
        </div>`;
    }
  }

  // ============================================================
  // RENDER UI
  // ============================================================
  static _renderBadge(count) {
    this._unreadCount = count;
    const badge = document.getElementById('notifBadge');
    if (!badge) return;
    if (count > 0) {
      badge.textContent = count > 99 ? '99+' : count;
      badge.style.display = 'flex';
    } else {
      badge.style.display = 'none';
    }
  }

  static _renderList() {
    const list = document.getElementById('notifList');
    if (!list) return;

    if (!this._notifications.length) {
      list.innerHTML = `
        <div style="display:flex;flex-direction:column;align-items:center;padding:48px 20px;gap:10px;color:var(--text-muted,#7d8590);">
          <i class="ti ti-bell-off" style="font-size:2.5rem;opacity:0.4;"></i>
          <span style="font-size:0.875rem;">Chưa có thông báo nào</span>
        </div>`;
      return;
    }

    list.innerHTML = this._notifications.map(n => this._renderItem(n)).join('');

    list.querySelectorAll('.notif-item').forEach(el => {
      el.addEventListener('click', () => {
        const id = parseInt(el.dataset.id);
        const postId = el.dataset.postId ? parseInt(el.dataset.postId) : null;
        this._handleItemClick(id, postId);
      });
    });
  }

  static _renderItem(n) {
    const icons = {
      PostUpvote:      { icon: 'ti-thumb-up',       color: '#00e5a0', bg: 'rgba(0,229,160,0.15)' },
      PostDownvote:    { icon: 'ti-thumb-down',      color: '#ff5a5f', bg: 'rgba(255,90,95,0.15)' },
      Comment:         { icon: 'ti-message-circle',  color: '#388bfd', bg: 'rgba(56,139,253,0.15)' },
      CommentUpvote:   { icon: 'ti-thumb-up',        color: '#00e5a0', bg: 'rgba(0,229,160,0.15)' },
      CommentDownvote: { icon: 'ti-thumb-down',      color: '#ff5a5f', bg: 'rgba(255,90,95,0.15)' },
      Mention:         { icon: 'ti-at',              color: '#bc8cff', bg: 'rgba(188,140,255,0.15)' },
    };

    // Hỗ trợ cả camelCase (typeLabel) và PascalCase (TypeLabel)
    const typeKey = n.typeLabel || n.TypeLabel || '';
    const cfg = icons[typeKey] || { icon: 'ti-bell', color: '#7d8590', bg: 'rgba(255,255,255,0.05)' };
    const actorName = n.actorUsername || n.ActorUsername || '?';
    const initials = actorName.charAt(0).toUpperCase();
    const avatarUrl = n.actorAvatar || n.ActorAvatar;
    const message = n.message || n.Message || 'Thông báo mới';
    const timeAgo = n.timeAgo || n.TimeAgo || '';
    const isRead = n.isRead ?? n.IsRead ?? false;
    const postId = n.postId || n.PostId || '';

    const avatarHtml = avatarUrl
      ? `<img src="${avatarUrl}" alt="${actorName}" style="width:100%;height:100%;object-fit:cover;border-radius:50%;">`
      : `<span style="font-weight:700;font-size:0.9rem;">${initials}</span>`;

    return `
      <div class="notif-item" data-id="${n.id || n.Id}" data-post-id="${postId}" role="button" tabindex="0" style="
        display:flex; align-items:flex-start; gap:12px; padding:14px 20px;
        cursor:pointer; transition: background 0.2s;
        background: ${isRead ? 'transparent' : 'rgba(0,229,160,0.04)'};
        border-left: 3px solid ${isRead ? 'transparent' : 'var(--accent,#00e5a0)'};
        position: relative;
      " onmouseenter="this.style.background='rgba(255,255,255,0.04)'" onmouseleave="this.style.background='${isRead ? 'transparent' : 'rgba(0,229,160,0.04)'}'">
        <div style="position:relative;flex-shrink:0;">
          <div style="width:44px;height:44px;border-radius:50%;background:rgba(255,255,255,0.07);display:flex;align-items:center;justify-content:center;color:var(--text-primary,#e6edf3);overflow:hidden;">
            ${avatarHtml}
          </div>
          <div style="position:absolute;bottom:-2px;right:-2px;width:20px;height:20px;border-radius:50%;background:${cfg.bg};color:${cfg.color};display:flex;align-items:center;justify-content:center;border:2px solid rgba(13,17,23,0.97);">
            <i class="ti ${cfg.icon}" style="font-size:0.65rem;"></i>
          </div>
        </div>
        <div style="flex:1;min-width:0;">
          <div style="font-size:0.85rem;color:var(--text-primary,#e6edf3);line-height:1.4;margin-bottom:4px;font-weight:${isRead ? '400' : '500'};">
            ${this._escapeHtml(message)}
          </div>
          <div style="font-size:0.75rem;color:${cfg.color};display:flex;align-items:center;gap:4px;">
            <i class="ti ti-clock" style="font-size:0.7rem;"></i> ${timeAgo}
          </div>
        </div>
        ${!isRead ? `<div style="width:8px;height:8px;border-radius:50%;background:var(--accent,#00e5a0);flex-shrink:0;margin-top:4px;box-shadow:0 0 6px rgba(0,229,160,0.5);"></div>` : ''}
      </div>`;
  }

  // ============================================================
  // ACTIONS
  // ============================================================
  static async _handleItemClick(id, postId) {
    const item = document.querySelector(`.notif-item[data-id="${id}"]`);
    if (item && item.style.borderLeftColor !== 'transparent') {
      item.style.borderLeft = '3px solid transparent';
      item.style.background = 'transparent';
      const dot = item.querySelector('[style*="border-radius:50%;background:var(--accent"]');
      this._unreadCount = Math.max(0, this._unreadCount - 1);
      this._renderBadge(this._unreadCount);
      try { await API.notifications.markAsRead(id); } catch (e) {}
    }
    if (postId) {
      this._closeDropdown();
      window.location.href = `/post.html?id=${postId}`;
    }
  }

  static async _markAllRead() {
    const btn = document.getElementById('notifMarkAllBtn');
    if (btn) { btn.disabled = true; btn.innerHTML = '<i class="ti ti-loader-2" style="animation:spin 0.8s linear infinite;"></i> Đang xử lý...'; }

    try {
      await API.notifications.markAllAsRead();
      this._unreadCount = 0;
      this._renderBadge(0);

      // Cập nhật UI
      this._notifications.forEach(n => { n.isRead = true; n.IsRead = true; });
      this._renderList();

      if (typeof Toast !== 'undefined') Toast.show('Đã đánh dấu tất cả là đã đọc', 'success');
    } catch (e) {
      if (typeof Toast !== 'undefined') Toast.show('Không thể cập nhật thông báo', 'error');
    } finally {
      if (btn) { btn.disabled = false; btn.innerHTML = '<i class="ti ti-checks"></i> Đọc tất cả'; }
    }
  }

  // ============================================================
  // OPEN / CLOSE
  // ============================================================
  static _openDropdown() {
    const panel = document.getElementById('notifDropdown');
    if (!panel) return;

    this._positionDropdown();
    this._isOpen = true;
    panel.style.display = 'flex';
    requestAnimationFrame(() => {
      panel.style.opacity = '1';
      panel.style.transform = 'scale(1)';
    });

    // Tải thông báo (cả cũ và mới)
    this._fetchNotifications();
  }

  static _closeDropdown() {
    const panel = document.getElementById('notifDropdown');
    if (!panel) return;

    this._isOpen = false;
    panel.style.opacity = '0';
    panel.style.transform = 'scale(0.95)';
    setTimeout(() => {
      if (!this._isOpen) panel.style.display = 'none';
    }, 200);
  }

  // ============================================================
  // HELPER
  // ============================================================
  static _escapeHtml(str) {
    return (str || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
  }
}

// CSS animation cho spinner
if (!document.getElementById('notif-keyframes')) {
  const style = document.createElement('style');
  style.id = 'notif-keyframes';
  style.textContent = `@keyframes spin { to { transform: rotate(360deg); } }`;
  document.head.appendChild(style);
}
