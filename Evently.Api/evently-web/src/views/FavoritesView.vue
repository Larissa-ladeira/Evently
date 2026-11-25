<template>
  <div>
    <div class="page-head">
      <div>
        <h1 class="page-title">Favoritos</h1>
        <p class="page-sub">Eventos que guardaste para ver mais tarde.</p>
      </div>
    </div>

    <div v-if="favoriteStore.events.length === 0" class="empty">
      Ainda não guardaste nenhum evento.
      <RouterLink to="/" class="link">Explorar eventos</RouterLink>
    </div>
    <div v-else class="grid">
      <EventCard v-for="event in favoriteStore.events" :key="event.id" :event="event" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useFavoriteStore } from '../stores/favorites'
import EventCard from '../components/EventCard.vue'

const favoriteStore = useFavoriteStore()

onMounted(() => favoriteStore.fetchEvents())
</script>
