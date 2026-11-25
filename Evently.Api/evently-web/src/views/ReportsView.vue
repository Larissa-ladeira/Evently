<template>
  <div>
    <div class="page-head">
      <div>
        <h1 class="page-title">Relatórios</h1>
        <p class="page-sub">Visão geral de vendas, receita e check-ins.</p>
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
        <div class="stat-label"><AppIcon name="ticket" :size="14" /> Ingressos vendidos</div>
        <div class="stat-value">{{ summary.ticketsSold }}</div>
      </div>
      <div class="stat">
        <div class="stat-label"><AppIcon name="check" :size="14" /> Check-ins</div>
        <div class="stat-value">{{ summary.ticketsCheckedIn }}</div>
      </div>
    </div>

    <h2 class="section-title mt-24" style="font-size: 20px">Por evento</h2>
    <p v-if="events.length === 0" class="empty mt-16">Sem dados.</p>
    <div v-else class="table-wrap mt-16">
      <table class="table">
        <thead>
          <tr>
            <th>Evento</th>
            <th>Data</th>
            <th>Pedidos pagos</th>
            <th>Ingressos vendidos</th>
            <th>Check-ins</th>
            <th>Receita</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in events" :key="item.id">
            <td>{{ item.title }}</td>
            <td>{{ new Date(item.date).toLocaleDateString() }}</td>
            <td>{{ item.paidOrders }}</td>
            <td>{{ item.ticketsSold }}</td>
            <td>{{ item.ticketsCheckedIn }}</td>
            <td>R$ {{ item.revenue.toFixed(2) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'
import AppIcon from '../components/AppIcon.vue'

interface Summary {
  totalEvents: number
  paidOrders: number
  totalRevenue: number
  ticketsSold: number
  ticketsCheckedIn: number
}

interface EventReport {
  id: string
  title: string
  date: string
  paidOrders: number
  ticketsSold: number
  ticketsCheckedIn: number
  revenue: number
}

const summary = ref<Summary | null>(null)
const events = ref<EventReport[]>([])

onMounted(async () => {
  const [summaryRes, eventsRes] = await Promise.all([
    api.get('/api/Reports/summary'),
    api.get('/api/Reports/events'),
  ])
  summary.value = summaryRes.data
  events.value = eventsRes.data
})
</script>
