import axios from 'axios'

const API_BASE = import.meta.env.VITE_API_BASE || 'http://localhost:5080'

export const api = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' }
})

export function setToken(token) {
  if (token) api.defaults.headers.common.Authorization = `Bearer ${token}`
  else delete api.defaults.headers.common.Authorization
}

const saved = localStorage.getItem('fx_token')
if (saved) setToken(saved)

export { API_BASE }
