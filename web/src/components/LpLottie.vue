<template>
  <DotLottieVue class="lp-lottie" :src="src" autoplay loop />
</template>

<script setup>
import { DotLottieVue } from '@lottiefiles/dotlottie-vue'

defineProps({
  src: { type: String, required: true }
})
</script>
