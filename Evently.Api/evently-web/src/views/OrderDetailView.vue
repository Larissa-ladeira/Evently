<template>
  <div v-if="order">
    <RouterLink to="/orders" class="link">&larr; Voltar aos pedidos</RouterLink>

    <div class="page-head mt-16">
      <div>
        <h1 class="page-title">Pedido #{{ order.id.slice(0, 8) }}</h1>
        <p class="page-sub">Realizado em {{ new Date(order.createdAt).toLocaleString() }}</p>
      </div>
      <span class="badge" :class="statusClass(order.status)">{{ statusLabel(order.status) }}</span>
    </div>

    <div class="detail-grid">
      <div>
        <div class="card">
          <h2 class="section-title" style="font-size: 20px">{{ order.event?.title }}</h2>
          <div class="detail-meta">
            <span><AppIcon name="calendar" :size="15" /> {{ order.event ? new Date(order.event.date).toLocaleString() : '-' }}</span>
            <span><AppIcon name="pin" :size="15" /> {{ order.event?.location }}</span>
          </div>

          <h3 class="section-title mt-24" style="font-size: 16px">Itens</h3>
          <div class="table-wrap mt-8">
            <table class="table">
              <thead>
                <tr><th>Tipo</th><th>Qtd</th><th>Preço</th><th>Subtotal</th></tr>
              </thead>
              <tbody>
                <tr v-for="(item, i) in order.items" :key="i">
                  <td>{{ item.ticketTypeName }}</td>
                  <td>{{ item.quantity }}</td>
                  <td>R$ {{ item.unitPrice.toFixed(2) }}</td>
                  <td>R$ {{ item.totalPrice.toFixed(2) }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="summary mt-16">
            <div v-if="order.discountAmount > 0" class="summary-row text-success">
              <span>Desconto {{ order.couponCode }}</span><span>- R$ {{ order.discountAmount.toFixed(2) }}</span>
            </div>
            <div class="summary-row summary-total"><span>Total</span><span>R$ {{ order.totalAmount.toFixed(2) }}</span></div>
          </div>
        </div>

        <div class="card mt-16">
          <h2 class="section-title" style="font-size: 20px">Ingressos</h2>
          <p v-if="order.tickets.length === 0" class="muted mt-8">Os ingressos aparecem aqui após o pagamento.</p>
          <div v-else class="tickets-grid mt-16">
            <div v-for="t in order.tickets" :key="t.id" class="ticket-mini">
              <QrcodeVue :value="t.qrCode" :size="140" level="M" class="ticket-qr" />
              <div class="ticket-mini-info">
                <span class="badge" :class="t.status === 'Valid' ? 'badge-success' : 'badge-muted'">
                  {{ t.status === 'Valid' ? 'Válido' : t.status === 'Used' ? 'Utilizado' : 'Cancelado' }}
                </span>
                <strong>{{ t.ticketType }}</strong>
                <code class="ticket-code">{{ t.qrCode.slice(0, 18) }}…</code>
                <button
                  v-if="t.status === 'Valid'"
                  class="btn btn-ghost btn-sm"
                  @click="transferir(t.id)"
                >
                  Transferir
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <aside class="card">
        <h2 class="section-title" style="font-size: 18px">Comprador</h2>
        <p class="mt-8">{{ order.buyer?.name }}</p>
        <p class="muted">{{ order.buyer?.email }}</p>
        <p class="muted mt-8">Pagamento: {{ order.paymentProvider || '—' }}</p>
        <RouterLink to="/my-tickets" class="btn btn-ghost btn-block mt-16">Todos os meus ingressos</RouterLink>
      </aside>
    </div>

    <p v-if="msg" class="alert" :class="ok ? 'alert-success' : 'alert-error'">{{ msg }}</p>
  </div>

  <div v-else class="empty">A carregar pedido...</div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useOrderStore } from '../stores/orders'
import api from '../services/api'
import AppIcon from '../components/AppIcon.vue'
import QrcodeVue from 'qrcode.vue'

const route = useRoute()
const orderStore = useOrderStore()
const order = ref(orderStore.current)
const msg = ref('')
const ok = ref(false)

const statusLabel = (status: string) =>
  ({ Paid: 'Pago', Pending: 'Pendente', Cancelled: 'Cancelado', Refunded: 'Reembolsado' }[status] ?? status)
const statusClass = (status: string) =>
  status === 'Paid' ? 'badge-success' : status === 'Pending' ? 'badge-warn' : 'badge-muted'

const transferir = async (ticketId: string) => {
  const email = window.prompt('E-mail do destinatário:')
  if (!email) return
  ok.value = false
  msg.value = ''
  try {
    const res = await api.post(`/api/Tickets/${ticketId}/transfer`, { email })
    ok.value = true
    msg.value = res.data.message
    await orderStore.fetchOrder(route.params.id as string)
    order.value = orderStore.current
  } catch (err: any) {
    msg.value = err?.response?.data?.message || 'Erro ao transferir o ingresso.'
  }
}

onMounted(async () => {
  await orderStore.fetchOrder(route.params.id as string)
  order.value = orderStore.current
})
</script>
