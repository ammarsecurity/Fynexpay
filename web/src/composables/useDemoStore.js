import { computed, ref } from 'vue'
import { demoProducts } from '../i18n/site'

const CART_KEY = 'fx_demo_cart'
const ORDERS_KEY = 'fx_demo_orders'

function read(key, fallback) {
  try {
    const raw = localStorage.getItem(key)
    return raw ? JSON.parse(raw) : fallback
  } catch {
    return fallback
  }
}

function write(key, value) {
  localStorage.setItem(key, JSON.stringify(value))
}

const cart = ref(read(CART_KEY, []))
const orders = ref(read(ORDERS_KEY, []))
const toast = ref('')
let toastTimer = 0

function persistCart() {
  write(CART_KEY, cart.value)
}

function persistOrders() {
  write(ORDERS_KEY, orders.value)
}

export function money(n) {
  return Number(n || 0).toLocaleString('en-US')
}

export function productById(id) {
  return demoProducts.find((p) => p.id === id) || null
}

export function useDemoStore() {
  const count = computed(() => cart.value.reduce((s, i) => s + i.qty, 0))
  const subtotal = computed(() =>
    cart.value.reduce((s, i) => {
      const p = productById(i.id)
      return s + (p ? p.price * i.qty : 0)
    }, 0)
  )

  const lines = computed(() =>
    cart.value
      .map((i) => {
        const p = productById(i.id)
        return p ? { ...p, qty: i.qty, line: p.price * i.qty } : null
      })
      .filter(Boolean)
  )

  function flash(message) {
    toast.value = message
    clearTimeout(toastTimer)
    toastTimer = setTimeout(() => { toast.value = '' }, 2200)
  }

  function add(id, qty = 1) {
    const row = cart.value.find((i) => i.id === id)
    if (row) row.qty += qty
    else cart.value.push({ id, qty })
    persistCart()
    flash('added')
  }

  function setQty(id, qty) {
    const n = Math.max(1, Number(qty) || 1)
    const row = cart.value.find((i) => i.id === id)
    if (row) row.qty = n
    persistCart()
  }

  function remove(id) {
    cart.value = cart.value.filter((i) => i.id !== id)
    persistCart()
  }

  function clearCart() {
    cart.value = []
    persistCart()
  }

  function createOrder(customer, method) {
    const id = `FXD-${Date.now().toString().slice(-8)}`
    const order = {
      id,
      createdAt: new Date().toISOString(),
      status: 'pending',
      method,
      customer,
      items: lines.value.map((l) => ({
        id: l.id,
        qty: l.qty,
        price: l.price,
        name: l.name
      })),
      total: subtotal.value
    }
    orders.value = [order, ...orders.value]
    persistOrders()
    return order
  }

  function getOrder(id) {
    return orders.value.find((o) => o.id === id) || null
  }

  function setStatus(id, status) {
    const order = getOrder(id)
    if (!order) return null
    order.status = status
    persistOrders()
    return order
  }

  return {
    products: demoProducts,
    cart,
    orders,
    toast,
    count,
    subtotal,
    lines,
    add,
    setQty,
    remove,
    clearCart,
    createOrder,
    getOrder,
    setStatus,
    money
  }
}
