<template>
  <div>
    <div class="page-head">
      <div>
        <h1 class="page-title">Dashboard</h1>
        <p class="page-sub">Visão geral dos teus eventos, vendas e check-ins.</p>
      </div>
      <div class="inline">
        <RouterLink to="/create-event" class="btn btn-primary btn-sm">
          <AppIcon name="plus" :size="15" /> Novo evento
        </RouterLink>
        <RouterLink to="/reports" class="btn btn-ghost btn-sm">
          <AppIcon name="chart" :size="15" /> Relatórios
        </RouterLink>
      </div>
    </div>

    <div v-if="summary" class="stats">
      <div class="stat">
        <div class="stat-label"><AppIcon name="calendar" :size="14" /> Eventos</div>
        <div class="stat-value">{{ summary.totalEvents }}</div>
      </div>
      <div class="stat">
        <div class="stat-label"><AppIcon name="ticket" :size="14" /> Pedidos pagos</div>
        <div class="stat-value">{{ summary.paidOrders }}</div>
      </div>
      <div class="stat">
        <div class="stat-label"><AppIcon name="sparkle" :size="14" /> Receita</div>
        <div class="stat-value">R$ {{ summary.totalRevenue.toFixed(2) }}</div>
      </div>
      <div class="stat">
        <div class="stat-label"><AppIcon name="scan" :size="14" /> Check-ins</div>
        <div class="stat-value">{{ summary.ticketsCheckedIn }}/{{ summary.ticketsSold }}</div>
      </div>
    </div>

    <h2 class="section-title mt-24" style="font-size: 20px">Os meus eventos</h2>
    <div v-if="eventStore.mine.length === 0" class="empty mt-16">Ainda não criaste eventos.</div>
    <div v-else class="table-wrap mt-16">
      <table class="table">
        <thead>
          <tr><th>Evento</th><th>Data</th><th>Categoria</th><th>Status</th><th>Vendidos</th><th></th></tr>
        </thead>
        <tbody>
          <tr v-for="e in eventStore.mine" :key="e.id">
            <td>{{ e.title }}</td>
            <td>{{ new Date(e.date).toLocaleDateString() }}</td>
            <td>{{ e.category }}</td>
            <td>
              <span class="badge" :class="e.status === 'Published' ? 'badge-success' : 'badge-muted'">
                {{ e.status === 'Published' ? 'Publicado' : 'Rascunho' }}
              </span>
            </td>
            <td>{{ e.sold }}/{{ e.totalTickets }}</td>
            <td>
              <div class="inline">
                <RouterLink :to="'/events/' + e.id" class="btn btn-ghost btn-sm">Ver</RouterLink>
                <RouterLink :to="'/events/' + e.id + '/manage'" class="btn btn-primary btn-sm">Gerir</RouterLink>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <h2 class="section-title mt-24" style="font-size: 20px">Vendas recentes</h2>
    <p v-if="recent.length === 0" class="empty mt-16">Sem vendas recentes.</p>
    <div v-else class="stack mt-16">
      <div v-for="o in recent" :key="o.id" class="card order-row" style="padding: 14px 18px">
        <div>
          <strong>{{ o.eventTitle }}</strong>
          <div class="muted">{{ o.buyerName }} · {{ o.buyerEmail }}</div>
        </div>
        <div class="order-side">
          <span class="badge" :class="o.status === 'Paid' ? 'badge-success' : o.status === 'Pending' ? 'badge-warn' : 'badge-muted'">
            {{ o.status }}
          </span>
          <span class="price">R$ {{ o.totalAmount.toFixed(2) }}</span>
          <span class="muted">{{ new Date(o.createdAt).toLocaleDateString() }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'
import { useEventStore } from '../stores/events'
import AppIcon from '../components/AppIcon.vue'

interface Summary {
  totalEvents: number
  paidOrders: number
  totalRevenue: number
  ticketsSold: number
  ticketsCheckedIn: number
}
interface RecentOrder {
  id: string
  status: string
  totalAmount: number
  createdAt: string
  eventTitle: string
  buyerName: string
  buyerEmail: string
}

const eventStore = useEventStore()
const summary = ref<Summary | null>(null)
const recent = ref<RecentOrder[]>([])

onMounted(async () => {
  eventStore.fetchMine()
  const [s, r] = await Promise.all([
    api.get('/api/Reports/summary'),
    api.get('/api/Reports/recent-orders'),
  ])
  summary.value = s.data
  recent.value = r.data
})
</script>
