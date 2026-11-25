<script setup lang="ts">
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import AppIcon from './AppIcon.vue'
import StarRating from './StarRating.vue'
import { useFavoriteStore } from '../stores/favorites'
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'
import type { Event } from '../types'

const props = defineProps<{ event: Event; delay?: number }>()

const favoriteStore = useFavoriteStore()
const authStore = useAuthStore()
const router = useRouter()

const coverClass = computed(() => {
  const cats: Record<string, string> = {
    Música: 'cover-0',
    Tecnologia: 'cover-1',
    Teatro: 'cover-2',
    Desporto: 'cover-3',
    Workshop: 'cover-4',
    Gastronomia: 'cover-5',
    Outros: 'cover-6',
  }
  return props.event.imageUrl ? '' : cats[props.event.category ?? 'Outros'] ?? 'cover-6'
})

const isFav = computed(() => favoriteStore.isFavorite(props.event.id))

const toggleFav = async (e: MouseEvent) => {
  e.preventDefault()
  e.stopPropagation()
  if (!authStore.isAuthenticated) {
    router.push('/login')
    return
  }
  await favoriteStore.toggle(props.event.id)
}
</script>

<template>
  <article class="event-card">
    <RouterLink :to="'/events/' + event.id" class="event-cover-link">
      <div class="event-cover" :class="coverClass">
        <img v-if="event.imageUrl" :src="event.imageUrl" :alt="event.title" class="cover-img" />
        <span v-else>{{ event.title.charAt(0).toUpperCase() }}</span>
        <span class="cover-badges">
          <span class="badge badge-primary badge-glass">{{ event.category || 'Outros' }}</span>
          <span v-if="event.isFeatured" class="badge badge-gold badge-glass">Destaque</span>
        </span>
        <span v-if="event.soldOut" class="badge badge-dark sold-badge">Esgotado</span>
      </div>
    </RouterLink>

    <button class="fav-btn" :class="{ active: isFav }" title="Favoritar" @click="toggleFav">
      <svg width="18" height="18" viewBox="0 0 24 24">
        <path
          d="M12 21s-7.5-4.6-10-9.3C.5 8.5 2 5 5.4 5c2 0 3.4 1.1 4.6 2.6C11.2 6.1 12.6 5 14.6 5 18 5 19.5 8.5 18 11.7 17.5 16.4 12 21 12 21z"
          :fill="isFav ? 'currentColor' : 'none'"
          stroke="currentColor"
          stroke-width="1.6"
        />
      </svg>
    </button>

    <div class="event-body">
      <div class="event-top">
        <StarRating :value="event.rating" :count="event.reviewCount" />
      </div>
      <RouterLink :to="'/events/' + event.id" class="event-title-link">
        <h3 class="event-title">{{ event.title }}</h3>
      </RouterLink>
      <p class="event-desc">{{ event.description }}</p>
      <div class="event-meta">
        <span>
          <AppIcon name="calendar" :size="15" />
          {{ new Date(event.date).toLocaleDateString() }} ·
          {{ new Date(event.date).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) }}
        </span>
        <span>
          <AppIcon name="pin" :size="15" />
          {{ event.location }}
        </span>
      </div>
      <div class="event-foot">
        <span class="price">R$ {{ event.price.toFixed(2) }}</span>
        <RouterLink :to="'/events/' + event.id" class="btn btn-primary btn-sm">
          Ver detalhes
        </RouterLink>
      </div>
    </div>
  </article>
</template>
