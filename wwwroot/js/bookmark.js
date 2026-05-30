/**
 * Bookmark UI — provides BookmarkManager with per-post debounce + optimistic UI.
 */
(function (window) {
  'use strict';

  const _debounceMap = new Map();
  const DEBOUNCE_MS = 300;

  function getDebounce(key) {
    const now = Date.now();
    const last = _debounceMap.get(key) || 0;
    if (now - last < DEBOUNCE_MS) return true;
    _debounceMap.set(key, now);
    return false;
  }

  /**
   * Toggles bookmark for a post. Works on any element with data-post-id
   * or accepts explicit postId + buttonElement.
   */
  async function toggle(postId, btnEl) {
    if (!btnEl) {
      btnEl = document.querySelector(`[data-post-id="${postId}"] .bookmark-btn, .bookmark-btn[data-post-id="${postId}"]`);
    }

    if (!AuthManager.isAuthenticated()) {
      const redirect = encodeURIComponent(window.location.pathname + window.location.search);
      window.location.href = `/login.html?redirect=${redirect}`;
      return;
    }

    if (getDebounce(postId)) return;

    const isBookmarked = btnEl.classList.contains('bookmarked');

    // Optimistic UI
    _applyBookmarkUI(btnEl, !isBookmarked);

    try {
      const result = await API.bookmarks.toggle(postId);
      if (!result.Success && !result.success) {
        _applyBookmarkUI(btnEl, isBookmarked);
        Toast.show(result.message || 'Có lỗi xảy ra', 'error');
        return;
      }
      Toast.show(result.message || (result.data ? 'Đã lưu bài viết' : 'Đã bỏ lưu bài viết'), 'success');
    } catch (error) {
      _applyBookmarkUI(btnEl, isBookmarked);
      Toast.show('Có lỗi xảy ra', 'error');
    }
  }

  /**
   * Applies bookmark UI state to a button element.
   */
  function _applyBookmarkUI(btnEl, isBookmarked) {
    if (!btnEl) return;
    const icon = btnEl.querySelector('i') || btnEl;
    if (isBookmarked) {
      btnEl.classList.add('bookmarked');
      btnEl.style.color = 'var(--accent)';
      if (btnEl.tagName === 'BUTTON' || btnEl.querySelector('i')) {
        icon.className = icon.className.replace(/ti ti-bookmark(-filled)?/, 'ti ti-bookmark-filled');
      }
    } else {
      btnEl.classList.remove('bookmarked');
      btnEl.style.color = '';
      if (btnEl.tagName === 'BUTTON' || btnEl.querySelector('i')) {
        icon.className = icon.className.replace(/ti ti-bookmark-filled/, 'ti ti-bookmark');
      }
    }
  }

  /**
   * Fetches and returns bookmark state for an array of post IDs.
   * Updates data-post-id elements on the page.
   */
  async function syncStates(postIds) {
    if (!AuthManager.isAuthenticated()) return;
    const results = await Promise.allSettled(
      postIds.map(id => API.bookmarks.checkStatus(id))
    );
    results.forEach((result, idx) => {
      if (result.status !== 'fulfilled' || !result.value.data) return;
      const postId = postIds[idx];
      const btn = document.querySelector(`[data-post-id="${postId}"] .bookmark-btn, .bookmark-btn[data-post-id="${postId}"]`);
      _applyBookmarkUI(btn, result.value.data);
    });
  }

  window.BookmarkManager = {
    toggle: toggle,
    syncStates: syncStates
  };

})(window);
