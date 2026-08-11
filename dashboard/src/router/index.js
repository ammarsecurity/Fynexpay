import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const routes = [
  { path: '/login', name: 'login', component: () => import('../views/AuthRedirect.vue'), meta: { guest: true } },
  { path: '/register', name: 'register', component: () => import('../views/AuthRedirect.vue'), meta: { guest: true } },
  { path: '/auth/handoff', name: 'auth-handoff', component: () => import('../views/AuthHandoff.vue'), meta: { guest: true } },
  {
    path: '/',
    component: () => import('../layouts/AppLayout.vue'),
    meta: { auth: true },
    children: [
      { path: '', name: 'home', component: () => import('../views/HomeRedirect.vue') },
      { path: 'merchant', name: 'merchant-overview', component: () => import('../views/merchant/Overview.vue'), meta: { merchant: true } },
      { path: 'merchant/docs', name: 'merchant-docs', component: () => import('../views/merchant/Docs.vue'), meta: { merchant: true } },
      { path: 'merchant/test', name: 'merchant-test', component: () => import('../views/merchant/PaymentTest.vue'), meta: { merchant: true } },
      { path: 'merchant/payment-methods', name: 'merchant-methods', component: () => import('../views/merchant/PaymentMethods.vue'), meta: { merchant: true } },
      { path: 'merchant/payments', name: 'merchant-payments', component: () => import('../views/merchant/Payments.vue'), meta: { merchant: true } },
      { path: 'merchant/platforms', name: 'merchant-platforms', component: () => import('../views/merchant/Platforms.vue'), meta: { merchant: true } },
      { path: 'merchant/keys', name: 'merchant-keys', component: () => import('../views/merchant/ApiKeys.vue'), meta: { merchant: true } },
      { path: 'merchant/payouts', name: 'merchant-payouts', component: () => import('../views/merchant/Payouts.vue'), meta: { merchant: true } },
      { path: 'admin', name: 'admin-overview', component: () => import('../views/admin/Overview.vue'), meta: { admin: true } },
      { path: 'admin/merchants', name: 'admin-merchants', component: () => import('../views/admin/Merchants.vue'), meta: { admin: true } },
      { path: 'admin/platforms', name: 'admin-platforms', component: () => import('../views/admin/Platforms.vue'), meta: { admin: true } },
      { path: 'admin/payments', name: 'admin-payments', component: () => import('../views/admin/Payments.vue'), meta: { admin: true } },
      { path: 'admin/payouts', name: 'admin-payouts', component: () => import('../views/admin/Payouts.vue'), meta: { admin: true } },
      { path: 'admin/providers', name: 'admin-providers', component: () => import('../views/admin/Providers.vue'), meta: { admin: true } },
      { path: 'admin/ultramsg', name: 'admin-ultramsg', component: () => import('../views/admin/Ultramsg.vue'), meta: { admin: true } },
      { path: 'admin/landing', name: 'admin-landing', component: () => import('../views/admin/Landing.vue'), meta: { admin: true } }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

const pendingMerchantAllowed = new Set([
  'merchant-overview',
  'merchant-platforms',
  'merchant-docs',
  'merchant-methods',
  'merchant-payments',
  'home'
])

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (to.meta.auth && !auth.isAuthenticated) return '/login'
  if (to.meta.guest && auth.isAuthenticated && to.name !== 'auth-handoff') return '/'
  if (to.meta.admin && !auth.isAdmin) return '/merchant'
  if (to.meta.merchant && !auth.isMerchant) return '/admin'

  const status = auth.user?.merchantStatus
  if (
    auth.isMerchant &&
    status &&
    status !== 'Active' &&
    to.meta.merchant &&
    !pendingMerchantAllowed.has(String(to.name || ''))
  ) {
    return { name: 'merchant-overview' }
  }

  return true
})

export default router
