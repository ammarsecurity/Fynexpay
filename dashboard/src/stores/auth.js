import { defineStore } from 'pinia'
import { api, setToken } from '../api'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('fx_token') || '',
    user: JSON.parse(localStorage.getItem('fx_user') || 'null')
  }),
  getters: {
    isAuthenticated: (s) => !!s.token,
    isAdmin: (s) => s.user?.role === 'Admin',
    isMerchant: (s) => s.user?.role === 'MerchantOwner' || s.user?.role === 'MerchantStaff'
  },
  actions: {
    async login(email, password) {
      const { data } = await api.post('/api/auth/login', { email, password })
      this.applyAuth(data)
      return data
    },
    async register(payload) {
      const { data } = await api.post('/api/auth/register', payload)
      this.applyAuth(data)
      return data
    },
    async verifyRegisterOtp(challengeId, code) {
      const { data } = await api.post('/api/auth/register/verify-otp', { challengeId, code })
      this.applyAuth(data)
      return data
    },
    applyAuth(data) {
      this.token = data.token
      this.user = {
        userId: data.userId,
        email: data.email,
        fullName: data.fullName,
        role: data.role,
        merchantId: data.merchantId,
        merchantStatus: data.merchantStatus
      }
      localStorage.setItem('fx_token', this.token)
      localStorage.setItem('fx_user', JSON.stringify(this.user))
      setToken(this.token)
    },
    logout() {
      this.token = ''
      this.user = null
      localStorage.removeItem('fx_token')
      localStorage.removeItem('fx_user')
      setToken('')
    }
  }
})
