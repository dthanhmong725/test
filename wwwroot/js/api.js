// Cybersecurity Forum - API Client
const API_BASE_URL = `${window.location.protocol}//${window.location.host}/api`;

class ApiClient {
    constructor() {
        this.baseUrl = API_BASE_URL;
        this.token = null;
        this._isRefreshing = false;
        this._refreshQueue = [];
    }

    setToken(token) {
        this.token = token;
    }

    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        if (this.token) {
            headers['Authorization'] = `Bearer ${this.token}`;
        }

        try {
            const response = await fetch(url, {
                ...options,
                headers
            });

            let data;
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                data = await response.json();
            } else {
                data = await response.text().then(text => {
                    try { return JSON.parse(text); }
                    catch { return { message: text }; }
                });
            }

            if (!response.ok) {
                if (response.status === 401) {
                    // Only refresh token for non-auth endpoints
                    if (!url.includes('/auth/')) {
                        const refreshed = await this._tryRefreshToken();
                        if (refreshed) {
                            headers['Authorization'] = `Bearer ${this.token}`;
                            const retryResponse = await fetch(url, { ...options, headers });
                            const retryData = await retryResponse.json();
                            if (!retryResponse.ok) throw new Error(retryData.message || 'Yêu cầu thất bại');
                            return retryData;
                        }
                        AuthManager.logout();
                    }
                }

                let errorMessage = data.message;
                if (!errorMessage && data.errors) {
                    // ASP.NET Core ValidationProblemDetails format
                    if (typeof data.errors === 'object' && !Array.isArray(data.errors)) {
                        errorMessage = Object.values(data.errors).flat().join('; ');
                    } else if (Array.isArray(data.errors)) {
                        errorMessage = data.errors.join('; ');
                    }
                }
                if (!errorMessage && data.title) {
                    errorMessage = data.title;
                }

                throw new Error(errorMessage || `Lỗi ${response.status}: Yêu cầu thất bại`);
            }

            return data;
        } catch (error) {
            // Provide clearer error messages for common issues
            if (error.name === 'TypeError' && error.message.includes('fetch')) {
                console.error('API connection failed. URL:', url, 'Error:', error);
                throw new Error('Không thể kết nối đến máy chủ. Vui lòng kiểm tra máy chủ đang chạy.');
            }
            console.error('API Error:', error);
            throw error;
        }
    }

    async _tryRefreshToken() {
        if (this._isRefreshing) {
            return new Promise((resolve) => {
                this._refreshQueue.push(resolve);
            });
        }

        this._isRefreshing = true;

        try {
            const refreshed = await AuthManager.silentRefresh();
            this._refreshQueue.forEach(resolve => resolve(refreshed));
            this._refreshQueue = [];
            return refreshed;
        } catch (e) {
            this._refreshQueue.forEach(resolve => resolve(false));
            this._refreshQueue = [];
            return false;
        } finally {
            this._isRefreshing = false;
        }
    }

    get(endpoint, params = {}) {
        const queryString = new URLSearchParams(params).toString();
        const url = queryString ? `${endpoint}?${queryString}` : endpoint;
        return this.request(url);
    }

    post(endpoint, data) {
        return this.request(endpoint, {
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    put(endpoint, data) {
        return this.request(endpoint, {
            method: 'PUT',
            body: JSON.stringify(data)
        });
    }

    delete(endpoint) {
        return this.request(endpoint, {
            method: 'DELETE'
        });
    }
}

// Khởi tạo ApiClient TRƯỚC khi định nghĩa API object
const api = new ApiClient();

// API endpoints
const API = {
    auth: {
        register: (data) => api.post('/auth/register', data),
        login: (data) => api.post('/auth/login', data),
        logout: () => api.post('/auth/logout'),
        forgotPassword: (data) => api.post('/auth/forgot-password', data),
        resetPassword: (token, data) => api.post('/auth/reset-password', data),
        changePassword: (data) => api.post('/auth/change-password', data),
        verifyEmail: (token) => api.post('/auth/verify-email', { token }),
        refresh: (refreshToken) => api.post('/auth/refresh-token', { refreshToken }),
        checkPasswordStrength: (password) => api.post('/auth/check-password-strength', { password })
    },

    users: {
        getById: (id) => api.get(`/users/${id}`),
        getProfile: (username) => api.get(`/users/profile/${username}`),
        getMe: () => api.get('/users/profile'),
        updateProfile: (data) => api.put('/users/profile', data),
        getUsers: (params) => api.get('/users', params),
        changeRole: (id, role) => api.put(`/users/${id}/role?newRole=${role}`),
        banUser: (id, reason) => api.post(`/users/${id}/ban?reason=${encodeURIComponent(reason)}`),
        unbanUser: (id) => api.post(`/users/${id}/unban`),
        getReputation: (id) => api.get(`/users/${id}/reputation`),
        getLeaderboard: (params) => api.get('/users/leaderboard', params)
    },

    categories: {
        getAll: () => api.get('/categories'),
        getBySlug: (slug) => api.get(`/categories/${slug}`),
        getPostsBySlug: (slug, params) => api.get(`/categories/${slug}/posts`, params),
        create: (data) => api.post('/categories', data),
        update: (id, data) => api.put(`/categories/${id}`, data),
        delete: (id) => api.delete(`/categories/${id}`)
    },

    posts: {
        getAll: (params) => api.get('/posts', params),
        getById: (id) => api.get(`/posts/${id}`),
        create: (data) => api.post('/posts', data),
        update: (id, data) => api.put(`/posts/${id}`, data),
        delete: (id) => api.delete(`/posts/${id}`),
        pin: (id, isPinned) => api.put(`/posts/${id}/pin?isPinned=${isPinned}`),
        lock: (id, isLocked) => api.put(`/posts/${id}/lock?isLocked=${isLocked}`),
        vote: (id, isUpvote) => api.post(`/posts/${id}/vote`, { isUpvote }),
        removeVote: (id) => api.delete(`/posts/${id}/vote`),
        incrementView: (id) => api.post(`/posts/${id}/view`),
        addBookmark: (id) => api.post(`/bookmarks/${id}`),
        removeBookmark: (id) => api.delete(`/bookmarks/${id}`)
    },

    upload: {
        file: (formData) => {
            // Đã fix: Đính kèm token hiện tại của phiên đăng nhập vào Header để tránh lỗi 401 khi gọi API Upload
            return fetch('/api/upload/document', {
                method: 'POST',
                body: formData,
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token') || api.token || AuthManager.getToken()}`
                }
            }).then(async r => {
                const text = await r.text();
                try { return JSON.parse(text); } catch { return { success: false, message: text || 'Lỗi server' }; }
            });
        }
    },

    comments: {
        getByPost: (postId, params) => api.get(`/posts/${postId}/comments`, params),
        create: (postId, data) => api.post(`/posts/${postId}/comments`, data),
        update: (postId, commentId, data) => api.put(`/posts/${postId}/comments/${commentId}`, data),
        delete: (postId, commentId) => api.delete(`/posts/${postId}/comments/${commentId}`),
        vote: (postId, commentId, isUpvote) => api.post(`/posts/${postId}/comments/${commentId}/vote`, { isUpvote }),
        removeVote: (postId, commentId) => api.delete(`/posts/${postId}/comments/${commentId}/vote`)
    },

    bookmarks: {
        getAll: (params) => api.get('/bookmarks', params),
        toggle: (postId) => api.post('/bookmarks', { postId }),
        add: (postId) => api.post(`/bookmarks/${postId}`),
        remove: (postId) => api.delete(`/bookmarks/${postId}`),
        checkStatus: (postId) => api.get(`/bookmarks/check/${postId}`)
    },

    badges: {
        getAll: () => api.get('/badges'),
        getUserBadges: (userId) => api.get(`/badges/user/${userId}`)
    },

    activity: {
        getMy: (params) => api.get('/activity/my', params),
        getAll: (params) => api.get('/activity', params)
    },

    chat: {
        getRooms: (params) => api.get('/chat/rooms', params),
        getRoom: (roomId) => api.get(`/chat/rooms/${roomId}`),
        createRoom: (data) => api.post('/chat/rooms', data),
        joinRoom: (roomId) => api.post(`/chat/rooms/${roomId}/join`),
        leaveRoom: (roomId) => api.post(`/chat/rooms/${roomId}/leave`),
        getMessages: (roomId, params) => api.get(`/chat/rooms/${roomId}/messages`, params),
        sendMessage: (roomId, data) => api.post(`/chat/rooms/${roomId}/messages`, data),
        editMessage: (roomId, messageId, content) => api.put(`/chat/messages/${messageId}`, { content }),
        deleteMessage: (roomId, messageId) => api.delete(`/chat/messages/${messageId}`),
        toggleReaction: (roomId, messageId, emoji) => api.post(`/chat/messages/${messageId}/reactions`, { emoji }),
        togglePin: (roomId, messageId) => api.post(`/chat/messages/${messageId}/pin`),
        getPinned: (roomId) => api.get(`/chat/rooms/${roomId}/pinned`),
        searchMessages: (roomId, q) => api.get(`/chat/rooms/${roomId}/search`, { q }),
        addMember: (roomId, username) => api.post(`/chat/rooms/${roomId}/members`, { username }),
        markAsRead: (roomId) => api.post(`/chat/rooms/${roomId}/read`),

        // Đã thêm: Hàm khởi tạo phòng Chat Room trực tiếp sau khi hai tài khoản Follow chéo nhau
        initiateDirectChat: (targetUserId) => api.post(`/chat/rooms/initiate/${targetUserId}`)
    },

    admin: {
        getUsers: (params) => api.get('/admin/users', params),
        changeRole: (id, role) => api.put(`/admin/users/${id}/role?newRole=${role}`),
        banUser: (id, reason) => api.post(`/admin/users/${id}/ban?reason=${encodeURIComponent(reason)}`),
        unbanUser: (id) => api.post(`/admin/users/${id}/unban`),
        getSecurityLogs: (params) => api.get('/admin/security-logs', params),
        getDashboard: (params) => api.get('/admin/dashboard', params)
    },

    security: {
        getMyLogs: (params) => api.get('/security/logs/mine', params)
    },

    notifications: {
        getAll: (params) => api.get('/notifications', params),
        getUnreadCount: () => api.get('/notifications/count'),
        markAsRead: (id) => api.put(`/notifications/${id}/read`),
        markAllAsRead: () => api.put('/notifications/read-all')
    },

    reputation: {
        getMy: () => api.get('/reputation/me/history'),
        getUser: (userId) => api.get(`/reputation/user/${userId}`),
        getUserHistory: (userId, params) => api.get(`/reputation/user/${userId}/history`, params),
        getLeaderboard: (params) => api.get('/reputation/leaderboard', params),
        getRules: () => api.get('/reputation/rules')
    }
};