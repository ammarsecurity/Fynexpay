import axios from 'axios'

const API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:5080'

export const api = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' }
})

let onUnauthorized = null
let unauthorizedBusy = false

export function setUnauthorizedHandler(fn) {
  onUnauthorized = fn
}

export function isJwtExpired(token, skewMs = 8000) {
  if (!token) return true
  try {
    const part = token.split('.')[1]
    if (!part) return true
    const json = JSON.parse(atob(part.replace(/-/g, '+').replace(/_/g, '/')))
    if (typeof json.exp !== 'number') return false
    return json.exp * 1000 <= Date.now() + skewMs
  } catch {
    return true
  }
}

function isAuthRequest(config) {
  const url = `${config?.baseURL || ''}${config?.url || ''}`
  return /\/api\/auth(\/|$|\?)/i.test(url)
}

function tokenFromConfig(config) {
  const header = config?.headers?.Authorization || api.defaults.headers.common.Authorization || ''
  return String(header).replace(/^Bearer\s+/i, '').trim()
}

function notifyUnauthorized() {
  if (unauthorizedBusy) return
  unauthorizedBusy = true
  try {
    onUnauthorized?.()
  } finally {
    window.setTimeout(() => { unauthorizedBusy = false }, 1500)
  }
}

api.interceptors.request.use((config) => {
  if (typeof FormData !== 'undefined' && config.data instanceof FormData) {
    if (config.headers) {
      delete config.headers['Content-Type']
      delete config.headers['content-type']
    }
  }
  const token = tokenFromConfig(config)
  if (token && isJwtExpired(token) && !isAuthRequest(config)) {
    notifyUnauthorized()
    return Promise.reject(new axios.CanceledError('session expired'))
  }
  return config
})

api.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error.response?.status === 401 && !isAuthRequest(error.config)) {
      notifyUnauthorized()
    }
    return Promise.reject(error)
  }
)

export function setToken(token) {
  if (token) api.defaults.headers.common.Authorization = `Bearer ${token}`
  else delete api.defaults.headers.common.Authorization
}

const saved = localStorage.getItem('fx_token')
if (saved) setToken(saved)

export { API_BASE }
