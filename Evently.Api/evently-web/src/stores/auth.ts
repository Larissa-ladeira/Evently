import { defineStore } from 'pinia'
import api from '../services/api'
import type { User } from '../types'

export type { User } from '../types'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: JSON.parse(localStorage.getItem('user') || 'null') as User | null,
    token: localStorage.getItem('token') || '',
  }),
  getters: {
    isAuthenticated: (state) => !!state.token,
    isOrganizer: (state) => state.user?.role === 'Organizer' || state.user?.role === 'Admin',
    isStaff: (state) =>
      state.user?.role === 'Organizer' || state.user?.role === 'Admin' || state.user?.role === 'Staff',
  },
  actions: {
    setSession(token: string, user: User) {
      this.token = token
      this.user = user
      localStorage.setItem('token', token)
      localStorage.setItem('user', JSON.stringify(user))
    },
    clearToken() {
      this.token = ''
      this.user = null
      localStorage.removeItem('token')
      localStorage.removeItem('user')
    },
    async login(email: string, password: string) {
      const res = await api.post('/api/Users/login', { email, password })
      this.setSession(res.data.token, {
        id: res.data.userId,
        name: res.data.name,
        email,
        role: res.data.role,
        token: res.data.token,
      })
    },
    async register(name: string, email: string, password: string, role: string = 'Client') {
      await api.post('/api/Users/register', { name, email, password, role })
    },
    logout() {
      this.clearToken()
    }
  }
})
