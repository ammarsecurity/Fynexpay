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
import Platform from './views/marketing/Platform.vue'
import PlatformCreate from './views/marketing/PlatformCreate.vue'
import PlatformFeatures from './views/marketing/PlatformFeatures.vue'
import Merchants from './views/marketing/Merchants.vue'
import ForPlatforms from './views/marketing/ForPlatforms.vue'
import Payments from './views/marketing/Payments.vue'
import Developers from './views/marketing/Developers.vue'
import Pricing from './views/marketing/Pricing.vue'
import DemoLayout from './views/demo/DemoLayout.vue'
import DemoHome from './views/demo/DemoHome.vue'
import DemoCart from './views/demo/DemoCart.vue'
import DemoCheckout from './views/demo/DemoCheckout.vue'
import DemoPay from './views/demo/DemoPay.vue'
import DemoResult from './views/demo/DemoResult.vue'
import DemoOrders from './views/demo/DemoOrders.vue'
import './assets/main.css'
import './assets/auth.css'
import './assets/marketing.css'
import './assets/demo.css'
import './assets/landing.css'

const saved = localStorage.getItem('fx_web_locale')
const loc = saved === 'en' || saved === 'ar' ? saved : 'ar'
document.documentElement.lang = loc
document.documentElement.dir = loc === 'ar' ? 'rtl' : 'ltr'
document.title = loc === 'en'
  ? 'FynexPay — E-commerce store platform'
  : 'FynexPay — منصة متاجر إلكترونية'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: Home },
    { path: '/platform', component: Platform },
    { path: '/platform/create', component: PlatformCreate },
    { path: '/platform/features', component: PlatformFeatures },
    { path: '/merchants', component: Merchants },
    { path: '/for-platforms', component: ForPlatforms },
    { path: '/payments', component: Payments },
    { path: '/developers', component: Developers },
    { path: '/pricing', component: Pricing },
    {
      path: '/demo',
      component: DemoLayout,
      children: [
        { path: '', component: DemoHome },
        { path: 'cart', component: DemoCart },
        { path: 'checkout', component: DemoCheckout },
        { path: 'pay/:id', component: DemoPay },
        { path: 'success/:id', component: DemoResult, props: { ok: true } },
        { path: 'failed/:id', component: DemoResult, props: { ok: false } },
        { path: 'orders', component: DemoOrders }
      ]
    },
    { path: '/contact', component: Contact },
    { path: '/login', component: Login },
    { path: '/forgot', component: ForgotPassword },
    { path: '/forgot-password', redirect: '/forgot' },
    { path: '/register', component: Register },
    { path: '/terms', component: LegalPage, meta: { legal: 'terms' } },
    { path: '/privacy', component: LegalPage, meta: { legal: 'privacy' } },
    { path: '/prohibited', component: LegalPage, meta: { legal: 'prohibited' } },
    { path: '/brand', component: Brand },
    { path: '/company', component: Company },
    { path: '/about', redirect: '/company' }
  ],
  scrollBehavior(to) {
    if (to.hash) return { el: to.hash, behavior: 'smooth' }
    return { top: 0 }
  }
})

createApp(App).use(router).mount('#app')
