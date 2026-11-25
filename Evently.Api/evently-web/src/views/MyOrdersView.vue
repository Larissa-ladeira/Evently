<template>
  <div>
    <div class="page-head">
      <div>
        <h1 class="page-title">Meus pedidos</h1>
        <p class="page-sub">Histórico de compras, ingressos e reembolsos.</p>
      </div>
      <RouterLink to="/my-tickets" class="btn btn-ghost btn-sm">
        <AppIcon name="ticket" :size="15" />
        Ver ingressos
      </RouterLink>
    </div>

    <div v-if="orderStore.loading" class="empty">A carregar pedidos...</div>
    <div v-else-if="orderStore.orders.length === 0" class="empty">
      Ainda não tens pedidos.
      <RouterLink to="/" class="link">Explorar eventos</RouterLink>
    </div>

    <div v-else class="stack">
      <article v-for="order in orderStore.orders" :key="order.id" class="card order-row">
        <div class="order-main">
          <div class="order-cover" :class="coverClass(order.event?.category, order.event?.imageUrl)" :style="coverStyle(order.event?.imageUrl)">
            <img v-if="order.event?.imageUrl" :src="order.event.imageUrl" alt="" />
            <span v-else>{{ order.event?.title.charAt(0) }}</span>
          </div>
          <div>
            <h3 class="event-title">{{ order.event?.title }}</h3>
            <div class="event-meta">
              <span><AppIcon name="calendar" :size="14" /> {{ order.event ? new Date(order.event.date).toLocaleString() : '-' }}</span>
              <span><AppIcon name="ticket" :size="14" /> {{ order.ticketCount }} ingresso(s)</span>
            </div>
          </div>
        </div>

        <div class="order-side">
          <span class="badge" :class="statusClass(order.status)">{{ statusLabel(order.status) }}</span>
          <span class="price">R$ {{ order.totalAmount.toFixed(2) }}</span>
          <div class="order-actions">
            <RouterLink :to="'/orders/' + order.id" class="btn btn-ghost btn-sm">Detalhes</RouterLink>
            <button
              v-if="order.status === 'Paid' || order.status === 'Pending'"
              class="btn btn-danger btn-sm"
              :disabled="cancelling === order.id"
              @click="cancelar(order.id)"
            >
              {{ order.status === 'Paid' ? 'Reembolsar' : 'Cancelar' }}
            </button>
          </div>
        </div>
      </article>
    </div>

    <p v-if="msg" class="alert alert-success mt-16">{{ msg }}</p>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useOrderStore } from '../stores/orders'
import AppIcon from '../components/AppIcon.vue'

const orderStore = useOrderStore()
const cancelling = ref('')
const msg = ref('')

const cats: Record<string, string> = {
  Música: 'cover-0',
  Tecnologia: 'cover-1',
  Teatro: 'cover-2',
  Desporto: 'cover-3',
  Workshop: 'cover-4',
  Gastronomia: 'cover-5',
}
const coverClass = (category?: string, imageUrl?: string | null) =>
  imageUrl ? '' : cats[category ?? ''] ?? 'cover-6'
const coverStyle = (imageUrl?: string | null) =>
  imageUrl ? { backgroundImage: `url(${imageUrl})` } : {}

const statusLabel = (status: string) =>
  ({ Paid: 'Pago', Pending: 'Pendente', Cancelled: 'Cancelado', Refunded: 'Reembolsado' }[status] ?? status)

const statusClass = (status: string) =>
  status === 'Paid'
    ? 'badge-success'
    : status === 'Pending'
      ? 'badge-warn'
      : 'badge-muted'

const cancelar = async (id: string) => {
  cancelling.value = id
  try {
    const res = await orderStore.cancelOrder(id)
    msg.value = res.message
    await orderStore.fetchMine()
  } catch (err: any) {
    msg.value = err?.response?.data?.message || 'Erro ao cancelar.'
  } finally {
    cancelling.value = ''
  }
}

onMounted(() => orderStore.fetchMine())
</script>
