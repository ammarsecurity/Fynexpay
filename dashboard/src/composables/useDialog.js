import { reactive } from 'vue'

const state = reactive({
  open: false,
  type: 'confirm', // confirm | alert
  variant: 'default', // default | danger | success
  title: '',
  message: '',
  confirmText: '',
  cancelText: '',
  resolve: null
})

function close(result) {
  const resolve = state.resolve
  state.open = false
  state.resolve = null
  if (resolve) resolve(result)
}

export function useDialog() {
  function confirm(options = {}) {
    const opts = typeof options === 'string' ? { message: options } : options
    return new Promise((resolve) => {
      state.open = true
      state.type = 'confirm'
      state.variant = opts.variant || 'danger'
      state.title = opts.title || ''
      state.message = opts.message || ''
      state.confirmText = opts.confirmText || ''
      state.cancelText = opts.cancelText || ''
      state.resolve = resolve
    })
  }

  function alert(options = {}) {
    const opts = typeof options === 'string' ? { message: options } : options
    return new Promise((resolve) => {
      state.open = true
      state.type = 'alert'
      state.variant = opts.variant || 'default'
      state.title = opts.title || ''
      state.message = opts.message || ''
      state.confirmText = opts.confirmText || ''
      state.cancelText = ''
      state.resolve = resolve
    })
  }

  return { state, confirm, alert, close }
}
