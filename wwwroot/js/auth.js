// Cybersecurity Forum - Auth Manager
class AuthManager {
  static _token = null;
  static _refreshToken = null;
  static _user = null;
  static _refreshTimer = null;
  static _tokenExpTime = null;

  static getUser() {
    return this._user;
  }

  static isAuthenticated() {
    return !!this._token && !!this._user;
  }

  static isAdmin() {
    const user = this._user;
    return user?.role === 'Admin';
  }

  static isModerator() {
    const user = this._user;
    return user?.role === 'Moderator' || user?.role === 'Admin';
  }

  static getRoleLevel() {
    const user = this._user;
    if (!user?.role) return -1;
    const levels = { User: 0, Moderator: 1, Admin: 2 };
    return levels[user.role] ?? -1;
  }

  static hasMinimumRole(minimumRole) {
    const user = this._user;
    if (!user?.role) return false;
    const levels = { User: 0, Moderator: 1, Admin: 2 };
    const userLevel = levels[user.role] ?? -1;
    const minLevel = levels[minimumRole] ?? -1;
    return userLevel >= minLevel;
  }

  // Permission matrix
  static permissions = {
    createPost:    ['User', 'Moderator', 'Admin'],
    editOwnPost:   ['User', 'Moderator', 'Admin'],
    editAnyPost:   ['Moderator', 'Admin'],
    deleteOwnPost: ['User', 'Moderator', 'Admin'],
    deleteAnyPost: ['Moderator', 'Admin'],
    pinPost:       ['Moderator', 'Admin'],
    lockPost:       ['Moderator', 'Admin'],
    banUser:       ['Admin'],
    manageCategories: ['Admin'],
    viewSecurityLogs: ['Moderator', 'Admin'],
    manageUsers:      ['Admin'],
    changeRole:       ['Admin'],
  };

  static can(permission) {
    const allowedRoles = this.permissions[permission];
    if (!allowedRoles) return false;
    const userRole = this._user?.role;
    if (!userRole) return false;
    return allowedRoles.includes(userRole);
  }

  static getToken() {
    return this._token;
  }

  static getTokenExpTime() {
    return this._tokenExpTime;
  }

  static async login(accessToken, refreshToken, user) {
    this._token = accessToken;
    this._refreshToken = refreshToken;
    this._user = user;
    this._tokenExpTime = Date.now() + 15 * 60 * 1000;

    api.setToken(accessToken);
    this._scheduleRefresh();
    await this._persistSession();
    this.updateUI();
    await NotificationManager.requestPermission();
  }

  static async logout() {
    if (this.isAuthenticated()) {
      try {
        await API.auth.logout();
      } catch (e) {}
    }
    this._clearSession();
    this._clearRefreshTimer();
    api.setToken(null);
    this.updateUI();
    Toast.show('Đã đăng xuất thành công', 'info');
    if (typeof router !== 'undefined') {
      router.navigate('/login');
    } else {
      window.location.href = '/login.html';
    }
  }

  static async silentRefresh() {
    if (!this._refreshToken) {
      this.logout();
      return false;
    }
    try {
      const result = await API.auth.refresh(this._refreshToken);
      if (result.Success || result.success) {
        this._token = result.data.accessToken;
        this._refreshToken = result.data.refreshToken;
        this._user = result.data.user;
        this._tokenExpTime = Date.now() + 15 * 60 * 1000;
        api.setToken(this._token);
        this._scheduleRefresh();
        await this._persistSession();
        return true;
      }
    } catch (e) {}
    this._clearSession();
    Toast.show('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.', 'warning');
    window.location.href = '/login.html';
    return false;
  }

  static _scheduleRefresh() {
    this._clearRefreshTimer();
    if (!this._tokenExpTime) return;
    const remaining = this._tokenExpTime - Date.now();
    const refreshIn = Math.max(remaining - 60000, 30000);
    this._refreshTimer = setTimeout(() => {
      this.silentRefresh();
    }, refreshIn);
  }

  static _clearRefreshTimer() {
    if (this._refreshTimer) {
      clearTimeout(this._refreshTimer);
      this._refreshTimer = null;
    }
  }

  static async _persistSession() {
    try {
      if (this._token) sessionStorage.setItem('cf_token', this._token);
      if (this._refreshToken) sessionStorage.setItem('cf_refresh', this._refreshToken);
      if (this._user) sessionStorage.setItem('cf_user', JSON.stringify(this._user));
      if (this._tokenExpTime) sessionStorage.setItem('cf_exp', this._tokenExpTime.toString());
    } catch (e) {}
  }

  static async restoreSession() {
    try {
      const userStr = sessionStorage.getItem('cf_user');
      const expStr = sessionStorage.getItem('cf_exp');
      const token = sessionStorage.getItem('cf_token');
      const refreshToken = sessionStorage.getItem('cf_refresh');
      if (!userStr || !expStr) return false;

      const user = JSON.parse(userStr);
      const exp = parseInt(expStr, 10);

      if (!exp || exp <= Date.now()) {
        this._clearSession();
        return false;
      }

      this._user = user;
      this._tokenExpTime = exp;
      if (token) {
        this._token = token;
        this._refreshToken = refreshToken;
        api.setToken(token);
        this._scheduleRefresh();
        this.updateUI();
        return true;
      }

      api.setToken(null);
      if (this._refreshToken) {
        const refreshed = await this.silentRefresh();
        if (refreshed) {
          this.updateUI();
          return true;
        }
      }
      this._clearSession();
      this._clearRefreshTimer();
      return false;
    } catch (e) {
      this._clearSession();
      this._clearRefreshTimer();
      return false;
    }
  }

  static _clearSession() {
    this._token = null;
    this._refreshToken = null;
    this._user = null;
    this._tokenExpTime = null;
    try {
      sessionStorage.removeItem('cf_token');
      sessionStorage.removeItem('cf_refresh');
      sessionStorage.removeItem('cf_user');
      sessionStorage.removeItem('cf_exp');
    } catch (e) {}
  }

  static updateUI() {
    const user = this._user;
    const authButtons = document.querySelectorAll('.auth-buttons');
    const userMenu = document.querySelectorAll('.user-menu');
    const protectedElements = document.querySelectorAll('.requires-auth');
    const adminElements = document.querySelectorAll('.requires-admin');
    const moderatorElements = document.querySelectorAll('.requires-moderator');

    authButtons.forEach(el => {
      el.style.display = this.isAuthenticated() ? 'none' : 'flex';
    });

    userMenu.forEach(el => {
      el.style.display = this.isAuthenticated() ? 'flex' : 'none';
      if (user) {
        const nameEl = el.querySelector('.user-name');
        const avatarEl = el.querySelector('.user-avatar');
        if (nameEl) nameEl.textContent = user.displayName || user.username;
        if (avatarEl) {
          avatarEl.textContent = (user.displayName || user.username).charAt(0).toUpperCase();
          if (user.avatarUrl) {
            avatarEl.innerHTML = `<img src="${user.avatarUrl}" alt="">`;
          }
        }
      }
    });

    protectedElements.forEach(el => {
      el.style.display = this.isAuthenticated() ? '' : 'none';
    });

    adminElements.forEach(el => {
      el.style.display = this.isAdmin() ? '' : 'none';
    });

    moderatorElements.forEach(el => {
      el.style.display = this.isModerator() ? '' : 'none';
    });
  }

  static requireAuth() {
    if (!this.isAuthenticated()) {
      Toast.show('Vui lòng đăng nhập để tiếp tục', 'warning');
      if (typeof router !== 'undefined') {
        router.navigate('/login');
      } else {
        window.location.href = '/login.html';
      }
      return false;
    }
    return true;
  }

  static requireRole(role) {
    if (!this.requireAuth()) return false;
    const user = this._user;
    if (role === 'admin' && !this.isAdmin()) {
      Toast.show('Cần quyền quản trị viên', 'error');
      return false;
    }
    if (role === 'moderator' && !this.isModerator()) {
      Toast.show('Cần quyền điều hành viên', 'error');
      return false;
    }
    return true;
  }

  static renderRoleBadge(role) {
    if (!role || role === 'User') return '';
    const config = {
      Admin: {
        label: 'ADMIN',
        class: 'badge-admin',
        icon: '<i class="ti ti-shield"></i>'
      },
      Moderator: {
        label: 'MOD',
        class: 'badge-mod',
        icon: '<i class="ti ti-star"></i>'
      }
    };
    const cfg = config[role];
    if (!cfg) return '';
    return `<span class="rank-badge ${cfg.class}">${cfg.icon} ${cfg.label}</span>`;
  }
}

// Notification Manager
class NotificationManager {
  static async requestPermission() {
    try {
      if ('Notification' in window && Notification.permission === 'default') {
        await Notification.requestPermission();
      }
    } catch (e) {}
  }

  static show(title, body, icon = '/icon.png') {
    if ('Notification' in window && Notification.permission === 'granted') {
      new Notification(title, { body, icon });
    }
  }
}

// Toast Notifications
class Toast {
  static container = null;

  static init() {
    if (!this.container) {
      this.container = document.createElement('div');
      this.container.className = 'toast-container';
      document.body.appendChild(this.container);
    }
  }

  static show(message, type = 'info', duration = 4000) {
    this.init();

    const icons = {
      success: '<i class="ti ti-check" style="font-size: 1.125rem;"></i>',
      error: '<i class="ti ti-x" style="font-size: 1.125rem;"></i>',
      warning: '<i class="ti ti-alert-triangle" style="font-size: 1.125rem;"></i>',
      info: '<i class="ti ti-info-circle" style="font-size: 1.125rem;"></i>'
    };

    const titles = {
      success: 'Thành công!',
      error: 'Có lỗi xảy ra',
      warning: 'Cảnh báo',
      info: 'Thông báo'
    };

    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.innerHTML = `
      ${icons[type] || icons.info}
      <div>
        <div style="font-weight: 600; margin-bottom: 2px;">${titles[type] || titles.info}</div>
        <div style="font-size: 0.8125rem;">${message}</div>
      </div>
    `;

    this.container.appendChild(toast);

    setTimeout(() => {
      toast.classList.add('removing');
      setTimeout(() => toast.remove(), 300);
    }, duration);
  }
}
