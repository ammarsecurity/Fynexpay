import { computed, onUnmounted, ref } from 'vue'

const COOLDOWN_SEC = 60

export function useOtpResend(seconds = COOLDOWN_SEC) {
  const remaining = ref(0)
  let timer = null

  const canResend = computed(() => remaining.value <= 0)
  const clock = computed(() => {
    const s = Math.max(0, remaining.value)
    const m = Math.floor(s / 60)
    const sec = String(s % 60).padStart(2, '0')
    return `${m}:${sec}`
  })

  function clearTimer() {
    if (timer) {
      clearInterval(timer)
      timer = null
    }
  }

  function startCooldown(next = seconds) {
    remaining.value = next
    clearTimer()
    timer = setInterval(() => {
      if (remaining.value <= 1) {
        remaining.value = 0
        clearTimer()
        return
      }
      remaining.value -= 1
    }, 1000)
  }

  function resetCooldown() {
    remaining.value = 0
    clearTimer()
  }

  onUnmounted(clearTimer)

  return { remaining, canResend, clock, startCooldown, resetCooldown }
}
