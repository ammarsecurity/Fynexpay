<template>
  <section class="hero">
    <h1 class="brand">Fynex<span>pay</span></h1>
    <p class="headline">محطة دفع واحدة لتطبيقك وموقعك في العراق</p>
    <p class="sub">اربط FIB و ZainCash و QI خلف API موحّد، امنح كل تاجر محفظة، وخذ عمولتك من كل عملية.</p>
    <div class="cta">
      <a class="btn primary" :href="dashboardUrl + '/register'">ابدأ كتاجر</a>
      <a class="btn ghost" :href="apiUrl + '/swagger'">وثائق API</a>
    </div>
  </section>

  <section class="section">
    <h2>للمطوّرين</h2>
    <p>أنشئ دفعة بمفتاح API واحد، ثم وجّه الزبون إلى رابط الدفع أو رمز QR حسب المزود.</p>
    <pre class="code">curl -X POST {{ apiUrl }}/v1/payments \
  -H "X-Api-Key: fx_your_key" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 5000,
    "orderId": "ORD-1001",
    "description": "اشتراك شهري",
    "provider": "auto",
    "callbackUrl": "https://your.app/hooks/fynexpay"
  }'</pre>
    <div class="providers">
      <span class="chip">FIB Web Payments</span>
      <span class="chip">ZainCash</span>
      <span class="chip">Qi Gate</span>
    </div>
  </section>

  <footer>© {{ year }} Fynexpay — محطة دفع عراقية</footer>
</template>

<script setup>
const apiUrl = import.meta.env.VITE_API_BASE || 'http://localhost:5080'
const dashboardUrl = import.meta.env.VITE_DASHBOARD_URL || 'http://localhost:5173'
const year = new Date().getFullYear()
</script>
