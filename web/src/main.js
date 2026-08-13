import { createApp } from 'vue'
import { createRouter, createWebHistory } from 'vue-router'
import App from './App.vue'
import Home from './views/Home.vue'
import Contact from './views/Contact.vue'
import Login from './views/Login.vue'
import Register from './views/Register.vue'
import ForgotPassword from './views/ForgotPassword.vue'
import LegalPage from './views/LegalPage.vue'
import Brand from './views/Brand.vue'
import Company from './views/Company.vue'
import './assets/main.css'
import './assets/auth.css'

const saved = localStorage.getItem('fx_web_locale')
const loc = saved === 'en' || saved === 'ar' ? saved : 'ar'
document.documentElement.lang = loc
document.documentElement.dir = loc === 'ar' ? 'rtl' : 'ltr'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: Home },
    { path: '/contact', component: Contact },
    { path: '/login', component: Login },
    { path: '/forgot', component: ForgotPassword },
    { path: '/forgot-password', redirect: '/forgot' },
    { path: '/register', component: Register },
    { path: '/terms', component: LegalPage, meta: { legal: 'terms' } },
    { path: '/privacy', component: LegalPage, meta: { legal: 'privacy' } },
    { path: '/prohibited', component: LegalPage, meta: { legal: 'prohibited' } },
    { path: '/brand', component: Brand },
    { path: '/company', component: Company }
  ],
  scrollBehavior(to) {
    if (to.hash) return { el: to.hash, behavior: 'smooth' }
    return { top: 0 }
  }
})

createApp(App).use(router).mount('#app')
