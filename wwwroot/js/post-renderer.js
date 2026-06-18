/**
 * Post Renderer — handles syntax highlighting, copy buttons, and lang badges
 * for <pre><code> blocks inside post/comment content.
 */
(function (window) {
    'use strict';

    const COPY_SVG = `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path></svg>`;
    const CHECK_SVG = `<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>`;

    let hljsReady = false;

    function loadHljsPacks() {
        if (hljsReady) return Promise.resolve();
        hljsReady = true;

        const packs = [
            'python', 'javascript', 'bash', 'sql', 'php',
            'cpp', 'yaml', 'json', 'xml', 'powershell',
            'java', 'csharp', 'css', 'html'
        ];

        const base = 'https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/';
        const promises = packs.map(lang =>
            fetch(`${base}min/${lang}.min.js`)
                .then(r => r.ok ? r.text() : '')
                .then(text => {
                    if (text) {
                        try { eval(text); } catch (_) { }
                    }
                })
                .catch(() => { })
        );

        return Promise.all(promises);
    }

    function stripHtml(html) {
        const tmp = document.createElement('div');
        tmp.innerHTML = html;
        return tmp.textContent || tmp.innerText || '';
    }

    function truncateText(text, maxLength) {
        if (text.length <= maxLength) return text;
        return text.slice(0, maxLength).trimEnd() + '…';
    }

    function extractLang(el) {
        const cls = Array.from(el.classList).join(' ');
        const match = cls.match(/language-(\w+)/) || cls.match(/lang-(\w+)/);
        return match ? match[1] : null;
    }

    function enhanceCodeBlock(pre) {
        if (pre.dataset.enhanced) return;
        pre.dataset.enhanced = 'true';

        const code = pre.querySelector('code');
        if (!code) return;

        const lang = extractLang(code) || extractLang(pre) || '';
        const langDisplay = lang.toLowerCase();

        pre.style.position = 'relative';

        if (langDisplay) {
            const badge = document.createElement('span');
            badge.className = 'code-lang-badge';
            badge.textContent = langDisplay;
            pre.appendChild(badge);
        }

        const copyBtn = document.createElement('button');
        copyBtn.className = 'code-copy-btn';
        copyBtn.setAttribute('aria-label', 'Sao chép mã');
        copyBtn.innerHTML = `<span class="copy-icon">${COPY_SVG}</span><span class="copy-text">Sao chép</span>`;
        pre.appendChild(copyBtn);

        copyBtn.addEventListener('click', function () {
            const text = code.textContent || '';
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text).then(() => showCopied(copyBtn));
            } else {
                const ta = document.createElement('textarea');
                ta.value = text;
                ta.style.cssText = 'position:fixed;top:-9999px;left:-9999px;opacity:0';
                document.body.appendChild(ta);
                ta.select();
                try { document.execCommand('copy'); showCopied(copyBtn); } catch (_) { }
                document.body.removeChild(ta);
            }
        });

        hljs.highlightElement(code);
    }

    function showCopied(btn) {
        const icon = btn.querySelector('.copy-icon');
        const text = btn.querySelector('.copy-text');
        const origIcon = icon.innerHTML;
        icon.innerHTML = CHECK_SVG;
        text.textContent = 'Đã sao chép';
        btn.classList.add('copied');
        setTimeout(function () {
            icon.innerHTML = origIcon;
            text.textContent = 'Sao chép';
            btn.classList.remove('copied');
        }, 2000);
    }

    function highlightCodeBlocks(root) {
        if (typeof hljs === 'undefined') return;
        root.querySelectorAll('pre code').forEach(function (block) {
            const pre = block.closest('pre');
            if (pre) enhanceCodeBlock(pre);
        });
    }

    // ĐÃ FIX: Hàm render cấu trúc an toàn bảo mật, chống lỗi NullReference khi khách vãng lai hoặc User chưa đăng nhập xem bài
    function renderPostPageHTML(post, currentUserId, isAuthenticated, isAdminOrMod) {
        const isAuthor = post.author && post.author.id === currentUserId;
        const bookmarkedClass = post.isBookmarked ? 'bookmarked' : '';
        const bookmarkIcon = post.isBookmarked ? 'ti-bookmark-filled' : 'ti-bookmark';
        const bookmarkColor = post.isBookmarked ? 'style="color: var(--accent) !important;"' : '';

        return `
      <article class="card" style="margin-bottom: 24px;">
        <div class="d-flex justify-between align-start mb-3">
          <div>
            <a href="/category.html?slug=${post.category.slug}" class="category-tag ${post.category.slug}">
              ${post.category.name}
            </a>
          </div>
          <div class="d-flex gap-2">
            <button class="btn btn-ghost btn-sm bookmark-btn ${bookmarkedClass}" onclick="toggleBookmark()" ${bookmarkColor}>
              <i class="ti ${bookmarkIcon}"></i>
            </button>
            ${isAdminOrMod ? `
              <button class="btn btn-ghost btn-sm" onclick="togglePin()" title="${post.isPinned ? 'Bỏ ghim' : 'Ghim bài'}">
                <i class="ti ti-pinned" style="color: ${post.isPinned ? 'var(--accent)' : ''}"></i>
              </button>
              <button class="btn btn-ghost btn-sm" onclick="toggleLock()" title="${post.isLocked ? 'Mở khóa' : 'Khóa bài'}">
                <i class="ti ti-${post.isLocked ? 'lock' : 'lock-open'}"></i>
              </button>
            ` : ''}
          </div>
        </div>

        <h1 style="font-size: 1.5rem; margin-bottom: 12px; line-height: 1.4; color: #ffffff;">
          ${post.isPinned ? '<span class="badge badge-success" style="margin-right: 8px; font-size: 0.625rem;"><i class="ti ti-pinned"></i> Đã ghim</span>' : ''}
          ${post.isLocked ? '<i class="ti ti-lock" style="color: var(--text-muted); margin-right: 6px;" title="Đã khóa"></i>' : ''}
          ${post.title}
        </h1>

        <div class="post-meta mb-3">
          <div class="d-flex align-center gap-2">
            <a href="/profile.html?username=${post.author.username}" class="d-flex align-center gap-2" style="color: inherit; text-decoration: none;">
              <div class="avatar avatar-sm">${(post.author.displayName || post.author.username).charAt(0).toUpperCase()}</div>
              <div>
                <div style="display: flex; align-items: center; gap: 6px;">
                  <span style="font-weight: 500; color: #ffffff;">${post.author.displayName || post.author.username}</span>
                  <span class="rank-badge rank-${post.author.rank.toLowerCase()}">${post.author.rank}</span>
                </div>
              </div>
            </a>
            <span class="text-muted">•</span>
            <span class="text-muted">${post.createdAt}</span>
          </div>
        </div>

        <div class="post-body" style="line-height: 1.8; margin-bottom: 16px; color: #e0e0e0;">
          ${post.content}
        </div>

        <div class="d-flex justify-between align-center" style="border-top: 1px solid var(--border); padding-top: 12px;">
          <div class="d-flex gap-2">
            <button class="btn btn-ghost btn-sm vote-btn upvote" onclick="vote(1)">
              <i class="ti ti-arrow-up"></i> <span>${post.upvoteCount}</span>
            </button>
            <button class="btn btn-ghost btn-sm vote-btn downvote" onclick="vote(-1)">
              <i class="ti ti-arrow-down"></i> <span>${post.downvoteCount}</span>
            </button>
          </div>
          <div class="d-flex gap-3 text-muted" style="font-size: 0.8125rem;">
            <span><i class="ti ti-eye"></i> ${post.viewCount} lượt xem</span>
            <span><i class="ti ti-message"></i> <span id="commentCount">${post.commentCount}</span> bình luận</span>
          </div>
        </div>

        ${(isAuthor || isAdminOrMod) ? `
          <div class="d-flex gap-2 mt-3" style="border-top: 1px solid var(--border); padding-top: 12px;">
            <a href="/edit-post.html?id=${post.id}" class="btn btn-ghost btn-sm">
              <i class="ti ti-edit"></i> Sửa
            </a>
            <button class="btn btn-ghost btn-sm" onclick="deletePost()" style="color: var(--accent-red);">
              <i class="ti ti-trash"></i> Xóa
            </button>
          </div>
        ` : ''}
      </article>
    `;
    }

    function renderPost(content, root) {
        if (typeof hljs === 'undefined') {
            loadHljsPacks().then(function () {
                if (root) highlightCodeBlocks(root);
            });
        } else {
            if (root) highlightCodeBlocks(root);
        }
    }

    function renderAll() {
        if (typeof hljs === 'undefined') {
            loadHljsPacks().then(function () {
                highlightCodeBlocks(document);
            });
        } else {
            highlightCodeBlocks(document);
        }
    }

    window.PostRenderer = {
        renderPost: renderPost,
        renderAll: renderAll,
        stripHtml: stripHtml,
        truncateText: truncateText,
        highlightCodeBlocks: highlightCodeBlocks,
        loadHljsPacks: loadHljsPacks,
        renderPostPageHTML: renderPostPageHTML
    };

})(window);