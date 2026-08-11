<template>
  <div class="volume-chart" v-if="hasData" :style="{ height: `${height}px` }">
    <Line :data="chartData" :options="chartOptions" />
  </div>
  <p v-else class="muted empty">{{ emptyText }}</p>
</template>

<script setup>
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Filler,
  Tooltip,
  Legend
} from 'chart.js'
import { Line } from 'vue-chartjs'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Filler, Tooltip, Legend)

const props = defineProps({
  series: { type: Array, default: () => [] },
  emptyText: { type: String, default: '' },
  height: { type: Number, default: 220 }
})

const { locale, t } = useI18n()

const hasData = computed(() => Array.isArray(props.series) && props.series.length > 0)

function money(v) {
  return new Intl.NumberFormat('en-IQ').format(Math.round(Number(v ?? 0))) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}

function formatDay(iso) {
  if (!iso) return ''
  const d = new Date(`${String(iso).slice(0, 10)}T00:00:00Z`)
  return d.toLocaleDateString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB', {
    day: '2-digit',
    month: 'short'
  })
}

const chartData = computed(() => {
  const days = props.series || []
  return {
    labels: days.map((d) => formatDay(d.date)),
    datasets: [
      {
        label: t('adminOverview.chartVol'),
        data: days.map((d) => Number(d.volume || 0)),
        borderColor: '#031838',
        backgroundColor: (ctx) => {
          const { chart } = ctx
          const { ctx: c, chartArea } = chart
          if (!chartArea) return 'rgba(3, 24, 56, 0.12)'
          const g = c.createLinearGradient(0, chartArea.top, 0, chartArea.bottom)
          g.addColorStop(0, 'rgba(3, 24, 56, 0.32)')
          g.addColorStop(1, 'rgba(3, 24, 56, 0.02)')
          return g
        },
        fill: true,
        tension: 0.35,
        pointRadius: 3.5,
        pointHoverRadius: 5,
        pointBackgroundColor: '#fff',
        pointBorderColor: '#031838',
        pointBorderWidth: 2,
        borderWidth: 2.5
      }
    ]
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  locale: locale.value === 'ar' ? 'ar-IQ' : 'en-GB',
  interaction: { mode: 'index', intersect: false },
  plugins: {
    legend: { display: false },
    tooltip: {
      backgroundColor: '#031838',
      titleColor: '#fff',
      bodyColor: '#e2e8f0',
      padding: 10,
      cornerRadius: 10,
      displayColors: false,
        callbacks: {
          title(items) {
            return items?.[0]?.label || ''
          },
          label(ctx) {
            const i = ctx.dataIndex
            const row = props.series[i] || {}
            const vol = money(ctx.parsed.y)
            const fees = money(row.fees)
            const count = Number(row.count || 0)
            if (locale.value === 'ar') {
              return [`المبلغ: ${vol}`, `العمولة: ${fees}`, `العمليات: ${count}`]
            }
            return [`Volume: ${vol}`, `Fees: ${fees}`, `Payments: ${count}`]
          }
        }
    }
  },
  scales: {
    x: {
      reverse: false,
      grid: { display: false },
      ticks: {
        color: '#64748b',
        font: { size: 11, weight: '600' },
        maxRotation: 0,
        autoSkip: true,
        maxTicksLimit: 6
      },
      border: { display: false }
    },
    y: {
      beginAtZero: true,
      grace: '8%',
      grid: {
        color: '#e2e8f0',
        borderDash: [4, 6],
        drawTicks: false
      },
      ticks: {
        color: '#64748b',
        font: { size: 11, weight: '600' },
        callback(v) {
          if (v >= 1_000_000) return `${Math.round(v / 1_000_000)}M`
          if (v >= 1000) return `${Math.round(v / 1000)}k`
          return String(v)
        }
      },
      border: { display: false }
    }
  }
}))
</script>

<style scoped>
.volume-chart {
  width: 100%;
  margin-top: 8px;
  padding: 12px 10px 6px;
  background: linear-gradient(180deg, #f5f7fb 0%, #fff 100%);
  border-radius: 14px;
  border: 1px solid var(--line);
}
.empty { margin-top: 12px; }
</style>
