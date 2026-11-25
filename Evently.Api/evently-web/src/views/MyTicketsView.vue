<template>
  <div>
    <div class="page-head">
      <div>
        <h1 class="page-title">Meus ingressos</h1>
        <p class="page-sub">Apresenta o QR code na entrada do evento.</p>
      </div>
    </div>

    <div v-if="tickets.length === 0" class="empty">Nenhum ingresso encontrado.</div>

    <div v-else class="stack">
      <article v-for="ticket in tickets" :key="ticket.id" class="ticket">
        <div class="stack" style="gap: 10px">
          <h2 class="event-title">{{ ticket.event?.title }}</h2>
          <div class="event-meta">
            <span>
              <AppIcon name="calendar" :size="15" />
              {{ ticket.event ? new Date(ticket.event.date).toLocaleString() : '-' }}
            </span>
            <span>
              <AppIcon name="pin" :size="15" />
              {{ ticket.event?.location }}
            </span>
          </div>
          <div class="inline">
            <span class="badge" :class="ticket.status === 'Valid' ? 'badge-success' : 'badge-muted'">
              <AppIcon name="check" :size="13" />
              {{ ticket.status }}
            </span>
            <span class="badge badge-primary">{{ ticket.ticketType }}</span>
          </div>
          <code class="ticket-code">{{ ticket.qrCodeText }}</code>
        </div>
        <div class="qr-box">
          <img :src="'data:image/png;base64,' + ticket.qrCode" alt="QR Code" width="150" height="150" />
        </div>
      </article>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import api from '../services/api'
import AppIcon from '../components/AppIcon.vue'
import type { Ticket } from '../types'

const tickets = ref<Ticket[]>([])

onMounted(async () => {
  const res = await api.get('/api/Tickets/my')
  tickets.value = res.data
})
</script>
