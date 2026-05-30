// Role-Based Access Control - Frontend Guard Helpers

const RoleGuard = {

  ROLE_LEVEL: {
    User: 0,
    Moderator: 1,
    Admin: 2
  },

  getUserRole() {
    const user = AuthManager.getUser();
    return user?.role || null;
  },

  getRoleLevel(role) {
    return this.ROLE_LEVEL[role] ?? -1;
  },

  hasRole(minimumRole) {
    const userRole = this.getUserRole();
    if (!userRole) return false;
    return this.getRoleLevel(userRole) >= this.getRoleLevel(minimumRole);
  },

  isUser() {
    return this.hasRole('User');
  },

  isModerator() {
    return this.hasRole('Moderator');
  },

  isAdmin() {
    return this.hasRole('Admin');
  },

  canCreatePost() {
    return this.isUser();
  },

  canEditOwnPost(authorId) {
    if (!AuthManager.isAuthenticated()) return false;
    return AuthManager.getUser()?.id === authorId;
  },

  canEditAnyPost() {
    return this.isModerator();
  },

  canEditPost(authorId) {
    return this.canEditOwnPost(authorId) || this.canEditAnyPost();
  },

  canDeleteOwnPost(authorId) {
    if (!AuthManager.isAuthenticated()) return false;
    return AuthManager.getUser()?.id === authorId;
  },

  canDeleteAnyPost() {
    return this.isModerator();
  },

  canDeletePost(authorId) {
    return this.canDeleteOwnPost(authorId) || this.canDeleteAnyPost();
  },

  canPinPost() {
    return this.isModerator();
  },

  canLockPost() {
    return this.isModerator();
  },

  canBanUser() {
    return this.isAdmin();
  },

  canManageCategories() {
    return this.isAdmin();
  },

  canViewSecurityLogs() {
    return this.isModerator();
  },

  canManageUsers() {
    return this.isAdmin();
  },

  canChangeRole() {
    return this.isAdmin();
  },

  canAccessAdminPanel() {
    return this.isAdmin();
  },

  // Render a role badge element
  renderRoleBadge(role, options = {}) {
    const {
      showLabel = true,
      size = 'sm',
      className = ''
    } = options;

    if (!role || role === 'User') return '';

    const config = {
      Admin: {
        label: 'ADMIN',
        class: 'badge-admin',
        icon: '<i class="ti ti-shield" style="font-size: 0.7rem;"></i>'
      },
      Moderator: {
        label: 'MOD',
        class: 'badge-mod',
        icon: '<i class="ti ti-star" style="font-size: 0.7rem;"></i>'
      }
    };

    const cfg = config[role];
    if (!cfg) return '';

    const sizeClass = size === 'lg' ? 'padding: 3px 10px; font-size: 0.75rem;' : 'padding: 2px 6px; font-size: 0.6875rem;';

    if (!showLabel) {
      return `<span class="rank-badge ${cfg.class}" style="${sizeClass} ${className}">${cfg.icon}</span>`;
    }

    return `<span class="rank-badge ${cfg.class}" style="${sizeClass} ${className}">${cfg.icon} ${cfg.label}</span>`;
  },

  // Render moderator toolbar for a post
  renderModeratorToolbar(postId, authorId) {
    if (!this.canPinPost()) return '';

    return `
      <div class="mod-toolbar" style="display: flex; align-items: center; gap: 6px; margin-top: 8px; padding-top: 8px; border-top: 1px solid var(--border);">
        <button class="btn btn-sm btn-ghost" onclick="RoleGuard.pinPost(${postId})" title="Ghim bài">
          <i class="ti ti-pinned"></i>
        </button>
        <button class="btn btn-sm btn-ghost" onclick="RoleGuard.lockPost(${postId})" title="Khóa bài">
          <i class="ti ti-lock"></i>
        </button>
        ${this.canDeleteAnyPost() ? `
        <button class="btn btn-sm btn-ghost" onclick="RoleGuard.deletePost(${postId})" title="Xóa bài" style="color: var(--accent-red);">
          <i class="ti ti-trash"></i>
        </button>
        ` : ''}
        <button class="btn btn-sm btn-ghost" onclick="RoleGuard.warnUser(${authorId})" title="Cảnh cáo người dùng">
          <i class="ti ti-alert-triangle"></i>
        </button>
      </div>
    `;
  },

  async pinPost(postId) {
    if (!AuthManager.isAuthenticated()) return;
    try {
      const result = await API.posts.pin(postId, true);
      if (result.success) {
        Toast.show('Đã ghim bài viết', 'success');
        if (typeof onPostPinned === 'function') onPostPinned(postId, true);
      } else {
        Toast.show(result.message || 'Không thể ghim bài', 'error');
      }
    } catch (e) {
      Toast.show('Lỗi khi ghim bài viết', 'error');
    }
  },

  async lockPost(postId) {
    if (!AuthManager.isAuthenticated()) return;
    try {
      const result = await API.posts.lock(postId, true);
      if (result.success) {
        Toast.show('Đã khóa bài viết', 'success');
        if (typeof onPostLocked === 'function') onPostLocked(postId, true);
      } else {
        Toast.show(result.message || 'Không thể khóa bài', 'error');
      }
    } catch (e) {
      Toast.show('Lỗi khi khóa bài viết', 'error');
    }
  },

  async deletePost(postId) {
    if (!confirm('Bạn có chắc muốn xóa bài viết này?')) return;
    try {
      const result = await API.posts.delete(postId);
      if (result.success) {
        Toast.show('Đã xóa bài viết', 'success');
        if (typeof onPostDeleted === 'function') onPostDeleted(postId);
      } else {
        Toast.show(result.message || 'Không thể xóa bài', 'error');
      }
    } catch (e) {
      Toast.show('Lỗi khi xóa bài viết', 'error');
    }
  },

  async warnUser(userId) {
    Toast.show('Chức năng cảnh cáo đang được phát triển', 'info');
  },

  // Apply role-based visibility to DOM elements
  applyVisibility() {
    document.querySelectorAll('.requires-admin').forEach(el => {
      el.style.display = this.isAdmin() ? '' : 'none';
    });
    document.querySelectorAll('.requires-moderator').forEach(el => {
      el.style.display = this.isModerator() ? '' : 'none';
    });
    document.querySelectorAll('.requires-user').forEach(el => {
      el.style.display = this.isUser() ? '' : 'none';
    });
  },

  // Require auth and role, redirect if fails
  require(role) {
    if (!AuthManager.isAuthenticated()) {
      Toast.show('Vui lòng đăng nhập để tiếp tục', 'warning');
      if (typeof router !== 'undefined') {
        router.navigate('/login');
      } else {
        window.location.href = '/login.html';
      }
      return false;
    }
    if (!this.hasRole(role)) {
      Toast.show('Bạn không có quyền thực hiện thao tác này', 'error');
      return false;
    }
    return true;
  },

  // Build permission matrix table for admin panel
  buildPermissionMatrix() {
    const rows = [
      { action: 'Tạo bài viết', user: true, moderator: true, admin: true },
      { action: 'Sửa bài viết của mình', user: true, moderator: true, admin: true },
      { action: 'Sửa bài viết bất kỳ', user: false, moderator: true, admin: true },
      { action: 'Xóa bài viết của mình', user: true, moderator: true, admin: true },
      { action: 'Xóa bài viết bất kỳ', user: false, moderator: true, admin: true },
      { action: 'Ghim bài viết', user: false, moderator: true, admin: true },
      { action: 'Khóa bài viết', user: false, moderator: true, admin: true },
      { action: 'Khóa tài khoản người dùng', user: false, moderator: false, admin: true },
      { action: 'Quản lý danh mục', user: false, moderator: false, admin: true },
      { action: 'Xem nhật ký bảo mật', user: false, moderator: true, admin: true },
      { action: 'Thay đổi vai trò người dùng', user: false, moderator: false, admin: true },
    ];

    const check = (val) => val
      ? '<span style="color: var(--accent); font-size: 1rem;">&#10003;</span>'
      : '<span style="color: var(--accent-red); font-size: 1rem;">&#10007;</span>';

    return `
      <table style="width: 100%; border-collapse: collapse; font-size: 0.875rem;">
        <thead>
          <tr style="border-bottom: 1px solid var(--border);">
            <th style="text-align: left; padding: 10px 12px; color: var(--text-muted); font-weight: 500;">Thao tác</th>
            <th style="text-align: center; padding: 10px 12px; color: var(--text-muted); font-weight: 500;">Người dùng</th>
            <th style="text-align: center; padding: 10px 12px; color: var(--text-muted); font-weight: 500;">Điều hành viên</th>
            <th style="text-align: center; padding: 10px 12px; color: var(--text-muted); font-weight: 500;">Quản trị viên</th>
          </tr>
        </thead>
        <tbody>
          ${rows.map(r => `
          <tr style="border-bottom: 1px solid var(--border);">
            <td style="padding: 10px 12px; color: var(--text-secondary);">${r.action}</td>
            <td style="text-align: center; padding: 10px 12px;">${check(r.user)}</td>
            <td style="text-align: center; padding: 10px 12px;">${check(r.moderator)}</td>
            <td style="text-align: center; padding: 10px 12px;">${check(r.admin)}</td>
          </tr>
          `).join('')}
        </tbody>
      </table>
    `;
  }
};

// Auto-apply visibility when auth state changes
const originalUpdateUI = AuthManager.updateUI;
AuthManager.updateUI = function() {
  if (originalUpdateUI) originalUpdateUI.call(this);
  RoleGuard.applyVisibility();
};
